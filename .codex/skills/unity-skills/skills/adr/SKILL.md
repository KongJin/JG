---
name: unity-adr
description: "Unity ADR helper for tradeoffs, option comparison, design decisions, pros/cons, and architecture choice records."
---

# Unity ADR

Use this when architecture choices may be revisited later or when multiple plausible options exist.

## Output Format

- Decision
- Context
- Options considered
- Chosen option
- Why this option won
- Consequences
- Revisit triggers

## Example Use Cases

- Coroutine vs UniTask
- Direct reference vs event-driven communication
- ScriptableObject config vs in-scene authoring
- One assembly vs multiple `asmdef`
- Runtime logic in `MonoBehaviour` vs pure C# service

## Guardrails

**Mode**: Both (Semi-Auto + Full-Auto) — advisory only, no REST skills

- Keep ADRs short.
- Record only decisions that materially affect code generation or architecture direction.
