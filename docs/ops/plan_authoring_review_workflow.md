# Plan Authoring Review Workflow

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: ops.plan-authoring-review-workflow
> role: ssot
> owner_scope: 계획 문서 작성/수정 후 반복 재리뷰 루프, 과한점/부족한점 점검 기준, plan closeout 조건
> upstream: docs.index, ops.cohesion-coupling-policy, ops.document-management-workflow
> artifacts: `docs/plans/*.md`

이 문서는 JG 레포에서 Codex가 계획 문서를 작성하거나 실질적으로 수정한 뒤 따라야 하는 **반복 재리뷰 루프**를 정한다.
목표는 초안 작성 직후 바로 닫아 버리지 않고, 같은 기준으로 여러 번 다시 읽으면서 남은 `과한점`과 `부족한점`을 걷어내는 것이다.

문서 역할 분리 원칙은 `ops.document-management-workflow`를 따른다.
즉 이 문서는 plan authoring closeout 루프만 소유하고, 각 도메인 규칙 본문은 해당 owner doc에 둔다.

## 적용 범위

아래에 해당하면 이 루프를 적용한다.

- `docs/plans/*.md`를 새로 작성할 때
- 기존 계획 문서의 구조, 우선순위, acceptance, implementation order, 검증 기준을 실질적으로 바꿀 때
- 사용자 제공 계획 문서를 repo 기준으로 다듬어 장기 기준으로 채택하려 할 때

아래는 기본적으로 제외한다.

- 맞춤법, 링크, 메타데이터 같은 경미한 수정만 있는 경우
- 상태 라벨이나 진행률만 짧게 갱신하는 경우
- historical/reference 문서를 보존 목적만으로 손보는 경우

## 핵심 규칙

### 1. plan은 초안 작성 직후 바로 closeout하지 않는다

실질적인 계획 작성 또는 수정이 끝나면, 바로 `완료`처럼 보고하지 않는다.
반드시 같은 턴 안에서 계획 자체를 다시 읽는 재리뷰 단계로 들어간다.

### 2. 재리뷰는 수정 후 한 번으로 끝내지 않는다

기본 루프는 아래다.

1. 초안 작성 또는 실질 수정
2. `과한점 / 부족한점` 기준으로 재리뷰
3. 문제가 있으면 계획 문서 수정
4. 다시 같은 기준으로 재리뷰
5. 최신 재리뷰에서 더 이상 남은 obvious issue가 없을 때만 closeout

즉 마지막 수정 뒤에는 반드시 `clean pass`가 한 번 더 있어야 한다.

### 3. closeout 조건은 `최신 재리뷰 clean`이다

아래 둘 중 하나여야 closeout할 수 있다.

- 최신 재리뷰에서 남은 obvious `과한점`과 `부족한점`이 없음
- 필요한 정보가 없어 더 줄일 수 없는 residual issue를 명시적으로 적고, draft/blocked로 남김

정보 부족이나 owner 충돌이 남았는데도 `이 정도면 됐다`고 닫지 않는다.

## 과한점 점검 기준

재리뷰에서는 아래를 먼저 본다.

1. 이 계획이 owner doc 대신 규칙 본문을 새로 정의하고 있지 않은가
2. entry 문서, skill-entry 문서, SSOT 문서의 책임을 섞고 있지 않은가
3. repo 밖에서 통제할 수 없는 변경을 이 계획의 필수 성공 조건으로 묶고 있지 않은가
4. machine-checkable 항목과 사람 review 항목을 한데 뭉개고 있지 않은가
5. 같은 사실을 여러 문서나 여러 레이어에 중복 유지하게 만들고 있지 않은가
6. 시각 작업 한정 개념을 비시각 작업 전체 규칙처럼 밀어붙이고 있지 않은가
7. hard-fail, 새 필드, 새 artifact를 필요 이상으로 늘리고 있지 않은가

## 부족한점 점검 기준

재리뷰에서는 아래 누락도 본다.

1. 이 계획의 owner doc이 어디인지 빠져 있지 않은가
2. 적용 범위와 제외 범위가 빠져 있지 않은가
3. acceptance, 비교 기준, closeout 조건이 빠져 있지 않은가
4. implementation order가 없어 실행 순서가 흐려지지 않는가
5. test/review 방법이 없어 실제 검증 가능성이 떨어지지 않는가
6. visual task와 non-visual task 중 한쪽 예시만 있어 범용성이 깨지지 않는가
7. unresolved risk나 residual handling 규칙이 없어 미완 상태를 성공처럼 닫게 만들지 않는가

## 재리뷰 입력 우선순위

계획 재리뷰 때는 아래 순서로 판단한다.

1. 최신 사용자 직접 지시
2. 현재 계획이 따르는 active owner docs
3. `docs/index.md` 기준 current path와 문서 역할 규칙
4. 관련 reference/historical 문서
5. 내부 편의 해석

repo-local skill entry는 owner authority가 아니라 routing layer로만 본다.

## closeout 보고 규칙

계획 문서를 닫을 때는 아래를 짧게라도 남긴다.

- 재리뷰를 수행했다는 사실
- 최신 재리뷰 결과가 `clean`인지, 아니면 residual issue가 남는지
- residual issue가 남으면 왜 더 줄이지 못했는지

권장 형식:

- `plan rereview: clean`
- `plan rereview: residual - owner conflict unresolved`

`clean`이 아니면 성공처럼 보고하지 않는다.

## 문서 배치 규칙

- 계획 내용은 `docs/plans/*`에 둔다
- 계획 작성/재리뷰 절차 본문은 이 문서가 소유한다
- entry 문서와 repo-local skill entry는 이 문서를 읽게만 한다

즉 `AGENTS.md`나 repo-local skill에 `과한점/부족한점` 체크리스트 본문을 장문으로 복제하지 않는다.

## 체크리스트

계획 closeout 직전에 아래를 확인한다.

1. 최신 수정 뒤에 재리뷰를 다시 했는가
2. 과한점과 부족한점을 같은 기준으로 다시 읽었는가
3. 남은 issue가 있으면 수정했는가
4. 아직 남는 issue가 있으면 residual로 명시했는가
5. 최신 재리뷰가 `clean`이 아니면 closeout 표현을 피했는가
