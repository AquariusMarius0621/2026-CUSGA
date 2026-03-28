---
name: update-memory-docs
description: Update the local AI memory docs after substantial `<PROJECT_TAG>` work in this Unity project. Use whenever systems, files, rules, UI flow, validation status, or roadmap meaningfully change.
---

After substantial work:
1. Update version/date in the touched memory docs.
2. Refresh the main-doc file index with `powershell -ExecutionPolicy Bypass -File docs/ai-memory/tools/refresh-memory-index.ps1 -UpdateMainDoc`.
3. Record new rules, scene objects, workflow constraints, or validation outcomes.
4. Move volatile details into architecture/history docs instead of bloating the main doc.
5. If no memory update is needed, state the reason explicitly.