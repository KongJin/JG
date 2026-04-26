# Code And Docs Simplification Recurrence Prevention Plan

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.code-doc-simplification-recurrence-prevention
> role: plan
> owner_scope: 코드 helper 중복, one-use shared abstraction, plan SSOT 중복, generated artifact diff 비대화 재발 방지 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow, plans.progress
> artifacts: `Assets/Scripts/`, `docs/plans/*.md`, `docs/index.md`, `artifacts/unity/`, `artifacts/rules/`

이 문서는 새 아키텍처 규칙이나 lint hard-fail을 만들지 않는다.
목표는 이번 간소화 pass에서 확인된 재발 패턴을 다음 작업 closeout 전에 빠르게 잡는 것이다.

## Problem Pattern

이번 작업에서 다시 나타난 패턴:

- 같은 값 생성 로직이 두 runtime 파일에 따로 생겼다. 예: BattleEntity network id 포맷.
- 한 곳에서만 쓰는 helper가 shared namespace로 올라갔다. 예: one-use material writer.
- 새 helper 파일이 생성됐지만 generated `.csproj`가 바로 따라오지 않아 compile-clean을 한 번 깨뜨렸다.
- parent plan과 child plan이 같은 Phase 5 acceptance를 동시에 자세히 소유했다.
- generated evidence artifact가 전체 dirty worktree를 담으면서 실제 변경보다 diff가 커졌다.

2026-04-26 Garage 간소화 pass에서 추가 확인한 패턴:

- page controller가 input, save async flow, chrome state sync, draft evaluation을 동시에 들고 있으면 작은 변경도 넓은 diff로 번진다.
- 새 `.cs` helper 파일이 generated project sync보다 먼저 compile path에 의존하면, 안전한 책임 분리도 compile-clean을 깨뜨릴 수 있다.
- 임시로 기존 컴파일 포함 파일에 internal helper를 넣을 수는 있지만, 파일 이름과 실제 책임이 어긋나면 다음 정리 후보로 남겨야 한다.
- UI 상태 문구와 색상 선택이 중첩 삼항식으로 들어가면 상태별 검토가 어려워진다.
- compile 실패가 이번 변경이 아닌 dirty worktree의 다른 파일에서 나올 수 있으므로, repo-level 실패와 changed-file 영향 범위를 분리해서 보고해야 한다.

## Scope

포함:

- gameplay/runtime 코드에서 작은 중복 helper를 정리하는 기준
- shared helper로 올릴지 inline으로 둘지 판단하는 closeout 체크
- plan 문서의 parent/child active owner 중복 점검
- generated artifact 갱신 시점과 dirty worktree 분리
- compile/lint/hygiene 검증 순서

제외:

- 새 analyzer, 새 hard-fail lint, 새 CI gate 추가
- 기존 active owner 문서의 규칙 본문 개정
- Unity scene/prefab 자동 mutation
- 제품 우선순위 재판정
- 대규모 리팩터나 폴더 구조 개편

## Prevention Checklist

작업 closeout 전 아래만 확인한다.

1. 같은 문자열 포맷, key 생성, mapper, queue/drain 패턴이 두 곳 이상 생겼는가?
2. 새 shared helper가 한 호출처만 갖는가?
3. 새 `.cs` 파일을 만들었는데 Unity/Bee compile이나 generated csproj가 즉시 따라오지 않는가?
4. 새 active plan이 기존 active parent plan의 acceptance를 장문으로 다시 소유하는가?
5. generated artifact가 이번 작업 파일보다 넓은 dirty worktree를 증거로 담는가?
6. status label을 바꿨다면 `docs.index`와 header가 함께 맞는가?
7. controller 하나가 input, async command, view state, evaluation을 함께 직접 처리하는가?
8. helper를 새 파일 대신 기존 파일에 넣었다면 파일 책임과 이름이 어긋나는가?
9. UI 문구, 색상, 상태 선택이 중첩 삼항식으로 남아 있는가?
10. compile 실패가 changed file이 아닌 기존 dirty file에서 발생했는가?

판단:

- 1번이 true면 helper를 한 곳으로 모으거나 한 파일 안의 internal helper로 먼저 둔다.
- 2번이 true면 shared helper 승격을 보류하고 호출부에 inline한다.
- 3번이 true면 새 파일 유지보다 기존 컴파일 포함 파일 안의 작은 internal helper를 우선한다.
- 4번이 true면 parent plan은 reference/summary로 낮추고 상세 acceptance는 child plan 하나가 소유한다.
- 5번이 true면 artifact를 덮어쓰기 전에 이번 작업 evidence인지 분리한다. 분리할 수 없으면 residual로 남긴다.
- 6번이 false면 closeout 전에 registry를 맞춘다.
- 7번이 true면 한 pass에 한 책임만 빼낸다. input, save flow, chrome state, evaluation 중 가장 독립적인 것부터 시작한다.
- 8번이 true면 compile-sync 이후 rehome 후보로 기록한다. 단, generated `.csproj`를 직접 patch하지 않는다.
- 9번이 true면 상태별 named method로 분리해 UI 반영 코드와 판단 코드를 나눈다.
- 10번이 true면 scope를 넓히지 않고, 이번 변경 검증과 repo-level blocker를 closeout에서 분리한다.

