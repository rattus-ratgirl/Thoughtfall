#!/usr/bin/env bash
set -euo pipefail

if [[ $# != 2 ]]; then
    echo 'Usage: bash scripts/Test-LinuxPackages.sh VERSION {x64|arm64}' >&2
    exit 1
fi
version=$1
arch=$2
case "$arch" in
    x64) machine=x86_64 ;;
    arm64) machine=aarch64 ;;
    *) exit 1 ;;
esac
[[ $(uname -s) == Linux && $(uname -m) == "$machine" ]]
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)

# Check rejection before the build script can publish or create directories.
for invalid in v1.0.0 1.0 01.0.0 1.0.0-preview '1.0.0;whoami' 65535.0.0 -1.0.0 1.0.0.0 $'1.0.0\n' ' 1.0.0' 999999999999999999999.0.0; do
    if bash "$root/scripts/Build-Linux.sh" "$invalid" "$arch" >/dev/null 2>&1; then
        echo "Invalid release version accepted: $invalid" >&2
        exit 1
    fi
done
if bash "$root/scripts/Build-Linux.sh" "$version" invalid >/dev/null 2>&1; then
    echo 'Invalid architecture accepted.' >&2
    exit 1
fi

for image in ubuntu:24.04 ubuntu:26.04 debian:12-slim debian:13-slim; do
    echo "Checking $image ($arch)"
    # Read-only source and fresh containers keep test installs away from host data.
    docker run --rm --env THOUGHTFALL_INSTALLER_TEST=1 \
        --mount "type=bind,source=$root,target=/work,readonly" \
        "$image" bash /work/scripts/Test-LinuxInstaller.sh "$version" "$arch"
done
