# Codex Coding Guardrails

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.codex-coding-guardrails
> role: ssot
> owner_scope: Codex coding implementation guardrails, assumption handling, minimal cohesive changes, surgical edits, validation-first execution
> upstream: docs.index, ops.document-management-workflow, ops.cohesion-coupling-policy, ops.acceptance-reporting-guardrails
> artifacts: none

이 문서는 JG 레포에서 Codex가 구현, 버그 수정, 리팩터, 테스트 보강을 할 때 적용하는 일반 코딩 가드레일의 SSOT다.
목표는 LLM 코딩 오류를 줄이되, 기존 owner 문서와 코드 선례로 안전하게 판단할 수 있는 일을 불필요하게 멈추지 않는 것이다.

## 적용 범위

적용한다:

- 코드 구현, 버그 수정, 리팩터, 테스트/검증 보강
- 구현 전에 가정, 해석, 성공 기준을 잠가야 하는 작업
- 요청 범위가 과하거나 부족해 보이는 작업
- LLM coding error, 과잉 구현, 추측 구현, 검증 누락을 줄이는 작업

제외한다:

- 단순 질의응답이나 경로 안내
- 문서 lifecycle 자체가 주 작업인 경우. 그 경우 `ops.document-management-workflow`를 우선한다.
- Unity scene/prefab/UI authoring 고유 규칙. 그 경우 해당 lane owner를 우선한다.

## 기본 순서

1. 현재 collaboration mode가 mutation을 허용하는지 먼저 확인한다.
2. `AGENTS.md -> docs/index.md -> 이 문서 -> 관련 lane owner 문서 -> 실제 코드/테스트` 순서로 읽는다.
3. 요청을 검증 가능한 target, success, failure, comparison으로 바꾼다.
4. 모호한 결정은 Clarification Loop로 잠그고, repo evidence로 답할 수 있는 질문은 먼저 탐색한다.
5. 같은 이유로 바뀌는 최소 범위를 정한다.
6. 구현 후 mechanical status와 actual acceptance를 분리해 보고한다.

## Mutation Gate

- Plan Mode에서는 실행형 요청도 실행 계획 요청으로 해석하고, repo-tracked 파일을 수정하지 않는다.
- Default mode처럼 mutation이 허용된 턴에서만 코드, 문서, skill, scene/prefab, artifact를 수정한다.
- 현재 모드와 사용자 요청이 충돌하면 모드를 우선하고, 가능한 non-mutating 탐색으로 계획을 구체화한다.

## Assumption Handling

- 먼저 repo 문서, 코드 선례, 타입, 테스트, 설정으로 확인한다.
- 확인 가능한 사실은 질문하지 않고 보수적으로 따른다.
- 탐색으로 해결되지 않고 제품 방향, UX, 아키텍처, API/DB/scene contract처럼 되돌리기 어려운 결정이면 질문한다.
- 여러 해석이 모두 유효하고 결과가 달라지면 해석 후보와 추천안을 짧게 드러낸다.
- 가설은 검증 전까지 원인이나 성공 근거로 쓰지 않는다.

## Clarification Loop

구현 전에 plan, design, request, acceptance가 흐리면 먼저 shared understanding을 만든다.
목표는 질문을 늘리는 것이 아니라, 잘못 구현하면 비싼 결정을 시작 전에 드러내는 것이다.

1. 결정 트리를 나눈다.
   - target, success/failure 기준, owner, out-of-scope, 검증 방법 중 비어 있는 항목을 찾는다.
   - domain term, UI label, scene contract, API/schema, data owner처럼 여러 뜻이 가능한 표현을 표시한다.
2. 질문 전에 탐색한다.
   - 관련 owner 문서, 코드 선례, 타입, 테스트, serialized scene/prefab contract, tool README로 답이 나오면 그 근거를 따른다.
   - 코드와 사용자 설명이 충돌하면 충돌 지점을 짧게 제시하고 어떤 기준을 우선할지 묻는다.
3. 질문은 하나씩 한다.
   - 한 번에 여러 결정을 묶어 묻지 않는다.
   - 각 질문에는 추천 답과 그 trade-off를 함께 둔다.
   - 답변에 따라 다음 질문이 달라지는 dependency를 먼저 묻는다.
