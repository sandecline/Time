# 将分层参考板整理为同画布的待机试验图层，不覆盖已确认原稿。
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$sourcePath = Join-Path $projectRoot 'ArtSource\V2\01_Player\KeyPoses\Player_Idle_Right_LayerReference_v01.png'
$outputDir = Join-Path $projectRoot 'ArtSource\V2\01_Player\KeyPoses\A1Trial_v01'
$names = @('Player_Idle_Right_Body.png', 'Player_Idle_Right_ClockHands.png', 'Player_Idle_Right_Weapon.png', 'Player_Idle_Right_Composite.png')

if (-not (Test-Path -LiteralPath $sourcePath)) { throw "缺少参考板：$sourcePath" }
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}
foreach ($name in $names) {
    if (Test-Path -LiteralPath (Join-Path $outputDir $name)) { throw "输出文件已存在，不覆盖：$name" }
}

function New-TransparentBitmap([int]$width, [int]$height) {
    return [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

# 仅保留参考板中的角色像素；硬 Alpha 避免半透明暗光进入试验图层。
function Copy-Part($source, $rectangle, [double]$scale, [int]$targetX, [int]$targetY, [bool]$flipX) {
    $result = New-TransparentBitmap 512 640
    $targetWidth = [int][Math]::Round($rectangle.Width * $scale)
    $targetHeight = [int][Math]::Round($rectangle.Height * $scale)
    for ($y = 0; $y -lt $targetHeight; $y++) {
        $sourceY = $rectangle.Y + [Math]::Min($rectangle.Height - 1, [int][Math]::Floor(($y + 0.5) / $scale))
        for ($x = 0; $x -lt $targetWidth; $x++) {
            $sourceOffsetX = [Math]::Min($rectangle.Width - 1, [int][Math]::Floor(($x + 0.5) / $scale))
            if ($flipX) { $sourceX = $rectangle.Right - 1 - $sourceOffsetX }
            else { $sourceX = $rectangle.X + $sourceOffsetX }
            $color = $source.GetPixel($sourceX, $sourceY)
            if ($color.A -gt 80) {
                $opaque = [System.Drawing.Color]::FromArgb(255, $color.R, $color.G, $color.B)
                $result.SetPixel($targetX + $x, $targetY + $y, $opaque)
            }
        }
    }
    return $result
}

# 每 8 个参考像素取一个中心样本，得到暂用的 64×80 逻辑像素画布。
function Convert-ToLogicalPixels($source) {
    $result = New-TransparentBitmap 64 80
    for ($y = 0; $y -lt 80; $y++) {
        for ($x = 0; $x -lt 64; $x++) {
            $result.SetPixel($x, $y, $source.GetPixel($x * 8 + 4, $y * 8 + 4))
        }
    }
    return $result
}

$source = $null
$weaponLarge = $null
$bodyLarge = $null
$clockLarge = $null
$weapon = $null
$body = $null
$clock = $null
$composite = $null
$graphics = $null
try {
    $source = [System.Drawing.Bitmap]::new($sourcePath)
    if ($source.Width -ne 1774 -or $source.Height -ne 887) { throw '参考板尺寸已变化，需要重新核对裁切位置。' }

    # 武器在身体后方；双针的公共转轴落在试验画布的 (41,27)，脚底基线为 y=74。
    $weaponLarge = Copy-Part $source ([System.Drawing.Rectangle]::new(883, 258, 312, 405)) 0.88 134 179 $true
    $bodyLarge = Copy-Part $source ([System.Drawing.Rectangle]::new(141, 177, 280, 551)) 1.0 116 42 $false
    $clockLarge = Copy-Part $source ([System.Drawing.Rectangle]::new(596, 368, 207, 133)) 0.36 294 176 $false

    $weapon = Convert-ToLogicalPixels $weaponLarge
    $body = Convert-ToLogicalPixels $bodyLarge
    $clock = Convert-ToLogicalPixels $clockLarge

    $composite = New-TransparentBitmap 64 80
    $graphics = [System.Drawing.Graphics]::FromImage($composite)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.DrawImageUnscaled($weapon, 0, 0)
    $graphics.DrawImageUnscaled($body, 0, 0)
    $graphics.DrawImageUnscaled($clock, 0, 0)

    $weapon.Save((Join-Path $outputDir $names[2]), [System.Drawing.Imaging.ImageFormat]::Png)
    $body.Save((Join-Path $outputDir $names[0]), [System.Drawing.Imaging.ImageFormat]::Png)
    $clock.Save((Join-Path $outputDir $names[1]), [System.Drawing.Imaging.ImageFormat]::Png)
    $composite.Save((Join-Path $outputDir $names[3]), [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "已生成：$outputDir"
}
finally {
    foreach ($item in @($graphics, $composite, $clock, $body, $weapon, $clockLarge, $bodyLarge, $weaponLarge, $source)) {
        if ($null -ne $item) { $item.Dispose() }
    }
}
