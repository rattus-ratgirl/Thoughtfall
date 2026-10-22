param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This install/uninstall smoke check runs only on disposable GitHub runners.' }
& "$PSScriptRoot/Release-Version.ps1" -Version $Version
$root = Split-Path $PSScriptRoot -Parent
$installer = Join-Path $root "artifacts/installers/$Version/Thoughtfall-$Version-win-x64-setup.exe"
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs/Thoughtfall'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D43775B7-822E-4C71-8A54-041708E0EE2D}_is1'
if (Test-Path $installDirectory) { throw 'Smoke check requires a fresh runner without Thoughtfall installed.' }
$diaryFolder = Join-Path $env:USERPROFILE '.thoughtfall'
New-Item $diaryFolder -ItemType Directory -Force | Out-Null
$sentinel = Join-Path $diaryFolder 'installer-smoke-check.txt'
[IO.File]::WriteAllText($sentinel, 'Preserve user data')
$silent = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
$marker = New-Object System.Threading.Mutex($false, 'Local\Thoughtfall.Running')
try {
    $blocked = Start-Process $installer -ArgumentList $silent -WindowStyle Hidden -Wait -PassThru
    if ($blocked.ExitCode -eq 0 -or (Test-Path $installDirectory)) { throw 'Installer did not reject a running application.' }
} finally { $marker.Dispose() }
$installed = Start-Process $installer -ArgumentList $silent -WindowStyle Hidden -Wait -PassThru
if ($installed.ExitCode -ne 0) { throw "Silent installation failed: $($installed.ExitCode)" }
$registration = Get-ItemProperty $registryPath
if ($registration.DisplayName -ne 'Thoughtfall' -or $registration.DisplayVersion -ne $Version -or $registration.Publisher -ne 'ratgirl') {
    throw 'Incorrect uninstall registration.'
}
foreach ($file in 'Thoughtfall.Desktop.exe', 'coreclr.dll', 'e_sqlite3.dll', 'LICENSE', 'THIRD-PARTY-NOTICES.md') {
    if (!(Test-Path (Join-Path $installDirectory $file))) { throw "Missing installed dependency: $file" }
}
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Thoughtfall.lnk'
if (!(Test-Path $shortcut)) { throw 'Start menu shortcut missing.' }
# Exercise reinstall without running the GUI or touching a real diary database.
$reinstall = Start-Process $installer -ArgumentList $silent -WindowStyle Hidden -Wait -PassThru
if ($reinstall.ExitCode -ne 0) { throw 'Reinstallation failed.' }
$uninstaller = Join-Path $installDirectory 'unins000.exe'
$marker = New-Object System.Threading.Mutex($false, 'Local\Thoughtfall.Running')
try {
    $blocked = Start-Process $uninstaller -ArgumentList $silent -WindowStyle Hidden -Wait -PassThru
    if ($blocked.ExitCode -eq 0 -or !(Test-Path $registryPath)) { throw 'Uninstaller did not reject a running application.' }
} finally { $marker.Dispose() }
$removed = Start-Process $uninstaller -ArgumentList $silent -WindowStyle Hidden -Wait -PassThru
if ($removed.ExitCode -ne 0) { throw "Uninstallation failed: $($removed.ExitCode)" }
if ((Test-Path $registryPath) -or (Test-Path $shortcut) -or (Test-Path (Join-Path $installDirectory 'Thoughtfall.Desktop.exe'))) {
    throw 'Uninstall left application registration or files behind.'
}
if ([IO.File]::ReadAllText($sentinel) -cne 'Preserve user data') { throw 'Installation changed user data.' }
Write-Output 'Installer smoke checks passed. GUI and cross-version upgrades still require manual validation.'
