; ============================================================================
; BIMConcierge Installer — Inno Setup 6 Script
; Supports Revit 2025 and 2026
; ============================================================================

#define AppName      "BIMConcierge"
#define AppVersion   "1.0.0"
#define AppPublisher "BIMConcierge"
#define AppURL       "https://bimconcierge.io"
#define BuildOutput  "..\src\BIMConcierge.Plugin\bin\Release"

[Setup]
AppId={{A4F83E2C-1B77-4D8A-B6C5-E2A91F5D3C08}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\dist
OutputBaseFilename=BIMConcierge-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
DisableDirPage=yes
DisableProgramGroupPage=yes
UninstallDisplayName={#AppName}
WizardStyle=modern
SetupIconFile=compiler:SetupClassicIcon.ico

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
brazilianportuguese.BeveledLabel=BIMConcierge
english.BeveledLabel=BIMConcierge

[CustomMessages]
brazilianportuguese.SelectRevitVersions=Selecione as versoes do Revit
brazilianportuguese.SelectRevitVersionsDesc=Escolha em quais versoes do Revit o BIMConcierge sera instalado:
brazilianportuguese.Revit2025=Revit 2025
brazilianportuguese.Revit2026=Revit 2026
brazilianportuguese.NoRevitSelected=Voce deve selecionar pelo menos uma versao do Revit.
brazilianportuguese.NoRevitInstalled=Nenhuma versao compativel do Revit foi detectada (2025 ou 2026).%nDeseja continuar mesmo assim?
english.SelectRevitVersions=Select Revit Versions
english.SelectRevitVersionsDesc=Choose which Revit versions to install BIMConcierge for:
english.Revit2025=Revit 2025
english.Revit2026=Revit 2026
english.NoRevitSelected=You must select at least one Revit version.
english.NoRevitInstalled=No compatible Revit version was detected (2025 or 2026).%nDo you want to continue anyway?

[Files]
; Plugin files for Revit 2025
Source: "{#BuildOutput}\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025\BIMConcierge"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Excludes: "runtimes\linux-*,runtimes\osx-*,runtimes\browser-*,runtimes\maccatalyst-*,runtimes\win-x86,runtimes\win-arm*,*.pdb,*.xml,BIMConcierge.addin"; \
    Check: IsRevit2025Selected

; .addin manifest for Revit 2025
Source: "..\src\BIMConcierge.Plugin\Resources\BIMConcierge.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; \
    Flags: ignoreversion; \
    Check: IsRevit2025Selected

; Plugin files for Revit 2026
Source: "{#BuildOutput}\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026\BIMConcierge"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Excludes: "runtimes\linux-*,runtimes\osx-*,runtimes\browser-*,runtimes\maccatalyst-*,runtimes\win-x86,runtimes\win-arm*,*.pdb,*.xml,BIMConcierge.addin"; \
    Check: IsRevit2026Selected

; .addin manifest for Revit 2026
Source: "..\src\BIMConcierge.Plugin\Resources\BIMConcierge.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026"; \
    Flags: ignoreversion; \
    Check: IsRevit2026Selected

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2025\BIMConcierge"
Type: files;          Name: "{userappdata}\Autodesk\Revit\Addins\2025\BIMConcierge.addin"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2026\BIMConcierge"
Type: files;          Name: "{userappdata}\Autodesk\Revit\Addins\2026\BIMConcierge.addin"

[Code]
var
  RevitVersionPage: TInputOptionWizardPage;
  Revit2025Installed: Boolean;
  Revit2026Installed: Boolean;

function IsRevit2025Installed: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\Autodesk\Revit\Autodesk Revit 2025');
end;

function IsRevit2026Installed: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\Autodesk\Revit\Autodesk Revit 2026');
end;

function IsRevit2025Selected: Boolean;
begin
  Result := RevitVersionPage.Values[0];
end;

function IsRevit2026Selected: Boolean;
begin
  Result := RevitVersionPage.Values[1];
end;

procedure InitializeWizard;
begin
  Revit2025Installed := IsRevit2025Installed;
  Revit2026Installed := IsRevit2026Installed;

  RevitVersionPage := CreateInputOptionPage(
    wpWelcome,
    CustomMessage('SelectRevitVersions'),
    CustomMessage('SelectRevitVersionsDesc'),
    '', True, False);

  RevitVersionPage.Add(CustomMessage('Revit2025'));
  RevitVersionPage.Add(CustomMessage('Revit2026'));

  // Pre-check installed versions
  RevitVersionPage.Values[0] := Revit2025Installed;
  RevitVersionPage.Values[1] := Revit2026Installed;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = RevitVersionPage.ID then
  begin
    // At least one version must be selected
    if not (RevitVersionPage.Values[0] or RevitVersionPage.Values[1]) then
    begin
      MsgBox(CustomMessage('NoRevitSelected'), mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;

  if CurPageID = wpWelcome then
  begin
    // Warn if no Revit detected
    if not (Revit2025Installed or Revit2026Installed) then
    begin
      if MsgBox(CustomMessage('NoRevitInstalled'), mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end;
  end;
end;
