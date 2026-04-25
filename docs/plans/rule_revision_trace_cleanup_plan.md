# Rule Revision Trace Cleanup Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.rule-revision-trace-cleanup
> role: plan
> owner_scope: 규칙 개정 후 active old trace가 남아 충돌하는 문제를 정리하고 closeout에서 강제하는 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `artifacts/rules/rule-revision-trace-inventory.md`

## 목적

규칙을 개정했는데 이전 규칙 문구, 테스트, 코드 주석, 툴팁, entry 요약이 active 상태로 남아 old/new 기준이 동시에 살아나는 문제를 막는다.

이 문서는 원칙 본문을 새로 소유하지 않는다. 현재 문서 역할, SSOT, closeout 기준은 `ops.document-management-workflow`를 따른다.

## 범위

포함:

- 규칙 개정 시 old trace를 active/current 판단 근거와 historical/reference 기록으로 분류하는 절차
- active old trace 정리 여부를 closeout 전에 확인하는 체크
- 필요하면 `rules:lint` 또는 advisory artifact로 stale trace 후보를 감지하는 자동화

제외:

- 모든 과거 기록의 물리 삭제 강제
- historical/changelog/discussion에 남긴 배경 기록 삭제
- 제품 밸런스 결정 자체의 재정의
- 각 도메인 owner 문서의 규칙 본문을 이 plan에 복제

## 핵심 판정

- current owner 문서, active plan, 테스트, 코드, UI 텍스트, tooltip, README에서 새 규칙과 충돌하는 old trace는 수정 또는 제거한다.
- 보존 가치가 있는 old trace는 `historical`, changelog, discussion 등으로 내리고 현재 판단 근거가 아님을 명시한다.
- old trace를 찾지 못했거나 정리하지 못한 범위가 있으면 success가 아니라 `residual` 또는 `blocked`로 보고한다.

## 실행 단계

### Phase 0: Owner Rule 반영

상태: 완료

작업:

- `ops.document-management-workflow`의 closeout/빠른 체크에 "규칙 개정 시 active old trace 정리" 항목을 추가한다.
- `rule-operations` skill description에 "old trace", "stale rule", "규칙 개정 후 흔적 정리" 트리거가 필요한지 확인한다.

Acceptance:

- 규칙 개정 작업에서 active old trace를 방치하면 closeout할 수 없다는 기준이 owner 문서에 있다.
- skill trigger 검토 결과가 closeout에 남는다.

### Phase 1: Trace Inventory

상태: 완료

작업:

- 개정 전/후 핵심 용어를 정한다.
- `rg`로 docs, code, tests, prefabs/scenes text, tool README, user-facing strings의 old trace 후보를 찾는다.
- 후보를 `active-current`, `historical/reference`, `false-positive`, `blocked`로 분류한다.

Acceptance:

- active-current 후보와 보존 후보가 분리되어 있다.
- 찾지 못한 범위가 있으면 이유와 residual이 기록되어 있다.

산출물:

- `artifacts/rules/rule-revision-trace-inventory.md`

### Phase 2: Cleanup

상태: 완료

작업:

- active-current old trace는 새 규칙에 맞게 수정하거나 제거한다.
- historical/reference로 남길 내용은 현재 기준이 아님을 명시한다.
- entry 문서와 owner 본문이 충돌하면 owner 본문을 우선하고 entry를 같은 변경에서 맞춘다.

Acceptance:

- active 문서, 코드, 테스트, user-facing text에 old/new 충돌이 남지 않는다.
- historical/reference 기록은 현재 판단 근거로 읽히지 않는다.

결과:

- active-current stale old trace 후보가 없어 별도 cleanup patch는 필요하지 않았다.
- 완료된 plan은 active로 방치하지 않고 reference로 내린다.

### Phase 3: Verification

상태: 완료

작업:

- 기본 검증으로 `npm run --silent rules:lint`를 실행한다.
- rules-only scope가 포함되면 `npm run --silent rules:sync-closeout`를 실행한다.
- 반복되는 stale trace 패턴이 있으면 hard fail 전 advisory lint 또는 `artifacts/rules/` inventory를 검토한다.

Acceptance:

- 검증 결과가 closeout에 남는다.
- 자동화가 불가능한 범위는 manual review residual로 남긴다.

검증 결과:

- `npm run --silent rules:sync-closeout`: passed
- `npm run --silent rules:lint`: passed

## Closeout 기준

- `success`: active-current old trace 후보가 없고, 검증이 통과했다.
- `residual`: 일부 범위가 자동/수동으로 확인되지 않았지만 현재 판단 근거 충돌은 발견되지 않았다.
- `blocked`: active old trace가 남아 있거나, owner 충돌 때문에 어떤 기준을 남길지 결정할 수 없다.
- `mismatch`: 새 규칙과 기존 구현/문서가 서로 다른 상태임을 확인했다.

최종 closeout:

- `success`: active-current stale old trace 후보가 없고, rules verification이 통과했다.
- residual: binary Unity assets/images는 의미 검색 대상이 아니었지만, 이번 변경은 문서/skill routing 규칙이라 product-facing Unity trace가 예상되지 않는다.

## Plan Rereview

- 2026-04-25 초안 리뷰: 과한점 발견. 모든 과거 기록 삭제를 강제하면 historical/reference 역할과 충돌한다.
- 2026-04-25 초안 반영: 물리 삭제가 아니라 active-current old trace 정리와 historical/reference 격리를 기준으로 좁혔다.
- 2026-04-25 재리뷰: plan rereview: clean. 과한점/부족한점 없음.
- 2026-04-25 Phase 0 실행 리뷰: 부족한점 발견. owner closeout 기준과 skill trigger에 old trace/stale rule 표현이 직접 연결되어 있지 않았다.
- 2026-04-25 Phase 0 실행 반영: `ops.document-management-workflow` closeout/빠른 체크와 `rule-operations` trigger/read-order를 보강했다.
- 2026-04-25 Phase 0 실행 재리뷰: plan rereview: clean. 과한점/부족한점 없음.
- 2026-04-25 Phase 1 실행 리뷰: 부족한점 발견. 검색 결과가 채팅에만 남으면 다음 세션에서 재확인해야 한다.
- 2026-04-25 Phase 1 실행 반영: `artifacts/rules/rule-revision-trace-inventory.md`에 검색 범위, 검색어, 분류, residual을 기록했다.
- 2026-04-25 Phase 2 실행 리뷰: active-current stale old trace 후보가 없어 cleanup patch가 필요하지 않았다. 완료된 plan이 active로 남는 것은 과한점이다.
- 2026-04-25 Phase 2 실행 반영: 이 plan 상태를 reference로 내리고, `docs/index.md` registry도 reference로 맞춘다.
- 2026-04-25 Phase 1/2 실행 재리뷰: plan rereview: clean. 과한점/부족한점 없음.
- 2026-04-25 Phase 3 실행 리뷰: 검증 증거가 plan closeout에 남아야 한다.
- 2026-04-25 Phase 3 실행 반영: `rules:sync-closeout`, `rules:lint` 통과 결과와 최종 closeout을 기록했다.
- 2026-04-25 Phase 3 실행 재리뷰: plan rereview: clean. 과한점/부족한점 없음.

## Skill Trigger Checked

- `skill trigger checked: covered by rule-operations, rule-plan-authoring`
- `skill trigger checked: added to rule-operations`
