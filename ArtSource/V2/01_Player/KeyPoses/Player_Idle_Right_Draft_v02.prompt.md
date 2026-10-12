# 右向待机草稿生成记录

日期：2026-09-27。方式：内置图像生成工具；两次调用均要求透明背景。

- 身份参考：`../Player_Design_Master.png`，已获用户确认。
- 初稿：`../Archive/Player_Idle_Right_Draft_v01.png`。
- 最新草稿：`Player_Idle_Right_Draft_v02.png`。
- 生成尺寸：1024×1536；PNG 包含真实透明通道。它仍是大尺寸生成原稿，未完成最终像素网格、分层、裁切或游戏内验收。
- 尚未作为正式 Sprite 导入；指针走动尚未实现。

## 初稿请求摘要

依据已确认设定生成单个完整角色，右向三分之四待机站姿、双脚同一基线、透明背景和充分边距。保持蓝色兜帽短袍、无五官黑色帽内区域、两根蓝色指针、小黑手和短黑腿、一体式背负指针武器。画面不包含背视图、武器附图、文字或场景。指针以约 10:10 静态角度示意，后续单独分层制作走针。

## v02 修订使用的完整提示词

输入图片为 v01；以下为本次编辑请求原文。请求内容不等于已经完成逐像素验收。

```text
Precise cleanup edit of this single pixel-art character PNG. Preserve the character's EXACT design, hood, robe, short shadow-black limbs, black face void with two blue clock hands, pose, proportions, colors, size, position and transparent canvas. Do NOT redesign or add anything.

CHANGE ONLY TWO THINGS:
1. Remove ALL soft blue/gray haze, aura, bloom, fog, radiating light and glow surrounding the silhouette and surrounding the weapon. Every pixel OUTSIDE the actual hard-edged character and metal weapon silhouette must be completely transparent alpha 0. Keep the dark black face and dark hands/legs OPAQUE so they stay visible on a light background. Keep the blue clock hands bright solid blue pixels but give them NO blurred glow spreading across the cloth or into the surrounding background. No shadows or background fill. Clean crisp pixel-art outline, no feathering or antialiasing, genuinely transparent background, no checkerboard.
2. Correct the stored weapon's endpoint alignment: keep the pointed weapon tip visible behind the character's LEFT shoulder on the IMAGE LEFT. Move the SMALL ROUND COUNTERWEIGHT that currently hangs at IMAGE LEFT of the hem to just outside the IMAGE RIGHT hip/hem. It must align with the same single straight clock-hand weapon running diagonally from IMAGE UPPER LEFT shoulder to IMAGE LOWER RIGHT hip, whose middle remains hidden behind the robe. There is exactly ONE weapon, not two separate pieces. No straps, handles, guards or scabbards. Do not move the pose or limbs.

Output one full-body character only, same canvas and generous padding, no sheet, no callouts, no text. Transparent PNG.
```

## 下一步

按照 `Docs/Art/V2ArtProductionPlan.md` 的 A1 阶段进行技术整理。分离指针时补齐其后的不透明黑色帽内区域；身体姿势、转轴和武器挂接点需统一，不能直接以大图透明边界生成碰撞框。
