# 主角右向待机清理候选 v03

生成日期：2026-09-28。使用内置图像生成工具，以 `Player_Design_Master.png` 为角色身份参考、`Player_Idle_Right_Draft_v02.png` 为编辑目标。

## 本次提示词

```text
Use case: background-extraction. Asset type: one clean right-facing idle pose reference for an original 2D pixel-cartoon platform game. Input image 1 is the approved CHARACTER IDENTITY reference (the design master); input image 2 is the EDIT TARGET (existing right-facing idle draft v02). Keep the edit target's one-character right-facing idle pose, proportions, blue hood and short travel robe, short black hands and legs, hood silhouette, two blue clock hands of visibly different lengths and their shared blue pivot, and the exact back-carried single silver-blue clock-pointer weapon with pointed tip emerging behind the image-left shoulder and small circular counterweight beside the image-right robe hem. Preserve the weapon as one straight object without a sword handle, guard, or sheath. Clean the artwork into a crisp hard-edged pixel-cartoon cutout: the hood's interior black shadow, hands, legs, body, blue clothing and weapon must be fully opaque, while every pixel outside the subject and weapon must be truly transparent alpha 0. Remove the large dark blue/black halo, glow, shadow, soft antialias fringe and stray pixels. Keep the entire figure and both weapon ends visible with spare transparent margin, stable horizontal sole baseline, and centered body. Single character only, no background, checkerboard, floor shadow, scenery, typography, watermark, extra views, new costume details, face, mask, clock dial, gear, or extra weapon. This is a faithful cleanup of v02 for the first A1 review, not a redesigned character or an animation sheet.
```

## 检查结果

- 输出为 1024×1536 RGBA，保留了主角和背负武器的大体身份。
- 仍有大范围半透明暗光；Alpha 最大值为 254，没有完全不透明的角色像素。未达到正式 Sprite 标准。
- 只作为可修改的美术候选稿。未导入 Unity，未替换 v02 或已确认的母版，未锁定像素规格。
