---
name: update-memory-docs
description: Update the local AI memory docs after substantial `【塔防开发】` work in this project. Use whenever systems, files, rules, UI flow, validation status, or roadmap meaningfully change.
---

After substantial work:
1. Update version/date in the touched memory docs.
2. Refresh the main-doc file index with `powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`.
3. Record new rules, scene objects, or workflow constraints.
4. Move implementation-specific details into architecture/history docs rather than bloating the main doc.
5. Record what was validated and what still needs manual testing.
6. If no memory update is needed, state the reason explicitly instead of silently skipping it.

For this project, memory docs are part of the deliverable, not optional polish.