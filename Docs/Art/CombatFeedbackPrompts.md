# 战斗前摇与受击反馈美术素材清单

## 需要补充的素材

| 文件名 | 内容 | 规格 | 原始素材目录 |
|---|---|---|---|
| `CombatFeedback_Master.png` | 统一颜色、线宽和像素语言设定 | 透明设定板 | `ArtSource/ChatGPT/08_VFX` |
| `Player_Hurt_Enhanced.png` | 玩家明显受击动画 | 5 帧，每帧 96×96 | `ArtSource/ChatGPT/01_Player` |
| `ClockGuard_Hurt_Enhanced.png` | 守卫受击动画 | 4 帧，每帧 96×96 | `ArtSource/ChatGPT/02_Enemies` |
| `ClockworkBug_Hurt_Enhanced.png` | 机械虫受击动画 | 4 帧，每帧 96×96 | `ArtSource/ChatGPT/02_Enemies` |
| `ClockArcher_Hurt_Enhanced.png` | 钟弩手受击动画 | 4 帧，每帧 96×96 | `ArtSource/ChatGPT/02_Enemies` |
| `Boss_Hurt_Enhanced.png` | Boss 强受击动画 | 5 帧，每帧 128×128 | `ArtSource/ChatGPT/03_Boss` |
| `Guard_AttackTelegraph.png` | 红色近战前摇和挥砍轨迹 | 6 帧，每帧 96×96 | `ArtSource/ChatGPT/08_VFX` |
| `Bug_ChargeTelegraph.png` | 黄色冲锋前摇和速度线 | 6 帧，每帧 96×96 | `ArtSource/ChatGPT/08_VFX` |
| `Archer_AimTelegraph.png` | 蓝色瞄准线、弩口蓄光 | 6 帧，每帧 96×96 | `ArtSource/ChatGPT/08_VFX` |
| `Boss_AttackTelegraphs.png` | 红、黄、蓝、紫四类 Boss 前摇 | 每类 8 帧，128×128 | `ArtSource/ChatGPT/08_VFX` |
| `HitSparks_Enhanced.png` | 玩家受击、敌人受击、重击三类火花 | 每类 6 帧，64×64 | `ArtSource/ChatGPT/08_VFX` |

清理完成后统一放入：

```text
Assets/_Project/Art/VFX/Combat
Assets/_Project/Art/Sprites/Characters/Player
Assets/_Project/Art/Sprites/Characters/Enemies
Assets/_Project/Art/Sprites/Characters/Bosses/EclipseWarden
```

## 提示词 A：战斗反馈统一设定板

```text
使用附件中的原创哥特钟表遗迹风格作为唯一美术参考，为2D横版像素动作游戏制作“战斗前摇与受击反馈”设定板。

必须包含：
- 红色近战攻击前摇：尖锐扇形、断裂钟针和危险边缘
- 黄色冲锋与落地重击前摇：向前聚集的齿轮线和高亮冲击区域
- 蓝色远程瞄准前摇：细长瞄准线、弩口蓄能和目标端小型钟环
- 紫色时间锚前摇：逆向旋转三角钟环
- 玩家受击：红白闪光、青蓝时间碎片和明确后仰方向
- 敌人受击：白色命中闪光、红色机械火花和黄铜碎屑
- Boss受击：更大的白红闪光、紫色核心裂纹和重型金属碎屑

所有反馈在深蓝和冷灰背景上必须高对比、轮廓清楚。颜色只承担固定语义：红色近战、黄色重击与冲锋、蓝色普通远程、紫色时间异常。

严格现代像素画，有限色板，硬边，无抗锯齿，无平滑渐变，无文字、Logo、水印或场景。完全透明Alpha背景，不得使用棋盘格模拟透明。
```

## 提示词 B：玩家强化受击动画

上传 `Player_Design_Master.png` 和 `CombatFeedback_Master.png` 后使用：

