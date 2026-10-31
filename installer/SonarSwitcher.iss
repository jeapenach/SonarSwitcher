; Instalador de Sonar Switcher (Inno Setup 6.3 o superior: https://jrsoftware.org/isinfo.php)
; Antes de compilar, genera el paquete en Visual Studio (Proyecto > Publicar > Crear paquetes de aplicación).
; Compilar:  ISCC.exe SonarSwitcher.iss     (o abrirlo en Inno Setup y pulsar F9)

#define AppVersion "1.0.0"
#define PkgVersion "1.0.0.0"
#define PackageDir "..\SonarWidget\AppPackages\SonarWidget_" + PkgVersion + "_x64_Test"

[Setup]
AppId={{B0E5F2A1-6C1D-4F7E-9A52-5D3C8E71A9B4}
AppName=Sonar Switcher
AppVersion={#AppVersion}
AppPublisher=Sonar Switcher
DefaultDirName={autopf}\Sonar Switcher
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=SonarSwitcher-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Sonar Switcher (instalador)
SetupIconFile=SonarSwitcher.ico
UninstallDisplayIcon={app}\SonarSwitcher.ico

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
spanish.InstallFailed=No se pudo instalar el widget. Revisa el registro en:%n%1
english.InstallFailed=The widget could not be installed. Check the log at:%n%1
spanish.FinishedText=Sonar Switcher quedó instalado.%n%nAbre Xbox Game Bar con Win + G (o el botón Xbox del mando), abre el menú de widgets y busca "Sonar Switcher".
english.FinishedText=Sonar Switcher is installed.%n%nOpen Xbox Game Bar with Win + G (or the controller's Xbox button), open the widget menu and look for "Sonar Switcher".

[Files]
Source: "SonarSwitcher.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDir}\*.msix"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDir}\*.cer"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDir}\Dependencies\x64\*.appx"; DestDir: "{app}\Dependencies"; Flags: ignoreversion
Source: "Install-SonarSwitcher.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "Uninstall-SonarSwitcher.ps1"; DestDir: "{app}"; Flags: ignoreversion

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Uninstall-SonarSwitcher.ps1"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveSonarSwitcher"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  Params: String;
begin
  if CurStep = ssPostInstall then
  begin
    WizardForm.StatusLabel.Caption := 'Sonar Switcher...';
    Params := '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\Install-SonarSwitcher.ps1') + '"';
    if (not Exec('powershell.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)) or (ResultCode <> 0) then
      MsgBox(FmtMessage(CustomMessage('InstallFailed'), [ExpandConstant('{app}\install.log')]), mbError, MB_OK);
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
    WizardForm.FinishedLabel.Caption := CustomMessage('FinishedText');
end;
