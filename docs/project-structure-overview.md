# Tower Defense Project Structure Overview
Updated: 2026-04-17

## 一句话理解项目
这是一个 Unity 2022.3 的 2D 塔防项目，当前正在从旧原型迁移到一套新的“继电器供电 + 废料经济 + 多出怪口地图结构”玩法。

## 当前制作方式的重要事实
- 关卡地图后续主要由用户自己在 Scene 视图里继续制作和调整。
- 当前美术资源是原型资源，后续会被替换成用户自己的资源。
- 所以后续脚本和场景结构都必须同时满足：
  - Scene 作者友好
  - Inspector 参数友好
  - 美术替换友好

## 当前阶段状态
- 第一阶段：完成
- 第二阶段：完成
- 第三阶段：完成

## 第三阶段当前已经落地的内容
- `DefenseTower` 已支持三类最小可运行行为：
  - 单体攻击
  - 减速场
  - 炸弹投射
- `Enemy` 已支持减速状态。
- `SampleScene` 右侧部署区已经扩展成四张显式场景卡片：
  - `RelayTowerButton`
  - `DefenseTowerButton`
  - `SlowFieldTowerButton`
  - `BombardTowerButton`
- 三类战斗塔现在既能通过右侧卡片点击/拖拽选择，也能继续通过热键 `2 / 3 / 4` 快速切换。
- 三类战斗塔已经开始具备更明确的升级成长差异。
- 炸弹塔已经拥有可替换美术入口的飞行物与爆炸占位反馈。
- 单体塔与减速塔也已经补入了基础 tracer / 脉冲反馈。
- `Enemy` 也已经补入了对应的受击反馈层。
- 战斗塔已经有持续等级标记，HUD 选择区也能更明确提示塔的定位与升级方向。

## 当前脚本结构怎么理解
### 总控与装配层
- `TowerDefenseGame.cs`
- `TowerDefenseSceneBootstrapper.cs`
- `TowerDefenseInputCoordinator.cs`

### 放置链
- `TowerPlacementRules.cs`
- `TowerPlacementVisualController.cs`
- `TowerPlacementInteractionController.cs`
- `TowerPlacementBuildExecutor.cs`
- `TowerPlacementSupportCoordinator.cs`

### 地图结构骨架
- `BattlefieldMapDefinition.cs`
- `EnemySpawnGate.cs`
- `DefensePointFlag.cs`
- `WaveSpawner.cs`

### 阶段 B 供电系统
- `TowerPowerGridCoordinator.cs`
- `RelayTower.cs`
- `RelayTower.PowerGrid.cs`

### 第三阶段塔类型体系
- `DefenseTower.cs`
- `Enemy.cs`
- `TowerCatalog.cs`
- `TowerTypeUtility.cs`

## 当前最重要的事实
- 第一阶段和第二阶段已经完成。
- 第三阶段也已经完成，而且 Scene 卡片入口、升级差异底座、持续等级标记和战斗反馈都已经补到运行链里。
- 后续最值得继续做的是经济系统与敌人收益迁移。
