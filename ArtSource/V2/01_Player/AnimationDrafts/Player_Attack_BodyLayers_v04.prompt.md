# 主角三向攻击身体分层 v04

2026-09-29，使用内置 image_gen，透明背景，参考 Player_BodyLayers_v05.png。

## 初次生成提示

Create a NEW production-ready transparent sprite sheet for this exact cobalt blue hooded clock-face hero, matching the attached character identity and proportions. Exactly 12 BODY-ONLY attack animation cels arranged in a precise 4-column by 3-row grid, equal cell sizes with ample transparent gutters. Flat orthographic side-view facing RIGHT, dark outline, crisp hand-drawn game sprite finish, no lettering. No weapons of any kind, no glows, NO background color, no checkerboard painted in image, no shadows, no motion trails. The weapon will be attached as a separate rigid sprite in Unity. Same head size and body scale all cels. Row1 horizontal attack: slight windup with black glove at chest, arm extending right with fist at mid chest, follow-through with fist at mid chest to right, recovery. Row2 UP attack: body slightly crouched preparing, one arm thrust straight UP with gripping fist just above crown on character central vertical axis, follow-through upward with same overhead grip, recovery. Row3 DOWN aerial attack: curled jump preparing, bent knees pulled aside and both hands gripping at belly on central vertical axis for downward pointer strike, compressed follow-through with both hands at belly, recovery in air. Make distinct expressive physical poses, cloak cloth follows force. Keep character complete inside each cell and all hands unobscured. The fist in up and down strike must align vertically with the body center, not forward of face. Entire sheet has true alpha transparency. NO props, no weapon ends behind the cloak.

## 上劈握点修正提示

Edit ONLY the two middle cels of the SECOND ROW of this transparent attack sprite sheet. Keep all other ten cels pixel-identical and keep canvas/grid/transparency. Those two UP ATTACK poses currently have a fully extended arm and fist ABOVE the hood. Replace each with a crouched powerful upward-attack pose: BOTH elbows bent LOW against the torso, both black gloves gripping together directly in FRONT OF THE CHEST at chin height (below the eyes/clock hands), on the center vertical axis of the torso. There will be a vertical weapon pointing upward from that chest grip; do NOT draw the weapon. Absolutely no glove above the hood, no raised extended arm. Preserve original hood/face identity, reference character head size and blue cloak art style. Maintain same feet baseline. All twelve cels body-only, transparent outside silhouette, NO weapons, glow, ground, shadow or lettering.

## 接入约束

- 4 列 × 3 行：横砍、上劈、下劈，每组 4 帧。
- 身体图不包含武器。运行时复用完整直指针，圆柄与尖端始终同轴。
- 上劈胸前持握，避免手举过头后武器超出上方攻击体积。
- 刀光由 PlayerSlashVisual 根据实际判定尺寸生成，身体图不烘焙刀光。
- 原碰撞体和战斗数值保持不变。
