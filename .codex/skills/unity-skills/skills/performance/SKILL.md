---
name: unity-performance
description: "Unity performance advisor for slow code, frame drops, Update hot paths, allocations, GC, pooling, physics, and profiler findings."
---

# Unity Performance Red Flags

Use this skill for a high-signal review of likely Unity performance issues. Focus on red flags, not speculative micro-optimizations.

## Check For

- Too many unrelated `Update` / `LateUpdate` / `FixedUpdate` loops
- Repeated `Find`, `GetComponent`, `Camera.main`, or tag lookups in hot paths
- Frequent `Instantiate` / `Destroy` suitable for pooling
- Avoidable per-frame allocations:
  - LINQ
  - string formatting
  - closures
  - boxing
- Reflection in runtime hot paths
- Expensive editor-only helpers leaking into runtime code
- Physics, animation, or UI updates happening at the wrong cadence

## Output Format

- Confirmed red flags
- Likely red flags
- Changes worth doing now
- Changes not worth doing now
- Expected gain category: clarity / frame time / GC / scalability

## Guardrails

**Mode**: Both (Semi-Auto + Full-Auto) — advisory only, no REST skills

- Do not recommend large refactors without a meaningful hotspot.
- Do not replace simple code with unreadable “optimized” code unless the hot path is real.
