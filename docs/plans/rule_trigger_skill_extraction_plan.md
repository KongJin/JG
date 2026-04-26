# Rule Trigger Skill Extraction Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.rule-trigger-skill-extraction
> role: plan
> owner_scope: 문서 규칙 중 행동 트리거가 필요한 내용을 skill로 분리하고, 이후 규칙 추가 시 재발을 막는 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `.codex/skills/rule-*/`, `artifacts/rules/`

## 목적

문서에는 규칙이 있지만 Codex가 해당 문서를 열지 않으면 규칙이 발동되지 않는 문제를 줄이기 위해 진행한 계획 기록이다.

이 계획은 규칙을 문서에 추가할 때 그 규칙이 `행동 트리거`인지 함께 판정하고, 트리거가 필요하면 repo/user skill의 description 또는 body에 라우팅을 추가하는 흐름을 정리했다.

## 현재 상태

- `rule-plan-authoring` skill을 새로 분리했다.
- 기존 `rule-operations` skill에는 계획 문서 작성/수정 작업을 `rule-plan-authoring`으로 라우팅하는 힌트를 추가했다.
- 문서 전체에서 skill로 분리해야 할 다른 행동 트리거 1차 목록은 `artifacts/rules/skill-trigger-inventory.md`에 작성했다.
- Phase 0~3은 완료됐고, 이 문서는 현재 실행 owner가 아니라 이후 skill trigger 자동화 검토를 위한 reference다.
- 아직 docs lint가 "새 행동 규칙 추가 시 skill trigger 검토 여부"를 자동으로 강제하지는 않는다.
- 2026-04-25 self-audit: 이 계획을 만들던 직후 보고에서 `skill trigger checked: ...` closeout 문구를 실제로 남기지 않았다. 따라서 이 계획 자체가 다루는 재발 사례가 즉시 재발했다.

## 범위

포함:

- 문서 규칙 중 skill trigger가 필요한 항목 식별
- 필요한 경우 `rule-*` skill 분리 또는 description 보강
- 문서 작성/수정 closeout에 "skill trigger 검토" 항목 추가
- 가능한 경우 docs lint나 rule harness로 누락을 감지하는 자동화 추가

제외:

- 모든 문서 내용을 skill로 복제하는 작업
- owner 문서의 규칙 본문을 skill에 장문으로 중복 유지하는 작업
- 제품 판단, 아키텍처 결정, Unity 직렬화 규칙 자체를 plan 문서가 새로 정의하는 작업

## 분리 기준

skill로 분리하거나 description에 넣을 내용:

- 특정 사용자 표현에서 반드시 발동해야 하는 절차
- 문서가 열리지 않으면 자주 빠지는 closeout/review/validation 루프
- 파일 경로 패턴과 함께 작동해야 하는 작업 진입 규칙
- 기존 넓은 skill 안에서 묻히는 반복 행동 규칙

문서에만 둘 내용:

- 배경 설명
- 자세한 owner/scope/upstream 정의
- 예외와 판단 근거
- 긴 체크리스트나 reference
- 현재 상태와 계획 상세

## 실행 단계

### Phase 0: Plan Authoring 분리

상태: 완료

작업:

- `rule-plan-authoring` skill 생성
- `rule-operations` skill에 계획 문서 라우팅 힌트 추가

Acceptance:

- 계획 문서 작성/수정, `docs/plans`, Phase/Acceptance/TODO, 과한점/부족한점 재리뷰 요청에 반응하는 skill description이 존재한다.
- skill body가 `docs/ops/plan_authoring_review_workflow.md`를 owner 문서로 읽게 한다.
- skill이 owner 문서 본문을 장문으로 복제하지 않고 실행 루프만 요약한다.

### Phase 1: 문서 트리거 인벤토리

상태: 완료

작업:

- `docs/ops`, `docs/plans`, `docs/design`, `AGENTS.md`, `.codex/skills/rule-*`를 훑어 행동 트리거 후보를 목록화한다.
- 후보를 `이미 skill 있음`, `description 보강 필요`, `새 skill 필요`, `문서만 유지`로 분류한다.

산출물:

- `artifacts/rules/skill-trigger-inventory.md`

Acceptance:

- 각 후보에 owner 문서, 트리거 문구, 현재 skill coverage, 권장 조치가 기록된다.
- 단순 규칙 본문과 실제 행동 트리거가 분리된다.

### Phase 2: Skill Coverage 보강

상태: 완료

작업:

- 누락된 트리거는 기존 `rule-*` skill description에 추가하거나 새 skill로 분리한다.
- 넓은 skill에 과하게 몰린 규칙은 더 좁은 skill로 나눈다.

Acceptance:

- 각 active 행동 트리거는 최소 하나의 skill description에서 직접 언급된다.
- skill body는 owner 문서를 읽는 순서와 실행 루프만 가진다.
- 같은 규칙 본문이 문서와 skill에 장문 중복되지 않는다.

Phase 2 실행 표:

