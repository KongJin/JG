# Skill Routing Registry

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: repo-local skill entries and repo-imported skill route names
> upstream: docs.index, ops.document-management-workflow
> artifacts: none

이 문서는 repo 문서와 repo-local skill이 의존하는 skill route 이름만 등록한다.
skill 본문이나 정책 판단은 여기서 재서술하지 않는다.
Prompt-signal별 기대 trigger coverage는 `ops.skill-trigger-matrix`가 소유한다.

Repo-local skills are stored in this repo under `.codex/skills/*/SKILL.md`, excluding `.codex/skills/.system`.
`.codex/skills/*/agents/*.yaml` and `evals/*.json` are skill interface/eval support metadata; route and trigger expectations follow this registry and `ops.skill-trigger-matrix`.
Global skills from the user Codex home were imported into `.codex/skills/` on 2026-05-05 so repo-facing skill bodies are now git-trackable from the repo.
The imported skill inventory and retention policy live in [`.codex/skills/IMPORT_MANIFEST.md`](../../../.codex/skills/IMPORT_MANIFEST.md).
현재 owner 판단은 항상 `docs/index.md`와 해당 owner 문서를 우선한다.
The user Codex home copy may still exist as an upstream installation source, but JG repo behavior should prefer the repo-imported skill body when both exist.

## JG Repo Skills

| Skill | Repo owner route |
|---|---|
| `jg-coding-guardrails` | `ops.codex-coding-guardrails` |
| `jg-coupling-review` | `ops.cohesion-coupling-policy` |
| `jg-doc-lifecycle` | `ops.document-management-workflow` |
| `jg-forward-rule-capture` | `ops.codex-coding-guardrails`, `ops.document-management-workflow`, `ops.skill-trigger-matrix` |
| `jg-game-design` | active `design/*` owner docs |
| `jg-issue-investigation` | `ops.acceptance-reporting-guardrails` |
| `jg-no-silent-fallback` | `ops.codex-coding-guardrails` |
| `jg-stitch-workflow` | Stitch owner docs through `docs.index` |
| `jg-stitch-unity-import` | Stitch and Unity UI owner docs through `docs.index` |
| `jg-unity-workflow` | Unity owner docs through `docs.index` |

## Imported Rule Skills

| Skill | Repo-facing use | Repo owner fallback |
|---|---|---|
| `rule-architecture` | Generic architecture, layer, port, Bootstrap, folder structure, and dependency-direction guidance | `ops.codex-coding-guardrails`, `ops.cohesion-coupling-policy`, relevant code owner docs |
| `rule-context` | Generic project context, team/release, Firebase, WebGL, automation, and product strategy background guidance | `plans.progress`, `ops.firebase-hosting`; product/design judgments route to `design.game-design` |
| `rule-operations` | Plan Mode, SSOT, operations, stale trace, closeout routing | `ops.document-management-workflow`, `ops.acceptance-reporting-guardrails` |
| `rule-patterns` | Generic prohibited pattern, event chain, exception/ErrorCode, and logging guidance | `ops.codex-coding-guardrails`, `ops.cohesion-coupling-policy` |
| `rule-plan-authoring` | Plan document authoring and rereview companion when available | `ops.plan-authoring-review-workflow` |
| `rule-unity` | Generic Unity serialization, meta GUID, Unity API, MCP/CLI mechanics, compile/build diagnostics fallback | `skill.jg-unity-workflow`, Unity owner docs |
| `rule-validation` | Generic Clean Levels, static/compile/runtime/WebGL validation guidance | `ops.acceptance-reporting-guardrails`, `playtest.runtime-validation-checklist`, `playtest.webgl-smoke-checklist` |

## Imported Unity Skills

| Skill | Repo-facing use | Repo owner fallback |
|---|---|---|
| `unity-uitoolkit` | Unity UI Toolkit UXML, USS, UIDocument, PanelSettings, VisualElement, ScrollView, runtime UI, and EditorWindow UI work | `skill.jg-unity-workflow`, `ops.unity-ui-authoring-workflow`, `design.ui-foundations` |

## Imported Utility Skills

The complete imported utility skill inventory belongs to [`.codex/skills/IMPORT_MANIFEST.md`](../../../.codex/skills/IMPORT_MANIFEST.md).
This registry lists a utility skill only when repo docs or JG owner routing depend on a repo-facing route name.

## Registry Rule

- Repo docs and repo-local skills may mention imported `rule-*` skills only when the skill name is registered above.
- Importing a general-purpose skill does not make its body a JG repo SSOT.
- Full imported skill inventory and bundle retention belong to `.codex/skills/IMPORT_MANIFEST.md`, not this registry.
- Skill trigger prompt fixtures belong in `ops.skill-trigger-matrix`, not this registry.
- If an imported skill trigger or body changes, update this registry only when the repo-facing route name or use changes, then leave `skill trigger checked` in the closeout.
