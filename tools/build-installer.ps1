# Build release app output + Inno Setup installer.
#
# Output:
#   <repo>\installer-output\DesktopIcons-Setup-<version>.exe

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$out  = Join-Path $root "installer-output"
$iss  = Join-Path $root "installer\DesktopIcons.iss"
$proj = Join-Path $root "src\DesktopIcons.App\DesktopIcons.App.csproj"
$buildOut = Join-Path $root "src\DesktopIcons.App\bin\Release\net8.0-windows10.0.19041.0\win-x64"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$stageRoot = Join-Path $root ".artifacts\installer-build\$stamp"
$pub = Join-Path $stageRoot "app"

function Find-Iscc {
    foreach ($p in @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )) {
        if (Test-Path $p) { return $p }
    }
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) {
    Write-Error "ISCC.exe not found. Install Inno Setup 6: winget install JRSoftware.InnoSetup"
    exit 1
}
Write-Host "Using ISCC: $iscc"

# Clean staging/output
if (Test-Path $stageRoot) { Remove-Item -Recurse -Force $stageRoot }
New-Item -ItemType Directory -Path $pub -Force | Out-Null
New-Item -ItemType Directory -Path $out -Force | Out-Null

if (-not $SkipBuild) {
    Push-Location $root
    try {
        Write-Host "Building release output..."
        dotnet build $proj -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }
    } catch {
        if (-not (Test-Path $buildOut)) { throw }
        Write-Warning "dotnet build failed; falling back to existing release output at $buildOut"
    } finally {
        Pop-Location
    }
}

if (-not (Test-Path $buildOut)) {
    Write-Error "Release build output not found: $buildOut"
    exit 1
}

Write-Host "Staging release output..."
Copy-Item -Path (Join-Path $buildOut "*") -Destination $pub -Recurse -Force

# Compile installer
Write-Host "Compiling installer..."
& $iscc `
    "/DMyPublishDir=$pub" `
    "/DMyOutputDir=$out" `
    "/DMyOutputBaseFilename=DesktopIcons-Setup-0.1.0" `
    $iss
if ($LASTEXITCODE -ne 0) {
    Write-Error "ISCC failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "Installer built:"
Get-ChildItem $out -Filter *.exe | ForEach-Object {
    $sizeMb = [math]::Round($_.Length / 1MB, 1)
    Write-Host ("  {0}  ({1} MB)" -f $_.FullName, $sizeMb)
}
