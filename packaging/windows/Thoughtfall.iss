#ifndef AppVersion
  #error AppVersion must be supplied by Build-Windows.ps1
#endif
#ifndef PublishDir
  #error PublishDir must be supplied by Build-Windows.ps1
#endif
#ifndef OutputDir
  #error OutputDir must be supplied by Build-Windows.ps1
#endif

[Setup]
; Permanent upgrade identity. Never generate a new AppId for a release.
AppId={{D43775B7-822E-4C71-8A54-041708E0EE2D}
AppName=Thoughtfall
AppVersion={#AppVersion}
AppPublisher=ratgirl
AppPublisherURL=https://github.com/rattus-ratgirl/Thoughtfall
AppSupportURL=https://github.com/rattus-ratgirl/Thoughtfall/issues
AppUpdatesURL=https://github.com/rattus-ratgirl/Thoughtfall/releases
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName=Thoughtfall
VersionInfoProductVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Thoughtfall
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex=Local\Thoughtfall.Running
CloseApplications=no
RestartApplications=no
RestartIfNeededByRun=no
SetupIconFile=..\..\Thoughtfall.Desktop\Assets\Thoughtfall.ico
UninstallDisplayIcon={app}\Thoughtfall.Desktop.exe
UninstallDisplayName=Thoughtfall
LicenseFile=..\..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=Thoughtfall-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Thoughtfall"; Filename: "{app}\Thoughtfall.Desktop.exe"; Parameters: "--capture"

; No Run, UninstallDelete, or diary-folder entries: installation does not launch
; the app, and uninstall removes only installed files, never the user's diary.
