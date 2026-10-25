param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$IsccPath
)

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/Release-Version.ps1" -Version $Version

if (!(Test-Path $IsccPath -PathType Leaf)) {
    throw "Inno Setup compiler not found: $IsccPath"
}

$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root "artifacts/publish/win-x64-$Version"
$output = Join-Path $root "artifacts/installers/$Version"

if (Test-Path $publish) {
    throw "Publish directory already exists: $publish. Use a fresh workspace or remove that build output first."
}

if (Test-Path $output) {
    throw "Installer directory already exists: $output. Refusing to replace build output."
}

$publishArguments = @(
    'publish'
    "$root/Thoughtfall.Desktop"
    '-c'
    'Release'
    '-r'
    'win-x64'
    '--self-contained'
    'true'
    '-p:PublishSingleFile=false'
    '-p:PublishTrimmed=false'
    "-p:Version=$Version"
    "-p:FileVersion=$Version.0"
    "-p:AssemblyVersion=$Version.0"
    '-o'
    $publish
    '-warnaserror'
)

& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw 'Windows publish failed.'
}

Copy-Item "$root/LICENSE" $publish

& python "$PSScriptRoot/collect-notices.py" "$root/Thoughtfall.Desktop/obj/project.assets.json" $publish

if ($LASTEXITCODE -ne 0) {
    throw 'Collecting third-party notices failed.'
}

& $IsccPath "/DAppVersion=$Version" "/DPublishDir=$publish" "/DOutputDir=$output" "$root/packaging/windows/Thoughtfall.iss"

if ($LASTEXITCODE -ne 0) {
    throw 'Installer compilation failed.'
}
