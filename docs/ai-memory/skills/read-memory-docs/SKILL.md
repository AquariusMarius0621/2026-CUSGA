---
name: read-memory-docs
description: Read the local memory docs before any substantial `【塔防开发】` task in this project. Use whenever the request touches code, scene, UI, gameplay rules, debugging, documentation, or validation in this Unity tower defense workspace.
---

1. Read `docs/ai-memory/td-memory-main.md` first.
2. If the task touches scene wiring, HUD, drag placement, build rules, or object structure, also read `docs/ai-memory/td-memory-architecture.md`.
3. If the task touches debugging, acceptance, known issues, history, or roadmap, also read `docs/ai-memory/td-memory-rules-and-history.md`.
4. Before editing anything, restate the relevant constraints to yourself:
   - this is a Unity tower defense prototype,
   - `【塔防开发】` tasks require dense teaching-style comments in scripts,
   - object names in scene matter because runtime lookup still uses names.
5. If docs and code disagree, trust current code/scene first, then schedule a memory-doc update at the end.