param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'
if ($Version -cnotmatch '\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z') {
    throw 'Version must be major.minor.patch, without a v prefix or prerelease suffix.'
}
foreach ($part in $Version.Split('.')) {
    if ([long]$part -gt 65534) { throw 'Each version component must be at most 65534 for Windows version resources.' }
}