## Execution Plan

### Phase 1 - Code Duplication Sweep

- 최근 변경 파일에서 `Build*Id`, `*Key`, `*Mapper`, queue/drain, one-use writer/helper를 검색한다.
- 두 파일 이상에서 같은 포맷을 만들면 한 helper로 모은다.
- 한 호출처만 있는 shared helper는 호출부로 되돌린다.

Acceptance:

- 새 helper는 최소 두 호출처 또는 분명한 owner boundary가 있다.
- runtime id/key 포맷 변경 지점이 하나다.

### Phase 2 - New File Compile Guard

- 새 `.cs` 파일을 추가했으면 즉시 compile-clean을 확인한다.
- generated `.csproj`가 stale일 때는 Unity/Bee compile truth를 우선하고, generated project file을 직접 patch하지 않는다.
- 작은 helper는 새 파일보다 기존 관련 파일의 internal type으로 끝낼 수 있는지 먼저 본다.

Acceptance:

- `tools/check-compile-errors.ps1`가 errors 0으로 통과한다.
- 새 helper 파일 때문에 compile이 깨졌다면 helper 위치를 줄이거나 Unity project sync를 명시한다.

### Phase 3 - Plan SSOT Sweep

- 새 active plan을 만들거나 Phase를 분리했으면 기존 parent plan이 여전히 active execution owner인지 확인한다.
- parent plan에는 scope/handoff만 남기고, detailed acceptance는 직접 실행 plan 하나가 소유하게 한다.
- 상태를 바꾸면 문서 header와 `docs.index` registry를 함께 맞춘다.

Acceptance:

- 같은 Phase acceptance가 두 active plan에 장문으로 반복되지 않는다.
- `plans.progress`는 code path 완료와 actual acceptance 미완료를 구분한다.

### Phase 4 - Generated Artifact Diff Guard

- `artifacts/unity/*.json`, `artifacts/rules/*.json`가 바뀌면 이번 작업 변경 목록만 담는지 확인한다.
- 전체 dirty worktree가 artifact에 들어가면 generated evidence를 최종 closeout 전에만 갱신하거나 residual로 분리한다.
- 이미 다른 작업의 dirty semantic state가 있으면 덮어쓰지 않는다.

Acceptance:

- artifact diff가 이번 작업 evidence로 설명된다.
- 설명할 수 없으면 success evidence가 아니라 residual/blocked로 보고한다.

### Phase 5 - Verification And Closeout

- code-only 또는 mixed 변경이면 compile-clean을 먼저 본다.
- docs/plan 변경이면 `npm run --silent rules:lint`를 본다.
- Unity asset/meta가 추가됐으면 `npm run --silent unity:asset-hygiene`를 본다.
- 재발 방지 자체가 작업 대상이어도 새 lint나 artifact를 만들지 않았다면 `rules:sync-closeout`는 기본 실행하지 않는다.

Acceptance:

- compile/lint/hygiene 결과가 closeout에 분리되어 있다.
- mechanical pass와 actual acceptance가 섞이지 않는다.

### Phase 6 - Garage Controller Follow-up Guard

- `GaragePageController`는 다음 간소화에서도 한 번에 한 책임만 줄인다.
- `GaragePageScrollController.cs`에 함께 들어간 keyboard shortcut helper는 generated project sync 이후 전용 파일로 옮길지 재검토한다.
- `GaragePageChromeController`처럼 상태 문구/색상 판단은 named helper로 분리하고, view mutation method 안에 중첩 상태식을 다시 늘리지 않는다.
- compile-clean이 다른 dirty file에 막히면 이번 변경 파일 diff, rules lint, repo-level compile blocker를 각각 분리해 남긴다.

Acceptance:

- Garage controller 간소화 diff가 input/save/chrome/evaluation 중 한 책임에 집중된다.
- 임시 helper 위치가 장기 owner처럼 굳지 않는다.
- UI 상태 판단은 상태 이름으로 읽히고, nested ternary가 closeout 전에 눈에 띄면 풀린다.
- repo-level compile blocker가 있어도 changed-file 영향 범위가 따로 보고된다.

## Residual Handling

- 새 shared helper가 정말 필요한지 애매하면 inline으로 두고 다음 두 번째 호출처가 생길 때 승격한다.
- parent plan reference 전환이 제품 우선순위 판단을 요구하면 상태 변경은 보류하고 lifecycle note만 남긴다.
- generated artifact가 dirty worktree를 넓게 담았지만 다른 작업 evidence일 수 있으면 덮어쓰지 않는다.
- 자동화가 필요해 보이면 바로 lint를 만들지 말고 한 번 더 같은 패턴이 반복될 때 별도 tooling plan으로 분리한다.
- generated project sync 때문에 helper를 부자연스러운 파일에 둔 경우, 기능 변경으로 묶어 고치지 말고 별도 rehome pass로 분리한다.
- compile blocker가 다른 dirty 작업에서 온 경우, 그 파일을 임의로 고치거나 되돌리지 않고 blocker owner를 분리해 보고한다.

