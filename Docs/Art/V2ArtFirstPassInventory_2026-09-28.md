# V2 美术首轮稿清单（2026-09-28）

这批图片主要是美术源稿和动作参考。部分地形、装饰、检查点和主角移动帧已切片接入独立试玩场景；全套资源尚未完成逐帧清线、统一像素网格与房间实机验收。房间概念图不能替代现有草图、平台碰撞与门的位置。

## 已保存的首轮稿

| 类别 | 文件 | 用途 |
| --- | --- | --- |
| 世界风格 | `ArtSource/V2/00_StyleReference/V2_WorldStyle_Master_Draft_v01.png` | 灰绿石墙、旧黄铜、苔藓、淡蓝时间能量的总基准 |
| 玩家定型 | `ArtSource/V2/01_Player/Player_Design_Master.png` | 蓝斗篷、黑色兜帽、两根不等长蓝时针与单根时针武器的定型依据 |
| 玩家动作 | `ArtSource/V2/01_Player/AnimationDrafts/Player_Idle_Run_FrameDraft_v01.png`、`Player_Air_Dash_FrameDraft_v01.png`、`Player_Attack_FrameDraft_v03.png`、`Player_Status_FrameDraft_v01.png` | 待机、跑动、空中、冲刺、横砍、上劈、下劈、受伤、死亡、治疗、蓄力的分帧草稿 |
| 敌人 | `ArtSource/V2/02_Enemies/Enemy_Guard_Design_v01.png`、`Enemy_Bug_Design_v01.png`、`Enemy_Flying_Design_v01.png`、`Enemy_Ranged_Design_v01.png` | 四种敌人造型定稿方向 |
| 敌人动作 | `ArtSource/V2/02_Enemies/Enemy_Guard_FrameDraft_v01.png`、`Enemy_Bug_FrameDraft_v01.png`、`Enemy_Flying_FrameDraft_v01.png`、`Enemy_Ranged_FrameDraft_v01.png` | 四种敌人待机、移动、出招及受伤/死亡参考 |
| Boss | `ArtSource/V2/03_Boss/Boss_TimeWarden_Design_v01.png`、`Boss_TimeWarden_Action_v02.png`、`Boss_Cast_Defeat_KeyPoses_v01.png` | 高耸时钟机体、两阶段状态、近战动作、施法与倒下姿势 |
| 弹体 | `ArtSource/V2/04_Projectiles/Boss_Projectile_Design_v02.png` | 蓝圆、橙菱形、紫三角的轮廓与配色基准 |
| 房间 | `ArtSource/V2/05_Environment/R1_Environment_Concept_v01.png` 至 `R8_Environment_Concept_v01.png` | 八个房间的场景气氛稿；以 `Docs/Design/Rooms/Sketches/` 的布局为准 |
| 地形模块 | `ArtSource/V2/05_Environment/Environment_Module_Reference_v01.png` | 石砖、草边、藤蔓、黄铜构件、远景的模块方向 |
| 地形与装饰补图 | `ArtSource/V2/05_Environment/Environment_Decor_Atlas_v01.png` | 新增 16 项透明素材，包含石块、横梁、立柱、草、藤蔓、根须、碎石、齿轮、布旗、残钟等；另保留最终提示词同名 `.prompt.md` |
| 背景分层 | `ArtSource/V2/05_Environment/Background_Far_Mid_Reference_v01.png` | 远景与中景的双层视差参考；尚需清理与拆层 |
| 交互物 | `ArtSource/V2/06_Props/Checkpoint_Clocks_v01.png`、`Doors_Switches_Obstacle_v01.png`、`Ability_ChronoCore_v01.png`、`Water_States_v01.png` | 检查点钟、门/开关/转轮、能力物、普通与冻结水域 |
| UI | `ArtSource/V2/07_UI/HUD_System_v01.png`、`Menus_And_Screens_v01.png`、`Map_UI_v01.png` | 血量与时间条、菜单/结算、地图的布局与样式 |
| 特效 | `ArtSource/V2/08_VFX/Player_Slash_VFX_v02.png`、`Player_TimeCombat_VFX_v01.png`、`Boss_Telegraphs_v01.png` | 玩家三向刀光、时间与打击反馈、Boss 弹幕预警参考 |
| 敌人与 Boss 攻击 | `ArtSource/V2/08_VFX/Enemy_Guard_AttackHitbox_v01.png`、`Enemy_ContactAttack_Reference_v02.png`、`EnemyRanged_BossProjectile_Hitbox_v02.png`、`Boss_Melee_AttackHitbox_v01.png` | 守卫近战、机械虫/飞行怪接触、射手与 Boss 弹体、Boss 横扫/砸地的碰撞约束稿 |
| 精确范围 | `Docs/Art/PlayerSlash_Hitbox_Guide_v01.svg`、`EnemyBossAttack_Hitbox_Guide_v01.svg` | 实际碰撞尺寸的攻击视觉边界；当概念图与范围图冲突时，以范围图和运行时 Collider 为准 |

