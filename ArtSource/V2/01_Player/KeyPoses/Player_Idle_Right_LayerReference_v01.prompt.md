# 主角右向待机分层参考板 v01

生成日期：2026-09-28。使用内置图像生成工具。输入：已确认的 `Player_Design_Master.png`（角色身份参考）、`Player_Idle_Right_Draft_v02.png`（右向待机姿势参考）。生成后以首版参考板和母版做了一次留白修订，最终文件为 `Player_Idle_Right_LayerReference_v01.png`。

## 第一轮提示词

```text
Use case: precise-object-edit. Asset type: A1 character layer-separation REFERENCE BOARD for an original 2D pixel-cartoon side-scrolling game; not final animation sprites. Image 1 is the APPROVED CHARACTER IDENTITY master; image 2 is the RIGHT-FACING IDLE POSE reference. Make one transparent PNG with FOUR widely separated, fully visible components on a common visual scale and clear space between them, no labels or text: (1) the complete right-facing blue hood, short travel robe, short black hands and legs, with the hood's inner area solid opaque black, but OMIT the blue clock hands and omit the weapon; (2) just the TWO blue clock hands of visibly different lengths plus their shared blue center pivot, isolated and unchanged in shape; (3) just ONE complete silver-blue long clock-pointer weapon, straight, pointed at one end and with a small round counterweight at the other, no sword grip or guard; (4) a smaller fully assembled right-facing idle pose for cross-checking the same exact parts and proportions. Preserve the master character's silhouette, blue palette and simple hard-edged pixel-cartoon blocks. In the assembled pose, the weapon lies diagonally close behind the back, pointed tip visible behind the image-left shoulder and round counterweight visible next to the image-right robe hem. Feet on a level baseline. Hood contains NO face, mask, eyes, mouth, clock dial, marks, or gear—only the two blue hands and shared pivot in the assembled pose. Keep all four components completely non-overlapping, with ample transparent margins. Transparent alpha background everywhere outside the components; no checkerboard, dark halo, glow, vignette, shadow, scenery, floor, UI, typography, logo, watermark, extra poses, or animation frames. This board only guides later manual slicing and alignment; do not portray it as a finished spritesheet.
```

## 留白修订提示词

```text
Use case: precise-object-edit. EDIT TARGET is image 1: a four-part character layer-separation reference board. Image 2 is the approved character identity reference. Make only one targeted layout correction: scale ALL FOUR existing parts down uniformly to about 75% of their current visual height and recenter them so that EVERY part, including both characters' feet, short hands, weapon tip and round counterweights, has at least 8% transparent margin from every canvas edge. Keep the same four-part left-to-right layout, same blue hood and short robe, opaque black hood interior, two blue clock hands and pivot, one standalone silver-blue clock pointer weapon, and one assembled right-facing pose. Do not alter the design, colors, directions, proportions, number of parts, or relative character features. Do not add labels, checkerboard, floor, halo, glow, scenery, or text. All parts must remain separate, fully visible and non-overlapping on actual transparent alpha background. This remains a manual-slicing reference board, not a finished sprite sheet.
```

## 用途与检查

- 四部分依次为无指针／武器的身体、双针与中心轴、完整背负武器、组合待机姿势；可供人工核对和修改。
- 输出尺寸 1774×887 RGBA，内容未贴画布边缘；但仍有大量半透明边缘像素，不能直接裁成正式 Sprite。
- 各部分尚未对齐到同一坐标、脚底基线与逻辑像素网格；武器和双针的转轴也未用于 Unity 动画。本次未导入 Unity 或改动场景。
