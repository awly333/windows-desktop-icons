# Build self-contained app + Inno Setup installer.
#
# Output:
#   <repo>\publish\app\        — dotnet publish output
#   <repo>\publish\installer\  — Inno Setup .exe

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$pub  = Join-Path $root "publish\app"
$out  = Join-Path $root "publish\installer"
$iss  = Join-Path $root "installer\DesktopIcons.iss"
$proj = Join-Path $root "src\DesktopIcons.App\DesktopIcons.App.csproj"

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

# Clean previous outputs
if (Test-Path $pub) { Remove-Item -Recurse -Force $pub }
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

# Publish self-contained
Push-Location $root
try {
    Write-Host "Publishing self-contained build..."
    dotnet publish $proj -c Release -r win-x64 -o $pub --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
} finally {
    Pop-Location
}

# Compile installer
Write-Host "Compiling installer..."
& $iscc $iss
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
