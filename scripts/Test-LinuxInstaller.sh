#!/usr/bin/env bash
set -euo pipefail
export LC_ALL=C

# Never install/remove packages on the developer's own desktop.
if [[ ${THOUGHTFALL_INSTALLER_TEST:-} != 1 || ! -f /.dockerenv || $EUID != 0 ]]; then
    echo 'Run this check only through Test-LinuxPackages.sh in disposable containers.' >&2
    exit 1
fi
if [[ $# != 2 ]]; then
    echo 'Expected a release version and architecture.' >&2
    exit 1
fi
version=$1
arch=$2
case "$arch" in
    x64) deb_arch=amd64; elf_machine=62 ;;
    arm64) deb_arch=arm64; elf_machine=183 ;;
    *) exit 1 ;;
esac
[[ $(dpkg --print-architecture) == "$deb_arch" ]]
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
package="$root/artifacts/installers/$version/Thoughtfall-$version-linux-$arch.deb"

[[ $(dpkg-deb -f "$package" Package) == thoughtfall ]]
[[ $(dpkg-deb -f "$package" Version) == "$version" ]]
[[ $(dpkg-deb -f "$package" Architecture) == "$deb_arch" ]]
if command -v dotnet || dpkg-query -W thoughtfall 2>/dev/null; then
    echo 'Smoke check requires a clean image without .NET or Thoughtfall.' >&2
    exit 1
fi
[[ ! -e /root/.thoughtfall ]]

# Only metadata is allowed: no maintainer scripts that launch the app or alter homes.
control_files=$(dpkg-deb --ctrl-tarfile "$package" | tar -tf -)
[[ $control_files == $'./\n./control' ]]
dpkg-deb --fsys-tarfile "$package" | tar --numeric-owner -tvf - | \
    awk '$2 != "0/0" { bad = 1 } END { exit bad }'

apt-get update -qq
apt-get install -y --no-install-recommends desktop-file-utils procps
mkdir -p /home/thoughtfall-installer-test/.thoughtfall
sentinel=/home/thoughtfall-installer-test/.thoughtfall/installer-smoke-check.txt
printf 'Preserve user data\n' > "$sentinel"

check_data() {
    [[ $(cat "$sentinel") == 'Preserve user data' ]]
    [[ $(find /home/thoughtfall-installer-test/.thoughtfall -type f | wc -l) == 1 ]]
    [[ ! -e /root/.thoughtfall ]]
    if pgrep -f '^/usr/lib/thoughtfall/Thoughtfall.Desktop( |$)'; then
        echo 'Package operation started Thoughtfall.' >&2
        exit 1
    fi
}

apt-get install -y --no-install-recommends "$package"
[[ $(dpkg-query -W -f='${Status} ${Version} ${Architecture}' thoughtfall) == "install ok installed $version $deb_arch" ]]
for file in Thoughtfall.Desktop libcoreclr.so libe_sqlite3.so libSkiaSharp.so libHarfBuzzSharp.so LICENSE THIRD-PARTY-NOTICES.md; do
    [[ -s /usr/lib/thoughtfall/$file ]]
done
[[ -d /usr/lib/thoughtfall/licenses ]]
[[ -x /usr/lib/thoughtfall/Thoughtfall.Desktop && -x /usr/bin/thoughtfall ]]
[[ -s /usr/share/doc/thoughtfall/copyright ]]
[[ -s /usr/share/icons/hicolor/256x256/apps/thoughtfall.png ]]
desktop-file-validate /usr/share/applications/thoughtfall.desktop
grep -qx 'Exec=thoughtfall --capture' /usr/share/applications/thoughtfall.desktop
[[ -z $(find /usr/lib/thoughtfall -perm /022 -print -quit) ]]
[[ -z $(find /usr/lib/thoughtfall \( ! -user root -o ! -group root \) -print -quit) ]]

while IFS= read -r -d '' file; do
    [[ $(od -An -tu2 -j18 -N2 "$file" | tr -d ' ') == "$elf_machine" ]]
    dependencies=$(ldd "$file")
    if [[ $dependencies == *'not found'* ]]; then
        missing=$(printf '%s\n' "$dependencies" | awk '/not found/ { print $1 }')
        # .NET's optional LTTng tracing provider targets an older ABI. The runtime
        # skips it when unavailable; it is not an application dependency.
        if [[ ${file##*/} == libcoreclrtraceptprovider.so && $missing == liblttng-ust.so.0 ]]; then
            continue
        fi
        echo "Unresolved native dependencies in $file: $dependencies" >&2
        exit 1
    fi
done < <(find /usr/lib/thoughtfall -type f \( -name '*.so' -o -name Thoughtfall.Desktop \) -print0)
check_data

apt-get install -y --reinstall --no-install-recommends "$package"
check_data
apt-get remove -y thoughtfall
for file in /usr/lib/thoughtfall /usr/bin/thoughtfall /usr/share/applications/thoughtfall.desktop \
    /usr/share/icons/hicolor/256x256/apps/thoughtfall.png /usr/share/doc/thoughtfall; do
    [[ ! -e $file ]]
done
check_data
apt-get install -y --no-install-recommends "$package"
apt-get purge -y thoughtfall
[[ ! -e /usr/lib/thoughtfall && ! -e /usr/bin/thoughtfall ]]
check_data
echo "Installer checks passed ($arch). GUI and cross-version upgrades require manual validation."
