# Tower Defense Agent Development Playbook
Version: 1.3.0
Updated: 2026-04-23
Audience: 后续继续开发本项目的人类维护者与智能体

## 开工前固定流程
1. 先读 `AGENTS.md`
2. 再读 `docs/ai-memory/td-memory-main.md`
3. 如果任务涉及结构、装配、UI、Prefab、Scene 接线，再读 `docs/ai-memory/td-memory-architecture.md`
4. 如果任务涉及历史、已知问题、验证状态或项目规则，再读 `docs/ai-memory/td-memory-rules-and-history.md`
5. 如果任务涉及玩法规则，再读 `docs/gameplay-redesign-spec.md`
6. 最后再读相关脚本、Prefab、Scene

## 执行规则
- 每次新需求先复述理解，等用户说“执行”再改文件。
- 如果这是一个“大阶段”任务，只在阶段开始时确认一次。
- 阶段内部可以自行分步推进，不要每个小步骤都停下来重新要确认。
- 优先自己解决可确认的问题，不要把本可通过代码/文档查清的事情反复抛回给用户。

## 开发时必须一直遵守的长期约束
1. 地图和关卡后续主要由用户自己在 Scene 视图里继续制作和调整。
2. 当前美术资源只是原型资源，项目结构必须长期保持便于替换。
3. 脚本、文档、注释中的中文都必须保持正常显示。
4. 新脚本必须按合理目录分层，不允许散放。
5. 显式引用优先，不依赖对象名查找承担主链装配职责。

## 代码与资源改动优先级
1. 能通过 Scene 对象、Prefab、Inspector、ScriptableObject 解决的，优先不要改玩法算法。
2. 能通过共享资产收口的，不要把参数重新写死回代码。
3. 能通过 Prefab 组合解决的，不要优先复制出新的大脚本。

## 文档维护规则
- 只要玩法规则、场景结构、Prefab 结构、脚本职责、UI 工作流、已知问题、验证状态发生实质变化，就更新 `docs/ai-memory/*`。
- 只要 AI 协作方法、智能体准则、模板或技能组织方式变化，就更新 `docs/ai-workspace-bootstrap-methodology.md`。
- 更新后刷新索引：
  `powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`

## 验证规则
- 运行时代码优先做命令行编译检查。
- 编辑器脚本优先在 Unity 内部实看 Inspector 结果。
- 如果任务进入最终人工验证阶段，统一使用 `docs/manual-validation-checklist.md` 记录结果。
- 用户把验证结果写回清单后，后续智能体应优先读取这份清单，再决定修复顺序。

## Git 工作流
- 当前远端 `main` 受保护，不能直接 push。
- 需要远端留档时，优先：
  - 推快照分支
  - 或创建 PR
- 做版本记录前，先确认当前工作区主题是否收口，不要把无关改动混进同一提交。

## 当前建议开发顺序
1. 先完成人工验证，确认敌人 prefab 与 Inspector 结果对齐。
2. 再推进 `Level02` 到 `Level05` 的内容制作。
3. 再推进故事横板实际内容并入。
4. 同步推进正式美术替换和显式作者入口维护。
## 2026-05-08 Tool Workflow Update
Recommended order for large map authoring changes:
1. Use `LevelTopologyEditorWindow` to wire spawn gates, enemy paths, and defense points.
2. If the scene needs a major route redesign, run `LevelRouteBlueprintApplier` next.
3. After topology or blueprint changes, run `TowerDefenseValidationRunner` before doing
   detailed wave tuning or visual polishing.
4. Only after the scene contract is stable should designers move to the balance console and
   per-wave tuning tools.
