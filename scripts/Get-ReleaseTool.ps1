param(
    [Parameter(Mandatory)]
    [ValidateSet('InnoSetup', 'WingetCreate')]
    [string]$Tool
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $root 'artifacts/tools'

New-Item $directory -ItemType Directory -Force | Out-Null

if ($Tool -eq 'InnoSetup') {
    $url = 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe'
    $hash = '0362a383ed217d4c4239b5933866dd96d3eb2102737da92f80f6057a4b40df2f'
    $file = Join-Path $directory 'innosetup-7.1.0-x64.exe'
} else {
    $url = 'https://github.com/microsoft/winget-create/releases/download/v1.12.13.0/wingetcreate.exe'
    $hash = '24042bd37915805615e6cf969ac57c6439124c3fe85823327f5f3fb24bd9ffea'
    $file = Join-Path $directory 'wingetcreate-1.12.13.0.exe'
}

if (!(Test-Path $file)) {
    Invoke-WebRequest $url -OutFile $file -UseBasicParsing
}

if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $hash) {
    throw "Checksum mismatch for $Tool."
}

if ($Tool -eq 'WingetCreate') {
    return $file
}

# Portable compiler extraction does not register an application or uninstaller.
$destination = Join-Path $directory 'InnoSetup-7.1.0'
$extraction = @{
    FilePath = $file
    ArgumentList = @(
        '/VERYSILENT'
        '/SUPPRESSMSGBOXES'
        '/NORESTART'
        '/CURRENTUSER'
        '/PORTABLE=1'
        ('/DIR="' + $destination + '"')
    )
    WindowStyle = 'Hidden'
    PassThru = $true
    Wait = $true
}

$process = Start-Process @extraction

if ($process.ExitCode -ne 0) {
    throw "Inno Setup installation failed: $($process.ExitCode)"
}

return (Join-Path $destination 'ISCC.exe')
