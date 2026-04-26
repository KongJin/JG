# Document Management Process Tuning Plan

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.document-management-process-tuning
> role: plan
> owner_scope: 문서관리 절차의 과한 부담과 부족한 판정 기준을 보정하는 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails
> artifacts: `docs/ops/document_management_workflow.md`, `docs/ops/plan_authoring_review_workflow.md`, `docs/ops/acceptance_reporting_guardrails.md`, `docs/index.md`
>
> 생성일: 2026-04-26
> 근거: 문서관리 방법 리뷰에서 rules-only closeout 부담, active plan lifecycle, plan 재리뷰 기록량, progress/update 경계가 다음 보정 후보로 확인됨

이 문서는 문서관리 규칙을 새로 늘리기보다, 현재 owner 문서의 판단 부담을 줄이고 빠진 판정 기준을 보강하는 실행 계획이다.
규칙 본문은 `ops.document-management-workflow`, `ops.plan-authoring-review-workflow`, `ops.acceptance-reporting-guardrails`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

## Goal

- 작은 문서 작업이 rules-only closeout artifact 부담으로 번지지 않게 경계를 좁힌다.
- active plan과 reference plan의 전환 기준을 더 명확히 한다.
- plan 재리뷰 기록은 필요한 판단만 남기고 반복 로그를 줄인다.
- `progress.md`가 현재 포커스와 다음 작업 SSOT로 남도록 업데이트 상한을 정한다.
- 새 문서를 만들지 않는 판단과 rename/delete 후 stale reference 확인 루틴을 짧게 보강한다.

## Scope

- primary owner: `ops.document-management-workflow`
- secondary owners: `ops.plan-authoring-review-workflow`, `ops.acceptance-reporting-guardrails`
- registry owner: `docs.index`

이 plan이 직접 다루는 것:

- rules-only closeout artifact 필수 범위 재정의
- active plan lifecycle 전환 기준 보강
- plan 재리뷰 기록 압축 기준 보강
- `progress.md` 기록 상한과 changelog/reference 분리 기준 보강
- 새 문서 미생성 판단의 지속 기록 기준 보강
- rename/delete 후 stale reference 검색 루틴 보강

범위 밖:

- 새 hard-fail lint 추가
- 새 필수 artifact 추가
- product, Unity, Stitch lane 진행 상태 변경
- 기존 historical/reference 문서의 대규모 본문 정리
- 전역 skill 파일 직접 수정

## Current Findings

- 작은 문서 작업은 lint와 짧은 요약으로 충분하다고 되어 있지만, rules-only scope의 closeout artifact 조건은 `docs/**` 변경 전체로 넓게 읽힐 수 있다.
- `active`는 현재 작업 기준으로 직접 참고하는 문서인데, residual만 남은 plan과 완료에 가까운 plan이 active로 오래 남을 수 있다.
- plan 문서의 재리뷰 기록이 과한점, 부족한점, 수정 후 재리뷰를 반복하면서 실행 순서보다 운영 로그가 커지는 경향이 있다.
- `progress.md`는 현재 상태와 우선순위 SSOT지만, 상태 주석이 길어지면 dated evidence와 현재 포커스가 섞일 수 있다.
- 새 문서를 만들지 않는 판단은 보통 최종 응답으로 충분하지만, 다시 찾을 필요가 있는 판단과 세션성 판단의 경계가 약하다.
- rename/delete 절차는 있지만, old path, `doc_id`, tool README, active skill 참조를 실제로 검색하는 짧은 루틴이 더 있으면 stale reference를 줄일 수 있다.

## Target Shape

- rules-only closeout artifact는 규칙, policy, lint, repo-local skill, rule-harness처럼 recurrence 예방 자체가 작업 대상일 때 필수로 둔다.
- 단순 문서 추가나 plan 작성은 `rules:lint`와 authoring review로 충분하게 하고, closeout artifact가 필요한 경우만 명시한다.
- active plan은 현재 실행 중이거나 곧 실행할 기준일 때만 유지한다.
- 완료됐거나 residual이 다른 owner로 이관된 plan은 reference로 내린다.
- plan 재리뷰는 최종 판단 중심으로 남기고, 반복 사고 과정은 필요한 residual만 보존한다.
- `progress.md`는 현재 포커스, 미완료 TODO, 다음 작업 중심으로 유지하고 dated evidence는 changelog나 owner plan으로 넘긴다.

