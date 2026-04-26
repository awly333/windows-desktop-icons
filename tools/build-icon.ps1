#requires -Version 5.1
<#
    Generates AppIcon.ico (multi-size) and AppIcon-32.png from code-drawn 3x3 grid.
    Run once when the icon design changes; commit the resulting files.

    Usage:
      powershell.exe -ExecutionPolicy Bypass -File tools\build-icon.ps1
#>

[CmdletBinding()]
param(
    [string]$OutDir
)

Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'

if (-not $OutDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $OutDir = Join-Path $scriptDir '..\src\DesktopIcons.App\Assets'
}
$OutDir = [System.IO.Path]::GetFullPath($OutDir)

# Cells render directly on the transparent canvas — system background (taskbar,
# title bar, tray) shows through, giving the native, non-image-y feel.
$gray = [System.Drawing.Color]::FromArgb(0xFF, 0xC8, 0xC8, 0xCD)
$blue = [System.Drawing.Color]::FromArgb(0xFF, 0x00, 0x71, 0xE3)

# size, cell, gap, pad, cell-radius
$specs = @(
    @{ Size = 16;  Cell = 4;  Gap = 1; Pad = 1; Radius = 1 },
    @{ Size = 20;  Cell = 4;  Gap = 2; Pad = 2; Radius = 1 },
    @{ Size = 24;  Cell = 6;  Gap = 1; Pad = 2; Radius = 1 },
    @{ Size = 32;  Cell = 8;  Gap = 2; Pad = 2; Radius = 2 },
    @{ Size = 48;  Cell = 12; Gap = 2; Pad = 4; Radius = 3 },
    @{ Size = 64;  Cell = 16; Gap = 4; Pad = 4; Radius = 3 },
    @{ Size = 256; Cell = 56; Gap = 12; Pad = 32; Radius = 12 }
)

function New-RoundedRectPath {
    param([float]$X, [float]$Y, [float]$W, [float]$H, [float]$R)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    if ($R -le 0) {
        $path.AddRectangle((New-Object System.Drawing.RectangleF -ArgumentList $X, $Y, $W, $H))
        return $path
    }
    $d = $R * 2.0
    $path.AddArc($X,           $Y,           $d, $d, 180.0, 90.0)
    $path.AddArc($X + $W - $d, $Y,           $d, $d, 270.0, 90.0)
    $path.AddArc($X + $W - $d, $Y + $H - $d, $d, $d,   0.0, 90.0)
    $path.AddArc($X,           $Y + $H - $d, $d, $d,  90.0, 90.0)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$Size, [int]$Cell, [int]$Gap, [int]$Pad, [int]$Radius)
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.Clear([System.Drawing.Color]::Transparent)

        # 3x3 grid cells on the transparent canvas
        $grayBrush = New-Object System.Drawing.SolidBrush -ArgumentList $gray
        $blueBrush = New-Object System.Drawing.SolidBrush -ArgumentList $blue
        try {
            for ($row = 0; $row -lt 3; $row++) {
                for ($col = 0; $col -lt 3; $col++) {
                    $x     = $Pad + $col * ($Cell + $Gap)
                    $y     = $Pad + $row * ($Cell + $Gap)
                    $brush = if ($row -eq 1 -and $col -eq 1) { $blueBrush } else { $grayBrush }
                    $path  = New-RoundedRectPath $x $y $Cell $Cell $Radius
                    try { $g.FillPath($brush, $path) }
                    finally { $path.Dispose() }
                }
            }
        }
        finally {
            $grayBrush.Dispose()
            $blueBrush.Dispose()
        }
    }
    finally {
        $g.Dispose()
    }
    return $bmp
}

function ConvertTo-PngBytes {
    param([System.Drawing.Bitmap]$Bitmap)
    $ms = New-Object System.IO.MemoryStream
    try {
        $Bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$ms.ToArray()
    }
    finally { $ms.Dispose() }
}

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

$blobs = New-Object System.Collections.Generic.List[object]
foreach ($spec in $specs) {
    $bmp = New-IconBitmap @spec
    try {
        $bytes = ConvertTo-PngBytes $bmp
        $blobs.Add(@{ Size = $spec.Size; Data = $bytes })
        if ($spec.Size -eq 32) {
            $pngPath = Join-Path $OutDir 'AppIcon-32.png'
            [System.IO.File]::WriteAllBytes($pngPath, $bytes)
            Write-Host "wrote $pngPath ($($bytes.Length) bytes)"
        }
    }
    finally {
        $bmp.Dispose()
    }
}

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter -ArgumentList $ms
try {
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$blobs.Count)

    $dataOffset = 6 + 16 * $blobs.Count
    foreach ($p in $blobs) {
        $w = if ($p.Size -ge 256) { [byte]0 } else { [byte]$p.Size }
        $h = if ($p.Size -ge 256) { [byte]0 } else { [byte]$p.Size }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$p.Data.Length)
        $bw.Write([uint32]$dataOffset)
        $dataOffset += $p.Data.Length
    }
    foreach ($p in $blobs) {
        $bw.Write([byte[]]$p.Data, 0, $p.Data.Length)
    }
    $bw.Flush()

    $icoPath = Join-Path $OutDir 'AppIcon.ico'
    [System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())
    $sizeList = ($blobs | ForEach-Object { $_.Size }) -join ', '
    Write-Host "wrote $icoPath ($($ms.Length) bytes) sizes: $sizeList"
}
finally {
    $bw.Dispose()
    $ms.Dispose()
}
