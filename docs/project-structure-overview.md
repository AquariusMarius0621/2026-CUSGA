# Tower Defense Project Structure Overview
Updated: 2026-03-31
Audience: 给项目维护者看的结构说明文档。
Goal: 用更好读的方式讲清楚当前项目由哪些场景、脚本、UI、文档和工具组成，以及它们怎么协作。

## 1. 一句话理解这个项目
这是一个 Unity 2022.3 的 2D 塔防原型项目。
当前核心特色是：
- 电量经济
- 自由拖拽部署
- 首塔初始部署区 + 后续沿建筑网络扩张
- 拖拽时显示精确合法区域覆盖层

当前入口流程是：
- 先进入 `MainMenu`
- 点击开始进入 `SampleScene`

## 2. 项目最重要的两个场景
### `Assets/Scenes/MainMenu.unity`
这是主页面场景。
你打开它时，`MainMenuController` 会在编辑器里把默认 UI 骨架补齐到场景中，所以你可以直接在 Scene 和 Inspector 里调标题、按钮、说明文字、布局等内容。

它的主要作用只有一个：
- 作为游戏入口，点击开始进入 `SampleScene`

### `Assets/Scenes/SampleScene.unity`
这是当前塔防玩法真正运行的场景。
这里面包含：
- 战场
- 敌人路径
- 建造区
- 禁建区
- 运行时根节点
- HUDCanvas
- 右侧部署区
- 玩法总控对象

如果你想改真正的塔防玩法，大多数时候都会回到这个场景。

## 3. 核心脚本分工
### `TowerDefenseGame.cs`
这是项目总控。
它负责整局游戏的规则编排：
- 资源和基地血量
- 当前选中的建筑类型
- 点击/拖拽放置
- 放置是否合法
- 放置预览塔
- 精确合法区覆盖层
- 真正建塔
- 刷 HUD
- Game Over

可以把它理解成“导演”。
几乎所有主流程最后都会收口到这里。

### `TowerCatalog.cs`
这是建筑静态定义中心。
它负责回答“某种建筑是什么”，而不是“这局里现在发生了什么”。
当前主要提供：
- 展示名
- 成本
- 占地半径
- 扩张方格大小
- 卡片文案
- 强调色

如果你以后要改“发电机是不是叫 Relay Generator”“炮塔卡片上写什么”“某种建筑铺路范围有多大”，优先看这里。

### `TowerDefenseHudPresenter.cs`
这是 HUD 表现层。
它不决定规则，只负责把规则结果显示出来。
当前它主要做：
- 查找 HUD 节点
- 更新顶部资源卡
- 更新右侧部署卡
- 更新拖拽提示面板
- 运行时美化 HUD
- 运行时显式修正布局，避免重叠

如果你想改：
- 顶部资源区样式
- 右侧发电机/炮塔卡片排版
- 拖拽提示文案和位置
- 状态条视觉
就优先看这里。

### `TowerShopCard.cs`
这是右侧部署卡交互入口。
它负责：
- 点击卡片
- 开始拖拽
- 拖拽中转发鼠标位置
- 结束拖拽
- 悬停反馈

它本身不判断能不能建，只是 UI 输入入口。

## 4. 放置系统相关脚本
### `BuildZone.cs`
大范围建造许可区。
只回答“这个点有没有在关卡允许建造的大区域里”。

### `PlacementBlocker.cs`
禁建标记。
路径、出生点、基地核心这些区域会挂它。
它回答“虽然在大建造区里，但这里依然不能建”。

### `BuildPad.cs`
旧固定塔位系统的兼容层。
当前主玩法已经不用它了，但它还保留着旧方案的占位逻辑和桥接代码。
现在更多是历史兼容和参考对象。

## 5. 战斗相关脚本
### `DefenseTower.cs`
基础攻击塔。
定期扫描最近敌人并直接命中。

### `RelayTower.cs`
发电机。
周期性给玩家增加电量，不攻击敌人。

