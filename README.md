# TimeMetroidvaniaDemo — Demo1 历史快照

这是一个以局部时间操控为核心的 2D 横版类银河战士恶魔城测试 Demo，目标平台为 Windows PC。`demo1` 分支用于永久保留当前七房间灰盒、战斗系统、两阶段 Boss 和第一版“原创哥特钟表遗迹”美术，不代表后续地图与美术的最终效果。

## 开发环境

- Unity 6.3 LTS（6000.3.24f1）
- Universal Render Pipeline / Universal 2D
- Unity Input System 1.20.0
- 目标平台：Windows PC

请使用 Unity Hub 和 Unity 6000.3.24f1 打开项目根目录，不要直接使用其他 Unity 版本升级工程。

## 当前版本内容

- 玩家移动、跳跃、地面／空中平砍、受击、击退、死亡重生和无无敌帧 Dash。
- 平 A 间隔为 0.72 秒；攻击动作期间可以移动，攻击有效段结束后可以衔接 Dash。
- 普通攻击具有独立刀光和命中范围，可斩毁蓝色弹幕；橙色弹幕与紫色时间锚不可斩毁。
- 攻击和斩弹补充时间槽；长按输入可以治疗。
- 免费时缓、消耗型时停、提前结束时停并按剩余时间返还资源。
- 发条近战守卫、高速机械虫、钟弩手，以及两阶段 Boss“时蚀守卫”。
- 房间切换会刷新相关房间敌人；检查点和死亡流程会重置对应区域的临时战斗状态。
- Boss 战开始后使用实体机关墙封锁出入口，Boss 击败或战斗重置后解除。
- 标题、暂停、地图、统一 HUD、死亡遮罩和结尾流程。

世界时间由对象局部倍率驱动，不修改全局 `Time.timeScale`。当前测试 Demo 不读取或写入磁盘存档，“继续游戏”保持禁用，运行进度在退出或重新加载场景后清空。

## 操作

- `A` / `D`：水平移动
- `S`：向下／穿过单向平台
- `K`：跳跃
- `J`：按下立即平砍；持续按住 1 秒后松开，触发持续 3 秒、世界倍率 0.35 的免费时缓
- `L`：Dash，仅提供位移，不提供无敌帧
- 短按 `I`：消耗 60 点时间槽，触发 5 秒时停；时停中再次按下可提前结束并按剩余比例返还
- 长按 `I`：0.3 秒后进入治疗准备，再持续 1.5 秒完成治疗
- `W`：与检查点椅子、储能装置等场景物体交互
- `M`：打开／关闭本次运行地图
- `Esc`：暂停／继续

## 场景

Build Settings 中包含四个测试场景：

- `MovementPrototype`：移动与跳跃基线。
- `CombatPrototype`：玩家战斗、资源、局部时间和弹幕对象池验证。
- `EnemyEncounter`：三类普通敌人的两段混合遭遇。
- `SevenRoomSlice`：当前主测试场景，包含七房间成长路线、检查点、能力解锁、Boss 和完整 Demo 流程。

当前七房间布局只用于玩法验证，并非最终地图。房间入口使用开放缺口，玩家走过边界后自动切换房间。

## 测试与灰盒重建

通过 Unity Test Runner 运行 EditMode 测试，覆盖玩家状态与资源、时缓与时停、延迟伤害、敌人状态转换、局部时间、弹药上限和弹幕对象复用。

可通过 Unity 菜单重建测试内容：

- `Tools > Time Metroidvania > Build Current Prototype`
- `Tools > Time Metroidvania > Build Phase 4 Encounter`
- `Tools > Time Metroidvania > Build Phase 5 Seven Rooms`
- `Tools > Time Metroidvania > Build Phase 6 Boss`
- `Tools > Time Metroidvania > Build Phase 7 Demo Flow`

## 目录

- `Assets/_Project`：游戏脚本、场景、预制体、动画和 Unity 正式素材。
- `ArtSource/ChatGPT`：第一版美术的原始生成图片。
- `Docs/Design`：阶段计划、系统设计和决策记录。
- `Docs/Art`：第一版美术规范、提示词和导入说明。

## 已知限制与后续计划

- 当前仍是测试用七房间灰盒，没有持久化存档。
- 当前“原创哥特钟表遗迹”素材已经接入，但整体美术表现尚未达到最终目标，后续将整套替换。
- 下一版地图将以新的八房间总览和后续逐房间地形草图为准；`demo1` 不提前实现或宣称这些内容。
- 关卡尺寸、敌人配置、机关节奏、攻击反馈和性能仍需在最终地图中继续调优。

`demo1` 是后续地图与美术重制前的可回退历史快照。
