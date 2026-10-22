$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Winget bootstrap is restricted to disposable GitHub runners.' }
# Windows runner images do not promise a registered winget client. Pin both the
# official bootstrap module and client rather than depending on the image.
Install-Module Microsoft.WinGet.Client -RequiredVersion 1.29.380 -Repository PSGallery -Scope CurrentUser -Force
Import-Module Microsoft.WinGet.Client -RequiredVersion 1.29.380
Repair-WinGetPackageManager -Version 1.29.380 -Force
$env:PATH += ";$env:LOCALAPPDATA\Microsoft\WindowsApps"
"$env:LOCALAPPDATA\Microsoft\WindowsApps" >> $env:GITHUB_PATH
& winget --version
if ($LASTEXITCODE -ne 0) { throw 'Winget bootstrap failed.' }