## 攻击视觉约束

- 玩家：横砍位于面向方向的 `1.65 × 1.84` 世界单位框内；上劈、下劈分别位于角色上/下方 `0.95 × 1.10` 的框内。角色动作帧不烘入刀光。
- 守卫：伤害框是身体前方 `0.15–1.80`、垂直 `±0.60` 的区域；有效刀光不越过框。前摇预警框与伤害框共边界。
- 机械虫：没有独立挥砍框；伤害来自身体 `Collider2D` 接触。冲锋速度 `7.5`，持续 `0.32` 秒，理论最大位移 `2.40` 世界单位；预警显示身体扫过的路径。
- 飞行敌人：伤害来自身体接触，不把翅膀扇动或大范围光效画成有效攻击。创建时身体碰撞框基础尺寸为 `1 × 1`，物体缩放 `0.9`，实际世界尺寸约 `0.9 × 0.9`。
- 射手：蓝色弹体碰撞圆半径 `0.20`；预警沿锁定目标与真实发射点绘制，弹体实体图案与碰撞圆相称。
- Boss：横扫为前方 `3.50 × 2.50`；砸地为落点中心 `4.70 × 3.20`。有效亮线包含线宽后仍在框内。
- Boss 弹体：蓝圆、橙菱形、紫三角的碰撞圆半径分别为 `0.10 / 0.13 / 0.18`。当前展示图案直径分别为 `0.32 / 0.40 / 0.56`，因此较大的外圈只能作为非伤害光晕；后续切图应让实心核心落在碰撞圆内。

## 暂待处理

1. 零散前景装饰补图已完成透明图集和 Unity 精灵切片，已接入草、碎石和藤蔓；其余装饰需按房间特点继续布置。
2. 已有的敌人与 Boss 攻击约束图仍需拆出真正的独立透明特效 Sprite，并逐像素核对发光边缘与 Collider。生成图中的虚线框仅用于制作参考，不能进入游戏画面。
3. 所有生成图做一致性清理：统一角色比例、锚点、帧间体积、透明像素、像素密度和多余柔光；再拆分为可导入的独立 Sprite。特别是 `Boss_Cast_Defeat_KeyPoses_v01.png` 中施法弹形仅作姿势参考，游戏内仍使用蓝圆/橙菱形/紫三角。
4. 在八个房间按实际摄像机尺寸与碰撞区试玩，复核玩家及敌人攻击前摇、命中帧和特效外缘。

旧稿 `Player_Attack_KeyPoses_v01.png`、`Boss_TimeWarden_Action_v01.png` 与 `Boss_Projectile_Design_v01.png` 保留用于追溯，当前不作为制作基准。

## 独立试玩场景接入（本轮补充）

