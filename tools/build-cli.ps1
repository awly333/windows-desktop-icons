# Build self-contained single-file CLI exe.
#
# Output:
#   <repo>\cli-output\DesktopIcons.Cli.exe

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "src\DesktopIcons.Cli\DesktopIcons.Cli.csproj"
$out  = Join-Path $root "cli-output"

New-Item -ItemType Directory -Path $out -Force | Out-Null

Push-Location $root
try {
    Write-Host "Publishing CLI..."
    dotnet publish $proj -c Release -o $out --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "CLI built:"
Get-ChildItem $out -Filter "DesktopIcons.Cli.exe" | ForEach-Object {
    $sizeMb = [math]::Round($_.Length / 1MB, 1)
    Write-Host ("  {0}  ({1} MB)" -f $_.FullName, $sizeMb)
}
