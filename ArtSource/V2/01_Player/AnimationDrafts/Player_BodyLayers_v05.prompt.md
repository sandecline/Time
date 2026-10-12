# 跑步武器尺寸与旧站姿错位修正

工具：内置 imagegen。源图 v01 的六帧跑步武器过短；v04 去掉跑步武器后，上排仍遗留错位的旧站姿，被用户指出。v05 全图只保留身体图层。游戏中的站立和跑步均叠加同一根完整武器，六帧跑步仅调整握点和角度，不改变武器长度与粗细。

后续用户确认平时可以背负。当前采用 v06 背负位置：六帧跑步只随背部小幅摆动，双手为空并自然摆臂，不再使用前一轮手持握点；武器长度、粗细和连续轴线保持不变。

## v04 跑步身体分层提示词

Surgical edit of this transparent pixel-art character animation atlas for layered game sprites. Keep the EXACT 1983x793 canvas and all character positions, proportions, poses, silhouettes, colors, faces and legs. Preserve the entire top row of four standing characters unchanged. In the LOWER ROW of SIX RUNNING CHARACTERS ONLY, remove ALL silver-blue weapon parts: every pointed blade sticking out behind shoulders and every round silver counterweight near a hand. Restore genuine transparency where those weapon pixels were outside the character, and preserve or reconstruct the small black glove behind the counterweight. Six lower-row running sprites should have EMPTY black hands and NO silver fragments, NO weapon. Do not resize, rearrange, redraw, simplify or change the running poses or character identity. Do not add a replacement weapon anywhere. No background, no halo, no text. Real transparent alpha. The game will render a separate full-length pointer behind these running body frames.

## v05 最终提示词

Correct the remaining inconsistent source sheet by making ALL TEN characters BODY-ONLY layers. Preserve exact 1983x793 canvas, all sprite positions, blue clothes, black gloves, clock faces, silhouettes and poses. Six running characters on the bottom are already weapon-free and MUST remain unchanged. On the FOUR standing characters in the TOP row remove the incorrectly aligned silver shoulder blades AND silver circular pommels, restoring transparent space and preserving black gloves. No weapon fragments on ANY character, no new weapons anywhere. Do not change the pose or body proportions. True transparent alpha, no checkerboard pattern or background. This is a body layer atlas; a SINGLE complete rigid straight weapon will be rendered behind each body by the game so the blade axis and circular counterweight center are mathematically collinear. Do not paint separate disconnected weapon ends.

## 使用说明

- `Player_Run_Layered_v04.png` 是被 v05 替代的过程稿，上排旧武器不合格，不作为当前展示或制作依据。
- 当前待机仍使用 v03 身体帧；跑动使用 v05 下排，保留 v01 原有切片画布和脚底基线。
- `Player_Weapon_AxisCheck_v05.png` 是游戏相机的检查截图，临时淡化身体以显示武器连续轴线；游戏角色不透明。
- 六帧跑动效果：`ArtSource/V2/Previews/Player_Run_Weapon_Frame0_v05.png` 至 `Player_Run_Weapon_Frame5_v05.png`。
- 当前背负跑动效果：`ArtSource/V2/Previews/Player_Run_BackWeapon_Frame0_v06.png` 至 `Player_Run_BackWeapon_Frame5_v06.png`，以及 `Player_Run_BackWeapon_Left_v06.png`。v05 手持检查图仅留作过程记录。