- 场景：`Assets/_Project/Scenes/V2/EightRoomArtPreview.unity`，通过 Unity 菜单 `Tools > 时序旅人 > Open V2 Art Preview` 打开。
- 八房间共 86 处静态地形接入石材、可落脚上沿接入苔边，部分地形添加草、碎石和藤蔓。冻结水面和战斗封门等动态机关排除在常驻石材之外。所有 Collider 的序列化数据、位置、缩放在接入前后比对一致；石材边界与 Collider 边界误差小于 0.01 世界单位。
- 2 座钟形地标替换为钟面美术，带交互组件的检查点已绑定未激活/激活 Sprite。
- 主角已绑定 4 帧分层待机、6 帧跑动，以及起跳、腾空顶点、下落和 4 帧冲刺。攻击、受伤等状态仍用待机帧占位，不能视为全动作完成。有效刀光继续由既有碰撞约束可视化绘制。
- 八房间背景暂时共用参考横幅的不同裁切和色调，尚未按各房间概念图完成正式布景。
- 新素材放在 `Assets/_Project/Art/V2/Preview/`，保留试样身份；源 PNG 未修改像素，使用 Unity 切片元数据定位内容。
- 当前场景图：`ArtSource/V2/Previews/R1_EnvironmentSupplement_v01.png`。
- 运行镜头：`ArtSource/V2/Previews/R1_RuntimeSupplement_v01.png`。运行模式验证素材引用、待机动画换帧和启动异常，记录为 PASS / errors=0；之后清理动态机关上的 6 个装饰子物体，并通过地形边界与碰撞不变校验。

## 待机武器修正与空中动作（后续补充）

- 用户指出站姿身体两侧露出的武器不共线。单图修正 v02 仍有残余偏差，保留为过程稿；实际使用 `ArtSource/V2/01_Player/AnimationDrafts/Player_Idle_Layered_v03.png`。
- v03 含四帧无武器站姿和一根完整直杆武器，游戏将直杆放在身体后方，两侧可见部分共用一个刚性变换。左右朝向镜像、受击颜色和身体缩放同步；非分层动作自动隐藏该武器。下排生成跑动没有替换已接入的 v01 跑动。
- 新补图：`ArtSource/V2/01_Player/AnimationDrafts/Player_Air_Dash_Clean_v02.png`，8 个透明姿势；起跳、顶点、下落和 4 帧冲刺已接入。落地帧暂为草稿，未接入状态机。
- 两张有效补图均由内置 imagegen 生成，同目录同名 `.prompt.md` 保存最终提示词。旧攻击稿透明清理请求长时间未返回，已终止，本轮未产出或替换三向攻击 Sprite。
- 冲刺动画按 `PlayerDash.DashProgress` 播放完整，冲刺速度、时长和攻击参数未改变。
- 近景：`ArtSource/V2/Previews/Player_Idle_Weapon_Right_v03.png` 与 `Player_Idle_Weapon_Left_v03.png`；场景镜头：`R1_RuntimeWeaponFix_v03.png`。
- `Temp/CodexIdleWeaponFix.log` 和 `Temp/CodexAirFrames.log` 记录接入前后碰撞数据不变；`Temp/CodexPlayerMotion.log` 为 PASS / errors=0，实际观察到 JumpRise、JumpApex、Fall、Dash0–Dash3 切换，且空中/冲刺时独立待机武器关闭。

## 本轮验证

### 跑步武器与展示图一致性修正

- 用户反馈跑步武器过小，并指出最新图集中站姿圆柄与指针仍未共线。该站姿来自 v04 图集保留的旧上排；v04 已标为过程稿，当前展示与接入依据改为无武器身体图层 `Player_BodyLayers_v05.png` 和游戏合成截图。
- 六帧跑步接入 v05 下排，沿用原切片画布和脚底锚点。武器与待机使用同一 Sprite、同一缩放；每帧只调整握点和摆动角度，因此长度、粗细与圆柄轴线保持一致。
- 内置 imagegen 的修改提示词保存于 `ArtSource/V2/01_Player/AnimationDrafts/Player_BodyLayers_v05.prompt.md`。
- 游戏相机检查图：`ArtSource/V2/Previews/Player_Idle_Aligned_v05.png`；`Player_Weapon_AxisCheck_v05.png` 仅在检查时淡化身体，显示完整武器连续轴线；正常游戏角色仍不透明。
- 跑步近景：同目录 `Player_Run_Weapon_Frame0_v05.png` 至 `Player_Run_Weapon_Frame5_v05.png`，以及 `Player_Run_Weapon_Left_v05.png`。
- 接入日志 `Temp/CodexRunWeaponFix.log` 确认全部碰撞数据不变。
- 用户随后说明平时可以背在背上，最终采用 v06 背负方案：待机和跑步共用完整武器，跑步只随背部轻微摆动，双手自然摆臂。跑步圆柄在部分姿势下被斗篷遮挡，完整武器没有截短。
- 当前跑步展示以 `Player_Run_BackWeapon_Frame0_v06.png` 至 `Player_Run_BackWeapon_Frame5_v06.png` 和 `Player_Run_BackWeapon_Left_v06.png` 为准，均在 `ArtSource/V2/Previews/`；v05 手持截图保留作过程记录。接入日志 `Temp/CodexBackWeaponFix.log` 确认碰撞数据不变。

