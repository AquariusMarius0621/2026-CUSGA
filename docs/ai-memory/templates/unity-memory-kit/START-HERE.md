# Unity Memory Kit - Start Here

This folder is a reusable memory-doc + local-skill starter kit for other Unity projects.

## What To Copy
Copy these items into a new Unity project:
- `AGENTS.template.md` -> rename to `AGENTS.md`
- `td-memory-main.template.md`
- `td-memory-architecture.template.md`
- `td-memory-rules-and-history.template.md`
- `memory-index.paths.template.txt` -> rename to `docs/ai-memory/memory-index.paths.txt`
- `tools/refresh-memory-index.ps1`
- `skills/*`

## What To Replace
Replace these placeholders:
- `<PROJECT_NAME>`
- `<PROJECT_TAG>`
- `<GAME_SUMMARY>`
- `<MAIN_SCENE>`
- `<MAIN_NAMESPACE_OR_FOLDER>`
- `<CORE_SYSTEMS>`
- `<VALIDATION_COMMANDS>`

## Recommended Setup Order
1. Create `docs/ai-memory/` in the new project.
2. Copy the three memory docs and the local skills.
3. Customize `AGENTS.md` so the AI always reads the right docs first.
4. Fill `memory-index.paths.txt` with the files that actually matter in the new project.
5. Run:
   `powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`
6. After each substantial milestone, update history, known issues, and roadmap.

## Principle
Keep the main memory doc short and stable.
Move volatile implementation detail into the architecture/history docs.
Use the local skills as process guardrails, not as ceremony.