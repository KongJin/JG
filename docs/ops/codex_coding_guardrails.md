# Codex Coding Guardrails

> 마지막 업데이트: 2026-05-02
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
2. 요청의 기술 표면을 먼저 분류하고 Skill Routing Gate를 적용한다.
3. `AGENTS.md -> docs/index.md -> 이 문서 -> 관련 lane owner 문서 -> 실제 코드/테스트` 순서로 읽는다.
4. 요청을 검증 가능한 target, success, failure, comparison으로 바꾼다.
5. 모호한 결정은 Clarification Loop로 잠그고, repo evidence로 답할 수 있는 질문은 먼저 탐색한다.
6. 같은 이유로 바뀌는 최소 범위를 정한다.
7. 구현 후 mechanical status와 actual acceptance를 분리해 보고한다.

## Skill Routing Gate

새 요청을 받으면 직전 작업 파일이나 진행 중인 수정 흐름보다 사용자 문장의 기술 표면을 우선한다.

- 사용자가 `UXML`, `USS`, `UI Toolkit`, `UITK`, `VisualElement`, `ScrollView`, `UIDocument`, `PanelSettings`, `navigation bar` 같은 UI Toolkit 표면을 말하면 `jg-unity-workflow`와 함께 `unity-uitoolkit`을 먼저 읽고 구조/스타일 owner를 확인한다.
- 사용자가 특정 global/external skill 이름을 직접 언급하면, 해당 skill을 읽기 전까지 같은 문제를 코드 추정으로 해결하지 않는다.
- 이미 만지던 C# adapter, smoke helper, runtime repair 흐름이 있어도 새 요청이 layout/style/scene/prefab/data contract 표면이면 해당 domain skill과 owner 문서로 다시 라우팅한다.
- domain-specific skill과 일반 skill이 함께 걸리면 domain-specific skill을 먼저 읽고, 일반 skill은 검증/가드레일로 붙인다.
- 적용하지 않은 obvious skill이 있으면 작업 전 사용자 업데이트나 closeout에 이유를 짧게 남긴다.

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

## Ambiguous Product Scope Gate

`빼다`, `제외`, `숨기다`, `비활성`, `삭제`, `되돌리다`, `정리`, `막다`처럼 제품 노출, 데이터 소유권, 저장/전투 동작, 생성 파이프라인 중 둘 이상으로 해석될 수 있는 지시는 바로 mutation하지 않는다.

- 먼저 UI 노출, playable catalog, 저장/전투 유효성, 생성 파이프라인, 원본 asset 보존 여부를 분리한다.
- repo evidence만으로 범위를 확정할 수 없으면 질문 하나로 scope를 잠근 뒤 실행한다.
- 질문 없이 진행할 수 있는 최대치는 명시적으로 되돌리기 쉬운 비파괴 변경이어야 하며, 데이터 삭제, generated owner 변경, pipeline 제외, save/load contract 변경은 포함하지 않는다.
- 사용자가 조급하거나 결론처럼 말해도, 제품 방향이나 UX 범위가 달라지는 해석이면 Clarification Loop를 우선한다.
- 이미 잘못 해석했다고 판단되면 추가 mutation으로 만회하지 말고 멈춘 뒤 현재 변경, 되돌릴 최소 범위, 필요한 사용자 결정을 짧게 보고한다.

## Removal Means Absence

사용자가 `A를 제거`, `A를 빼`, `A 없애`라고 지시한 뒤 범위가 잠겼다면 기본 결과는 "A가 제거됐다는 설명이 보이는 상태"가 아니라 "대상 표면에서 A가 있었는지 모르는 상태"다.

- 제품 UI, 선택 목록, generated catalog, 사용자-facing report, 테스트 이름, 문서의 현재 기준 문장에 `removed A`, `A excluded`, `A blocked` 같은 tombstone 표현을 남기지 않는다.
- 제거 사실을 남겨야 할 때는 사용자-facing 표면이 아니라 closeout, changelog, historical/reference artifact, migration note처럼 추적 전용 owner에만 둔다.
- 회귀 테스트는 "A가 제거됐다는 문구가 보인다"가 아니라 "A가 조회/선택/생성/표시되지 않는다"를 검증한다.
- 생성 파이프라인은 제거 대상 asset을 계속 재생성하거나 catalog/report에 다시 노출하지 않아야 한다.
- 예외는 법적 고지, 저장 데이터 migration, 호환성 오류 메시지처럼 사용자가 알아야 안전한 경우뿐이며, 그 경우에도 owner와 제거 조건을 명시한다.

## Minimal Cohesive Changes

- 요청된 목표를 만족하는 최소 동작을 구현한다.
- 같은 이유로 바뀌는 파일만 같은 작업에 포함한다.
- 일회성 코드에 추상화를 만들지 않는다.
- 미래 유연성, 새 설정, 새 fallback, 새 extension point는 현재 성공 기준에 필요할 때만 추가한다.
- 줄 수, 파일 수, DRY는 보조 기준이다. 응집도 판단은 `ops.cohesion-coupling-policy`를 따른다.

## Fail-Closed Contract Rule