| inventory 후보 | 판정 | 대상 skill | description 보강 방향 | 새 skill 여부 |
|---|---|---|---|---|
| Reporting closeout / blocked / mismatch / success | description 보강 | `rule-operations` | `acceptance`, `blocked`, `mismatch`, `success`, `residual`, `closeout 보고`를 직접 트리거 문구에 추가 | 보류. 반복 미스가 계속될 때만 `rule-acceptance-reporting` 검토 |
| Stitch data and translation workflow | router skill 확인 | `jg-stitch-workflow` -> `jg-unity-workflow` | Stitch-side source freeze, prompt brief, contract review는 `jg-stitch-workflow`가 라우팅하고 Unity 구현 handoff는 `jg-unity-workflow`로 넘긴다 | 새 rule skill 만들지 않음 |
| Unity UI authoring policy | description 보강 | `jg-unity-workflow` | `Unity UI authoring policy`, `new UI prefab`, `presentation ownership`, `workflow policy check`를 trigger에 추가 | 새 skill 만들지 않음 |
| Skill trigger coverage for new rules | owner 승격 전 interim 유지 | `rule-plan-authoring`, `rule-operations` | `새 규칙`, `행동 트리거`, `skill trigger checked`, `규칙 추가`를 trigger에 추가 | 새 skill 만들지 않음 |

Phase 2 실행 순서:

1. 위 네 항목만 대상으로 skill description을 보강한다.
2. skill body에는 owner 문서 본문을 장문 복제하지 않고 read order와 라우팅 힌트만 둔다.
3. 보강 후 `artifacts/rules/skill-trigger-inventory.md`의 current coverage/recommended action을 갱신한다.
4. 새 skill은 만들지 않는다. 단, 보강 후에도 같은 miss가 재발하면 해당 후보를 `new-skill-candidate`로 승격한다.
5. `rule_trigger_skill_extraction_plan.md`에 Phase 2 상태와 재리뷰 결과를 남긴다.

### Phase 3: 문서 작성 Closeout 보강

상태: 완료

작업:

- 문서 운영 closeout 또는 plan authoring workflow에 "새 행동 규칙 추가 시 skill trigger 검토" 항목을 추가한다.
- 규칙 추가 작업의 보고에 `skill trigger checked` 또는 `not needed - <reason>`을 남기는 방식을 정한다.
- 사용자 지시가 기존 규칙과 충돌하거나 과하거나 부족해 보일 때 질문 또는 대안 제안을 먼저 하도록 owner 문서에 반영한다.

Acceptance:

- 새 규칙을 문서에 추가할 때 skill trigger 검토가 closeout 기준에 포함된다.
- skill trigger가 필요 없을 때도 이유를 남길 수 있다.
- 충돌/과범위/부족범위 지시는 `ops.document-management-workflow`와 `ops.plan-authoring-review-workflow`에서 closeout 전 점검 대상으로 확인된다.

### Phase 4: 자동화 검토

상태: 대기

작업:

- docs lint 또는 rule harness로 감지할 수 있는 신호를 찾는다.
- 예: `must`, `항상`, `반드시`, `closeout`, `검증`, `Plan`, `docs/plans` 같은 문구가 active owner 문서에 추가됐는데 skill coverage가 없으면 warning을 낸다.

Acceptance:

- 자동화가 가능한 최소 warning 규칙이 정의된다.
- false positive가 크면 hard fail이 아니라 advisory artifact로 시작한다.

## 검증 루프

- skill 변경 후 skill frontmatter의 `name`과 `description`을 확인한다.
- 문서 변경 후 `npm run --silent rules:lint`를 실행한다.
- rules-only scope를 건드렸고 owner 문서가 요구하면 `npm run --silent rules:sync-closeout` 필요 여부를 확인한다.
- 새 skill은 다음 세션부터 trigger metadata에 잡히는지 확인한다.

## 리스크와 처리

| 리스크 | 처리 |
|---|---|
| skill이 owner 문서 본문을 복제해 이중 진실이 됨 | skill은 trigger와 read order, 실행 루프만 소유한다 |
| skill이 너무 많아져 선택이 어려워짐 | 넓은 domain은 유지하되 반복 행동 루프만 분리한다 |
| description이 약해서 여전히 발동하지 않음 | 사용자 표현, 파일 경로, 작업 타입을 description에 직접 넣는다 |
| 자동화가 false positive를 많이 냄 | hard fail 전에 advisory inventory로 시작한다 |

## 첫 실행 순서

1. `rule-plan-authoring`이 다음 계획 작업에서 발동되는지 확인한다.
2. `artifacts/rules/skill-trigger-inventory.md`를 만든다.
3. 누락된 trigger를 `description 보강`과 `새 skill`로 나눈다.
4. 문서 closeout에 skill trigger 검토 항목을 추가한다.
5. advisory lint 가능성을 검토한다.

## Closeout 문구 기록

Phase 2~3 실행 중에는 새 규칙을 active owner 문서에 추가하거나 실질 수정하는 작업에서 아래 문구 중 하나를 남기는 방식을 목표로 했다.
현재 이 문서는 reference이며, 실제 trigger 라우팅은 `rule-operations`와 `rule-plan-authoring` skill description이 맡는다.

