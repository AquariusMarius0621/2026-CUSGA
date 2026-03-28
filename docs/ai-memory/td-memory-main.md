# Tower Defense AI Memory - Main
Version: 0.1.1
Updated: 2026-03-28
Scope: 仅用于本项目的 `【塔防开发】` 相关任务。
Read Priority: 必读主文档；按需补读 `td-memory-architecture.md` 与 `td-memory-rules-and-history.md`。

## Navigation
- Main memory: `docs/ai-memory/td-memory-main.md`
- Architecture memory: `docs/ai-memory/td-memory-architecture.md`
- Rules + history memory: `docs/ai-memory/td-memory-rules-and-history.md`
- Local skills: `docs/ai-memory/skills/*/SKILL.md`
- Unity template kit: `docs/ai-memory/templates/unity-memory-kit/START-HERE.md`

## ch1 项目概况
一句话描述：Unity 2022.3 的 2D 塔防原型，目标是做出一个类似《王国保卫战》但带“电量经济 + 自由拖拽部署”特色的最小可玩测试关卡。

技术栈：
- Unity `2022.3.62f3c1`
- 2D 功能包 `com.unity.feature.2d`
- UGUI `com.unity.ugui`
- TextMeshPro `com.unity.textmeshpro`
- UnitySkills REST 自动化工具

当前阶段：
- 已完成基础战斗闭环：敌人沿路径前进、塔攻击、继电器产能、波次刷怪、基地掉血、Game Over。
- 已完成建造方式升级：从固定 BuildPad 点击放置升级为 `BuildZone + PlacementBlocker + TowerShopCard` 的自由放置方案。
- 已完成游戏中 HUD 的第三轮风格化重排，方向参考“明日方舟式”深色战术终端风格。
- 已完成编译验证、Play 启动验证，以及图标卡、地图装饰、拖拽投影圈、微动效与章节角标的联调验证。
- 已完成第一轮基础架构切分：新增 `TowerCatalog` 与 `TowerDefenseHudPresenter`，把塔静态定义和 HUD 刷新从 `TowerDefenseGame` 总控中抽离。

当前不做：
- 敌人攻击/技能
- 塔升级/售卖
- 正式美术资源替换
- 复杂持久化/关卡选择

## ch2 文件结构
关键文件索引（文件一行描述 + 当前行数）：

维护方式：
- 索引源配置：docs/ai-memory/memory-index.paths.txt
- 自动刷新脚本：docs/ai-memory/tools/refresh-memory-index.ps1
- 刷新命令：powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc

