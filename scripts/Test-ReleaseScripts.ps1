$ErrorActionPreference = 'Stop'

foreach ($valid in '0.0.0', '0.1.0', '1.20.300', '65534.0.0') {
    & "$PSScriptRoot/Release-Version.ps1" -Version $valid
}

$invalidVersions = @(
    'v1.0.0'
    '1.0'
    '01.0.0'
    '1.0.0-preview'
    '1.0.0;whoami'
    '65535.0.0'
    '-1.0.0'
    '1.0.0.0'
    "1.0.0`n"
    ' 1.0.0'
    '999999999999999999999.0.0'
)

foreach ($invalid in $invalidVersions) {
    $rejected = $false

    try {
        & "$PSScriptRoot/Release-Version.ps1" -Version $invalid
    } catch {
        $rejected = $true
    }

    if (!$rejected) {
        throw "Invalid release version accepted: $invalid"
    }
}

Write-Output 'Release version checks passed.'
