# Tower Defense Project Structure Overview
Updated: 2026-04-17

## 一句话理解项目
这是一个 Unity 2022.3 的 2D 塔防项目，当前正在从旧原型迁移到一套新的“继电器供电 + 废料经济 + 多出怪口地图结构”玩法。

## 配套文档
- `docs/art-replacement-workflow.md`
  以后替换塔、敌人、UI、地图和主菜单美术时，优先看这份文档。
- `docs/manual-validation-checklist.md`
  以后你在 Unity 里做最终人工验证时，直接按这份清单逐项填写结果。

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
- 第四阶段：完成
- 下一阶段：进行中（HUD 正式反馈承载已开始）

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
- 第四阶段也已经完成：敌人死亡奖励废料、废料主循环、第一版经济曲线和运行时资源反馈都已经接进主链。
- 当前样例场景的运行时原型字段也已经同步到现行脚本结构。
- 当前下一步更值得继续做的是 UI、关卡表现和正式美术承载。
- 其中下一阶段的第一轮已经开始：
  - 操作区现在会同时承载当前状态消息
  - 操作区会显示供电全局摘要
  - 瞬时提示会在高亮后继续留在最近事件流里
- 下一阶段的第二轮也已经开始：
  - `EnemyPath` 会自动补路径描边、方向箭头和转弯热点
  - `EnemySpawnGate` 会自动补更明显的出怪口标记
  - `DefensePointFlag` 会自动补更明确的核心目标区提示
- 下一阶段的第三轮也已经开始：
  - `TowerDefenseGame` 开始承载塔卡展示配置和 HUD 主题配置
  - `TowerShopCard` 开始承载卡片图标/底板/强调图形的样式入口
  - `DefenseTower`、`Enemy`、`RelayTower` 都开始暴露更明确的视觉挂点或主渲染器入口
  - 放置圆环资源现在优先支持显式 Sprite 引用
  - `SampleScene` 里这些新入口也已经补了一轮实际场景接线
  - `DefenseTowerPrototype` 还额外拆出了 `FeedbackRoot / TypeSignatureRoot / LevelMarkerRoot` 三个真实子物体
  - `EnemyPrototype` 也额外拆出了 `VisualScaleRoot`，让敌人身体缩放和血条层级分离
  - `RelayTowerPrototype` 也额外拆出了 `VisualRoot`，并重新接好主渲染器入口
