# 主角状态身体帧 v02

2026-09-29，内置 image_gen，参考 Player_BodyLayers_v05.png 和 Player_Status_KeyPoses_v01.png，透明背景。

```text
Create one clean RGBA sprite sheet for the attached blue hooded clock-face hero. Use a precise 4-column by 2-row grid with generous transparent spacing. Eight separate complete body-only game animation cels, consistently proportioned to the reference: same cobalt hood, short blue cloak, black face with two thin cyan clock hands, black gloves and boots. Top row left to right: hurt recoil, hurt crouch, falling into death, lying defeated. Bottom row left to right: healing with hands cupped at chest, healing hands open at chest, time-power charge with both arms opened slightly, time-power charge tense hold. The blade is carried by a separate game sprite, so DO NOT draw any sword, blade, pommel, or sheath. No glow, aura, magic circle, shadows, ground, background, gradient, text, borders, or other objects. Exact true transparency outside character silhouettes. Flat clean dark outlines and painted blue shading matching reference; no clipped body parts.
```

四列两行。第一行受伤两帧、死亡两帧；第二行治疗两帧、蓄时两帧。身体图无武器；运行时背负层需要跟随状态绑定。
