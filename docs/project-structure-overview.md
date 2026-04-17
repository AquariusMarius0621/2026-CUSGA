# Tower Defense Project Structure Overview
Updated: 2026-04-17

## 一句话理解项目
这是一个 Unity 2022.3 的 2D 塔防项目，当前正从旧原型迁移到一套新的“继电器供电 + 废料经济 + 多出怪口地图结构”玩法。

## 当前制作方式的重要事实
- 关卡地图后续主要由用户自己在 Scene 视图里继续制作和调整。
- 当前美术资源是原型资源，后续会被替换成用户自己的资源。
- 所以后续脚本和场景结构都必须同时满足：
  - Scene 作者友好
  - Inspector 参数友好
  - 美术替换友好

## 最重要的两个场景
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/SampleScene.unity`

## SampleScene 当前已落地的阶段 A 对象
- `BattlefieldMap`
- `SpawnGate_Main`
- `SpawnGate_Alt`
- `DefensePoint_Core`
- `EnemyPath_B`
- `PathSegment_A2`
- `PathSegment_B2`
- `PathShadow_A2`
- `PathShadow_B2`

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

### 阶段 B 供电系统
- `TowerPowerGridCoordinator.cs`
- `RelayTower.cs`
- `RelayTower.PowerGrid.cs`
- `DefenseTower.cs`

## 当前最重要的事实
- 第一阶段已经从“有骨架脚本”推进到“场景里真正有双出怪口和并线路径对象”。
- 第二阶段已经完成主要落地：
  - 继电器覆盖放置
  - 断电停工
  - 预摆结构自动编号
  - 结构销毁后供电重算
  - 选中结构、升级、拆除和 HUD 摘要反馈
- 后续所有实现都要优先保证你能在 Unity 编辑器里自己继续调地图和换美术资源。