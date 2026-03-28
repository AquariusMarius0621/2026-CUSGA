---
name: dev-acceptance-sop
description: Run the local acceptance SOP after substantial `【塔防开发】` work. Use after code changes, scene mutations, UI rebuilds, drag-placement changes, or bug fixes in this project.
---

Default acceptance checklist:
1. Refresh Unity assets if scripts or scene files changed.
2. Run `dotnet build Assembly-CSharp.csproj -nologo`.
3. Save the scene if any scene mutation happened.
4. If runtime behavior changed, do at least one Play-mode startup check.
5. Read console `Error` and `Exception` logs.
6. Report any remaining unverified areas explicitly.

Minimum bar for completion:
- compile clean,
- no new runtime errors discovered,
- scene saved when mutated,
- user is told what was verified vs not fully hand-tested.