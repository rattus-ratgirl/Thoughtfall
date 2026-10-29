"""Collect NuGet metadata, packaged notices, and runtime licences for distribution."""

import json
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET


assets = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
publish = Path(sys.argv[2])
root = Path(__file__).resolve().parent.parent
destination = publish / "licenses"
destination.mkdir()
package_roots = [Path(folder) for folder in assets["packageFolders"]]
summary = ["# Third-party notices", "", "Includes restored dependencies and bundled runtime notices.", ""]
mit = (root / "LICENSE").read_text().split("Permission is hereby granted", 1)[1]
mit = "Permission is hereby granted" + mit

for identity, library in sorted(assets["libraries"].items()):
    if library["type"] != "package":
        continue
    folder = next(base / library["path"] for base in package_roots if (base / library["path"]).is_dir())
    metadata = ET.parse(next(folder.glob("*.nuspec"))).getroot().find("{*}metadata")
    license_node = metadata.find("{*}license")
    if license_node is None:
        raise ValueError(f"Review licence metadata for {identity} before distributing it")
    license_value = license_node.text
    copyright_text = metadata.findtext("{*}copyright") or metadata.findtext("{*}authors")
    target = destination / identity.replace("/", "-")
    target.mkdir()
    if license_node.attrib["type"] == "expression":
        if license_value == "MIT":
            (target / "LICENSE.txt").write_text(f"{copyright_text}\n\n{mit}", encoding="utf-8")
        elif license_value == "Apache-2.0":
            shutil.copyfile(root / "packaging/licenses/Apache-2.0.txt", target / "LICENSE.txt")
        else:
            raise ValueError(f"Add the licence text for {identity}: {license_value}")
    else:
        shutil.copyfile(folder / license_value, target / Path(license_value).name)
    for relative in library["files"]:
        source = folder / relative
        if source.is_file() and any(word in source.name.lower() for word in ("license", "notice", "copying")):
            # Keep upstream notices verbatim, including native dependency attributions.
            shutil.copyfile(source, target / source.name)
    shutil.copyfile(next(folder.glob("*.nuspec")), target / "package.nuspec")
    summary.extend([f"## {identity}", "", copyright_text, "", f"Licence: {license_value}", ""])

dependencies = json.loads((publish / (sys.argv[3] if len(sys.argv) > 3 else "Thoughtfall.Desktop.deps.json")).read_text())
runtime_packs = [name for name in dependencies["libraries"] if name.startswith("runtimepack.")]
if not runtime_packs:
    raise ValueError("Expected a self-contained .NET runtime")
for identity in runtime_packs:
    relative = identity.removeprefix("runtimepack.").lower()
    folder = next(base / relative for base in package_roots if (base / relative).is_dir())
    target = destination / relative.replace("/", "-")
    target.mkdir()
    for name in ("LICENSE.TXT", "THIRD-PARTY-NOTICES.TXT"):
        shutil.copyfile(folder / name, target / name)
    summary.extend([f"## {identity}", "", "See the runtime licence and third-party notices in licenses/.", ""])

# Inter is embedded in Avalonia.Fonts.Inter; its font licence is separate from
# the MIT licence on the NuGet wrapper.
if any(name.startswith("Avalonia.Fonts.Inter/") for name in assets["libraries"]):
    shutil.copyfile(root / "packaging/licenses/Inter-OFL.txt", destination / "Inter-OFL.txt")
(publish / "THIRD-PARTY-NOTICES.md").write_text("\n".join(summary), encoding="utf-8")
