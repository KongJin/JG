# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: repo-local skill entries and repo-facing external/global skill route names
> upstream: docs.index, ops.document-management-workflow
> artifacts: none

이 문서는 repo 문서와 repo-local skill이 의존하는 skill route 이름만 등록한다.
skill 본문이나 정책 판단은 여기서 재서술하지 않는다.
Prompt-signal별 기대 trigger coverage는 `ops.skill-trigger-matrix`가 소유한다.

Repo-local `jg-*` skill은 이 repo 안의 `.codex/skills/jg-*/SKILL.md`가 실제 entrypoint다.
External/global `rule-*` skill은 사용자 Codex home에 설치된 skill이며 repo 밖에 있으므로, 이 문서는 repo에서 참조하는 이름과 용도만 고정한다.
현재 owner 판단은 항상 `docs/index.md`와 해당 owner 문서를 우선한다.
External/global skill body나 description을 직접 수정하면 repo git/lint가 그 파일 변경을 추적하지 못한다. 그런 변경은 closeout에서 absolute path와 검증 한계를 별도로 보고한다.

## Repo-Local Skills

| Skill | Repo owner route |
|---|---|
| `jg-coding-guardrails` | `ops.codex-coding-guardrails` |
| `jg-coupling-review` | `ops.cohesion-coupling-policy` |
| `jg-doc-lifecycle` | `ops.document-management-workflow` |
| `jg-game-design` | active `design/*` owner docs |
| `jg-issue-investigation` | `ops.acceptance-reporting-guardrails` |
| `jg-stitch-workflow` | Stitch owner docs through `docs.index` |
| `jg-stitch-unity-import` | Stitch and Unity UI owner docs through `docs.index` |
| `jg-unity-workflow` | Unity owner docs through `docs.index` |

## External / Global Rule Skills

| Skill | Repo-facing use | Repo owner fallback |
|---|---|---|
| `rule-architecture` | Generic architecture, layer, port, Bootstrap, folder structure, and dependency-direction guidance | `ops.codex-coding-guardrails`, `ops.cohesion-coupling-policy`, relevant code owner docs |
| `rule-context` | Generic project context, team/release, Firebase, WebGL, automation, and product-strategy guidance | `plans.progress`, `ops.firebase-hosting`, `design.game-design` |
| `rule-operations` | Plan Mode, SSOT, operations, stale trace, closeout routing | `ops.document-management-workflow`, `ops.acceptance-reporting-guardrails` |
| `rule-patterns` | Generic prohibited pattern, event chain, exception/ErrorCode, and logging guidance | `ops.codex-coding-guardrails`, `ops.cohesion-coupling-policy` |
| `rule-plan-authoring` | Plan document authoring and rereview companion when available | `ops.plan-authoring-review-workflow` |
| `rule-unity` | Generic Unity serialization, meta GUID, prefab, scene, compile, Play Mode guidance | `skill.jg-unity-workflow`, Unity owner docs |
| `rule-validation` | Generic Clean Levels, static/compile/runtime/WebGL validation guidance | `ops.acceptance-reporting-guardrails`, `playtest.runtime-validation-checklist`, `playtest.webgl-smoke-checklist` |

## Registry Rule

- Repo docs and repo-local skills may mention external/global `rule-*` skills only when the skill name is registered above.
- Registering a global skill name does not make its external body a repo SSOT.
- Skill trigger prompt fixtures belong in `ops.skill-trigger-matrix`, not this registry.
- If a global skill trigger or body changes, update this registry only when the repo-facing route name or use changes, then leave `skill trigger checked` and the external file path in the closeout.