## Phases

### Phase 1 - rules-only closeout 부담 축소

상태: completed

- `document_management_workflow.md`의 자동 검증 섹션에서 closeout artifact 필수 범위를 좁힌다.
- 단순 docs-only plan 작성과 rules/policy/tooling 변경을 구분한다.
- 이미 dirty artifact가 있는 경우 같은 턴에서 무리하게 덮어쓰지 않는 기준을 짧게 남긴다.

결과:

- 단순 docs-only plan 작성, 작은 문서 보정, 상태 한두 줄 갱신은 `rules:lint`와 authoring review로 충분하게 했다.
- rules/policy/tooling recurrence 예방 자체가 작업 대상일 때만 closeout artifact를 같은 변경에서 갱신하도록 좁혔다.
- closeout artifact가 이미 다른 작업의 dirty state이면 덮어쓰지 않고 residual/blocked로 남기는 기준을 추가했다.

### Phase 2 - active plan lifecycle 기준 보강

상태: completed

- `active -> reference` 전환 기준에 residual 이관 케이스를 추가한다.
- residual이 `progress.md`, 다른 active plan, owner 문서에 명시되어 있으면 원 plan은 reference 후보로 본다.
- 현재 active plan 목록을 훑고, 완료/이관 후보를 별도 실행 작업으로 분리한다.

결과:

- residual이 `plans.progress`, 다른 active plan, 또는 owner 문서로 이관되면 원 plan을 reference 후보로 보는 기준을 추가했다.
- 현재 active plan 재분류 실행은 제품/Unity 우선순위 판단과 섞일 수 있어 별도 작업으로 남겼다.

### Phase 3 - plan 재리뷰 기록 압축

상태: completed

- `plan_authoring_review_workflow.md`에 재리뷰 기록의 권장 최소 형태를 추가한다.
- 새 plan 생성 시에는 owner/scope, acceptance, residual, lifecycle만 남겨도 충분한 경우를 명시한다.
- 기존 완료 reference plan은 이번 범위에서 대규모 재작성하지 않는다.

결과:

- 문서 본문에는 최종 판단 중심으로 남기고, 반복 사고 과정은 길게 보존하지 않도록 했다.
- `plan rereview: clean/residual` 한 줄 closeout으로 충분한 경우를 명시했다.
- 기존 완료 reference plan 본문은 이번 범위에서 재작성하지 않았다.

### Phase 4 - `progress.md` 기록 상한 보강

상태: completed

- `document_management_workflow.md` 또는 `progress.md` 주변 기준에 현재 상태와 dated evidence 분리 기준을 짧게 남긴다.
- 길어진 구현 이력은 `progress_changelog.md`나 해당 owner plan으로 넘기는 기준을 정한다.
- 현재 포커스와 다음 작업이 오래된 active plan 목록에 묻히지 않게 한다.

결과:

- `plans.progress`는 현재 상태, 현재 포커스, 미완료 TODO, 다음 작업만 짧게 소유한다고 명시했다.
- dated implementation log와 긴 evidence 목록은 `progress_changelog.md`나 해당 owner plan/reference로 넘기도록 했다.
- 현재 포커스가 오래된 active plan 목록에 묻히면 plan lifecycle을 먼저 재검토하도록 했다.

### Phase 5 - no-new-doc / rename-delete 보강

상태: completed

- 새 문서를 만들지 않는 판단 중 다시 찾아야 하는 것은 owner 문서나 `progress.md`에 남기고, 세션성 판단은 최종 응답으로 충분하게 둔다.
- rename/delete 작업에는 `rg old_path`, `rg doc_id`, `rg filename`, `rg owner id` 수준의 짧은 stale reference 검색 루틴을 추가한다.
- 새 자동화나 hard-fail은 추가하지 않는다.

결과:

- 다시 찾아야 하는 owner 판단, 우선순위 판단, residual 이관 판단은 `plans.progress`나 owner 문서에 짧게 남기도록 했다.
- 세션 안에서만 의미가 있는 판단은 문서 본문에 늘리지 않도록 했다.
- rename/delete 절차에 old path, filename, `doc_id`, owner id 참조 검색을 추가했다.
- 새 자동화나 hard-fail은 추가하지 않았다.

### Phase 6 - verification and lifecycle

상태: residual

