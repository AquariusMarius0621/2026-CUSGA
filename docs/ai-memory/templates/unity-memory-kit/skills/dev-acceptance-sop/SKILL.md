---
name: dev-acceptance-sop
description: Run the local acceptance SOP after substantial `<PROJECT_TAG>` work. Use after code changes, scene mutations, UI rebuilds, system additions, or bug fixes in this project.
---

Default acceptance checklist:
1. Refresh Unity assets if scripts or scene files changed.
2. Run the project's compile/build command.
3. Save the scene if any scene mutation happened.
4. If runtime behavior changed, do at least one Play-mode startup or equivalent runtime check.
5. Read console `Error` and `Exception` logs.
6. Report any remaining unverified areas explicitly.