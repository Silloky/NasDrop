[Setup]
AppName=NasDrop
AppVersion=1.0.0
DefaultDirName={commonpf}\NasDrop
DefaultGroupName=NasDrop
OutputDir=.
OutputBaseFilename=NasDropSetup
Compression=lzma
SolidCompression=yes

[Files]
// Add all files from the build output directory
Source: "bin/Debug/net8.0-windows/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\NasDrop"; Filename: "{app}\NasDrop.exe"

[Run]
Filename: "{app}\NasDrop.exe"; Description: "Launch NasDrop"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCR; Subkey: "*\shell\NasDrop"; ValueType: string; ValueName: ""; ValueData: "Share with NasDrop"; Flags: uninsdeletekey
Root: HKCR; Subkey: "*\shell\NasDrop"; ValueType: string; ValueName: "Icon"; ValueData: """%SystemRoot%\System32\shell32.dll"",-16770"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "*\shell\NasDrop\command"; ValueType: string; ValueName: ""; ValueData: """{app}\NasDrop.exe"" ""%1"""; Flags: uninsdeletekey

[Dirs]
Name: "{userappdata}\NasDrop"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  configFile: string;
begin
  if CurStep = ssPostInstall then
  begin
    configFile := ExpandConstant('{userappdata}\NasDrop\config.json');
    SaveStringToFile(configFile, '{"auth": {"token": "", "tokenExpiry": ""}}', False);
  end;
end;

