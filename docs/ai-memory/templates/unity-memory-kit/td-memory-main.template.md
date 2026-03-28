# <PROJECT_NAME> AI Memory - Main
Version: 0.1.0
Updated: <YYYY-MM-DD>
Scope: Only for `<PROJECT_TAG>` tasks in this project.
Read Priority: Read this first; load architecture/history docs on demand.

## Navigation
- Main memory: `docs/ai-memory/td-memory-main.md`
- Architecture memory: `docs/ai-memory/td-memory-architecture.md`
- Rules + history memory: `docs/ai-memory/td-memory-rules-and-history.md`
- Local skills: `docs/ai-memory/skills/*/SKILL.md`

## ch1 项目概况
一句话描述：<GAME_SUMMARY>

技术栈：
- Unity `<UNITY_VERSION>`
- Render/UI/Input stack: <STACK>
- Key packages: <KEY_PACKAGES>

当前阶段：
- <CURRENT_STATE_1>
- <CURRENT_STATE_2>
- <CURRENT_STATE_3>

当前不做：
- <OUT_OF_SCOPE_1>
- <OUT_OF_SCOPE_2>
- <OUT_OF_SCOPE_3>

## ch2 文件结构
关键文件索引（文件一行描述 + 当前行数）：

维护方式：
- 索引源配置：`docs/ai-memory/memory-index.paths.txt`
- 自动刷新脚本：`docs/ai-memory/tools/refresh-memory-index.ps1`
- 刷新命令：`powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`

<!-- MEMORY_INDEX:START -->
| Path | Lines | Role |
| --- | ---: | --- |
| `<REPLACE_ME>` | 0 | `<REPLACE_ME>` |
<!-- MEMORY_INDEX:END -->

## ch3 核心规则
- 核心数值规则：<CORE_RULES>
- 核心限制条件：<CORE_CONSTRAINTS>
- 关键命名/对象约定：<NAMING_RULES>

## ch4 游戏流程
1. <FLOW_STEP_1>
2. <FLOW_STEP_2>
3. <FLOW_STEP_3>
4. <FLOW_STEP_4>

## 读取策略
以下任务必须补读架构文档：<ARCH_TRIGGER_TASKS>
以下任务必须补读历史/规范文档：<RULES_TRIGGER_TASKS>
如果文档与代码冲突：以当前代码/场景为准，然后修正文档。