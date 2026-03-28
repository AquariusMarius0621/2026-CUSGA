# Tower Defense AI Memory - Architecture
Version: 0.1.1
Updated: 2026-03-28
Depends on: `docs/ai-memory/td-memory-main.md`

## ch5 架构模式
当前项目采用“原型期集中总控 + 小组件分责”的轻量结构：
- `TowerDefenseGame` 负责全局规则收口，不做过早拆系统。
- 已完成第一轮轻量拆分：`TowerCatalog` 收口塔静态数据，`TowerDefenseHudPresenter` 收口 HUD 查找与刷新。
- 小组件保持单一职责：`TowerShopCard` 只管拖拽输入，`BuildZone` 只管大范围许可，`PlacementBlocker` 只管禁建标记。
- 场景对象仍以“名称约定 + `SceneObjectFinder` 查找”为主，优先保证原型搭建速度。

设计意图：
- 先把最小可玩闭环做通，再基于真实玩法压力点拆系统。
- 第一轮优先拆出“稳定且低风险”的两块：塔静态定义与 HUD 表现层，先减耦再继续拆更深的 Placement 状态机。
- 如果未来继续扩张，优先拆出 `Build/Placement`, `HUD`, `Economy`, `Combat` 四块，而不是一开始就引入复杂框架。

## ch6 场景装配层
`SampleScene` 当前应包含这些关键根对象：
- `Main Camera`
- `GameController`（挂 `TowerDefenseGame`）
- `WaveSpawner`
- `EnemyPath`
- `PathVisuals`
- `RuntimePrototypes`
- `PlacedTowers`
- `PlacementPreviewRoot`
- `EnemiesRoot`
- `HUDCanvas`
- `EventSystem`
- `BuildZone`
- `BuildPads`（保留但禁用，仅做兼容参考）

路径阻挡装配要求：
- `PathSegment_A-E`、`SpawnMarker`、`BaseCore`：应有 `BoxCollider2D + PlacementBlocker`
- 这些碰撞体用于禁建判定，不用于真实物理阻挡，默认应保持 `isTrigger = true`

建造区装配要求：
- `BuildZone`：应有 `BoxCollider2D + BuildZone`
- 当前建造区使用大范围盒体，位置约 `(0, 0.25, 0)`，缩放约 `(18, 10.5, 1)`

## ch7 UI 系统
HUD 当前仍沿用旧对象名，以便总控继续通过名字找引用：
- 顶部：`TopBar`, `EnergyCard`, `BaseCard`, `WaveCard`, `SelectionCard`, `StatusStrip`, `EnergyText`, `BaseHealthText`, `WaveText`, `SelectionText`, `StatusText`
- 右侧部署面板：`BottomBar`, `RelayTowerButton`, `DefenseTowerButton`, `ClearSelectionButton`, `DeployHeaderText`, `RelayIconBadge`, `DefenseIconBadge`
- 关卡角标：`StageCodeText`, `SpawnTagText`, `BaseTagText`, `MapCornerTL`, `MapCornerBR`, `MapEdgeTop`, `MapEdgeBottom`
- 拖拽提示：`DragPreviewPanel`, `DragPreviewLabel`
- 结束界面：`GameOverPanel`, `GameOverTitle`, `GameOverHint`

UI 风格方向：
- 参考“明日方舟式”深色战术终端
- 深色半透明底板 + 高对比白字 + 橙/青强调色
- 卡片左侧使用独立图标徽记区，拖拽时配合地面投影圈形成双层反馈
- 地图使用章节化边框、角标和出生/基地标签强化关卡语义
- 右侧垂直部署卡，而不是底部传统塔位按钮条

重要约束：
- 不随意改掉上述对象名，除非同步修改 `TowerDefenseGame` / `TowerDefenseHudPresenter` 的查找字段
- HUD 现在由 `TowerDefenseGame` 产出状态快照，再交给 `TowerDefenseHudPresenter` 做文本、按钮和拖拽提示刷新
- 如果新增 HUD 节点，尽量保持命名直观，并写入主文档或本文件

## ch8 自由放置链路
当前部署链路：
1. `TowerShopCard.OnBeginDrag` 请求 `TowerDefenseGame.BeginPlacementDrag`
2. 总控生成预览塔并显示 `DragPreviewPanel`
3. 拖拽中持续调用 `UpdatePlacementDrag`
4. 总控按 `BuildZone -> PlacementBlocker -> 已有塔碰撞` 三层规则判定是否合法
5. 松手时 `EndPlacementDrag` 根据落点决定取消、失败提示或正式建塔

兼容链路：
- 旧 `BuildPad` 仍能通过 `TryPlaceTower(BuildPad)` 走进统一建造入口
- 当前关卡里 `BuildPads` 已整体禁用，避免与自由放置 UI 混淆

实现注意：
- 预览塔只做视觉反馈，必须禁用真实战斗行为脚本
- 预览对象现在还包含 `PlacementRing` 子对象，用于显示塔的实际落点范围
- `TowerShopCard` 现在带有悬停呼吸动效，用于增强部署卡的交互节奏感
- `TowerCatalog` 统一提供塔的 `DisplayName / BuildCost / PlacementRadius / CardRoleSummary / AccentColor`
- 任何改动自由放置判定时，都要同时更新主文档的“核心规则”章节




