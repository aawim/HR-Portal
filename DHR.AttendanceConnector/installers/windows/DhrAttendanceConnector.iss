; Inno Setup Script for DHR Attendance Connector (Windows x64)
; Build prerequisite: run ..\build-all.ps1 -Runtimes win-x64
; Production signing: configure SignTool in Inno Setup with your EV/OV code-signing certificate.

[Setup]
AppId={{8D4F63B2-6F1C-4B8F-9A41-2C6F9E0B7A12}
AppName=DHR Attendance Connector
AppVersion=1.0.0
AppPublisher=Digital HR (DHR) Maldives
DefaultDirName={autopf}\DHR\AttendanceConnector
DefaultGroupName=DHR Attendance Connector
OutputDir=..\dist\installers
OutputBaseFilename=DHR-Attendance-Connector-Setup-1.0.0-win-x64
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\dist\win-x64\worker\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\win-x64\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DHR Attendance Connector"; Filename: "{app}\DHR.AttendanceConnector.App.exe"
Name: "{commondesktop}\DHR Attendance Connector"; Filename: "{app}\DHR.AttendanceConnector.App.exe"

[Run]
Filename: "sc.exe"; Parameters: "create DHRAttendanceConnector binPath= ""{app}\DHR.AttendanceConnector.Worker.exe"" start= auto DisplayName= ""DHR Attendance Connector"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "failure DHRAttendanceConnector reset= 86400 actions= restart/5000/restart/15000/restart/60000"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start DHRAttendanceConnector"; Flags: runhidden
Filename: "{app}\DHR.AttendanceConnector.App.exe"; Description: "Launch DHR Attendance Connector to activate"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop DHRAttendanceConnector"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete DHRAttendanceConnector"; Flags: runhidden
