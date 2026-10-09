#ifndef AppVersion
  #define AppVersion "1.0.3"
#endif
#ifndef ReleaseDir
  #define ReleaseDir "..\artifacts\HonsenToolbox-v1.0.0-win-x64"
#endif

#define AppName "Honsen Toolbox"
#define AppExeName "HonsenToolbox.exe"
#define AppId "honsen.toolbox"
#define RegistryKey "Software\Honsen Program\Apps\" + AppId

[Setup]
AppId={{DDA9BFC7-A6CD-4B92-87E5-5159C1C0C531}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Honsen
AppPublisherURL=https://github.com/etianwang
AppSupportURL=https://github.com/etianwang/Honsen-toolbox
DefaultDirName={autopf}\Honsen Program\Honsen Toolbox
DefaultGroupName=Honsen 工具箱
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=Honsen-Toolbox-{#AppVersion}-Setup
SetupIconFile=..\logo.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#AppVersion}
VersionInfoCompany=Honsen
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Files]
Source: "{#ReleaseDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "AppId"; ValueData: "{#AppId}"; Flags: uninsdeletekey
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "DisplayName"; ValueData: "Honsen工具箱"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "InstallLocation"; ValueData: "{app}"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "ExecutablePath"; ValueData: "{app}\{#AppExeName}"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "InstallScope"; ValueData: "machine"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "Publisher"; ValueData: "Honsen"
Root: HKLM64; Subkey: "{#RegistryKey}"; ValueType: string; ValueName: "UpdateManifestUrl"; ValueData: "https://api.github.com/repos/etianwang/Honsen-toolbox/releases/latest"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"

[Icons]
Name: "{group}\Honsen 工具箱"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\Honsen 工具箱"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,Honsen 工具箱}"; Flags: nowait postinstall skipifsilent
