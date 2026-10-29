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
ChangesEnvironment=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Thoughtfall"; Filename: "{app}\Thoughtfall.Desktop.exe"

; No Run, UninstallDelete, or diary-folder entries: installation does not launch
; the app, and uninstall removes only installed files, never the user's diary.

[Tasks]
Name: "clipath"; Description: "Add Thoughtfall CLI to your user PATH"

[Code]
const
  EnvironmentKey = 'Environment';
  OwnershipKey = 'Software\Thoughtfall\Installer';

function RemovePathEntry(Value, Entry: String): String;
var
  Part: String;
  Separator: Integer;
  First: Boolean;
begin
  Result := '';
  First := True;
  while True do begin
    Separator := Pos(';', Value);
    if Separator = 0 then begin
      Part := Value;
      Value := '';
    end else begin
      Part := Copy(Value, 1, Separator - 1);
      Delete(Value, 1, Separator);
    end;
    if CompareText(RemoveBackslash(Trim(Part)), RemoveBackslash(Entry)) <> 0 then begin
      if not First then Result := Result + ';';
      Result := Result + Part;
      First := False;
    end;
    if Separator = 0 then Break;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Value, Entry, Owned: String;
begin
  if CurStep <> ssPostInstall then exit;
  Entry := ExpandConstant('{app}\cli');
  RegQueryStringValue(HKCU, EnvironmentKey, 'Path', Value);
  RegQueryStringValue(HKCU, OwnershipKey, 'CliPath', Owned);
  if WizardIsTaskSelected('clipath') then begin
    if RemovePathEntry(Value, Entry) = Value then begin
      if Value <> '' then Value := Value + ';';
      RegWriteExpandStringValue(HKCU, EnvironmentKey, 'Path', Value + Entry);
      RegWriteStringValue(HKCU, OwnershipKey, 'CliPath', Entry);
    end;
  end else if Owned <> '' then begin
    RegWriteExpandStringValue(HKCU, EnvironmentKey, 'Path', RemovePathEntry(Value, Owned));
    RegDeleteValue(HKCU, OwnershipKey, 'CliPath');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Value, Owned: String;
begin
  if CurUninstallStep <> usUninstall then exit;
  if RegQueryStringValue(HKCU, OwnershipKey, 'CliPath', Owned) then begin
    RegQueryStringValue(HKCU, EnvironmentKey, 'Path', Value);
    RegWriteExpandStringValue(HKCU, EnvironmentKey, 'Path', RemovePathEntry(Value, Owned));
    RegDeleteValue(HKCU, OwnershipKey, 'CliPath');
  end;
end;
