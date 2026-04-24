# Acceptance Reporting Guardrails

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: ops.acceptance-reporting-guardrails
> role: ssot
> owner_scope: acceptance 입력 우선순위, acceptance lock, blocked/mismatch/success 판정, intermediate/final reporting semantics, 최소 machine-checkable contract
> upstream: docs.index, ops.cohesion-coupling-policy, ops.document-management-workflow
> artifacts: `artifacts/unity/*.json`, `tools/stitch-unity/*.ps1`, `tools/unity-mcp/*.ps1`

이 문서는 JG 레포에서 `mechanical pass`와 `actual acceptance`를 섞어 보고하지 않기 위한 repo-wide 기준을 정한다.
막으려는 대상은 아래 세 가지다.

1. `빨리 진전 보여주기` 때문에 기준을 낮추는 것
2. 어려운 부분을 정면으로 다루지 않고 우회하는 것
3. 성공 기준을 작업 중 임의로 바꾸는 것

문서 역할 분리 원칙은 `ops.document-management-workflow`를 따른다.
즉 규칙 본문은 이 문서가 소유하고, `AGENTS.md`와 repo-local skill entry는 이 문서를 읽게만 한다.

이 규칙은 `실질 구현/검증/판단이 들어가는 작업`에 적용한다.
단순 질의응답이나 trivial fact reply에는 무겁게 적용하지 않는다.

## 적용 범위

아래에 해당하면 이 기준을 적용한다.

- 구현 결과가 acceptance와 맞는지 실제 판단해야 하는 작업
- visual fidelity, runtime correctness, smoke/test completeness처럼 `mechanical != acceptance`가 쉽게 갈라지는 작업
- closeout에서 `성공`, `완료`, `pass` 같은 표현을 쓰게 되는 작업

아래는 기본적으로 제외한다.

- trivial fact reply
- 단순 경로 안내, 파일 위치 확인, 로컬 탐색 요약
- owner doc에 아직 안 닿은 초기 탐색 단계의 가벼운 상태 보고

## acceptance 입력 우선순위

충돌이 날 때 우선순위는 아래로 고정한다.

1. 최신 사용자 직접 지시
2. 현재 작업에서 명시적으로 잠근 acceptance lock
3. `docs/index.md` 기준 current path의 active owner docs
4. reference / historical 문서
5. 내부 편의 해석

repo-local skill entry는 owner authority가 아니라 routing layer로만 본다.

## acceptance lock

`acceptance lock`은 현재 작업에서 acceptance를 임의로 흐리지 않기 위한 최소 템플릿이다.

- `대상`
- `맞아야 하는 것`
- `틀리면 실패인 것`
- `무엇과 비교할지`

시각 작업 예시:

- 대상: Set B Garage rebuild
- 맞아야 하는 것: accepted source와 주요 구조/내용/레이아웃 일치
- 틀리면 실패인 것: placeholder, skeleton, empty block, distorted layout
- 무엇과 비교할지: accepted source freeze + review evidence

비시각 작업 예시:

- 대상: lobby account restore regression fix
- 맞아야 하는 것: 지정된 실패 케이스가 재현되지 않고 기존 진입 흐름 유지
- 틀리면 실패인 것: fallback bypass, hidden repair path, broken smoke
- 무엇과 비교할지: failing reproduction + target test/smoke evidence

## mechanical과 acceptance 분리

작업 중 보고와 closeout에서는 아래 둘을 분리해서 본다.

- `mechanical status`
  - 스크립트 실행, lint, compile, capture, artifact 생성처럼 기계적으로 확인 가능한 상태
- `acceptance status`
  - 실제 결과가 시작 시 잠근 acceptance와 맞는지에 대한 판정

`mechanical pass`만으로 `acceptance success`를 대신하지 않는다.

## 판정 상태

owner doc 기준 판정은 아래 세 가지로 둔다.

- `success`
  - comparison이 끝났고 acceptance에 맞음