- 两张 SVG 范围图通过 XML 解析检查。
- 本轮攻击视觉代码通过 `git diff --check`。
- 使用项目指定的 Unity `6000.3.24f1` 完成批处理编译，编辑器日志显示 `Exiting batchmode successfully now!`，未发现 C# 编译错误。
- 补图接入验证日志：`Temp/CodexArtSupplement.log`；运行验证：`Temp/CodexArtRuntime.log`；最终地形对齐检查：`Temp/CodexArtAlignment.log`。完整跑跳、战斗和八房间流程仍待手动试玩。

## 2026-09-29 三向攻击补充

- 新绘 `ArtSource/V2/01_Player/AnimationDrafts/Player_Attack_BodyLayers_v04.png`，12 个透明身体帧，4 列 × 3 行分别对应横砍、上劈、下劈。同名 `.prompt.md` 保存生成与胸前握点修正提示。
- 已接入 `EightRoomArtPreview.unity`。三向分别选择对应动作，攻击锁定朝向，按实际有效窗口分配四帧时间。上劈采用胸前持握，下劈收腿。
- 同一完整指针在攻击期间挂到身体帧握点，固定世界长度 1.16；父级比例补偿避免旋转时轴线与长度失真。背负层自动隐藏。
- 现有程序刀光增加两端收尖与曲线采样，仍限制在实际碰撞体积内；攻击时长、伤害、范围、位移和所有碰撞数据未改。
- `Temp/CodexAttackArt.log` 记录接入完成和碰撞数据不变。`Temp/CodexAttackRuntime.log` 检查三方向双朝向共六次攻击，四帧播放、有效帧时序、单武器、固定长度、刀刃和刀光边界。
- 预览图 `ArtSource/V2/Previews/Player_Attack_Hitbox_0_v04.png` 至 `Player_Attack_Hitbox_5_v04.png`，顺序为右横/右上/右下/左横/左上/左下，绿色框仅检查时绘制。
- 补记上次背负跑步运行结果：`Temp/CodexBackWeaponRuntime.log` 为 PASS，左右各 6 帧、武器缩放恒定、86 处地形、errors=0。
- 后续仍需补受伤/死亡/治疗/蓄时、普通敌人和 Boss 动画，以及其他机关与房间背景；当前不视为全套美术验收完成。
- 最终运行验证为 `V2_ATTACK_RUNTIME_PASS: swings=6; activeFrames=96`，六组各四帧、武器固定长度及带宽度的刀刃/刀光边界检查通过；`git diff --check` 通过。
- 本次另有主角状态与守卫动作两次出图请求长时间未返回，已终止，未生成或接入对应 PNG。原提示与待补说明见 `Docs/Art/PendingImageRequests_2026-09-29.md`，不推断为额度问题。

## 2026-09-29 角色、房间、机关补图与接入