<!-- MEMORY_INDEX:START -->
| Path | Lines | Role |
| --- | ---: | --- |
| `Assets/Scenes/SampleScene.unity` | 11458 | 当前唯一测试关卡，包含战场、路径、HUD、原型对象与场景装配 |
| `Assets/Scripts/TowerDefense/TowerDefenseGame.cs` | 772 | 总控：资源、基地、自由放置、拖拽预览、建造判定 |
| `Assets/Scripts/TowerDefense/TowerCatalog.cs` | 136 | 塔静态定义目录：统一提供成本、占地半径、展示名与部署卡文案数据 |
| `Assets/Scripts/TowerDefense/TowerDefenseHudPresenter.cs` | 386 | HUD 表现层适配器：负责界面查找、文本刷新、按钮状态与拖拽提示 |
| `Assets/Scripts/TowerDefense/TowerShopCard.cs` | 280 | 部署卡拖拽入口，负责 UI 拖拽交互转发 |
| `Assets/Scripts/TowerDefense/BuildZone.cs` | 125 | 自由放置的大范围许可区 |
| `Assets/Scripts/TowerDefense/PlacementBlocker.cs` | 106 | 敌人路径、出生点、基地核心等禁建标记 |
| `Assets/Scripts/TowerDefense/BuildPad.cs` | 307 | 旧固定塔位方案与 PlacedTower 兼容桥接，当前场景中已整体禁用 |
| `Assets/Scripts/TowerDefense/DefenseTower.cs` | 172 | 基础攻击塔，扫描最近敌人并造成伤害 |
| `Assets/Scripts/TowerDefense/RelayTower.cs` | 93 | 经济塔，周期性产出电量 |
| `Assets/Scripts/TowerDefense/Enemy.cs` | 338 | 敌人移动、血量、死亡、抵达基地逻辑 |
| `Assets/Scripts/TowerDefense/EnemyPath.cs` | 115 | 路径点容器与 Gizmo 绘制 |
| `Assets/Scripts/TowerDefense/WaveSpawner.cs` | 253 | 波次配置与刷怪状态机 |
| `Assets/Scripts/TowerDefense/SceneObjectFinder.cs` | 87 | 原型期按名字查找场景对象的辅助工具 |
| `Packages/manifest.json` | 54 | 当前包依赖清单 |
| `ProjectSettings/ProjectVersion.txt` | 2 | Unity 版本锁定 |
| `AGENTS.md` | 39 | 项目入口说明，负责告诉 AI 先读哪份记忆文档与本地 Skill |
| `docs/ai-memory/td-memory-main.md` | 108 | 主记忆文档：项目概况、文件索引、核心规则、游戏流程 |
| `docs/ai-memory/td-memory-architecture.md` | 68 | 架构子文档：Scene 装配、HUD 结构、自由放置链路 |
| `docs/ai-memory/td-memory-rules-and-history.md` | 59 | 规范子文档：已知问题、规范、历史、路线图 |
| `docs/ai-memory/skills/read-memory-docs/SKILL.md` | 12 | 开工前读取记忆文档的本地 Skill |
| `docs/ai-memory/skills/update-memory-docs/SKILL.md` | 12 | 收工后同步记忆文档的本地 Skill |
| `docs/ai-memory/tools/refresh-memory-index.ps1` | 93 | 半自动刷新主记忆文档文件索引表的脚本 |
| `docs/ai-memory/memory-index.paths.txt` | 26 | 主文档文件索引的源配置 |
| `docs/ai-memory/templates/unity-memory-kit/START-HERE.md` | 32 | 可复用 Unity 记忆文档与本地 Skill 模板包入口 |
<!-- MEMORY_INDEX:END -->

## ch3 核心规则
经济与建造：
- 开局电量：`80`
- 基地生命：`10`
- 继电器塔成本：`30`
- 防御塔成本：`45`
- 继电器产能：每 `3s` 产出 `12` 电量

战斗与敌人：
- 敌人只做三件事：出生、沿路径移动、受伤/死亡或抵达基地
- 敌人抵达终点：基地 `-1 HP`
- 防御塔：范围扫描最近敌人，固定间隔直接命中
- 当前默认波次：`4 / 6 / 8` 只敌人三波，速度和血量逐波提高

自由放置规则：
- 第一层：落点必须在 `BuildZone` 内
- 第二层：不能与 `PlacementBlocker` 重叠，当前阻挡区包括路径段、出生点、基地核心
- 第三层：不能与已有塔的占地区过近
- 当前占地半径：`Relay 0.52`，`Defense 0.58`
- 当前拖拽预览：塔本体 + 地面投影圈，合法/非法状态同步变色
- 当前部署卡：支持点击选塔、悬停轻微呼吸、拖拽时进入抓起反馈

交互规则：
- 主入口：从右侧部署卡拖拽到地图放置
- 辅助入口：按钮或快捷键选择塔后，直接点击地图放置
- 取消：`Esc` 或鼠标右键，或点击 `ClearSelectionButton`

AI 协作规则（本项目特有）：
- 只在 `【塔防开发】` 相关对话中启用“高注释密度 + 老师式讲解”模式
- 修改现有脚本逻辑时，必须同步更新注释，且注释密度保持高
- 非本项目对话不自动套用这种高注释密度要求

## ch4 游戏流程
当前关卡流程：
1. 进入 `SampleScene`
2. HUD 初始化，显示资源/基地/波次/部署提示
3. 玩家拖拽部署卡或点击地图进行建造
4. `WaveSpawner` 按波次生成敌人
5. 敌人沿 `EnemyPath` 前进
6. 继电器塔补资源，防御塔击杀敌人
7. 漏怪则基地掉血
8. 基地血量归零时显示 `GAME OVER`
9. 若全部波次结束且场上无敌人，状态栏显示测试关卡完成

## 读取策略
以下任务必须先补读子文档：
- 改 Scene、改 HUD、改建造/拖拽、改组件装配：读 `td-memory-architecture.md`
- 改规范、做验收、追历史、补路线图：读 `td-memory-rules-and-history.md`
- 如果主文档与代码冲突：以代码和当前场景为准，然后修正文档