## Validation

기본 검증:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1
npm run --silent rules:lint
npm run --silent unity:asset-hygiene
```

이 문서만 수정하는 경우 compile/hygiene은 생략 가능하고 `rules:lint`만 필수로 본다.

## Closeout Criteria

- 이번 재발 패턴이 code, docs, artifact로 나뉘어 보인다.
- 새 규칙이나 hard-fail 없이 다음 작업에서 쓸 체크포인트가 있다.
- 기존 문서관리 recurrence plan과 중복되지 않는다.
- `docs.index`에서 찾을 수 있다.
- Garage helper 임시 위치와 controller 책임 분리의 다음 판단 조건이 보인다.
- `npm run --silent rules:lint`가 통과하거나 실패 원인이 분리되어 있다.

## 문서 재리뷰

- 새 문서 생성 판단: 기존 `document_management_recurrence_prevention_plan.md`는 index/lifecycle/wording 중심이고, 이번 이슈의 runtime helper 중복과 generated artifact diff 비대화까지 소유하지 않는다. 따라서 code+docs mixed recurrence를 별도 active plan으로 둔다.
- 과한점 리뷰: 새 lint, 새 artifact, 새 status, 새 architecture rule을 만들지 않고 closeout checklist와 실행 순서만 둔다.
- 부족한점 리뷰: owner/scope, 제외 범위, problem pattern, checklist, phases, acceptance, residual, validation, closeout criteria를 포함했다.
- 수정 후 재리뷰: generated artifact 처리는 덮어쓰기 지시가 아니라 residual 분리 기준으로 낮췄고, helper 승격도 hard rule이 아니라 호출처 수와 owner boundary 판단으로 제한했다.
- owner impact: primary `plans.code-doc-simplification-recurrence-prevention`; secondary `docs.index`, `plans.progress`, runtime feature files, generated evidence artifacts; out-of-scope new lint, new artifact, policy owner rewrite, product priority changes.
- doc lifecycle checked: 새 active plan으로 등록한다. 기존 문서관리 recurrence reference plan은 대체하지 않고, 이 plan은 code/docs mixed recurrence가 닫힌 뒤 reference 전환 후보로 본다.
- skill trigger checked: not changed.
- plan rereview: clean
- 2026-04-26 Garage 간소화 후 보강: controller 책임 집중, generated project sync와 새 helper 파일, 임시 helper 위치, UI 상태 nested ternary, dirty worktree compile blocker 분리 기준을 추가했다.
- 과한점 재리뷰: 새 lint, 새 artifact, 새 hard rule을 만들지 않고 기존 checklist/phase에 판단 기준만 추가했다.
- 부족한점 재리뷰: Garage-specific residual과 acceptance를 추가해 다음 간소화 작업에서 같은 문제가 다시 섞이지 않게 했다.
- 수정 후 재리뷰: 임시 helper rehome은 즉시 실행 조건이 아니라 generated project sync 이후 별도 pass 후보로 낮췄고, compile blocker는 scope 확장이 아니라 보고 분리 기준으로 남겼다.
- plan rereview: clean
- 2026-04-26 execution start: Phase 6 첫 조각으로 `GaragePageController`의 draft compose/validate 세부 로직을 `GarageDraftEvaluator`로 옮겼다. 새 `.cs` 파일은 만들지 않고 기존 compile target인 `GarageDraftEvaluation.cs` 안에서 처리했다.
- execution residual: `GaragePageScrollController.cs`에 함께 있는 keyboard shortcut helper는 generated project sync 이후 전용 파일 rehome 후보로 남긴다.
- execution verification: `tools/check-compile-errors.ps1`, `npm run --silent rules:lint`, `git diff --check` 통과.
- execution rereview: 과한점 없음. 새 lint, 새 artifact, 새 generated project patch를 만들지 않았다. 부족한점은 keyboard helper rehome residual로 분리했다.
- plan rereview: clean
- 2026-04-26 execution pass: 현재 worktree에 체크리스트를 적용했다. `BattleEntityNetworkId`는 단일 helper로 모여 있고, one-use `UiRendererMaterialWriter`는 제거됐다. Agent A parent plan은 reference이고 Phase 5 상세 acceptance는 전용 active plan이 소유한다.
- residual: `PendingArrivalBuffer<T>`는 두 registry가 쓰지만 generated project sync를 피하려고 `PlayerSceneRegistry.cs` 안의 internal helper로 남아 있다. 기능 변경 없이 rehome할 수 있는 compile-sync pass가 생기면 별도 정리 후보로 본다.
- residual: `artifacts/unity/unity-ui-authoring-workflow-policy.json`와 `artifacts/rules/issue-recurrence-closeout.json`는 넓은 dirty worktree를 담은 generated evidence일 수 있어 이번 pass에서 덮어쓰지 않는다.
- doc lifecycle checked: 실행 pass가 끝났으므로 이 plan은 active 실행 계획이 아니라 reference 체크리스트로 내린다. `docs.index` 상태 라벨도 함께 맞춘다.
- plan rereview: clean
