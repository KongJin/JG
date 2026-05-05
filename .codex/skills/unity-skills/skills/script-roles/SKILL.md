---
name: unity-script-roles
description: "Plan Unity script responsibilities: MonoBehaviour vs ScriptableObject vs pure C# service/installer, presenters, and class roles."
---

# Unity Script Roles

Use this skill before creating a batch of gameplay scripts.

## Goal

Turn a rough script list into explicit roles so AI does not generate everything as `MonoBehaviour`.

## Output Format

- Script name
- Recommended role
- Main responsibility
- Main dependencies
- Why this role fits better than the alternatives

## Common Roles

- `MonoBehaviour` bridge
- `ScriptableObject` config/data
- pure C# domain/service
- presenter / controller
- state / state machine node
- installer / bootstrap helper

## Guardrails

**Mode**: Both (Semi-Auto + Full-Auto) — advisory only, no REST skills

- Do not make every class a `MonoBehaviour`.
- Do not force `ScriptableObject` onto runtime state that should stay in memory-only objects.
