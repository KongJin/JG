---
name: unity-asmdef
description: "Unity asmdef advisor for module boundaries, faster compilation, dependency cleanup, and editor/runtime/test separation."
---

# Unity asmdef Advisor

Use this skill when the project is large enough that compile boundaries and dependency direction matter.

## Recommend Only When Worth It

`asmdef` is usually worth discussing when:

- the project has multiple domains/systems
- editor code and runtime code are mixed
- compile times are becoming noticeable
- tests should be isolated cleanly

## Output Format

- Whether `asmdef` is justified now
- Proposed assemblies
- Allowed dependency direction
- Editor/runtime/test split
- Migration steps
- Risks or churn to avoid

## Default Guidance

- Prefer a few meaningful assemblies over many tiny ones.
- Split editor code from runtime first.
- Keep the dependency graph directional and shallow.

## Guardrails

**Mode**: Both (Semi-Auto + Full-Auto) — advisory only, no REST skills

- Do not introduce `asmdef` fragmentation for a tiny prototype.
- Do not create circular dependencies or force everything through a shared dumping-ground assembly.
