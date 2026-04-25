# Plan Authoring Review Workflow

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: ops.plan-authoring-review-workflow
> role: ssot
> owner_scope: plan 문서 작성/수정 후 closeout 원칙을 적용하는 재리뷰 절차
> upstream: docs.index, ops.document-management-workflow
> artifacts: `docs/plans/*.md`

이 문서는 `ops.document-management-workflow`의 Closeout 원칙을 plan 문서에 적용하는 절차만 소유한다.
plan은 규칙 본문을 새로 만드는 곳이 아니라, 실행 순서와 현재 상태를 담는 문서다.

## 적용 범위

적용한다:

- `docs/plans/*.md`를 새로 작성할 때
- 계획의 우선순위, acceptance, 실행 순서, 검증 기준을 실질적으로 바꿀 때
- 사용자 제공 계획을 repo 기준으로 장기 reference로 채택할 때

제외한다:

- 오탈자, 링크, 메타데이터 같은 경미한 수정
- 상태 라벨이나 진행률만 짧게 갱신하는 경우
- historical/reference 문서 보존 목적의 작은 정리

## 핵심 루프

1. 초안 작성 또는 실질 수정
2. 작은 수정은 owner/scope를 확인하고, 큰 문서 작업이나 여러 owner 작업이면 변경 이유, primary/secondary owner, 제외 범위를 확인
3. 과한점/부족한점 재리뷰
4. 문제가 있으면 수정
5. 수정 후 한 번 더 재리뷰
6. clean이거나 residual을 명시했을 때만 closeout

## 재리뷰 기준

과한점:

- owner 문서 대신 규칙 본문을 새로 정의하지 않는가
- entry, ssot, plan, reference 역할을 섞지 않는가
- 새 hard-fail, 새 field, 새 artifact를 필요 이상으로 늘리지 않는가
- 같은 사실을 여러 문서에 중복 유지하게 만들지 않는가

부족한점:

- owner와 scope가 보이는가
- 큰 문서 작업이나 여러 owner 작업이면 primary owner, secondary owner, 제외 범위가 구분되는가
- 큰 문서 작업이면 active/reference/historical/delete 후보 확인이 보이는가
- acceptance와 closeout 조건이 있는가
- 실행 순서와 검증 방법이 있는가
- unresolved risk나 residual handling이 있는가
- 사용자 지시가 기존 규칙과 충돌하거나, 범위가 과하거나 부족할 때 질문 또는 대안 제안이 closeout 전에 처리되는가

복잡하거나 blocked/capability 작업이면 추가로 본다:

- blocked 또는 unsupported를 같은 계획 안의 공통 capability/policy 변경으로 success 처리하려 하지 않는가
- capability expansion이 필요한 지점에서 별도 lane 재선언과 blocked 처리 기준이 보이는가

## Closeout

closeout 표현은 아래 둘 중 하나일 때만 쓴다.

- 최신 재리뷰에서 obvious 과한점/부족한점이 없다.
- 아직 남는 문제가 있고, 그 이유를 residual로 명시했다.

권장 표현:

- `plan rereview: clean`
- `plan rereview: residual - <reason>`

draft plan에 closeout 문구를 남기면 완료된 plan처럼 읽히므로 피한다.
