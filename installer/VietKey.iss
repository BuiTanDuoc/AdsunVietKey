; ============================================================================
;  VietKey – bộ cài đặt (Inno Setup 6.5+)
;  Build:  .\build-installer.ps1        (khuyên dùng – tự publish rồi đóng gói)
;     hoặc ISCC.exe installer\VietKey.iss /DAppVersion=0.3.0 /DPublishDir=..\artifacts\publish\win-x64
; ============================================================================

#ifndef AppVersion
  #define AppVersion "0.3.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

#define AppName      "VietKey"
#define AppExe       "VietKey.exe"
#define AppPublisher "VietKey"
#define AppId        "{{6F1B7A52-3C9E-4E7B-9C1D-5A2B8E0F4D17}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppComments=Bộ gõ tiếng Việt Telex/VNI
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} Setup

; Mặc định cài cho người dùng hiện tại (không cần quyền admin, giống cấu hình HKCU của VietKey);
; người dùng có thể chọn "cài cho mọi người" ở hộp thoại đầu tiên.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes

#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
MinVersion=10.0

OutputDir={#OutputDir}
OutputBaseFilename=VietKey-Setup-{#AppVersion}-win-{#Arch}
SetupIconFile=..\src\VietKey\VietKey.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName} {#AppVersion}
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=auto

[Languages]
Name: "vi"; MessagesFile: "Vietnamese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
vi.TaskStartup=Khởi động VietKey cùng Windows
vi.TaskDesktop=Tạo biểu tượng trên màn hình Desktop
vi.GroupOptions=Tuỳ chọn:
vi.LaunchApp=Chạy VietKey ngay
vi.RemoveSettings=Xoá cả cấu hình và bảng gõ tắt của VietKey?%n%n(Chọn "No" để giữ lại cho lần cài sau.)
vi.NeedRuntime=VietKey cần .NET 8 Desktop Runtime (x64).%n%nMở trang tải về ngay bây giờ?
en.TaskStartup=Start VietKey with Windows
en.TaskDesktop=Create a desktop shortcut
en.GroupOptions=Options:
en.LaunchApp=Launch VietKey now
en.RemoveSettings=Also remove VietKey settings and macro table?%n%n(Choose "No" to keep them for a future install.)
en.NeedRuntime=VietKey requires the .NET 8 Desktop Runtime (x64).%n%nOpen the download page now?

[Tasks]
Name: "startup"; Description: "{cm:TaskStartup}"; GroupDescription: "{cm:GroupOptions}"
Name: "desktopicon"; Description: "{cm:TaskDesktop}"; GroupDescription: "{cm:GroupOptions}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Cùng khoá mà VietKey dùng cho tuỳ chọn "Khởi động cùng Windows" (luôn theo người dùng hiện tại)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "VietKey"; \
    ValueData: """{app}\{#AppExe}"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "KillVietKey"

[Code]
{ Đóng VietKey đang chạy (bộ gõ chạy nền ở khay hệ thống nên Restart Manager có thể không đóng được) }
procedure KillVietKey();
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#AppExe}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

#ifdef FrameworkDependent
const
  RuntimeUrl = 'https://dotnet.microsoft.com/download/dotnet/8.0';

{ Bản publish framework-dependent cần .NET 8 Desktop Runtime trên máy }
function HasDesktopRuntime8(): Boolean;
var
  FindRec: TFindRec;
  Base: String;
begin
  Result := False;
  Base := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  if FindFirst(Base + '8.*', FindRec) then
  try
    repeat
      if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
      begin
        Result := True;
        Exit;
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

#endif

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
#ifdef FrameworkDependent
  if not HasDesktopRuntime8() then
  begin
    if MsgBox(CustomMessage('NeedRuntime'), mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', RuntimeUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;
  end;
#endif
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillVietKey();
  Result := '';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    { VietKey có thể tự bật "Khởi động cùng Windows" sau khi cài → luôn xoá khi gỡ }
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'VietKey');

  if CurUninstallStep = usPostUninstall then
  begin
    if UninstallSilent() then
      Exit;
    if MsgBox(CustomMessage('RemoveSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    begin
      RegDeleteKeyIncludingSubkeys(HKCU, 'Software\VietKey');
      DelTree(ExpandConstant('{userappdata}\VietKey'), True, True, True);
    end;
  end;
end;
