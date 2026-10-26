#!/usr/bin/env bash
set -euo pipefail

if [[ $# != 2 ]]; then
    echo 'Usage: bash scripts/Build-Linux.sh VERSION {x64|arm64}' >&2
    exit 1
fi

version=$1
arch=$2

# Match Release-Version.ps1, including the Windows version-resource ceiling.
if [[ ! $version =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]]; then
    echo 'Version must be major.minor.patch, without leading zeros or a suffix.' >&2
    exit 1
fi
IFS=. read -r -a parts <<< "$version"
for part in "${parts[@]}"; do
    if (( ${#part} > 5 )) || (( 10#$part > 65534 )); then
        echo 'Each version component must be at most 65534.' >&2
        exit 1
    fi
done

case "$arch" in
    x64) deb_arch=amd64 ;;
    arm64) deb_arch=arm64 ;;
    *) echo 'Architecture must be x64 or arm64.' >&2; exit 1 ;;
esac

if [[ $(uname -s) != Linux ]] || [[ $(dpkg --print-architecture) != "$deb_arch" ]]; then
    echo 'Build the package on a Linux host matching its target architecture.' >&2
    exit 1
fi

root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
publish="$root/artifacts/publish/linux-$arch-$version"
stage="$root/artifacts/packages/linux-$arch-$version"
output="$root/artifacts/installers/$version/Thoughtfall-$version-linux-$arch.deb"

for path in "$publish" "$stage" "$output"; do
    if [[ -e $path ]]; then
        echo "Build output already exists: $path. Use a fresh workspace or remove that output first." >&2
        exit 1
    fi
done

dotnet publish "$root/Thoughtfall.Desktop" -c Release -r "linux-$arch" --self-contained true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false \
    "-p:Version=$version" "-p:FileVersion=$version.0" "-p:AssemblyVersion=$version.0" \
    -o "$publish" -warnaserror
cp "$root/LICENSE" "$publish/"
python3 "$root/scripts/collect-notices.py" "$root/Thoughtfall.Desktop/obj/project.assets.json" "$publish"

mkdir -p "$stage/DEBIAN" "$stage/usr/lib/thoughtfall" "$stage/usr/bin" \
    "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/256x256/apps" \
    "$stage/usr/share/doc/thoughtfall" "$(dirname -- "$output")"
cp -a "$publish/." "$stage/usr/lib/thoughtfall/"
install -m 755 "$root/packaging/linux/thoughtfall" "$stage/usr/bin/thoughtfall"
install -m 644 "$root/packaging/linux/thoughtfall.desktop" "$stage/usr/share/applications/thoughtfall.desktop"
install -m 644 "$root/packaging/linux/thoughtfall.png" "$stage/usr/share/icons/hicolor/256x256/apps/thoughtfall.png"
install -m 644 "$root/LICENSE" "$stage/usr/share/doc/thoughtfall/copyright"
chmod -R u+rwX,go+rX,go-w "$stage"
chmod 755 "$stage/usr/lib/thoughtfall/Thoughtfall.Desktop"

installed_size=$(du -sk "$stage/usr" | cut -f1)
sed -e "s/@VERSION@/$version/g" -e "s/@ARCH@/$deb_arch/g" -e "s/@INSTALLED_SIZE@/$installed_size/g" \
    "$root/packaging/linux/control" > "$stage/DEBIAN/control"
dpkg-deb --root-owner-group --build "$stage" "$output"
echo "Built $output"