- `blocked`
  - 핵심 acceptance를 아직 판정할 수 없음
  - hard part가 미해결이거나 비교/검증이 끝나지 않음
- `mismatch`
  - comparison은 끝났고 결과가 acceptance와 다름

명시 규칙:

- placeholder / skeleton / 빈 블록 / 찌그러진 레이아웃은 `mismatch`
- 비교 자체를 아직 안 했으면 `blocked`
- mechanical pass여도 acceptance와 안 맞으면 `mismatch`
- 안 맞는지 아직 모르면 `blocked`

`blocked`와 `mismatch`는 둘 다 다음 단계 진행과 성공 표현을 막는다.

## hard part 처리 규칙

어려운 부분을 만나면 선택지는 둘뿐이다.

- `정면 해결`
- `blocked 선언`

금지:

- 쉬운 부분만 진행해서 성공처럼 보이게 만들기
- 골격/루프/캡처/산출물만 남기고 acceptance처럼 보고하기
- 실제 병목을 안 적고 주변 증거만 늘리기
- “일단 이 정도를 성공으로 보자” 식으로 기준 바꾸기

`blocked` 상태에서 허용되는 행동:

- 원인 명시
- 비교/증거 수집
- 로그 확인, 재현 확인, 원인 탐색
- 필요한 경우 사용자 재지시 또는 재합의 요청

## intermediate / final reporting

중간 보고와 최종 보고 모두 아래 의미는 분리해서 말해야 한다.

- acceptance lock
- mechanical status
- acceptance status
- blocked 또는 mismatch reason

강한 규칙:

- success처럼 읽히는 문장을 먼저 놓고 나중에 단서를 붙이지 않는다
- `blocked` 또는 `mismatch` 상태면 첫 문장에서 그 상태를 먼저 밝힌다
- mechanical completion을 acceptance completion처럼 보고하지 않는다

모든 자연어 표현을 workflow hard-fail로 전역 차단하지는 않는다.
wording rule은 owner doc과 review 기준이 먼저 맡고, script는 machine-checkable 항목만 다룬다.

## closeout 재대조

closeout이 있는 workflow에서는 아래 순서를 따른다.

1. 시작 시 잠근 acceptance lock을 다시 본다
2. 현재 mechanical 상태를 확인한다
3. 현재 acceptance 충족 여부를 대조한다
4. `success / blocked / mismatch` 중 하나로 단일 판정한다

규칙:

- acceptance 재대조 없이 success closeout 금지
- mechanical pass만으로 success closeout 금지
- lock과 다르면 `mismatch` 또는 `blocked`로 닫는다

## 최소 machine-checkable contract

이 문서는 workflow가 실제로 강제할 최소 contract만 소유한다.

### `blockedReason`

자동화 workflow가 아래 둘 중 하나를 기록할 때는 `blockedReason`을 남긴다.

- terminal `blocked` verdict
- `blocked` 때문에 다음 단계 전환을 멈춘 경우

`blockedReason`은 왜 현재 acceptance 또는 단계 전환이 막혔는지 설명하는 짧은 문자열이다.

### `advanceVerdict`

`advanceVerdict`는 visual staged workflow에서만 쓴다.

- 값: `match | mismatch`
- 용도: 사람이 다음 단계로 넘겨도 되는지 stage decision을 남김
- 기본 원칙: verdict 없으면 stage advance를 성공처럼 해석하지 않는다

모든 lane에 이 필드를 강제하지는 않는다.
해당 lane에 stage artifact가 실제로 있을 때만 도입한다.

## 문서 배치 규칙

- acceptance/reporting 본문은 이 문서가 소유한다
- entry 문서와 repo-local skill entry는 이 문서를 읽게만 한다
- domain별 acceptance의 세부 의미는 각 active owner doc이 소유한다

즉 `AGENTS.md`나 repo-local skill entry에 이 문서 본문을 장문으로 복제하지 않는다.
