---
name: unity-history
description: "Use UnitySkills operation history for undo, redo, revert, previous state, and edit history checks."
---

# History Skills

Manage Unity Editor undo/redo history.

## Guardrails

**Mode**: Full-Auto required

**DO NOT** (common hallucinations):
- `history_list` / `history_get` do not exist → use `history_get_current` for current undo group
- `history_clear` does not exist → Unity undo history cannot be cleared via API
- `history_save` does not exist → undo history is managed by Unity automatically

**Routing**:
- For simple undo/redo → `history_undo` / `history_redo` (this module) or `editor_undo` / `editor_redo`
- For persistent task-level undo → use `workflow` module
- For conversation-level undo → use `workflow` module's `workflow_session_undo`

## Skills

### `history_undo`
Undo the last operation.
**Parameters:** None.

### `history_redo`
Redo the last undone operation.
**Parameters:** None.

### `history_get_current`
Get current undo history state.
**Parameters:** None.

## Exact Signatures

Exact names, parameters, defaults, and returns are defined by `GET /skills/schema` or `unity_skills.get_skill_schema()`, not by this file.
