# Acceptance Reporting Guardrails

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: ops.acceptance-reporting-guardrails
> role: ssot
> owner_scope: mechanical status와 actual acceptance 분리, success/blocked/mismatch 판정, 최소 closeout contract
> upstream: docs.index, ops.document-management-workflow
> artifacts: `artifacts/rules/issue-recurrence-closeout.json`

이 문서는 `ops.document-management-workflow`의 Closeout 원칙을 구현/검증 보고에 적용하는 기준만 소유한다.
목표는 mechanical pass를 actual acceptance처럼 보고하지 않는 것이다.

## 적용 범위

적용한다:

- 구현 결과가 acceptance와 맞는지 판단해야 하는 작업
- visual fidelity, runtime correctness, smoke/test completeness처럼 mechanical status와 acceptance가 갈라질 수 있는 작업
- closeout에서 성공, 완료, pass 표현을 쓰는 작업

제외한다:

- 단순 질의응답
- 경로 안내, 파일 위치 확인, 로컬 탐색 요약
- owner 판단 전의 가벼운 상태 보고

## 입력 우선순위

충돌이 나면 아래 순서로 본다.

1. 최신 사용자 직접 지시
2. 현재 작업의 acceptance lock
3. `docs/index.md` 기준 active owner docs
4. reference / historical 문서
5. 내부 편의 해석

repo-local skill entry는 owner authority가 아니라 routing layer다.

## Mechanical != Acceptance

- mechanical status: lint, compile, smoke 실행, capture 생성, artifact 생성처럼 기계적으로 확인 가능한 상태
- acceptance status: 실제 결과가 시작 시 잠근 기준과 맞는지에 대한 판정

mechanical pass만으로 acceptance success를 대신하지 않는다.

## 판정

- `success`: 비교가 끝났고 acceptance에 맞음
- `blocked`: 핵심 acceptance를 아직 판정할 수 없음
- `mismatch`: 비교가 끝났고 결과가 acceptance와 다름

규칙:

- 비교 전이면 `blocked`
- 비교 후 다르면 `mismatch`
- hard part가 남았으면 `blocked`
- 기준을 바꿔 success로 만들지 않는다

## Silent Lane Escalation

`silent lane escalation`은 시작한 작업 lane의 acceptance를 만족하지 못했을 때, 같은 closeout 안에서 공통 capability, policy, lint, tooling, evidence gate를 수정해 성공 판정을 만들려는 상태다.

금지 기준:

- `unsupported` 또는 `blocked`가 나온 뒤 같은 작업에서 기준/도구를 바꿔 원래 lane의 `success`로 보고하지 않는다.
- feature onboarding, visual fidelity, runtime correctness evidence와 공통 policy/tooling 변경을 섞으면 기본값은 `blocked`다.
- capability expansion이 필요하면 먼저 lane을 capability expansion으로 재선언하고, 기존 onboarding이나 feature closeout과 별도로 검증한다.
- escalation 여부를 판단하지 못하면 `success` 대신 `blocked`로 보고한다.

## Root Cause Investigation

문제 원인을 파악할 때는 확인된 사실과 아직 검증되지 않은 가설을 분리한다.
원인 분석은 항상 가설 검증 루프를 포함한다.

- `확인된 사실`: 코드, 로그, 테스트, 실행 결과, owner 문서, artifact로 확인한 내용
- `가설`: 아직 원인으로 확정하지 않은 후보
- `검증 방법`: 재현, 로그 확인, 테스트, 코드 추적, artifact 비교처럼 가설을 확인하거나 기각할 방법
- `판정`: 검증 후 맞음, 아님, 또는 아직 판정 불가인지에 대한 결론

가설 검증 루프:

1. 관찰된 증거에서 가능한 원인 후보를 세운다.
2. 각 후보가 맞다면 보여야 할 증거와, 틀리다면 없어야 할 증거를 정한다.
3. 코드, 로그, 테스트, 실행 결과, owner 문서, artifact로 확인한다.
4. 확인된 후보만 원인으로 판정하고, 확인하지 못한 후보는 가설 또는 `blocked`로 남긴다.

규칙:

- 검증 전 가설을 `rootCause`로 쓰지 않는다.
- `아마`, `추정`, `가능성`, `보임`, `듯`, `것 같`, `maybe`, `probably`, `likely`, `appears`, `seems`처럼 불확실한 표현이 남아 있으면 원인 확정이 아니다.
- 원인을 아직 좁히지 못했으면 `blockedReason`으로 닫고 `success` 표현을 쓰지 않는다.

## Recurrence Check

버그 수정, 회귀 수정, 기술부채 cleanup, 규칙/파이프라인 수정처럼 같은 문제가 다시 날 수 있는 작업은 closeout 전에 세 가지를 확인한다.

