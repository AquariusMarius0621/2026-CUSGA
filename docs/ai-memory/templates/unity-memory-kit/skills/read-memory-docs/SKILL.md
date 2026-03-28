---
name: read-memory-docs
description: Read the local memory docs before any substantial `<PROJECT_TAG>` task in this Unity project. Use whenever the request touches code, scene, UI, gameplay rules, debugging, documentation, or validation.
---

1. Read `docs/ai-memory/td-memory-main.md` first.
2. If the task touches scene wiring, runtime object structure, UI, or gameplay rules, also read `docs/ai-memory/td-memory-architecture.md`.
3. If the task touches validation, known issues, history, roadmap, or project rules, also read `docs/ai-memory/td-memory-rules-and-history.md`.
4. Before editing anything, restate the relevant constraints to yourself.
5. If docs and code disagree, trust current code/scene first, then queue a doc update at the end.