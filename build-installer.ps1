<#
.SYNOPSIS
    Build bộ cài đặt VietKey: chạy test → dotnet publish → đóng gói bằng Inno Setup.

.EXAMPLE
    .\build-installer.ps1                       # x64, tự chứa .NET (không cần cài runtime)
    .\build-installer.ps1 -FrameworkDependent   # file nhỏ, máy đích cần .NET 8 Desktop Runtime
    .\build-installer.ps1 -Arch arm64 -SkipTests
#>
[CmdletBinding()]
param(
    [string] $Version,
    [ValidateSet('x64', 'arm64')] [string] $Arch = 'x64',
    [switch] $FrameworkDependent,
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\VietKey\VietKey.csproj'
$artifacts = Join-Path $root 'artifacts'
$publishDir = Join-Path $artifacts "publish\win-$Arch"

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# ---- phiên bản: lấy từ <Version> trong csproj nếu không truyền vào
if (-not $Version) {
    $Version = ([xml](Get-Content $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $Version) { $Version = '0.1.0' }
}
Write-Host "VietKey $Version (win-$Arch, $(if ($FrameworkDependent) { 'framework-dependent' } else { 'self-contained' }))"

# ---- tìm Inno Setup trước để báo lỗi sớm
$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $iscc = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $iscc) {
    throw "Không tìm thấy Inno Setup 6 (ISCC.exe). Cài bằng:  winget install JRSoftware.InnoSetup"
}

# ---- test
if (-not $SkipTests) {
    Step 'dotnet test'
    dotnet test (Join-Path $root 'VietKey.sln') -c Release
    if ($LASTEXITCODE) { throw 'Test thất bại.' }
}

# ---- publish
Step "dotnet publish → $publishDir"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

$publishArgs = @(
    'publish', $project,
    '-c', 'Release',
    '-r', "win-$Arch",
    '-o', $publishDir,
    "-p:Version=$Version",
    '-p:PublishSingleFile=true',
    '-p:DebugType=none'
)
if ($FrameworkDependent) {
    $publishArgs += '--self-contained', 'false'
} else {
    $publishArgs += '--self-contained', 'true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:EnableCompressionInSingleFile=true'
}
dotnet @publishArgs
if ($LASTEXITCODE) { throw 'dotnet publish thất bại.' }

# ---- đóng gói
Step "Inno Setup ($iscc)"
$isccArgs = @(
    (Join-Path $root 'installer\VietKey.iss'),
    "/DAppVersion=$Version",
    "/DPublishDir=$publishDir",
    "/DOutputDir=$artifacts",
    "/DArch=$Arch"
)
if ($FrameworkDependent) { $isccArgs += '/DFrameworkDependent=1' }
& $iscc @isccArgs
if ($LASTEXITCODE) { throw 'Inno Setup thất bại.' }

$setup = Join-Path $artifacts "VietKey-Setup-$Version-win-$Arch.exe"
Step "Xong: $setup"
