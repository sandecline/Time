# V2 美术接入试样

2026-09-28：此目录存放试玩接入试样。已包含地形装饰图集、检查点钟、主角待机/跑动帧和背景裁切，不能作为全套正式动画验收结果。

- 场景：`Assets/_Project/Scenes/V2/EightRoomArtPreview.unity`。
- Unity 菜单：`Tools > Time Metroidvania > Open V2 Art Preview`，打开后点击 Play。
- 主角接入 4 帧分层待机、6 帧跑动、起跳/顶点/下落、4 帧冲刺及三向各 4 帧攻击；受伤、死亡、治疗、蓄时暂时共用待机帧。站姿武器改为身体后方的同一根直杆，左右露出端保持共线，切换到攻击时由手持层使用同一完整指针，背负层隐藏。战斗逻辑和碰撞可视化沿用八房间场景。
- 八房间接入 86 处静态石材地形、苔边及部分草/碎石/藤蔓，2 座钟形地标有了美术外观。交互检查点绑定未激活/激活 Sprite。冻结水面、战斗封门等动态机关保留原有显示控制。
- 八房间背景暂时共用参考横幅的不同裁切和色调；敌人、Boss 和其他机关仍待接入。
- 原始八房间场景保留。此独立场景不会自动同步之后的关卡编辑。
- 场景截图：`ArtSource/V2/Previews/R1_EnvironmentSupplement_v01.png`；运行截图：`ArtSource/V2/Previews/R1_RuntimeSupplement_v01.png`。尚未完成八房间手动试玩验收。
- 最新主角修正镜头：`ArtSource/V2/Previews/R1_RuntimeWeaponFix_v03.png`；左右待机近景为同目录 `Player_Idle_Weapon_Right_v03.png` / `Player_Idle_Weapon_Left_v03.png`。起跳、顶点、下落、四帧冲刺状态切换已通过运行验证。
- 跑步武器现与待机共用同一根完整长指针。六帧身体改用 `Player_BodyLayers_v05.png`，只调整握点和摆动角度；不再使用 v01 短武器或 v04 上排错位站姿。最新检查图为同目录 `Player_Idle_Aligned_v05.png`、`Player_Weapon_AxisCheck_v05.png` 和 `Player_Run_Weapon_Frame0_v05.png` 至 `Player_Run_Weapon_Frame5_v05.png`。
- 按后续用户意见，当前采用 v06 背负方式：跑步时武器随背部轻微摆动，双手自由。最终跑姿见同目录 `Player_Run_BackWeapon_Frame0_v06.png` 至 `Player_Run_BackWeapon_Frame5_v06.png`；前述 v05 手持图为过程记录。

源 PNG 按原样复制，通过 Unity 精灵矩形切片显示。地形接入前后比对全部 Collider 的序列化数据、位置和缩放，结果一致。后续仍需补齐场景模块、敌人和 Boss 动画，按真实攻击体积逐项验收。

2026-09-29：三向攻击使用 `Player_Attack_BodyLayers_v04.png`。上劈握点在胸前，下劈收腿，攻击方向锁定至本次攻击朝向。动作的前摇、两帧有效攻击及收招与实际有效窗口同步；两端收尖的刀光受现有判定边界约束。碰撞数据未变。运行截图 `ArtSource/V2/Previews/Player_Attack_Hitbox_0_v04.png` 至 `Player_Attack_Hitbox_5_v04.png` 中绿色框仅用于检查，不保存在游戏场景中。