### `Enemy.cs`
敌人本体。
负责：
- 沿路径移动
- 受伤/死亡
- 抵达基地扣血
- 血条显示

### `EnemyPath.cs`
路径点容器。
负责保存敌人路径和出生点信息。

### `WaveSpawner.cs`
波次状态机。
负责刷怪节奏、每波数量、波间间隔，以及通关条件判断。

## 6. 原型期辅助脚本
### `SceneObjectFinder.cs`
这是原型期按对象名查找场景对象的工具。
因为项目里仍有一部分对象引用不是 Inspector 直拖，而是通过名字去找，所以它现在还很重要。

这也意味着一个项目约束：
有些对象名不能随便改。

## 7. UI 和资源文件
### 图标与预览资源
- `Assets/UI/Icons/relay-card-icon.png`：发电机卡图标
- `Assets/UI/Icons/defense-card-icon.png`：炮塔卡图标
- `Assets/Resources/UI/placement-ring.png`：地面放置预览圈

### TMP 资源
`Assets/TextMesh Pro/**` 是 TextMeshPro 的字体和默认资源，主要负责文本渲染基础设施。

## 8. 配置文件
### `Packages/manifest.json`
Unity 包依赖清单。

### `Packages/packages-lock.json`
包版本锁定文件。

### `ProjectSettings/ProjectVersion.txt`
Unity 版本锁定，目前项目使用 `2022.3.62f3c1`。

### `ProjectSettings/EditorBuildSettings.asset`
构建场景顺序。
当前顺序是：
1. `MainMenu`
2. `SampleScene`

## 9. AI 协作文档与工具
### `AGENTS.md`
项目入口规则。
它规定 AI 进入这个项目后要先读哪些文档、什么时候更新记忆、脚本注释要保持什么风格。

### `docs/ai-memory/td-memory-main.md`
主记忆文档，记录项目概况、核心规则、文件索引和流程。

### `docs/ai-memory/td-memory-architecture.md`
架构文档，记录场景装配、HUD 结构、放置链路。

### `docs/ai-memory/td-memory-rules-and-history.md`
规范、历史和路线图文档。

### `docs/ai-memory/td-project-file-guide.md`
持续维护用的文件指南。
它更偏“索引表”和“维护规则”。

### `docs/ai-memory/skills/maintain-project-file-guide/SKILL.md`
本地 skill。
它规定了：
- 文件结构变化后，必须更新哪些文档
- 这类文档应该怎么维护

## 10. 如果你想改某类内容，先看哪里
### 改主菜单
先看：
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/TowerDefense/MainMenuController.cs`
- `ProjectSettings/EditorBuildSettings.asset`

### 改放置规则
先看：
- `Assets/Scripts/TowerDefense/TowerDefenseGame.cs`
- `Assets/Scripts/TowerDefense/TowerCatalog.cs`
- `Assets/Scripts/TowerDefense/BuildZone.cs`
- `Assets/Scripts/TowerDefense/PlacementBlocker.cs`
- `Assets/Scripts/TowerDefense/TowerShopCard.cs`

### 改 HUD / 右侧部署区
先看：
- `Assets/Scripts/TowerDefense/TowerDefenseHudPresenter.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/UI/Icons/*`

### 改战斗
先看：
- `Assets/Scripts/TowerDefense/DefenseTower.cs`
- `Assets/Scripts/TowerDefense/RelayTower.cs`
- `Assets/Scripts/TowerDefense/Enemy.cs`
- `Assets/Scripts/TowerDefense/WaveSpawner.cs`
- `Assets/Scripts/TowerDefense/EnemyPath.cs`

## 11. 这个文档怎么用
你以后如果只是想快速判断“某个东西该去哪里改”，优先看这份文档。
如果你想看更像索引表、并且带维护约束的版本，就看：
- `docs/ai-memory/td-project-file-guide.md`

如果你发现：
- 新增了关键脚本
- 新增了关键场景
- 场景入口变了
- 某个脚本职责明显变化了
那说明这份文档和文件指南都应该更新。