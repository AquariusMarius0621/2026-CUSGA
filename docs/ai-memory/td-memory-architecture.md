# Tower Defense AI Memory - Architecture
Version: 1.5.2
Updated: 2026-05-02
Depends on: `docs/ai-memory/td-memory-main.md`

## Architecture Goals
- 让核心塔防玩法可以稳定运行，并便于继续扩展。
- 让用户能长期直接在 Unity 的 Scene 和 Inspector 中维护关卡与美术。
- 让运行时逻辑尽量摆脱对象名查找、隐式 fallback 和占位美术耦合。
- 控制复杂度，避免重新长出“上帝脚本”。

## Core Principles
### 1. Scene Authored First
- 地图骨架、路径、出怪口、防御点、UI 骨架、Prefab 引用，优先通过场景显式接线完成。
- 脚本优先消费显式引用，而不是自己在运行时到处找对象。
- 建造区形状允许由多个 `Collider2D` 在 Scene 中直接组合，而不是强制单矩形。

### 2. Shared Runtime Bridges + Authored Assets
- 共用运行时逻辑放在脚本里。
- 静态参数、共享目录、默认展示配置优先放在 `ScriptableObject` 资产里。
- 运行时真正生成的实体外观优先放到 Prefab 资产里。

### 3. Composition Over Copy
- 三类战斗塔共享 `DefenseTower` 主逻辑，但有各自独立运行时 prefab。
- 敌人共享 `Enemy` 基础壳，但特殊机制开始拆成独立模块，而不是每种怪复制一份主脚本。

### 4. Keep The Bootstrap Thin
- `TowerDefenseGame` 负责装配、状态门面和系统串联。
- 具体规则、表现、输入、供电、放置和敌人能力，继续下沉到独立组件。

## Runtime Module Boundaries
### Core
- `TowerDefenseGame`
  运行时总装配层。
- `TowerDefenseSessionState`
  局内资源、生命、波次和结算状态。
- `TowerDefenseSceneBootstrapper`
  消费场景显式引用，组装总控需要的依赖。
- `TowerDefenseInputCoordinator`
  统一输入轮询与屏幕坐标相关逻辑。
- `TowerDefensePresentationCoordinator`
  管理 HUD 刷新、结算表现和部分统一提示。
- `CampaignFlowAsset`
  剧情段与塔防段的顺序定义。
- `CampaignFlowController`
  剧情-塔防交错链的跨场景推进。
- `StorySceneStepController`
  2D 剧情场景的最小桥接器：负责等待必要对话完成，再统一推进到下一段流程。

### Map
- `BattlefieldMapDefinition`
  地图显式入口，收口 `BuildZone`、出怪口、防御点等引用。
- `BuildZone`
  地图可建造区域定义，当前已支持多碰撞体组合的不规则地形工作流。
- `Level04RingGuide`
  `Level04` 专用作者语义组件：把外环 / 中环 / 内环的标签、颜色与锚点显式挂在场景里，帮助后续继续手改地图。
- `EnemyPath`
  路径数据与路径可读性表现。
- `EnemySpawnGate`
  出怪口场景对象。
- `DefensePointFlag`
  防御点场景对象。
- `WaveSpawner`
  波次推进、刷怪与路线预告。
  当前结构上已经明确：地图骨架在 Scene，波次节奏在资产，不再继续混用旧数组兼容主链。

### Placement
- `TowerPlacementRules`
  放置合法性规则。
- `TowerPlacementInteractionController`
  玩家从卡片进入放置流程的交互状态机。
- `TowerPlacementBuildExecutor`
  最终实例化并落地结构。
- `TowerPlacementSupportCoordinator`
  放置链的辅助上下文、规则代理和覆盖层控制。
- `PlacedTower`
  正式落地塔实例的归属桥接组件。

### Towers
- `RelayTower`
  继电器本体。
- `RelayTower.PowerGrid`
  继电器供电相关逻辑。
- `TowerPowerGridCoordinator`
  供电重算与升级校验主链。
- `DefenseTower`
  共享战斗塔主逻辑。
- `TowerTypeUtility`
  塔类型辅助分类。
- `TowerPresentationCatalogAsset`
  塔展示目录。

