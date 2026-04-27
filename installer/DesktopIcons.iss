; Inno Setup script for Desktop Icons
; Builds a self-contained installer that lets the user choose:
;   - Install scope (per-user vs per-machine)
;   - Install directory
;   - Whether to create a desktop shortcut

#ifndef MyAppName
  #define MyAppName       "Desktop Icons"
#endif
#ifndef MyAppVersion
  #define MyAppVersion    "0.1.1"
#endif
#ifndef MyAppPublisher
  #define MyAppPublisher  "Desktop Icons"
#endif
#ifndef MyAppExeName
  #define MyAppExeName    "DesktopIcons.App.exe"
#endif
#ifndef MyPublishDir
  #define MyPublishDir    "..\publish\app"
#endif
#ifndef MyOutputDir
  #define MyOutputDir     "..\publish\installer"
#endif
#ifndef MyOutputBaseFilename
  #define MyOutputBaseFilename "DesktopIcons-Setup-" + MyAppVersion
#endif

[Setup]
; Stable AppId — do not change between releases (used to match upgrades).
AppId={{4F9C8E2A-7B3D-4E5F-9A1C-8D6F2B4E7C9D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL=https://github.com
VersionInfoVersion={#MyAppVersion}

; Default to LocalAppData\Programs (per-user). User can switch to Program Files
; via the privileges dialog at start of the installer (PrivilegesRequiredOverridesAllowed).
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
SetupIconFile=..\src\DesktopIcons.App\Assets\AppIcon.ico

; Use Restart Manager so an open instance is closed gracefully during upgrade.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove auto-start registry value if the user enabled it from inside the app.
; /f makes reg.exe succeed even if the value does not exist.
Filename: "{cmd}"; Parameters: "/C reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v DesktopIcons /f"; Flags: runhidden; RunOnceId: "RemoveAutoStart"
