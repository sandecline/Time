# 守卫动画 v03

2026-09-29，内置 image_gen，参考 Enemy_Guard_FrameDraft_v01.png，透明背景。

## 新绘 12 帧

```text
New clean transparent animation sprite sheet for the attached moss-covered clockwork stone guard. Four columns by three rows, 12 distinct isolated cels with generous transparent gutters and same character scale. Identity: squat round cracked grey stone body, short two legs, brass clock eye and side gear, small green moss tufts, one compact stone cleaver arm. Right facing 2D side view, crisp dark outline. Row 1: idle, left stepping, right stepping, idle. Row 2: retract the cleaver near shoulder as warning, strike with short cleaver horizontally forward at chest level, second short horizontal striking pose, recover. During strike the blade tip only one torso width in front of the guard, and blade never rises above head or drops below feet. Row 3: hurt recoil, damaged crouch, falling, fallen. Do not draw any attack arc. True alpha transparency outside silhouettes, NO glow, shadow, haze, aura, ground, background, black rectangle, text, separators or cut-off parts. Restrained stone grey, muted brass, moss green, orange clock eye; game sprite quality.
```

## 底光清理请求

```text
Edit this existing guard animation sheet. Keep ALL TWELVE guard drawings, their exact positions, pose designs and canvas resolution. Remove the diffuse olive background glow and every rectangular or circular backdrop so each guard is an isolated opaque sprite on fully transparent RGBA alpha. Outside the guard silhouettes and a crisp 1-pixel anti-aliased outline, alpha must be exactly zero. Preserve each moss tuft, clock eye, brass gear, stone cleaver and dark outline. Do not rearrange the 4 by 3 grid. No ground shadow or aura. This is a clean game sprite sheet.
```

v03 仍有少量半透明底光，作为试玩稿使用。接入时需要按有效攻击判定边界检查第二行第 2、3 帧的石刀。