- 증상을 실제 feedback loop로 재현했거나, 재현 불가 이유를 `blocked`로 남겼는가?
- 원인을 코드, 로그, 테스트, 실행 결과, owner 문서, artifact 중 하나 이상의 증거로 확인했는가?
- 재발방지를 final, owner 문서/plan/progress, recurrence closeout shard 중 어디에 남길지 판단했는가?

증상 재현 또는 원인 검증이 없으면 원인 확정이나 예방 완료로 보고하지 않는다.
저장 위치 판단은 `ops.document-management-workflow`의 Recurrence Carryover를 따른다.

## Acceptance Lock

필요한 작업에서는 시작 시 최소 네 가지를 잠근다.

- 대상
- 맞아야 하는 것
- 틀리면 실패인 것
- 무엇과 비교할지

lock 없이도 명확한 작은 작업은 이 템플릿을 생략할 수 있지만, closeout 전에는 실제 비교 기준을 설명할 수 있어야 한다.

## Fresh Evidence Discipline

visual fidelity, capture, runtime smoke, generated artifact처럼 evidence가 시간에 따라 바뀌는 작업은 현재 판정 기준과 과거 비교 자료를 분리한다.

규칙:

- 수정 후 판정은 현재 코드/현재 scene/현재 데이터로 생성한 최신 실행, 최신 캡쳐, 최신 artifact만 기준으로 한다.
- 최신 evidence를 아직 만들지 못했으면 `blocked: fresh evidence pending`으로 보고하고, 시각 판단이나 acceptance 판정을 하지 않는다.
- 과거 캡쳐나 이전 artifact는 사용자가 비교를 요청했거나 regression 범위를 설명할 때만 열고, `old`와 `current` 경로를 명시한다.
- 과거 evidence를 본 경우에도 현재 판정 문장에 섞지 않고, 참고 비교 결과와 현재 acceptance verdict를 분리한다.
- 새 evidence를 만들었으면 중간 보고와 closeout에 기준 파일 또는 실행 artifact 경로를 적어 어떤 자료를 보고 판단했는지 추적 가능하게 한다.

## Reporting

중간 보고와 최종 보고는 아래를 섞지 않는다.

- acceptance lock
- mechanical status
- acceptance status
- blocked 또는 mismatch reason

`blocked` 또는 `mismatch`라면 성공처럼 읽히는 문장을 먼저 두지 않는다.

## 최소 Machine-Checkable Contract

### `blockedReason`

자동화 workflow가 terminal `blocked` verdict를 남기거나 blocked 때문에 다음 단계 전환을 멈추면 `blockedReason`을 남긴다.

### `advanceVerdict`

visual staged workflow에서만 사용한다.

- 값: `match | mismatch`
- verdict 없으면 stage advance를 success처럼 해석하지 않는다.

### rules-only recurrence closeout

문서, repo-local skill, repo-maintained rule/script lane에서 closeout artifact를 운용할 때는 아래 필드를 사용한다.
모든 docs-only 변경이 이 artifact를 필요로 하는 것은 아니며, 필수 여부는 `ops.document-management-workflow`의 자동 검증 기준을 따른다.
기본 closeout artifact는 병렬 작업 충돌을 줄이기 위해 `artifacts/rules/issue-recurrence-closeout.d/*.json` shard를 사용하고, legacy aggregate는 `artifacts/rules/issue-recurrence-closeout.json`로 유지한다.

- `issueDetected`
- `declaredLane`
- `observedMutationClass`
- `acceptanceEvidenceClass`
- `escalationRequired`
- `rootCause`
- `prevention`
- `verification`
- `blockedReason`

규칙:

- `issueDetected = true`이면 `rootCause`, `prevention`, `verification`이 비어 있으면 안 된다.
- `declaredLane`, `observedMutationClass`, `acceptanceEvidenceClass`는 비어 있으면 안 된다.
- `escalationRequired = true`이면 `blockedReason` 없이 closeout하지 않는다.
- 원인을 아직 못 좁혔으면 `blockedReason`으로 닫고 success를 금지한다.
- closeout artifact를 쓰는 lane에서는 `verification`이 비어 있으면 안 되며, `rootCause`가 있으면 실행 명령, artifact path, owner path 같은 concrete evidence anchor를 함께 적는다.

lane별 hook/CI는 `changedPaths` 같은 보조 필드를 추가할 수 있지만, success/blocked/mismatch 의미는 이 문서가 소유한다.
rules-only closeout shard를 쓰는 경우 `changedPaths`에는 shard 파일 자신과 해당 shard가 닫는 rules-only target file을 함께 포함한다.
