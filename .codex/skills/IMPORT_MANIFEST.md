# Skill Import Manifest

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: skill.import-manifest
> role: reference
> owner_scope: repo-imported Codex skill inventory, source provenance, retention policy
> upstream: docs.index, ops.document-management-workflow, ops.skill-routing-registry
> artifacts: none

This manifest records the non-system Codex skills imported into this repo on 2026-05-05.
The repo copy is the JG-facing working copy for repo behavior; do not edit the user Codex home copy for JG-specific routing or policy.

## Import Source

| Field | Value |
|---|---|
| Source kind | User Codex home skills directory |
| Imported root | `.codex/skills/` |
| Excluded | `.codex/skills/.system/**` |
| Imported skill dirs | 22 |
| Imported files | 483 |
| Imported bytes | 11024749 |

## Imported Skills

| Skill | Entry |
|---|---|
| `brand-guidelines` | [SKILL.md](brand-guidelines/SKILL.md) |
| `canvas-design` | [SKILL.md](canvas-design/SKILL.md) |
| `claude-api` | [SKILL.md](claude-api/SKILL.md) |
| `docx` | [SKILL.md](docx/SKILL.md) |
| `mcp-builder` | [SKILL.md](mcp-builder/SKILL.md) |
| `pdf` | [SKILL.md](pdf/SKILL.md) |
| `pptx` | [SKILL.md](pptx/SKILL.md) |
| `rule-architecture` | [SKILL.md](rule-architecture/SKILL.md) |
| `rule-context` | [SKILL.md](rule-context/SKILL.md) |
| `rule-operations` | [SKILL.md](rule-operations/SKILL.md) |
| `rule-patterns` | [SKILL.md](rule-patterns/SKILL.md) |
| `rule-plan-authoring` | [SKILL.md](rule-plan-authoring/SKILL.md) |
| `rule-unity` | [SKILL.md](rule-unity/SKILL.md) |
| `rule-validation` | [SKILL.md](rule-validation/SKILL.md) |
| `skill-creator` | [SKILL.md](skill-creator/SKILL.md) |
| `theme-factory` | [SKILL.md](theme-factory/SKILL.md) |
| `unity-editor-integration` | [SKILL.md](unity-editor-integration/SKILL.md) |
| `unity-skills` | [SKILL.md](unity-skills/SKILL.md) |
| `unity-ui` | [SKILL.md](unity-ui/SKILL.md) |
| `web-artifacts-builder` | [SKILL.md](web-artifacts-builder/SKILL.md) |
| `webapp-testing` | [SKILL.md](webapp-testing/SKILL.md) |
| `xlsx` | [SKILL.md](xlsx/SKILL.md) |

## Retention

- Keep `SKILL.md` files and markdown references under docs-lint management.
- Keep bundled scripts, assets, templates, and binary resources when a skill body references them or when they are part of the upstream skill package.
- Treat large bundled references as imported reference material, not JG owner policy.
- Re-import only with an explicit skill lifecycle task, then update this manifest, `ops.skill-routing-registry`, and `ops.skill-trigger-matrix` if the repo-facing route or trigger surface changes.
- Leave `.system` skills outside repo import; system skills stay environment-managed.

## Environment Cache Scope

- `.codex/.tmp/**` is environment cache, not JG repo-facing skill inventory.
- `.codex/.tmp/plugins/` contains upstream plugin bundles installed by the user Codex environment.
- JG repo behavior depends only on `.codex/skills/` (repo-imported copy) and repo-local `jg-*` skills.
- Do not lint, validate, or reference `.codex/.tmp/**` as JG owner policy.
- If a plugin skill becomes JG repo-facing, import it to `.codex/skills/` and register it here.