- 主角受伤、死亡、治疗和蓄时补图为 `Player_Status_BodyLayers_v02.png`（各 2 帧）；四类普通敌人和 Boss 的动作补图分别为 `Enemy_Guard_Animation_v03.png`、`Enemy_Bug_Animation_v02.png`、`Enemy_Flying_Animation_v02.png`、`Enemy_Ranged_Animation_v02.png`、`Boss_TimeWarden_BodyActions_v03.png`、`Boss_TimeWarden_Status_v03.png`。已接入独立试玩场景的主角、15 个地面敌人、7 个飞行敌人和 1 个 Boss。
- `Environment_R1_R4_Backdrops_v02.png`、`Environment_R5_R8_Backdrops_v02.png` 提供八个不同的房间远景，已替换重复参考背景。远景不改变平台、门、通路或碰撞。
- 新绘 `Projectile_RuntimeAtlas_v03.png` 包含三类 Boss 弹体和射手短箭，已绑定 Boss 弹幕池与射手弹幕池。蓝圆、橙菱、紫三角按透明像素外缘缩放到各自 0.10／0.13／0.18 世界单位碰撞圆内；射手短箭落在半径 0.20 的碰撞圆内。
- 新绘 `Mechanisms_RuntimeAtlas_v02.png`，捷径门和拉杆已有开关两态并接入 R4；转闸中心齿轮接入 R4，旋转致伤刀臂沿用现有显示与碰撞。机关碰撞、触发逻辑与位置未改变。
- 新绘 `Water_RuntimeAtlas_v02.png`，正常与冻结水体接入 R3；正常波纹与冻结冰面分开，冰面只在时停时显示。已有 `Ability_ChronoCore_v01.png` 第一格接入 R5 时停拾取物，取得后沿用现有消失逻辑。
- 新绘 `HUD_RuntimeAtlas_v02.png`，包含满血/空血钟面、时停能力图标、时间槽框与填充条、Boss 血槽框；已在独立预览场景绑定主角生命/时间 UI 与 Boss 血条。关卡信息面板也换成独立绘制的黄铜石板面板，文字仍由运行时动态生成。
- 新绘 `Menus_RuntimeAtlas_v02.png`、`Map_Frame_v02.png`、`Map_Markers_v02.png`，分别提供标题/暂停/死亡/胜利面板、空白地图框和六种地图标记；均为独立透明组件。地图与菜单运行组件当前不在该八房间预览场景中，先完成切片导入，待界面流程启用后绑定。
- 以上源图、同名提示词和预览图均在 `ArtSource/V2/`；切片放在 `Assets/_Project/Art/V2/Preview/`。机关门预览见 `ArtSource/V2/Previews/R4_ShortcutGate_v02.png`；八房间预览见 `R1_RoomBackdrop_v02.png` 等。每次接入均先后比对 Collider 序列化、位置和缩放，未改动原始 `EightRoomGreybox.unity`。

### 还缺的可玩美术与验收

1. HUD 已绑定八房间独立预览场景；地图、标题/暂停/死亡/结算已有可切片图，仍需连接对应运行界面并做分辨率验收。
2. Boss 技能预警、玩家时间技能、命中/碎裂等 `08_VFX` 仍以程序效果或参考图为主，需逐项清线并核对伤害边界；不能把参考图中的虚线框当作游戏效果。
3. 八房间中景、前景和地形块仍重复较多，尤其水池岸线、机关支架和 Boss 场地需要按房间继续补模块。
4. 新接入角色动作与机关需逐房间手动试玩，检查贴地、遮挡、色彩可读性以及全部攻击前摇和实际命中帧；现有批处理验证不能替代玩家试玩反馈。

## 2026-09-29 后续可玩场景替换

- `Menus_RuntimeAtlas_v02.png`、`Map_Frame_v02.png`、`Map_Markers_v02.png` 已生成并作为独立精灵导入预览资源目录。当前八房间试玩场景没有标题/暂停/地图流程组件，因此这些组件尚未显示在运行界面；避免把整张概念 UI 当作静态图片叠到屏幕。
- `HUD_RuntimeAtlas_v02.png` 的钟面、时间槽和 Boss 条已绑定实际运行 HUD；玩家状态、关卡目标仍为实时文本。独立美术预览场景的启动运行检查为 `V2_ART_RUNTIME_PASS`，`errors=0`，86 处石材显示正常。
- `TimeCombat_RuntimeAtlas_v02.png` 已生成。青/橙命中火花、时间钟环、玩家蓄时与治疗亮点接入预览场景，均无独立伤害判定；碎晶和紫裂纹已切片保存，待对应事件确定时机再绑。
- `RotorBossSeal_Runtime_v01.png` 已生成并接入 R4 旋转刀臂、R6↔R8 两侧 Boss 封门。显示宽度分别为 0.44 和 0.55 世界单位，小于原有 0.50 和 0.60 碰撞宽度；原旋转与开关逻辑不变。
- 项目启动场景与构建场景现指向 `Assets/_Project/Scenes/V2/EightRoomArtPreview.unity`；原 `EightRoomGreybox.unity` 保留，仍可从 Unity 菜单单独打开。美术切片目录仍为预览身份，待逐房间试玩验收。

