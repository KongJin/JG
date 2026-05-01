# Plan Authoring Review Workflow

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.plan-authoring-review-workflow
> role: ssot
> owner_scope: plan 문서 작성/수정 후 closeout 원칙을 적용하는 재리뷰 절차
> upstream: docs.index, ops.document-management-workflow
> artifacts: none

이 문서는 `ops.document-management-workflow`의 Closeout 원칙을 plan 문서에 적용하는 절차만 소유한다.
plan은 규칙 본문을 새로 만드는 곳이 아니라, 실행 순서와 현재 상태를 담는 문서다.
`docs/plans/*.md`는 적용 대상이지 이 문서가 closeout/evidence owner로 소유하는 artifact가 아니다.

## 적용 범위

적용한다:

- `docs/plans/*.md`를 새로 작성할 때
- 계획의 우선순위, acceptance, 실행 순서, 검증 기준을 실질적으로 바꿀 때
- 사용자 제공 계획을 repo 기준으로 장기 reference로 채택할 때

제외한다:

- 작업 시작 전 세션 계획만 세우는 경우
- 사용자가 "계획 세워줘"라고만 요청했고, repo에 지속 기록할 필요가 없는 경우
- 오탈자, 링크, 메타데이터 같은 경미한 수정
- 상태 라벨이나 진행률만 짧게 갱신하는 경우
- historical/reference 문서 보존 목적의 작은 정리

## 세션 계획과 plan 문서

작업 시작 전 계획의 기본값은 채팅 또는 세션 체크리스트다.
`docs/plans/*.md`는 multi-session handoff, persistent acceptance/residual/blocked 판단, 여러 owner scope 고정이 필요할 때만 만든다.
`progress.md` 한 줄이나 기존 owner 문서의 짧은 섹션으로 충분하면 새 plan 문서를 만들지 않는다.

## 요청 Triage Lite

새 요청이 들어왔을 때 issue tracker를 만들거나 label workflow를 흉내 내지 않는다.
먼저 `docs/index.md`의 current route를 기준으로 category 하나와 next action 하나를 고른다.
요청이 여러 변경 이유를 섞으면 하나의 plan으로 합치지 말고, owner별로 나누거나 이번 세션의 primary owner와 out-of-scope를 밝힌다.

category는 정확한 route 본문이 아니라 첫 분류 신호다.

- `bug/regression`: 깨진 동작, 성능 저하, 재현 가능한 실패. 구현 전 `jg-issue-investigation` route로 최소 재현 또는 feedback loop를 먼저 만든다.
- `feature/design`: 새 동작, UX, 제품 판단. `design/*`, active owner plan, 또는 `plans.progress` current focus로 보낸다.
- `docs/workflow/rule/skill`: 문서 운영, 규칙, owner route, repo-local skill trigger 변경. `ops.document-management-workflow`, `ops.cohesion-coupling-policy`, 관련 owner skill을 먼저 본다.
- `Unity/Stitch/validation`: Unity scene/prefab/UI, Stitch handoff, build/smoke/test 검증. `docs/index.md`의 lane route와 active owner plan을 따른다.

next action은 실행 준비 상태를 뜻한다.

- `ready-AFK`: 기존 owner 규칙과 코드 선례로 진행 가능하고, 제품/UX/아키텍처 판단이나 외부 권한이 필요 없다.
- `HITL`: 사용자 판단, 수동 smoke, credential, 외부 서비스, 제품 선택, UX 선택, 아키텍처 결정이 필요하다.
- `needs-info`: repo 탐색과 최소 재현을 해도 안전하게 진행할 정보가 부족하다. 이미 확인한 사실과 필요한 질문을 구분하고, 질문은 구체적이어야 한다.
- `defer/no-action`: 현재 owner 체계에 들이지 않기로 판단한다. durable residual이면 owner plan이나 `plans.progress`에 짧게 남기고, 세션성 판단이면 채팅에만 둔다.