- `skill trigger checked: covered by <skill-name>`
- `skill trigger checked: added to <skill-name>`
- `skill trigger checked: not needed - <reason>`
- `skill trigger checked: residual - <reason>`

Historical interim:

- Phase 3에서 owner 문서에 반영하기 전에는, 규칙/skill/문서 workflow를 추가하거나 실질 수정하는 보고에 위 문구 중 하나를 남기도록 운용했다.
- 당시 이 문구를 못 남겼다면 closeout을 완료로 보지 않고 `residual` 또는 `blocked`로 정정했다.
- 이 내용은 현재 기준을 새로 소유하지 않고, 당시 재발 방지 실험 기록으로만 남긴다.

## Plan Rereview

- 2026-04-25 1차 리뷰: 초안 기준 부족한점 있음. Phase별 acceptance와 제외 범위는 있으나 closeout 강제 문구가 실행 보고까지 닿는지 약하다.
- 2026-04-25 1차 반영: 목표 closeout 문구를 추가해, 새 규칙 추가 시 skill trigger 검토 결과를 남기는 방향을 명확히 했다.
- 2026-04-25 2차 리뷰: plan rereview: clean. 과한점/부족한점 없음.
- 2026-04-25 self-audit 리뷰: 부족한점 발견. 목표 closeout 문구를 "나중에 강제"로 둔 탓에 바로 다음 보고에서 적용되지 않았다.
- 2026-04-25 self-audit 반영: Phase 3 전이라도 적용할 interim rule을 추가했다.
- 2026-04-25 self-audit 재리뷰: plan rereview: residual - skill metadata는 현재 세션의 available skills 목록에 즉시 재주입되지 않을 수 있다. 다음 세션부터 자동 trigger 확인이 필요하다.
- 2026-04-25 Phase 1 리뷰: 부족한점 발견. 인벤토리 없이 다음 skill 분리로 가면 skill이 과증식할 위험이 있었다.
- 2026-04-25 Phase 1 반영: `artifacts/rules/skill-trigger-inventory.md`를 작성하고 후보를 covered/description-needs-review/new-skill-candidate/docs-only로 분류했다.
- 2026-04-25 Phase 1 재리뷰: plan rereview: residual - inventory는 1차 수동 분류라 Phase 4 advisory lint 설계 전까지 machine-checkable coverage는 아니다.
- 2026-04-25 Phase 2 계획 리뷰: 부족한점 발견. "description 보강"이 추상적이라 어떤 skill에 어떤 trigger를 추가할지 실행 가능하지 않았다.
- 2026-04-25 Phase 2 계획 반영: description-needs-review 네 항목을 대상으로 한 실행 표와 순서를 추가했다.
- 2026-04-25 Phase 2 계획 재리뷰: plan rereview: residual - 실제 skill description 보강은 아직 실행 전이며, 이 문서는 다음 mutation step의 범위만 고정한다.
- 2026-04-25 Phase 2 실행 리뷰: 과한점 발견. 새 skill을 더 만들면 과증식이므로 description 보강만 진행하는 것이 맞다. 부족한점은 `description-needs-review` 4개가 아직 covered로 갱신되지 않은 점이었다.
- 2026-04-25 Phase 2 실행 반영: `rule-operations`, `rule-plan-authoring`, `jg-unity-workflow` description을 보강하고 inventory coverage를 갱신했다.
- 2026-04-25 Phase 2 실행 재리뷰: plan rereview: residual - skill 파일은 보강됐지만 현재 세션의 available skill metadata에는 즉시 반영되지 않을 수 있어 다음 세션 확인이 필요하다.
- 2026-04-26 skills 관리 점검 반영: Stitch coverage 기록을 현재 repo-local router인 `jg-stitch-workflow` 기준으로 정정했다. Unity 구현 handoff는 계속 `jg-unity-workflow`가 맡는다.
- 2026-04-26 skills 관리 재리뷰: plan rereview: clean. 새 skill이나 새 hard-fail lint를 만들지 않고 stale coverage 기록만 수정했다. 현재 세션 available skill metadata에서 `jg-stitch-workflow` 노출 확인은 residual로 남긴다.
- 2026-04-25 Phase 3 실행 리뷰: 부족한점 발견. 새 규칙 closeout만 다루면 사용자의 충돌/과범위/부족범위 지시를 즉시 제안하는 행동이 빠진다.
- 2026-04-25 Phase 3 실행 반영: `ops.document-management-workflow`에 Instruction Fit 원칙을 추가하고, `ops.plan-authoring-review-workflow`의 부족한점 체크에 closeout 전 처리 기준을 추가했다.
- 2026-04-25 Phase 3 실행 재리뷰: plan rereview: clean. 과한점/부족한점 없음.

## Skill Trigger Checked

- `skill trigger checked: added to rule-operations, rule-plan-authoring, jg-unity-workflow`
- `skill trigger checked: added to rule-operations, rule-plan-authoring`
- `skill trigger checked: corrected Stitch coverage to jg-stitch-workflow`
