# 时序旅人｜2D 时间系银河恶魔城 Demo

《时序旅人》是开发中的 Unity 6 横版动作探索 Demo，核心玩法是方向攻击、局部时缓与时停。当前版本提供八房间探索流程、四类普通敌人和两阶段 Boss；美术已接入可玩场景，仍需根据实机试玩继续调整。

## 开发环境与试玩

- Unity 6.3 LTS（6000.3.24f1）、URP 2D、Input System；目标平台为 Windows x64。
- 使用对应版本的 Unity Hub 打开项目。主场景为 `Assets/_Project/Scenes/V2/EightRoomArtPreview.unity`，已列入 Build Settings。
- 在 Unity 中选择 **Tools → 时序旅人 → 构建 Windows Demo**，输出位于 `Builds/Windows/时序旅人.exe`。分享时需保留整个 `Builds/Windows` 文件夹的相对结构；详见 [Windows 打包说明](Docs/BuildWindowsDemo.md)。
- `Builds`、`Library`、`Temp` 等生成目录不纳入 Git；克隆项目后需在 Unity 中自行构建。

## 当前可玩内容

- 移动、跳跃、Dash、横砍、上砍、空中下劈反弹、受伤与复活。三方向攻击分别使用对应刀光和攻击判定范围。
- 时间槽、免费时缓、持续消耗时间槽的时停、治疗、时间回声，以及可斩毁与不可斩毁的弹幕。
- 八房间探索、跨房间镜头、时停水池、时缓机关、能力拾取、捷径门、检查点和 Boss 战场封锁。
- 守卫、机械虫、飞行敌人、远程敌人，以及两阶段 Boss。角色、敌人、场景和界面美术已接入当前可玩场景。

世界时间通过对象的局部倍率处理，不依赖全局 `Time.timeScale` 实现时缓或时停。检查点与 Boss 进度仅在本次运行中有效；退出或重新加载场景后不会恢复磁盘存档。

## 操作

| 按键 | 操作 |
| --- | --- |
| `A` / `D` | 水平移动 |
| `K` | 跳跃 |
| `J` | 攻击；按住 `W` 上砍，空中按住 `S` 下劈；按住 `J` 约 1 秒后松开触发时缓 |
| `L` | Dash（无无敌帧） |
| `I` | 开启或结束时停 |
| 长按 `U` | 治疗 |
| `W` | 向上；靠近检查点或机关时交互 |
| `S` | 向下；穿过单向平台 |
| `M` | 打开或关闭地图 |
| `Esc` | 暂停或继续 |

## 工程结构

- `Assets/_Project/Scenes/V2/EightRoomArtPreview.unity`：当前可玩场景。
- `Assets/_Project/Scenes/V2/EightRoomGreybox.unity`：八房间灰盒参考场景。
- `Assets/_Project/Scenes/V2/V2RuntimeTemplate.unity`：供编辑器构建工具复用的模板，不参与打包。
- `Assets/_Project/Scripts`：玩家、时间、敌人、Boss、关卡和界面运行时代码。
- `Assets/_Project/Art`：接入 Unity 的美术资源；`ArtSource/V2` 保存对应源图。
- `Assets/_Project/Editor`：场景、美术、验证和 Windows 构建工具。
- [文档索引](Docs/README.md)：制作计划、设计、美术资料和历史记录。

V1／Demo1 保留在 `demo1` 分支。当前版本仍是开发中的可玩 Demo，战斗手感、关卡节奏与性能需继续通过完整实机试玩验证。