4. 결정은 올바른 owner에 남긴다.
   - 현재 세션에서만 필요한 해석은 final이나 작업 요약에 남긴다.
   - 반복해서 쓰일 domain language는 관련 `design/*` owner에 둔다.
   - 되돌리기 어렵고 나중에 다시 제안될 가능성이 있는 trade-off는 새 문서보다 기존 ops/design/plan owner에 짧게 남길 수 있는지 먼저 판단한다.
   - `CONTEXT.md`, `docs/adr/`, `docs/agents/`를 JG의 새 기본 구조로 만들지 않는다.

## Minimal Cohesive Changes

- 요청된 목표를 만족하는 최소 동작을 구현한다.
- 같은 이유로 바뀌는 파일만 같은 작업에 포함한다.
- 일회성 코드에 추상화를 만들지 않는다.
- 미래 유연성, 새 설정, 새 fallback, 새 extension point는 현재 성공 기준에 필요할 때만 추가한다.
- 줄 수, 파일 수, DRY는 보조 기준이다. 응집도 판단은 `ops.cohesion-coupling-policy`를 따른다.

## Surgical Edits

- 꼭 필요한 기존 코드만 수정하고, 주변 코드의 스타일 개선이나 unrelated cleanup을 섞지 않는다.
- 기존 스타일이 마음에 들지 않아도 현재 파일과 feature의 패턴을 따른다.
- 이번 변경이 만든 unused import, unused variable, unreachable branch는 제거한다.
- 이번 변경과 무관한 기존 dead code나 이상 징후는 삭제하지 말고 보고한다.
- dirty worktree가 있으면 이번 변경과 기존 변경을 분리해 다룬다.

## Behavior-First Test Loop

테스트를 추가하거나 수정할 때는 구현 모양보다 관찰 가능한 동작을 먼저 잡는다.
목표는 test count를 늘리는 것이 아니라, 실제 caller/user path가 깨졌을 때 흔들리지 않는 피드백 루프를 만드는 것이다.

- 테스트 이름과 검증 기준은 public behavior를 설명한다.
- private method, 내부 호출 순서, 임시 자료구조처럼 refactor만 해도 깨지는 세부 구현을 주 테스트 표면으로 삼지 않는다.
- mock은 외부 API, 시간, randomness, filesystem, network, Unity runtime처럼 통제하기 어려운 경계에 우선 사용한다. 내부 collaborator를 mock해야만 테스트가 가능하다면 owner boundary나 test seam을 재검토한다.
- 새 feature나 bugfix가 test-first에 맞으면 한 번에 하나의 behavior만 `RED -> GREEN -> refactor`로 진행한다.
- 계획된 테스트를 먼저 전부 쓰고 나중에 구현을 몰아서 붙이지 않는다. 각 cycle에서 배운 내용을 다음 behavior와 test seam에 반영한다.
- GREEN 전에는 refactor를 섞지 않는다. 모든 관련 check가 통과한 뒤에만 duplication 제거, module deepening, 이름 정리 같은 refactor를 분리한다.
- 정확한 regression seam이 없으면 얕은 false-confidence test를 만들지 말고 `blocked` 또는 testability residual로 보고한다.

## Validation First

- 시작 전에 성공 기준과 검증 방법을 짧게 정한다.
- 버그 수정은 가능하면 재현 또는 실패 기준을 먼저 확인한다.
- 리팩터는 전후 테스트, compile, lint, static check 중 해당 owner에 맞는 기준으로 회귀를 확인한다.
- 테스트가 비현실적이면 이유와 대체 검증을 남긴다.
- mechanical pass를 actual acceptance success처럼 보고하지 않는다.

## Reporting

- `success`: 기준과 실제 결과를 비교했고 맞다.
- `blocked`: 핵심 acceptance를 아직 판정할 수 없다.
- `mismatch`: 비교가 끝났고 기준과 다르다.
- closeout에서는 변경 범위, 검증 결과, 남은 리스크를 분리한다.
- 규칙, owner, skill trigger를 바꾼 작업은 `doc lifecycle checked`와 `skill trigger checked` 필요 여부를 확인한다.
