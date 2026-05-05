---
name: rule-plan-authoring
description: "계획 문서 작성/수정 규칙. Triggers: docs/plans, Phase/TODO/Acceptance, 재리뷰, 과한점/부족한점, closeout, 새 규칙."
---

# Plan Authoring

계획 문서 작성/수정 전용 트리거 스킬.

이 스킬은 세부 규칙을 새로 소유하지 않는다. 실행 전에 repo owner 문서를 읽고, 계획 작업에서 빠지기 쉬운 재리뷰 루프를 강제로 호출한다.

## 반드시 읽을 문서

1. `docs/index.md`
2. `docs/ops/plan_authoring_review_workflow.md`
3. `docs/ops/document_management_workflow.md`
4. 계획이 현재 상태나 우선순위를 바꾸면 `docs/plans/progress.md`
5. Unity/UI/검증/배포 등 특정 lane 계획이면 해당 owner 문서
6. 새 규칙이나 행동 트리거를 다루면 `docs/index.md`에서 current owner를 확인하고, `docs/ops/document_management_workflow.md`, `docs/ops/cohesion_coupling_policy.md`, 관련 repo-local skill route를 읽기
7. skill route/trigger 변경이면 `docs/ops/skill_routing_registry.md`와 `docs/ops/skill_trigger_matrix.md`도 확인하기

## 적용 범위

적용:

- `docs/plans/*.md` 새 파일 작성
- `docs/plans/*.md`의 우선순위, acceptance, 실행 순서, 검증 기준 수정
- 사용자가 계획, 실행 순서, TODO, Phase, Acceptance, closeout을 요청
- 사용자가 "과한부분/부족한부분", "재리뷰", "clean/residual"을 언급
- 계획 지시가 기존 규칙과 충돌하거나 범위가 과하거나 부족해 보임
- 사용자가 새 규칙, 행동 트리거, skill trigger checked, 규칙 추가를 언급
- 계획과 관련해 `docs/index.md` registry 갱신이 필요한 경우

제외:

- 단순 링크/오탈자/메타데이터만 수정하는 경우
- 코드 구현만 하고 계획 문서를 건드리지 않는 경우
- 이미 historical/reference인 문서를 보존 목적으로 짧게 정리하는 경우

## 실행 루프

계획 초안 또는 실질 수정 후 항상 아래 순서를 따른다.

1. 초안 작성 또는 수정
2. 과한점 리뷰
3. 부족한점 리뷰
4. 문제가 있으면 문서에 반영
5. 수정 후 다시 과한점/부족한점 리뷰
6. 더 이상 obvious issue가 없으면 `plan rereview: clean`을 남김
7. 남는 문제가 있으면 이유와 후속 조건을 `plan rereview: residual - <reason>`으로 남김

한 번 수정한 뒤 바로 끝내지 말고, 수정 후 재리뷰를 반드시 한 번 더 한다.

## 과한점 체크

- owner 문서가 가진 규칙 본문을 plan이 다시 정의하지 않는가
- entry, ssot, plan, reference 역할을 섞지 않는가
- 새 hard-fail, 새 artifact, 새 field를 불필요하게 늘리지 않는가
- 같은 사실을 여러 문서가 장문으로 중복 소유하게 만들지 않는가
- 실행 계획이 구현 작업, 검증 기준, 제품 판단을 한 문서에 과하게 합치지 않는가

## 부족한점 체크

- owner, scope, 현재 상태, 제외 범위가 보이는가
- acceptance와 closeout 조건이 있는가
- 실행 순서와 검증 방법이 있는가
- unresolved risk, blocked, residual handling이 있는가
- `docs/index.md` 등록 또는 상태 라벨 갱신이 필요한데 빠지지 않았는가
- 큰 문서 작업이면 `doc lifecycle checked: ...`를 남겼는가
- 진행 상태가 바뀌었으면 `docs/plans/progress.md` 갱신 필요 여부를 확인했는가
- 새 규칙이나 행동 트리거를 추가했다면 `skill trigger checked: ...`를 남겼고, skill route/trigger 변경이면 registry/matrix를 대조했는가

## 보고 규칙

- 실제 구현 전 계획만 했다면 "계획했다", "정리했다", "검토했다"라고 말한다.
- 완료/성공 표현은 기준, 증거, 남은 리스크를 함께 말할 수 있을 때만 쓴다.
- policy나 lint가 blocked이면 성공으로 포장하지 말고 blocked 이유를 분리한다.

## 검증

문서 변경 후 기본 검증:

- `npm run --silent rules:lint`

rules-only scope가 포함되고 repo 문서 workflow가 요구하면:

- `npm run --silent rules:sync-closeout`

검증 실패 시 이번 변경 때문인지 기존 dirty worktree 때문인지 분리해서 보고한다.