### 当前仍缺

1. **运行界面绑定**：标题、暂停、地图、死亡和结算的可切片图已齐，但独立八房间预览场景尚未接入相应流程界面。HUD 已接入。
2. **场景细化**：八房间已有各自远景，地形是统一石材模块；各房间的中景、前景、岸线、机关支架、Boss 场地独立细节仍需补画和摆放。
3. **特效细化**：碎晶、紫裂纹尚未绑定；Boss 密集弹幕的预警仍使用动态几何参考，待实机确认清晰度后再决定是否需要独立 Sprite。现有程序刀光与伤害框已对齐，不应换成超出 Collider 的大面积概念刀光。
4. **逐房间验收**：检查 8 个房间的角色贴地、前后景遮挡、机关两态和敌方攻击可读性，并根据玩家试玩反馈微调。

## 2026-09-29 房间地形替换

- 新增两张透明地形图集：`Terrain_R1_R2_R6_R7_Atlas_v01.png` 与 `Terrain_R3_R5_R8_Atlas_v01.png`，共 7 套石材和 7 条对应平台边缘；R4、R5 共用黄铜加固砌块。源图和同名 `.prompt.md` 位于 `ArtSource/V2/05_Environment/`。
- 已将 86 处静态地形表面、其中 56 处可落脚边缘替换为房间对应图案，并对冷色房间的旧草、碎石和藤蔓做色彩协调。替换范围仅为 `EightRoomArtPreview.unity` 的 SpriteRenderer；原灰盒场景保留供对照。试玩场景仍是项目启动和构建场景。
- 另绘 `BossArenaRuneTrim_v01.png` 替换 R8 场地的纯红灰盒线，黄铜沙漏纹沿原 18 世界单位水平范围重复。该线没有 Collider；五个代表房间的画面保存为 `ArtSource/V2/Previews/R1_RoomTerrain_v01.png` 等。
- Unity 构建器 `V2RoomTerrainArtBuilder.Apply` 验证所有地形显示边界与原 Collider 相差小于 0.01 世界单位，并比对全部 Collider 序列化、位置和缩放一致。
- 构建日志 `Temp/CodexRoomTerrainBuild.log` 记录 `surfaces=86; edges=56; themes=7; arenaLine=1; COLLIDERS_UNCHANGED`。运行日志 `Temp/CodexRoomTerrainRuntime.log` 为 `V2_ART_RUNTIME_PASS`，86 处地形引用有效、动画换帧正常、`errors=0`；`git diff --check` 通过。
- 这轮完成可玩地形的区域化替换。仍待精修水池岸线、机关支架和 Boss 场地的独立前景模块；标题/地图等界面流程绑定和逐房间手动试玩也仍需继续。

## 2026-09-29 上下劈与 Boss 身体碰撞微调

- 上劈、下劈判定由 `0.95 × 1.10` 调为 `1.15 × 1.35` 世界单位，中心距角色由 `±0.75` 调为 `±0.85`。同步修改玩家预制体、八房间美术试玩场景及原灰盒场景的序列化覆盖值；攻击时长、伤害和横砍范围不变。
- 刀光继续读取实际 `PlayerCombat.AttackSize` 与中心，带宽度的亮线留在碰撞框内。双朝向三向攻击运行检查 `V2_ATTACK_RUNTIME_PASS`，共六次完整攻击、96 个有效帧；新上下劈尺寸、四帧动作、指针和刀光边界均通过。检查截图为 `ArtSource/V2/Previews/Player_Attack_Hitbox_1_v04.png`、`Player_Attack_Hitbox_2_v04.png` 等。
- Boss 美术试玩场景移除灰盒根节点的 45° 旋转，让身体碰撞由菱形恢复竖直；位置下移 `0.25` 世界单位使脚部贴近场地。以两帧待机可见边界的并集重算 `BoxCollider2D`，结果与贴图中心、宽、高均一致；Boss 横扫与砸地的独立伤害框参数未改。核对图为 `ArtSource/V2/Previews/Boss_BodyColliderFit_v01.png`，绿色框仅出现在检查图中。
