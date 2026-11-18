; =====================================================================
;  PCFORGE - Programa de instalación (Inno Setup 6)
;
;  Arma un único setup.exe con:
;    {app}\PCFORGE-ValdezThiago-96VA.exe + DLLs + Resources   (la aplicación)
;    {app}\Instalador.exe                                       (instalador / reinstalador de BD)
;    (los scripts NN_*.sql van embebidos dentro de Instalador.exe)
;
;  Al terminar de copiar archivos ejecuta Instalador.exe (elevado):
;    - Primera vez: crea la base y la deja lista (usuario admin / Admin1234).
;    - Ya instalado: ofrece Reparar / Reinstalar desde cero / Restaurar un .bak,
;      siempre con backup automático previo.
;  El Instalador queda en el menú Inicio para volver a usarlo como reinstalador.
;
;  Cómo generarlo:
;    1. Compilar la solución en Release:
;         msbuild PCFORGE-ValdezThiago-96VA.sln /p:Configuration=Release
;    2. Abrir este archivo con Inno Setup 6 y compilar (Ctrl+F9),
;       o: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" PCFORGE_Setup.iss
;    3. El resultado queda en Setup_Output\PCFORGE_Setup_v2.exe
; =====================================================================

#define MyAppName      "PCFORGE"
#define MyAppVersion   "2.0"
#define MyAppPublisher "Valdez Thiago - UAI"
#define MyAppExeName   "PCFORGE-ValdezThiago-96VA.exe"
#define AppBin         "PCFORGE-ValdezThiago-96VA\bin\Release"
#define InstBin        "Instalador\bin\Release"

[Setup]
AppId={{3E342E01-2E71-4655-B9BE-1F3237F27D04}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Setup_Output
OutputBaseFilename=PCFORGE_Setup_v2
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el Escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Aplicación
Source: "{#AppBin}\{#MyAppExeName}";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\*.dll";                  DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppBin}\Resources\*";            DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs
; Instalador / reinstalador de base de datos
Source: "{#InstBin}\Instalador.exe";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#InstBin}\Instalador.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[InstallDelete]
; Versiones anteriores copiaban los scripts sueltos; ahora van embebidos en Instalador.exe.
Type: filesandordirs; Name: "{app}\Scripts"

[Icons]
Name: "{group}\{#MyAppName}";                         Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Configurar / reinstalar base de datos"; Filename: "{app}\Instalador.exe"; WorkingDir: "{app}"
Name: "{group}\Desinstalar {#MyAppName}";             Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                   Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; postinstall: corre con el usuario original (no elevado).
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
; Archivo de conexión que genera Instalador.exe. La base de datos NO se borra.
Type: files; Name: "{app}\conexion.config"

[Code]
const
  NET481_RELEASE = 533320;

function TieneNet481: Boolean;
var
  release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release)
            and (release >= NET481_RELEASE);
end;

function TieneSqlServerLocal: Boolean;
var
  instancias: TArrayOfString;
begin
  Result := False;
  if IsWin64 then
    Result := RegQueryMultiStringValue(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server', 'InstalledInstances', instancias);
  if not Result then
    Result := RegQueryMultiStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server', 'InstalledInstances', instancias);
  if Result then
    Result := GetArrayLength(instancias) > 0;
end;

function InitializeSetup: Boolean;
begin
  Result := True;

  if not TieneNet481 then
  begin
    MsgBox('PCFORGE necesita .NET Framework 4.8.1 o superior.' + #13#10#13#10 +
           'Instalalo desde https://dotnet.microsoft.com/download/dotnet-framework/net481 ' +
           'y volvé a ejecutar este instalador.', mbCriticalError, MB_OK);
    Result := False;
    exit;
  end;

  if not TieneSqlServerLocal then
    Result := MsgBox('No se detectó SQL Server instalado en este equipo.' + #13#10#13#10 +
                     'Si la base de datos va a estar en otro servidor podés continuar; ' +
                     'si no, instalá SQL Server 2022 (Express sirve) antes.' + #13#10#13#10 +
                     '¿Continuar igualmente?', mbConfirmation, MB_YESNO) = IDYES;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  resultado: Integer;
  params: String;
begin
  if CurStep <> ssPostInstall then
    exit;

  if WizardSilent then
    params := '--silent'
  else
    params := '--desde-setup';

  WizardForm.Hide;
  try
    if not Exec(ExpandConstant('{app}\Instalador.exe'), params, ExpandConstant('{app}'),
                SW_SHOW, ewWaitUntilTerminated, resultado) then
      resultado := -1;
  finally
    WizardForm.Show;
  end;

  if (resultado <> 0) and (not WizardSilent) then
    MsgBox('La base de datos no quedó configurada (código ' + IntToStr(resultado) + ').' + #13#10#13#10 +
           'Los archivos del sistema sí se instalaron. Para configurar la base más tarde usá ' +
           '"Configurar / reinstalar base de datos" desde el menú Inicio.', mbInformation, MB_OK);
end;