### Enemies
- `Enemy`
  敌人基础壳，只负责：
  - 路径移动
  - 生命与护盾
  - 受击、死亡、到点结算
  - 基础身体与血条表现
  - 把目录定义和上下文分发给机制模块
- `EnemyCatalogAsset`
  敌人静态目录与 prefab 对应关系。
- `EnemyMechanicModule`
  敌人机制模块基类。
- `EnemyStealthModule`
  隐身与探测显形。
- `EnemyShieldAuraModule`
  护盾光环。
- `EnemyRepairModule`
  修理友军。
- `EnemySplitOnDeathModule`
  死亡分裂。

### UI
- `MainMenuController`
  主菜单行为层。
- `LevelSelectController`
  关卡选择页行为层。
- `LevelSelectCard`
  单张关卡卡片。
- `TowerShopCard`
  右侧部署卡。
- `TowerDefenseHudPresenter`
  把运行时状态写入场景 HUD。
- `TowerDefenseHudThemeAsset`
  HUD 主题资产。
- `TowerDefenseHudCopyAsset`
  HUD 固定文案资产。
- `LevelSelectCatalogAsset`
  关卡选择数据资产。

## Data Ownership
### Scene Owns
- 地图骨架与路径点。
- 出怪口、防御点、BuildZone。
- UI 物体层级和布局。
- 总控显式场景引用。
- 2D 剧情场景中的玩家、NPC、对话系统、相机跟随和交互触发器。

### ScriptableObject Owns
- 塔展示目录。
- 敌人目录。
- 波次目录。
- 关卡选择目录。
- 剧情流程目录。
- HUD 主题与 HUD 固定文案。
- 放置可视化主题。

### Prefab Owns
- 运行时塔与敌人的具体外观。
- 运行时 VFX 外观。
- 敌人机制模块的本地覆盖参数。
- 如果路径 / 出怪口 / 防御点切到“作者接管模式”，则可读性根节点下的正式视觉资源也由 Scene / authored root 承担。

## Enemy System Design Choice
- 不再推荐“每种敌人单独一个大脚本”。
- 当前采用：
  - 共用基础壳：`Enemy`
  - 差异静态数据：`EnemyCatalogAsset`
  - 差异特殊机制：独立模块
- 这个结构更适合当前项目，因为：
  - 不同敌人仍共享同一套移动/受击/死亡骨架
  - 特殊能力可以组合
  - prefab Inspector 更直观
  - 以后新增敌人时，不必复制第九份、第十份敌人主脚本

## Authoring Tool Boundaries
- `EnemyEditor`
  显示目录匹配、被动特征、机制模块摘要，并在缺少目录要求模块时提供补挂按钮。
- `EnemyMechanicModuleEditors`
  统一说明模块当前是吃目录默认值，还是 prefab 本地覆盖值。
- `TowerDefenseGameEditor`
  把总控检查、HUD 结构状态和共享资产入口集中到一个地方。
- `Level04RingGuideEditor`
  只服务于作者可读性：在 `Scene` 视图里直接绘制外环 / 中环 / 内环标签，不介入运行时逻辑。

## What The Architecture Is Deliberately Not Doing
- 不引入大型框架或事件总线来包住整个游戏。
- 不把所有运行时状态都塞进 `ScriptableObject`。
- 不把所有敌人拆成层层继承的类树。
- 不再通过对象名承担主链装配职责。

## Current Recommended Growth Direction
1. 继续扩展内容，不再优先大改主链结构。
2. 在保持架构边界稳定的前提下，推进关卡制作和波次调参。
3. 继续围绕 `CampaignFlow` 把后续 `StoryInterludePlaceholder` 替换成真实 2D 横板场景，而不是把 2D 交互与塔防强行堆到同一个 Scene。
## 2026-05-08 Authoring Tooling Additions
- `LevelRouteBlueprintApplier`
  Now uses a bulk-authoring suppression scope so large scene rewrites do not fight
  `EnemyPath` / `EnemySpawnGate` / `DefensePointFlag` readability regeneration mid-edit.
- `TowerDefenseValidationRunner`
  Upgraded from a `SampleScene`-only validator into a cross-scene contract checker for
  `Level02`, `Level03`, and `Level04`.
- `LevelTopologyEditorWindow`
  Added as a dedicated scene-authoring tool for multi-gate / multi-defense-point maps.
  It exposes the authored topology directly instead of introducing a second hidden data layer.
