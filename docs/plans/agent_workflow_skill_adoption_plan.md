# Agent Workflow Skill Adoption Plan

> 마지막 업데이트: 2026-05-01
> 상태: reference
> doc_id: plans.agent-workflow-skill-adoption
> role: plan
> owner_scope: Matt Pocock skills audit, JG owner mapping, one-at-a-time adoption route
> upstream: docs.index, ops.document-management-workflow, ops.cohesion-coupling-policy, ops.codex-coding-guardrails, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails
> artifacts: none

이 문서는 외부 agent skill workflow를 JG 문서/skill 체계로 흡수할 때의 audit와 채택 순서만 소유한다.
실제 규칙 본문은 기존 owner 문서와 repo-local skill이 소유하며, 이 문서는 upstream skill 본문을 복제하지 않는다.

## Source Snapshot

- Source: `https://github.com/mattpocock/skills`
- Snapshot checked: `b843cb5ea74b1fe5e58a0fc23cddef9e66076fb8`
- License observed: MIT
- Reviewed scope: README reference skills under `skills/engineering`, `skills/productivity`, and `skills/misc`
- Excluded scope: `skills/deprecated` and `skills/personal`

## Adoption Rule

- 외부 skill을 설치하거나 그대로 복사하지 않는다.
- 좋은 행동 단위만 JG의 existing owner 문서나 repo-local skill entry에 재작성해 흡수한다.
- 한 patch는 한 candidate와 한 primary owner만 바꾼다.
- `CONTEXT.md`, `docs/adr/`, `docs/agents/`를 새 표준으로 만들지 않는다. JG는 `docs/index.md`, `design/*`, `ops/*`, `plans/*`, stable `doc_id`를 공식 owner 체계로 쓴다.
- 문서/skill/rule 변경 후에는 `npm run --silent rules:lint`를 기본 검증으로 둔다.

## Candidate Map

| Upstream skill | JG fit | Adoption target | Notes |
|---|---|---|---|
| `grill-me` | high | `docs/ops/codex_coding_guardrails.md`, `docs/ops/document_management_workflow.md` | 구현/문서 변경 전 모호한 결정을 질문으로 잠그는 loop만 흡수한다. 코드에서 확인 가능한 질문은 먼저 탐색한다는 원칙이 JG `Assumption Handling`과 잘 맞는다. |
| `grill-with-docs` | high | `docs/ops/codex_coding_guardrails.md`, `docs/ops/document_management_workflow.md`, relevant `design/*` owner | `CONTEXT.md`/ADR 생성 방식은 그대로 쓰지 않는다. 대신 용어 확정은 현재 design owner로, 되돌리기 어려운 결정은 적절한 ops/design/plan owner로 라우팅한다. |
| `diagnose` | high | `.codex/skills/jg-issue-investigation/SKILL.md`, `docs/ops/acceptance_reporting_guardrails.md` | 이미 가설 검증 loop는 존재한다. 추가 흡수 후보는 fast feedback loop, repro fidelity, targeted instrumentation, debug cleanup, regression seam 판정이다. |
| `tdd` | high | `docs/ops/codex_coding_guardrails.md`, relevant validation/test owner | vertical red-green-refactor와 behavior-first test 기준을 흡수한다. JG에서는 Unity direct/EditMode/smoke 기준과 함께 owner별 검증으로 번역해야 한다. |
| `zoom-out` | medium | `.codex/skills/jg-coupling-review/SKILL.md`, `docs/ops/cohesion_coupling_policy.md` | unfamiliar code/doc area에서 바로 수정하지 않고 caller/module/owner map을 먼저 만드는 습관만 흡수한다. |
| `improve-codebase-architecture` | medium | `.codex/skills/jg-coupling-review/SKILL.md`, `docs/ops/cohesion_coupling_policy.md` | deletion test, interface-as-test-surface, deep module vocabulary는 유용하다. 단, 일반 score나 전역 hard-fail로 만들지 않고 review gate로만 둔다. |
| `to-issues` | medium | `docs/ops/plan_authoring_review_workflow.md` | GitHub issue 생성은 보류한다. thin vertical slice, HITL/AFK 구분, dependency order 개념만 JG plan/TODO 작성법으로 번역 가능하다. |
| `to-prd` | low | `docs/ops/plan_authoring_review_workflow.md`, `docs/design/game_design.md` when product scope is involved | 현재 대화와 repo 탐색으로 PRD를 합성하는 방식은 유용하지만, JG에서는 issue tracker가 아니라 owner plan 또는 design owner로 남긴다. |
| `triage` | partial | `docs/ops/plan_authoring_review_workflow.md` | 요청 category와 next action을 고르는 triage-lite만 흡수한다. GitHub issue/label/comment state machine은 공식 current path가 아니므로 계속 보류한다. |
| `setup-matt-pocock-skills` | no direct adoption | `AGENTS.md`, `docs/index.md` already cover route setup | JG는 이미 entry/registry/owner route가 있다. `docs/agents/*`를 새 SSOT로 추가하지 않는다. |
| `write-a-skill` | no direct adoption | global `skill-creator` skill | 현재 설치된 skill creator가 더 넓은 workflow를 소유한다. |
| `caveman` | no adoption | none | 개인 커뮤니케이션 모드이며 repo policy로 둘 내용이 아니다. |
| `git-guardrails-claude-code` | no direct adoption | none | Claude Code hook 전용이다. JG는 현재 Codex 운영 규칙과 `.githooks/pre-commit` rules lint가 별도 역할을 가진다. |
| `setup-pre-commit` | no direct adoption | none | repo에는 이미 `.githooks/pre-commit`과 `npm run --silent rules:lint` route가 있다. Husky/lint-staged는 현재 JG의 Unity 중심 workflow와 맞지 않는다. |
| `migrate-to-shoehorn` | no adoption | none | TypeScript test fixture 전용이다. JG의 Unity/C# 주 작업과 맞지 않는다. |
| `scaffold-exercises` | no adoption | none | course exercise scaffold 전용이다. |

