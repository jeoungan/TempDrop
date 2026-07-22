#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "TempDrop"
#define MyAppPublisher "TempDrop contributors"
#define MyAppURL "https://github.com/jeoungan/TempDrop"
#define MyAppExeName "TempDrop.exe"

[Setup]
AppId={{8B0EC423-39EC-4C36-9CE7-3C65CFC4EBC0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=TempDrop-{#MyAppVersion}-setup-x64
SetupIconFile=..\assets\TempDrop.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=..\LICENSE
InfoBeforeFile=NOTICE.txt
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoDescription=TempDrop installer
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (c) 2026 TempDrop contributors

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\build\TempDrop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.ko.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\TempDrop"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall TempDrop"; Filename: "{uninstallexe}"
Name: "{autodesktop}\TempDrop"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch TempDrop and review permanent-deletion settings"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--remove-schedule"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveTempDropSchedule"

[Code]
function InitializeSetup(): Boolean;
begin
  if WizardSilent then
  begin
    Result := True;
    exit;
  end;

  Result := MsgBox(
    'TempDrop permanently deletes expired files from its dedicated managed folder.' + #13#10 + #13#10 +
    'Deletion is disabled until the user launches TempDrop and explicitly accepts this behavior.' + #13#10 +
    'No files or telemetry are sent over the network.' + #13#10 + #13#10 +
    'Continue with installation?',
    mbConfirmation,
    MB_YESNO) = IDYES;
end;
