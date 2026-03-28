# AGENTS.md

This project keeps a local AI memory system for `【塔防开发】` work.

## UnitySkills
- `unity-skills`: Unity Editor automation via REST API

## Workspace Identity
- Expected project root: `D:\unity\做游戏\塔防开发`
- Expected memory-doc root: `D:\unity\做游戏\塔防开发\docs\ai-memory`
- If the current workspace is not this project root, re-check whether this `AGENTS.md` still applies before following it.

## Startup Protocol
For any substantial `【塔防开发】` task in this workspace, follow this order:
1. Read this `AGENTS.md` first.
2. Read `docs/ai-memory/td-memory-main.md`.
3. Read `docs/ai-memory/td-memory-architecture.md` when the task touches scene wiring, UI, drag placement, build rules, runtime object structure, or scene composition.
4. Read `docs/ai-memory/td-memory-rules-and-history.md` when the task touches validation, known issues, history, roadmap, or project-specific coding rules.
5. Follow the local skill docs under `docs/ai-memory/skills/` as project protocol.

## Operating Rules
- Treat the memory docs as the project's collaboration memory, not as a replacement for reading code and scene state.
- If memory docs and current code or scene state disagree, trust current code and scene state first, then update the memory docs afterward.
- Preserve object names carefully when scene lookup still depends on names.
- Keep changes aligned with the existing prototype-first architecture unless there is a strong reason to restructure.

## Project-Specific Rule
Only in `【塔防开发】` conversations, when creating or modifying Unity scripts:
- keep comment density high,
- explain in a teacher-style way,
- update comments together with behavior changes,
- prefer explaining intent, tradeoffs, and system boundaries instead of only stating what the code does.

## Memory Update Triggers
After substantial `【塔防开发】` work, update the local memory system when any of these changed:
- core gameplay rules,
- scene structure or important object names,
- UI hierarchy or interaction flow,
- key scripts or file structure,
- known issues, validation status, or roadmap.

When file structure or line counts changed materially, refresh the main-doc index with:
`powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`

## Cross-Session Expectation
- As long as the agent is working inside `D:\unity\做游戏\塔防开发`, it should read `AGENTS.md` on its own before substantial work.
- The user does not need to repeat these rules every time in the same project.
- If work moves to a different workspace or a different project, this file no longer applies unless copied there or explicitly referenced.