contract 누락을 fallback으로 덮는 구현은 작은 편의가 아니라 acceptance를 거짓으로 만드는 문제다.
scene/prefab wiring, asset catalog/profile, network payload, UI token, visual preview, generated metadata처럼 owner가 따로 있는 값이 없거나 `pending/review/unsure` 상태이면 production 경로는 fail-closed로 동작해야 한다.

- `direction-only`, bounds, generated default, `pending`, `review`, `unsure` 같은 불완전한 신호를 `auto_ok`, normal preview, normal gameplay, 또는 acceptance success로 승격하지 않는다.
- 필수 contract가 없으면 정상 결과를 흉내 내지 말고 `blocked`, `mismatch`, explicit unavailable state, validation failure, 또는 test failure로 드러낸다.
- fallback이 필요한 경우 먼저 `domain default`, `compat adapter`, `explicit unavailable state` 중 하나로 분류하고 owner, 이유, 제거 조건, regression check를 남긴다.
- production controller, setup, page controller는 contract 누락 보정 owner가 아니다. truth owner는 serialized contract, asset/profile data, adapter/helper, 또는 domain rule이어야 한다.
- UI/preview 경로에서는 깨진 결과를 그럴듯하게 배치하지 않는다. 승인되지 않은 data/profile은 placeholder, disabled state, review-required state처럼 사용자가 실패를 볼 수 있는 상태로 렌더한다.
- 새 code change가 fallback, hidden lookup, runtime repair, `auto_ok` promotion, review/pending success path를 건드리면 `jg-no-silent-fallback` route를 적용하고 fail-closed regression check를 우선한다.

## Refactor Slice Lite

리팩터는 동작을 바꾸지 않는 정리와 동작 변경을 분리한다.
목표는 GitHub issue나 긴 RFC를 만드는 것이 아니라, 각 단계가 작고 검증 가능한 상태로 남게 하는 것이다.

1. 현재 상태를 먼저 확인한다.
   - 사용자 설명을 그대로 가정하지 말고 관련 코드, caller, tests, owner 문서로 실제 문제를 확인한다.
   - 리팩터가 해결할 friction과 바꾸지 않을 동작을 한 문장씩 분리한다.
2. scope와 out-of-scope를 잠근다.
   - API/schema/scene contract/UX/product judgment가 바뀌면 리팩터가 아니라 별도 behavior or design change로 분리한다.
   - 같은 이유로 바뀌지 않는 cleanup, rename, dead-code removal은 섞지 않는다.
3. test coverage와 feedback loop를 확인한다.
   - 기존 compile/test/static/smoke 중 refactor regression을 잡을 수 있는 가장 좁은 check를 고른다.
   - 테스트 표면이 부족하면 먼저 behavior check를 만들거나, 불가능한 이유를 testability residual로 남긴다.
4. 작은 reviewable step으로 나눈다.
   - 각 step은 끝났을 때 codebase가 동작 가능한 상태여야 한다.
   - rename/move, seam extraction, behavior-preserving cleanup, behavior change를 한 step에 섞지 않는다.
   - path 목록보다 책임, interface, caller impact, validation decision을 우선 기록한다.
5. 단계마다 검증한다.
   - 큰 리팩터를 한 번에 구현한 뒤 마지막에만 검증하지 않는다.
   - 실패하면 마지막 안전한 step 기준으로 원인과 남은 residual을 분리한다.

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
- 같은 fixture, setup, assertion 구조의 테스트가 2개를 넘으면 closeout 전에 parameterized test나 helper로 접는다. 서로 다른 failure owner를 검증하는 경우만 예외로 둔다.
- 정확한 regression seam이 없으면 얕은 false-confidence test를 만들지 말고 `blocked` 또는 testability residual로 보고한다.

## Validation First

- 시작 전에 성공 기준과 검증 방법을 짧게 정한다.
- 버그 수정은 가능하면 재현 또는 실패 기준을 먼저 확인한다.
- 리팩터는 전후 테스트, compile, lint, static check 중 해당 owner에 맞는 기준으로 회귀를 확인한다.
- 테스트가 비현실적이면 이유와 대체 검증을 남긴다.
- mechanical pass를 actual acceptance success처럼 보고하지 않는다.

## Recurrence Carryover Lite

버그 수정, 회귀 수정, 기술부채 cleanup, 규칙/파이프라인 수정은 closeout 전에 `ops.acceptance-reporting-guardrails`의 Recurrence Check를 적용한다.
체크는 세 문항으로 충분하다: 증상을 재현했는가, 원인을 증거로 확인했는가, 재발방지를 어디에 남길 것인가.
저장 위치는 `ops.document-management-workflow`의 Recurrence Carryover를 따르고, 모든 문제를 새 문서나 새 hard-fail로 만들지 않는다.

## Reporting

- `success`: 기준과 실제 결과를 비교했고 맞다.
- `blocked`: 핵심 acceptance를 아직 판정할 수 없다.
- `mismatch`: 비교가 끝났고 기준과 다르다.
- closeout에서는 변경 범위, 검증 결과, 남은 리스크를 분리한다.
- 규칙, owner, skill trigger를 바꾼 작업은 `doc lifecycle checked`와 `skill trigger checked` 필요 여부를 확인하고, skill route/trigger 변경이면 `ops.skill-routing-registry`와 `ops.skill-trigger-matrix`를 대조한다.
