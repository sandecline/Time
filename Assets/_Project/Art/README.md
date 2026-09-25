# 正式美术资源

这里只存放已经完成透明通道检查、裁切、像素清理和尺寸统一的正式游戏资源。ChatGPT 原始输出保存在项目根目录 `ArtSource/ChatGPT`。

`PixelArtTexturePostprocessor` 会自动统一 32 PPU、Point 过滤、无压缩、关闭 Mipmap 和透明通道。动画表仍需在 Sprite Editor 中设为 Multiple，并按主角／普通敌人 `96 × 96`、Boss `128 × 128` 的网格切片。