bug/regression 요청은 `ready-AFK`로 넘기기 전에 reporter가 준 단계, 관련 코드 path, 가능한 test/tool/smoke 중 가장 좁은 feedback loop를 먼저 확인한다.
재현 실패나 정보 부족은 실패가 아니라 `needs-info` 또는 `HITL` 판단의 근거다.
GitHub issue comment, label, close action, `.out-of-scope/` 지식베이스 생성은 JG 기본 triage-lite 범위가 아니다.

## 실행 Slice 작성

broad plan, spec, PRD 성격의 내용을 repo에 남길 때는 layer별 TODO보다 검증 가능한 vertical slice를 우선한다.
목표는 issue tracker를 새로 만드는 것이 아니라, `docs/plans/*`나 기존 owner 문서의 실행 항목이 독립적으로 검토, 구현, 검증될 수 있게 하는 것이다.

- 각 slice는 얇지만 완결된 사용자/시스템 동작을 담는다. schema/API/UI/test 같은 layer 이름을 나열하는 horizontal slice는 피한다.
- 완료된 slice는 자체 acceptance, 검증 방법, blocked/residual 판정이 가능해야 한다.
- slice마다 `HITL` 또는 `AFK` 성격을 구분한다.
  - `HITL`: 제품 판단, UX 선택, 아키텍처 결정, 수동 smoke처럼 사람 판단이 필요한 항목.
  - `AFK`: 기존 owner 규칙과 코드 선례로 구현/검증 가능한 항목.
- dependency가 있으면 blocker를 먼저 둔다. blocker 없이 시작 가능한 slice는 명확히 표시한다.
- 너무 큰 slice는 end-to-end path를 유지한 채 더 얇게 나누고, 너무 작은 layer-only 항목은 동작 기준으로 합친다.
- product-facing PRD 요약이 필요하면 문제, 사용자 관점의 해결, 주요 user story, implementation/testing decision, out-of-scope를 기존 `design/*` 또는 owner plan에 맞춰 짧게 합성한다. 별도 issue tracker publish는 JG 기본 흐름이 아니다.

## 핵심 루프

1. 초안 작성 또는 실질 수정
2. 작은 수정은 owner/scope를 확인하고, 큰 문서 작업이나 여러 owner 작업이면 변경 이유, primary/secondary owner, 제외 범위를 확인
3. 과한점/부족한점 재리뷰
4. 문제가 있으면 수정
5. 수정 후 한 번 더 재리뷰
6. clean이거나 residual을 명시했을 때만 closeout

문서 본문에는 최종 판단 중심으로 남긴다.
초안 작성 중의 반복 사고 과정은 길게 보존하지 않고, 남는 위험이나 이관 판단이 있을 때만 residual로 남긴다.

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
- 완료 후 삭제 가능한지, 아니면 residual 때문에 reference 압축 보존이 필요한지 보이는가
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

- `plan rereview: clean - <checked scope>`
- `plan rereview: residual - <reason>`

`clean`은 bare label로 남기지 않고 owner/scope, acceptance, residual/lifecycle 중 실제 확인한 범위를 짧게 붙인다.
새 plan 생성이나 큰 plan 수정이라도 owner/scope, acceptance, residual/lifecycle 판단이 보이면 위 한 줄 closeout으로 충분하다.
반복 리뷰 로그는 실행 판단을 바꾼 경우나 residual 근거가 될 때만 남긴다.
완료된 plan의 `doc lifecycle checked`에는 delete 후보인지, reference 압축 보존이 필요한지, 또는 아직 active 유지가 필요한지 드러나야 한다.
완료된 plan에 새 TODO를 계속 붙여 재사용하지 않는다. 새 변경 이유가 생기면 `ops.document-management-workflow`의 새 문서 생성 기준으로 다시 판단한다.

draft plan에 closeout 문구를 남기면 완료된 plan처럼 읽히므로 피한다.