```text
使用附件中的无性别机械旅人作为唯一角色身份参考，制作横版像素动画条“玩家明显受击”。

共5帧，每帧96×96像素，面向右侧：
1. 命中瞬间身体明显反向折起，表盘出现纯白闪光
2. 强烈后仰，斗篷和四肢朝受力反方向甩动
3. 表盘出现短暂红色裂纹，飞出少量青蓝时间碎片和黄铜火花
4. 身体开始恢复重心，但仍保持清楚的受伤轮廓
5. 回到可衔接待机或下落的姿势

受击方向必须一眼可读，不能只改变颜色。脚底基线、角色比例、钟表头、斗篷和分针剑必须与原设定一致。角色和碎片不得被裁切。

严格像素画、硬边、无抗锯齿、无运动模糊。完全透明Alpha背景，无地面阴影、文字、Logo、水印或棋盘格。
```

## 提示词 C：三类普通敌人强化受击动画

上传 `Enemies_Design_Master.png` 和 `CombatFeedback_Master.png` 后使用；三种敌人分别生成，不要放在同一动画条中：

```text
使用附件敌人设定作为唯一身份参考，为“{发条近战守卫 / 高速机械虫 / 钟弩手}”制作横版像素动画条“明显受击”。

共4帧，每帧96×96像素，面向右侧：
1. 命中点出现高亮白色机械火花
2. 身体朝受力反方向明显偏移或压缩，轮廓变化必须清楚
3. 飞出少量红色火花、旧黄铜碎屑和一个小齿轮
4. 恢复到可衔接待机状态，但保留一帧红白余光

守卫表现重甲震动和肩甲错位；机械虫表现六足收缩和低矮甲壳弹起；钟弩手表现细长身体弯折、弩臂偏转。不得改变角色比例、武器或主要配色。

严格像素画、硬边、无抗锯齿、无运动模糊。完全透明Alpha背景，无文字、Logo、水印或棋盘格。
```

## 提示词 D：普通敌人攻击前摇特效

```text
使用原创哥特钟表遗迹风格制作三组横版像素攻击前摇特效，完全透明Alpha背景。

第一组：发条近战守卫，6帧，每帧96×96。红色危险能量从胸口沿机械刃聚集，最后形成清晰的前方扇形挥砍边缘；不能提前画出已经命中的姿势。

第二组：高速机械虫，6帧，每帧96×96。黄色核心由暗到亮，齿轮线和速度箭头向冲锋方向收束，最后一帧清楚显示即将突进的方向和宽度。

第三组：钟弩手，6帧，每帧96×96。蓝色弩口逐渐蓄光，出现细长瞄准线和目标端小钟环；瞄准线必须清晰但不能遮挡角色。

三组前摇即使转为灰度，也必须能通过形状区分：守卫是扇形、机械虫是箭头楔形、钟弩手是细长瞄准通道。有限色板、硬边、无抗锯齿、无平滑渐变、无文字和水印。
```

## 提示词 E：Boss 前摇与强化受击

```text
使用附件中的两阶段Boss“时蚀守卫”作为唯一身份参考，制作Boss战斗反馈像素动画素材。

包含：
- 红色横扫前摇8帧：钟摆刃边缘逐步点亮并形成巨大扇形危险弧
- 黄色跳跃落地前摇8帧：脚下齿轮环收缩，落点出现清晰黄色冲击范围
- 蓝色弹幕前摇8帧：肩部和钟环出现三个蓝色蓄能点
- 紫色时间锚前摇8帧：胸口紫色核心脉冲，逆向钟环逐步闭合
- 强化受击5帧：白红命中闪光、重甲错位、紫色核心裂纹和黄铜碎屑

每帧128×128像素。第一阶段和第二阶段身份必须一致，不能改变Boss轮廓。所有攻击颜色严格遵循既定战斗语义，前摇必须比普通待机亮且一眼可辨。

严格像素画、硬边、无抗锯齿、无运动模糊。完全透明Alpha背景，无场景、文字、Logo、水印或棋盘格。
```

## 导入设置

- Texture Type：`Sprite (2D and UI)`
- Sprite Mode：`Multiple`
- Pixels Per Unit：`32`
- Filter Mode：`Point`
- Compression：`None`
- Generate Mip Maps：关闭
- Wrap Mode：`Clamp`
- 受击与前摇动画的 Pivot 保持一致，建议为 `Bottom Center`
