<#
.SYNOPSIS
    Builds the BIMConcierge plugin and creates the installer (.exe).

.DESCRIPTION
    1. Builds the solution in Release|x64
    2. Removes unnecessary runtime folders from the output
    3. Compiles the Inno Setup script to produce the .exe installer

.PARAMETER Version
    The version number for the installer (default: 1.0.0)

.PARAMETER InnoSetupPath
    Path to ISCC.exe (Inno Setup Compiler). Auto-detected if not specified.

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version "1.2.0"
#>

param(
    [string]$Version = "1.0.0",
    [string]$InnoSetupPath = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BuildOutput = Join-Path $RepoRoot "src\BIMConcierge.Plugin\bin\Release"
$DistDir = Join-Path $RepoRoot "dist"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  BIMConcierge Installer Builder v$Version"  -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Find Inno Setup ──────────────────────────────────────────────────

if ([string]::IsNullOrEmpty($InnoSetupPath)) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $InnoSetupPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrEmpty($InnoSetupPath) -or -not (Test-Path $InnoSetupPath)) {
    Write-Host "ERRO: Inno Setup (ISCC.exe) nao encontrado." -ForegroundColor Red
    Write-Host "Instale o Inno Setup 6: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "Ou passe o caminho com: -InnoSetupPath 'C:\...\ISCC.exe'" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Inno Setup encontrado: $InnoSetupPath" -ForegroundColor Green

# ── Step 2: Build Solution ───────────────────────────────────────────────────

Write-Host ""
Write-Host "[BUILD] Compilando solucao em Release|x64..." -ForegroundColor Yellow

$sln = Join-Path $RepoRoot "BIMConcierge.sln"
dotnet build $sln -c Release -p:Platform=x64 --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Build falhou." -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Build concluido com sucesso." -ForegroundColor Green

# ── Step 3: Clean unnecessary runtimes ───────────────────────────────────────

Write-Host ""
Write-Host "[CLEAN] Removendo runtimes desnecessarios..." -ForegroundColor Yellow

$runtimesDir = Join-Path $BuildOutput "runtimes"
if (Test-Path $runtimesDir) {
    $unwantedRuntimes = Get-ChildItem -Path $runtimesDir -Directory |
        Where-Object { $_.Name -notlike "win-x64" }

    foreach ($dir in $unwantedRuntimes) {
        Remove-Item -Path $dir.FullName -Recurse -Force
        Write-Host "  Removido: runtimes\$($dir.Name)" -ForegroundColor DarkGray
    }
}

Write-Host "[OK] Limpeza concluida." -ForegroundColor Green

# ── Step 4: Compile Installer ────────────────────────────────────────────────

Write-Host ""
Write-Host "[INSTALLER] Compilando instalador..." -ForegroundColor Yellow

$issFile = Join-Path $PSScriptRoot "BIMConcierge.iss"

if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

& $InnoSetupPath /DAppVersion="$Version" $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Compilacao do instalador falhou." -ForegroundColor Red
    exit 1
}

# ── Done ─────────────────────────────────────────────────────────────────────

$outputFile = Join-Path $DistDir "BIMConcierge-Setup-$Version.exe"
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Instalador criado com sucesso!" -ForegroundColor Green
Write-Host "  $outputFile" -ForegroundColor White
if (Test-Path $outputFile) {
    $size = [math]::Round((Get-Item $outputFile).Length / 1MB, 2)
    Write-Host "  Tamanho: ${size} MB" -ForegroundColor White
}
Write-Host "============================================" -ForegroundColor Green
