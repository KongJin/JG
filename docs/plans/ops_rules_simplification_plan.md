# Ops Rules Simplification Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.ops-rules-simplification
> role: reference
> owner_scope: 운영 규칙을 상위 원칙 중심으로 압축하고 하위 규칙을 낮춘 결과 기록
> upstream: docs.index, ops.document-management-workflow, ops.cohesion-coupling-policy, ops.acceptance-reporting-guardrails, ops.plan-authoring-review-workflow
> artifacts: `docs/ops/document_management_workflow.md`, `docs/ops/cohesion_coupling_policy.md`, `docs/ops/acceptance_reporting_guardrails.md`, `docs/ops/plan_authoring_review_workflow.md`, `docs/index.md`

이 문서는 JG 레포의 운영 규칙을 더 덜 답답하게 만들기 위해 수행한 정리 계획과 결과 기록이다.
당시 목표는 새 규칙을 추가하는 것이 아니라, 상위 원칙을 선명하게 해서 하위 규칙과 체크리스트를 줄이는 것이었다.
현재 운영 기준은 각 `docs/ops/*` owner 문서가 소유하고, 이 문서는 정리 과정의 reference로만 읽는다.

## 배경

최근 문서 정리로 active plan 수와 historical 문서는 줄었지만, 운영 규칙 문서들은 여전히 각각 독립적인 규칙 본문처럼 읽힌다.
이 구조는 안전하지만, 작은 작업에도 여러 문서를 확인해야 하는 부담을 만든다.

원하는 방향은 아래다.

- 상위 규칙이 명확해지면 하위 규칙은 예시나 reference로 내려간다.
- 새 hard-fail을 늘리기보다, 기존 owner 문서의 책임을 더 선명하게 한다.
- 문서 운영 판단은 적은 수의 상위 원칙에서 시작한다.

## 당시 정리한 상위 4원칙

### 1. SSOT

- 한 사실은 한 owner만 가진다.
- 현재 상태는 `plans.progress`를 우선한다.
- 설계 판단은 `design/*`, 운영 절차는 `ops/*`, 실행 순서는 `plans/*`가 맡는다.
- 같은 결정을 여러 문서에 장문으로 풀어 쓰지 않는다.

### 2. Role

- `entry`는 길 안내만 한다.
- `ssot`는 규칙과 판단 기준만 가진다.
- `plan`은 실행 순서와 현재 상태만 가진다.
- `reference`는 필요할 때 보는 배경과 예시다.
- `historical`은 현재 판단 근거가 아니다.

### 3. Closeout

- 완료나 성공을 말하려면 기준, 증거, 남은 리스크가 있어야 한다.
- mechanical pass와 actual acceptance를 섞지 않는다.
- 막혔으면 `blocked`, 비교 결과가 다르면 `mismatch`로 말한다.
- 문제를 발견했으면 원인, 예방, 검증이 함께 남아야 한다.

### 4. Cohesion

- 같은 이유로 바뀌는 것만 한 문서나 한 패치에 둔다.
- 문서가 다른 owner의 본문을 재서술하지 않는다.
- 링크와 경로 탐색은 entry/index에 모으고, 본문 문서는 역할 위임을 우선한다.
- coupling을 숨기기 위해 fallback, hidden lookup, runtime repair를 정답처럼 문서화하지 않는다.

## 실행 범위

이번 plan이 직접 다루는 문서:

- `docs/ops/document_management_workflow.md`
- `docs/ops/cohesion_coupling_policy.md`
- `docs/ops/acceptance_reporting_guardrails.md`
- `docs/ops/plan_authoring_review_workflow.md`
- `docs/index.md`

이번 plan이 직접 다루지 않는 것:

- Unity UI authoring 세부 규칙
- Stitch translation 세부 규칙
- gameplay/runtime code 규칙
- 새 lint hard-fail 추가
- 새 운영 문서 신설

## 실행 순서와 결과

### Phase 1. 상위 헌장 정리

`document_management_workflow.md`를 문서 운영 헌장 역할로 정리한다.

작업:
- 문서 상단에 `SSOT / Role / Closeout / Cohesion` 4원칙을 짧게 둔다.
- 기존 역할, 참조, 삭제/리네임, 검증 규칙은 유지하되 중복 설명을 줄인다.
- `cohesion_coupling_policy.md`는 응집도/결합도 정의 owner로 유지하고, 문서 운영 헌장은 그 정의를 적용하는 위치로 둔다.

완료 기준:
- 문서 관리 판단은 `document_management_workflow.md`만 읽어도 시작할 수 있다.
- 응집도 의미 판단이 필요할 때만 `cohesion_coupling_policy.md`로 내려간다.

결과:
- 완료. `document_management_workflow.md` 상단에 4원칙을 올리고, 세부 문장을 압축했다.

### Phase 2. Plan authoring 규칙 축약

`plan_authoring_review_workflow.md`를 독립 헌장처럼 보이지 않게 줄인다.

