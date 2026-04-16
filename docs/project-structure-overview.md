# Tower Defense Project Structure Overview
Updated: 2026-04-15
Audience: 项目维护者与后续接手的智能体
Goal: 用易读方式说明当前项目由哪些场景、脚本、文档和工具组成，以及后续应从哪里继续开发。

## 1. 一句话理解项目
这是一个 Unity 2022.3 的 2D 塔防项目，当前正在从旧原型迁移到一套新的“继电器供电 + 废料经济 + 多出怪口防线”玩法设计。

## 2. 最重要的两个场景
### `Assets/Scenes/MainMenu.unity`
- 当前入口场景。
- `MainMenuController` 负责菜单 UI 和进入 `SampleScene`。

### `Assets/Scenes/SampleScene.unity`
- 当前主玩法场景。
- 旧原型仍然在这里运行。
- 后续新玩法也应从这里继续迁移，而不是另起一套无关结构。

## 3. 最重要的文档
### `docs/gameplay-redesign-spec.md`
这是后续玩法开发的最高优先级规则文档。
如果当前代码和它冲突，后续开发应以它为准。

### `docs/ai-memory/td-memory-main.md`
主记忆文档，记录项目现状、索引和开发基线。

### `docs/ai-memory/td-memory-architecture.md`
说明当前模块边界和推荐扩展方式。

### `docs/ai-memory/td-memory-rules-and-history.md`
记录已知状态、项目规则、历史和路线图。

### `docs/ai-memory/td-agent-development-playbook.md`
说明后续开发应按什么顺序推进。

## 4. 当前脚本结构怎么理解
### 4.1 总控与装配层
- `TowerDefenseGame.cs`
  当前主要负责 Unity 生命周期、模块装配和少量对外兼容门面。
- `TowerDefenseSceneBootstrapper.cs`
  负责场景引用回填、运行时根节点装配、BuildZone 兜底创建。
- `TowerDefenseInputCoordinator.cs`
  负责热键轮询、快速点击放置、坐标换算和 UI 阻挡判断。

### 4.2 放置链
- `TowerPlacementRules.cs`
  回答“这里能不能放”。
- `TowerPlacementVisualController.cs`
  回答“拖拽时看见什么”。
- `TowerPlacementInteractionController.cs`
  回答“玩家现在处于哪个放置交互阶段”。
- `TowerPlacementBuildExecutor.cs`
  回答“确定要建之后，塔怎么真正落地”。
- `TowerPlacementSupportCoordinator.cs`
  负责放置链剩余的支持型能力，例如规则桥接、塔定义查询、起手区标记与自检。

### 4.3 状态与表现
- `TowerDefenseSessionState.cs`
  负责保存这一局当前状态。
- `TowerDefensePresentationCoordinator.cs`
  负责把状态广播到 HUD 和结算表现。
- `TowerDefenseHudPresenter.cs`
  负责真正把文字和面板写到场景 HUD 上。
- `TowerCatalog.cs`
  负责塔静态定义。

### 4.4 单位与战斗对象
- `TowerShopCard.cs`
  部署卡 UI 输入入口。
- `DefenseTower.cs`
  基础攻击塔。
- `RelayTower.cs`
  旧原型里的发电塔脚本，后续应迁到新版继电器供电机制。
- `Enemy.cs`
  敌人行为。
- `EnemyPath.cs`
  路径点容器。
- `WaveSpawner.cs`
  刷怪与波次状态机。

## 5. 现在如果你想改某类内容
### 想改玩法规则
- 先看 `docs/gameplay-redesign-spec.md`
- 再看 `docs/ai-memory/td-memory-architecture.md`

### 想改放置行为
- 看 `TowerPlacementRules.cs`
- 看 `TowerPlacementInteractionController.cs`
- 看 `TowerPlacementBuildExecutor.cs`
- 看 `TowerPlacementSupportCoordinator.cs`

### 想改局内资源、生命和波次
- 看 `TowerDefenseSessionState.cs`

### 想改 HUD 与 Game Over 表现
- 看 `TowerDefensePresentationCoordinator.cs`
- 看 `TowerDefenseHudPresenter.cs`
- 再回场景里改 HUD 对象

### 想改输入与点击手感
- 看 `TowerDefenseInputCoordinator.cs`
- 看 `TowerShopCard.cs`

### 想改场景装配或运行时根节点
- 看 `TowerDefenseSceneBootstrapper.cs`
- 再看 `TowerDefenseGame.cs`

## 6. 当前最重要的事实
- 当前代码已经完成了一轮比较彻底的组件化收口。
- `TowerDefenseGame.cs` 已经不再是原始意义上的上帝脚本。
- 但玩法本身还没有完全切换到新版设计。
- 所以后续工作重点不再是继续大拆结构，而是按新玩法文档逐步实现具体系统。

## 7. 给后续维护者的建议
- 如果你只是想继续做玩法，不要再随意发起大规模结构重构。
- 优先把“多出怪口、继电器供电、断电塔、升级校验、废料经济”这些新玩法核心系统做出来。
- 每次玩法规则变化后，都同步更新 `docs/gameplay-redesign-spec.md` 和几份记忆文档。