- `npm run --silent rules:lint`를 실행한다.
- closeout artifact가 필요한 범위라면 `RULES_LINT_CHANGED_FILES`로 이번 작업 파일만 넘겨 `npm run --silent rules:sync-closeout`를 실행한다.
- 완료 후 이 plan을 reference로 내리고 `docs.index` 상태 라벨을 맞춘다.

결과:

- `rules:lint`는 2026-04-26 문서 간소화 pass에서 통과했다.
- 이 작업은 `docs/ops/*` 운영 기준 변경이라 closeout artifact 갱신 대상이지만, `artifacts/rules/issue-recurrence-closeout.json`가 이미 다른 rules-only 작업의 dirty semantic state를 갖고 있어 이번 변경과 섞지 않았다.
- artifact sync는 해당 dirty state가 정리된 뒤 별도 closeout으로 처리한다.

## Acceptance

- 작은 문서 작업과 rules/policy/tooling 변경의 closeout artifact 기준이 구분된다.
- active plan을 reference로 내리는 residual 이관 기준이 보인다.
- plan 재리뷰 기록이 실행 문서를 과하게 늘리지 않는 방향으로 정리된다.
- `progress.md`와 changelog/owner plan의 기록 경계가 보인다.
- 새 문서 미생성 판단과 rename/delete stale reference 확인 기준이 짧게 보강된다.
- 새 hard-fail lint, 새 필수 artifact, 새 status/role 체계가 생기지 않는다.
- `npm run --silent rules:lint`가 통과하거나, 실패 원인이 이번 변경과 분리되어 blocked로 기록된다.

## Blocked / Residual 처리

- closeout artifact 범위를 줄이는 과정이 existing lint나 policy와 충돌하면, artifact 기준 변경은 `blocked`로 남기고 다른 경량화 단계와 섞어 success로 닫지 않는다.
- active plan 재분류가 현재 구현 우선순위 판단까지 요구하면, 재분류 실행은 별도 작업으로 분리한다.
- `progress.md` 압축 중 현재 상태가 불명확하면 삭제하지 말고 changelog/reference 이관 후보로 남긴다.
- rename/delete 검색 루틴이 너무 길어지면 빠른 체크가 아니라 리네임/삭제 섹션의 짧은 절차로 둔다.

## 검증 명령

- `npm run --silent rules:lint`
- 필요한 경우: `RULES_LINT_CHANGED_FILES=<이번 rules-only 파일 목록> npm run --silent rules:sync-closeout`

## 문서 재리뷰

- 새 문서 생성 판단: 이전 `document_management_lightweight_plan.md`와 `document_management_followup_tuning_plan.md`는 완료 기록이므로, 이번 리뷰에서 나온 새 실행 항목을 추적하려면 별도 active plan이 필요하다.
- 과한점 리뷰: 새 hard-fail, 새 artifact, 새 status/role 체계를 만들지 않고 기존 owner 문서의 경계 보정만 계획한다.
- 부족한점 리뷰: 목표, 범위, 현재 finding, target shape, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 수정 후 재리뷰: rules-only closeout 범위 축소가 기존 artifact를 즉시 덮어쓰지 않도록 dirty artifact 처리 기준을 Phase 1에 추가했고, active plan 재분류는 별도 실행으로 분리했다.
- owner impact: primary `ops.document-management-workflow`; secondary `ops.plan-authoring-review-workflow`, `ops.acceptance-reporting-guardrails`; registry `docs.index`; out-of-scope product/Unity/Stitch lane state, new lint, new artifact.
- doc lifecycle checked: 새 active plan으로 등록한다. 기존 document management completed plans는 reference로 유지하고 대체/삭제하지 않는다.
- skill trigger checked: not changed.
- plan rereview: clean
- 2026-04-26 implementation pass: Phase 1~5를 owner 문서에 반영했다. 새 lint, 새 artifact, 새 status/role 체계는 추가하지 않았다.
- 2026-04-26 implementation rereview: residual - closeout artifact가 이미 다른 rules-only 작업의 dirty semantic state라 이번 작업의 artifact sync와 plan reference 전환은 보류한다.
- 2026-04-26 simplification pass: active plan으로 둘 필요는 없어 reference로 내린다. residual은 closeout artifact sync만 남고, owner 기준은 `ops.document-management-workflow`로 이관됐다.