## Recommended Order

1. `grill-me` / `grill-with-docs` behavior adoption.
   - Primary owner: `ops.codex-coding-guardrails`
   - Secondary owner: `ops.document-management-workflow` only if document mutation questions need explicit route wording
   - Goal: mutation 전에 hard-to-reverse ambiguity를 잠그되, repo 탐색으로 답할 수 있는 질문은 묻지 않는 rule을 선명하게 한다.

2. `diagnose` feedback-loop adoption.
   - Primary owner: `skill.jg-issue-investigation`
   - Secondary owner: `ops.acceptance-reporting-guardrails` only if root-cause reporting body needs new wording
   - Goal: 재현 loop, minimised repro, targeted instrumentation, cleanup checklist를 JG investigation route에 녹인다.

3. `tdd` vertical-slice adoption.
   - Primary owner: `ops.codex-coding-guardrails`
   - Secondary owner: lane-specific validation docs only when a concrete lane needs examples
   - Goal: all-tests-first/horizontal slicing을 피하고, one behavior -> one failing check -> minimal implementation -> refactor 순서로 구현한다.

4. `zoom-out` / architecture review adoption.
   - Primary owner: `skill.jg-coupling-review`
   - Secondary owner: `ops.cohesion-coupling-policy`
   - Goal: unfamiliar area에서 caller/owner map을 먼저 만들고, deletion test와 interface-as-test-surface를 review vocabulary로 추가한다.

5. `to-issues` / `to-prd` plan slicing adoption.
   - Primary owner: `ops.plan-authoring-review-workflow`
   - Secondary owner: `plans.progress` only if current priority changes
   - Goal: GitHub issue publish 대신 JG plan/TODO를 vertical slice와 dependency order로 나누는 guidance만 흡수한다.

## Adoption Closeout

| Candidate | Applied owner | Closeout |
|---|---|---|
| `grill-me` / `grill-with-docs` | `ops.codex-coding-guardrails` | `Clarification Loop`로 흡수. 질문 전에 repo evidence를 탐색하고, hard-to-reverse ambiguity만 질문으로 잠근다. |
| `diagnose` | `skill.jg-issue-investigation` | feedback loop, repro fidelity, targeted instrumentation, cleanup, regression seam 판단을 investigation route에 흡수. |
| `tdd` | `ops.codex-coding-guardrails` | behavior-first test loop와 one-behavior `RED -> GREEN -> refactor` 기준으로 흡수. |
| `zoom-out` / `improve-codebase-architecture` | `skill.jg-coupling-review` | unfamiliar area caller/owner map, deletion test, interface-as-test-surface를 review probe로 흡수. |
| `to-issues` / `to-prd` | `ops.plan-authoring-review-workflow` | issue publish 없이 vertical slice, HITL/AFK, dependency order, short PRD synthesis 기준만 흡수. |
| `triage` | `ops.plan-authoring-review-workflow` | issue tracker 없이 request category와 next action을 고르는 triage-lite routing rubric으로 부분 흡수. |

## Residual / Deferred

- `triage`: partial. JG triage-lite는 흡수했고, GitHub issue label/comment/close state machine은 공식 issue tracker workflow가 정해지기 전까지 deferred로 둔다.
- `setup-matt-pocock-skills`: no adoption. JG는 `AGENTS.md`, `docs/index.md`, stable `doc_id` owner route를 이미 사용한다.
- `write-a-skill`: no adoption. 현재 설치된 `skill-creator`가 더 넓은 skill authoring workflow를 소유한다.
- `caveman`, `git-guardrails-claude-code`, `setup-pre-commit`, `migrate-to-shoehorn`, `scaffold-exercises`: no adoption. JG current workflow나 Unity/C# 중심 작업과 직접 맞지 않는다.
- 다음 개선은 새 upstream skill 흡수보다 실제 작업에서 위 루프가 과소/과잉 발동하는지 관찰한 뒤 해당 owner 문서나 repo-local skill을 좁게 조정한다.

## Acceptance For This Audit

- README-listed non-deprecated engineering/productivity/misc skills are classified.
- Each adoptable candidate has a JG primary owner candidate.
- Adoptable candidates 1-5 and the follow-up `triage` routing subset have been absorbed into their target JG owners.
- Deferred and rejected skills have a reason and no active owner mutation pending.
- Future work can start from the target owner docs/skills without rereading the upstream repo.

## Validation

- `npm run --silent rules:sync-closeout`
- `npm run --silent rules:lint`

owner impact:

- primary: `plans.agent-workflow-skill-adoption` for audit closeout and residual status; `ops.plan-authoring-review-workflow` for the follow-up triage-lite behavior.
- secondary: `ops.codex-coding-guardrails`, `skill.jg-issue-investigation`, `skill.jg-coupling-review`, `artifacts/rules/issue-recurrence-closeout.json`
- out-of-scope: issue tracker setup, upstream skill installation, `CONTEXT.md`/ADR/docs-agents structure adoption

doc lifecycle checked:

- reference 유지. 이 문서는 audit closeout과 residual/deferred 판단만 보존한다. 다음 개선은 해당 primary owner 문서나 repo-local skill을 직접 바꾸고, 이 문서는 stale residual status를 막을 때만 짧게 맞춘다.
- plan rereview: clean
