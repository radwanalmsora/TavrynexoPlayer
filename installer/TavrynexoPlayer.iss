#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

[Setup]
AppId={{8F1B6C42-5E7A-4D3B-9A10-7A5C2E9D4B61}
AppName=Tavrynexo Player
AppVersion={#MyAppVersion}
AppVerName=Tavrynexo Player {#MyAppVersion}
AppPublisher=Tavrynexo
DefaultDirName={autopf}\Tavrynexo Player
DefaultGroupName=Tavrynexo Player
DisableProgramGroupPage=yes
OutputDir=..\artifacts
OutputBaseFilename=TavrynexoPlayer-Setup-{#MyAppVersion}
SetupIconFile=setup.ico
UninstallDisplayIcon={app}\TavrynexoPlayer.exe
WizardStyle=modern
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "assoc"; Description: "Show Tavrynexo Player in the ""Open with"" list for media files"; GroupDescription: "File types:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Tavrynexo Player"; Filename: "{app}\TavrynexoPlayer.exe"
Name: "{autodesktop}\Tavrynexo Player"; Filename: "{app}\TavrynexoPlayer.exe"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Classes\TavrynexoPlayer.Media"; ValueType: string; ValueName: ""; ValueData: "Tavrynexo Player media file"; Flags: uninsdeletekey; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\TavrynexoPlayer.Media\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\TavrynexoPlayer.exe,0"; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\TavrynexoPlayer.Media\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\TavrynexoPlayer.exe"" ""%1"""; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mp4\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mkv\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.avi\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mov\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.wmv\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.flv\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.webm\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.m4v\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mpg\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mpeg\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.ts\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.3gp\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.mp3\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.flac\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.wav\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.aac\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.m4a\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.ogg\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.opus\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc
Root: HKA; Subkey: "Software\Classes\.wma\OpenWithProgids"; ValueType: string; ValueName: "TavrynexoPlayer.Media"; ValueData: ""; Flags: uninsdeletevalue; Tasks: assoc

[Run]
Filename: "{app}\TavrynexoPlayer.exe"; Description: "{cm:LaunchProgram,Tavrynexo Player}"; Flags: nowait postinstall skipifsilent