작업:
- 이 문서가 `Closeout` 원칙을 plan 문서에 적용하는 파생 규칙임을 명시한다.
- 과한점/부족한점 체크리스트를 핵심 항목 중심으로 줄인다.
- plan closeout의 핵심은 "최신 재리뷰 clean 또는 residual 명시"로 유지한다.

완료 기준:
- plan 작성자가 이 문서를 읽을 때 새 철학을 배우는 느낌보다, 상위 원칙의 적용 절차를 보는 느낌이 든다.

결과:
- 완료. `plan_authoring_review_workflow.md`를 Closeout 원칙의 plan 적용 절차로 축약했다.

### Phase 3. Acceptance/reporting 규칙 축약

`acceptance_reporting_guardrails.md`를 실제 판정 의미 중심으로 줄인다.

작업:
- `mechanical != acceptance`
- `success / blocked / mismatch`
- `blockedReason`
- `rules-only recurrence closeout`

위 네 덩어리를 중심으로 남긴다.

완료 기준:
- closeout 판단에 필요한 의미는 남아 있다.
- 시각 작업이나 특정 lane 예시는 장문 규칙처럼 보이지 않는다.

결과:
- 완료. `acceptance_reporting_guardrails.md`를 mechanical/acceptance 분리와 판정 의미 중심으로 축약했다.

### Phase 4. Recurrence prevention plan 은퇴

재발 방지 실행 계획은 이미 reference였고 1차 구현도 끝났다.

작업:
- 살아 있는 기준이 `document_management_workflow.md`와 `acceptance_reporting_guardrails.md`에 이미 흡수됐는지 확인한다.
- 흡수 완료면 index에서 제거하고 문서를 삭제한다.
- 아직 남은 실행 항목이 있으면 reference로 유지하되, 다음 판단 포인트만 짧게 남긴다.

완료 기준:
- 재발 방지 기준이 별도 plan을 읽어야만 이해되는 상태가 아니다.

결과:
- 완료. 살아 있는 기준은 `document_management_workflow.md`와 `acceptance_reporting_guardrails.md`에 흡수했고, 별도 plan은 삭제했다.

### Phase 5. Index 단순화

`docs/index.md`의 운영 경로를 줄인다.

작업:
- 문서/운영 판단 시작점은 `document_management_workflow.md`로 둔다.
- 응집도 판단은 `cohesion_coupling_policy.md`로 둔다.
- 진행 상태는 `progress.md`로 둔다.
- 파생 workflow 문서는 "먼저 볼 곳"보다 폴더별 registry에서 찾게 한다.

완료 기준:
- 운영 관련 "먼저 볼 곳" 항목이 늘어져 보이지 않는다.
- 세부 문서는 필요할 때 내려가는 구조가 된다.

결과:
- 완료. `docs/index.md`와 `AGENTS.md`의 운영 진입 경로를 `document_management_workflow.md` 중심으로 줄였다.

## Acceptance 기록

- 운영 판단 시작에 필요한 active ops 문서 수가 줄어든다.
- 상위 4원칙이 `document_management_workflow.md`에서 바로 보인다.
- `plan_authoring_review_workflow.md`와 `acceptance_reporting_guardrails.md`는 파생 규칙처럼 읽힌다.
- 재발 방지 실행 계획은 삭제되거나, 남더라도 후속 reference로 축약된다.
- `docs/index.md`의 운영 진입 경로가 단순해진다.
- 새 hard-fail lint를 추가하지 않는다.
- `npm run --silent rules:lint`가 통과한다.

## 당시 리스크와 보류 기준

- 너무 많이 줄여서 blocked/mismatch/success 의미가 흐려지면 보류한다.
- 상위 원칙만 남기고 lane별 실제 작업자가 필요한 절차를 잃으면 보류한다.
- 삭제하려는 문서가 active skill이나 tool README에서 아직 직접 참조되면 삭제하지 않는다.
- lint가 잡는 machine-checkable safety는 유지하고, 이번 plan에서 새 hard-fail은 추가하지 않는다.

## 재리뷰 기록 기준

이 plan은 작성 당시 `ops.plan-authoring-review-workflow`를 따랐다.

- 과한점: 새 문서나 새 lint를 늘리고 있지 않은가
- 부족한점: owner, scope, acceptance, 실행 순서, 검증 기준이 빠져 있지 않은가
- closeout: 최신 재리뷰가 clean이거나 residual을 명시해야 한다

## Closeout

- status: completed
- 과한점: 새 운영 문서나 새 hard-fail lint를 만들지 않고, 기존 owner 문서의 책임 재배치만 다룬다.
- 부족한점: owner, scope, 제외 범위, 실행 순서, acceptance, 보류 기준, 검증 기준이 포함되어 있다.
- verification: `npm run --silent rules:lint`
- residual: machine-checkable safety는 유지했고, 이후 더 줄일 때도 새 hard-fail을 늘리기보다 owner 문서의 상위 원칙을 먼저 본다.
