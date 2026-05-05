# UITK Page Routing Refactor Plan

> 마지막 업데이트: 2026-05-05
> 상태: reference
> doc_id: plans.uitk-page-routing-refactor
> role: plan
> owner_scope: Lobby/Garage UITK runtime page routing 책임 분리, Shell/Layout + explicit page route 모델 적용, 과한 router framework 방지 실행 순서
> upstream: docs.index, design.ui-foundations, ops.unity-ui-authoring-workflow, ops.cohesion-coupling-policy, ops.acceptance-reporting-guardrails
> artifacts: `artifacts/unity/uitk-page-routing-refactor/`

이 문서는 현재 scene의 UITK 관리 방식을 Next.js의 Layout/Page 사고방식에 가깝게 정리하되, Unity에 맞지 않는 자동 라우터/프레임워크화를 피하기 위한 refactor 기록이다.
구현 대상은 Lobby shell의 page visibility, nav state, shell title/state orchestration이었으며, Garage의 page controller/presenter/usecase 흐름은 유지했다.

## Current Judgment

- 가져올 것: Next.js의 `layout -> page` 책임 분리 사고방식.
- 가져오지 않을 것: 파일 기반 자동 라우팅, reflection discovery, 범용 `IPage` framework, route loader/action abstraction.
- 현재 부족한 부분은 `LobbyUitkRuntimeAdapter`가 page route 전환, nav selected state, shell title/state, page-specific ensure 작업을 직접 들고 있어 page가 늘수록 비대해지는 점이다.
- 현재 과해질 수 있는 부분은 이 문제를 해결하려고 generic router framework를 만들거나 모든 page를 동일 interface에 억지로 맞추는 것이다.
- v1은 explicit route registry만 둔다. Unity serialized reference와 UXML named element contract는 계속 명시적으로 유지한다.

## Current Shape

```text
User
  -> UITK Button/VisualElement
  -> LobbyUitkRuntimeAdapter
  -> LobbyPageController
  -> Application UseCase
  -> Domain/Application state
  -> LobbyPageController
  -> LobbyPagePresenter + LobbyPageViewModels
  -> LobbyUitkRuntimeAdapter
  -> UITK
```

현재 `LobbyUitkRuntimeAdapter` 안에는 아래 책임이 함께 있다.

- UIDocument binding and UXML clone
- named element query
- room/account/operation view model rendering
- UI event bridge
- Garage host bridge
- page visibility toggle
- nav selected state
- shell title/state text
- page-specific ensure callbacks

이 중 page visibility, nav selected state, shell title/state, route enter callback만 shell routing helper로 분리한다.

## Target Shape

```text
Unity Scene
  -> LobbyPageController
      -> command/usecase orchestration
      -> presenter.Build(...)
      -> LobbyUitkRuntimeAdapter.Render(...)
      -> LobbyUitkRuntimeAdapter.ShowPage(...)
          -> LobbyShellPageRouter.Show(...)
```

```text
LobbyUitkRuntimeAdapter
  owns:
    - UIDocument binding
    - named element query
    - render(view model) to concrete UITK elements
    - UI event bridge
    - Garage host bridge

LobbyShellPageRouter
  owns:
    - explicit page route registry
    - page host display toggle
    - nav selected class toggle
    - shell title/state text
    - route enter callbacks such as EnsureRecordsSurface
```

`LobbyShellPageRouter`는 Presentation 내부 helper이며 새 architecture layer가 아니다. 다이어그램에서는 `LobbyUitkRuntimeAdapter`의 내부 routing helper로 취급한다.

## Naming And Contracts

- `LobbyShellPageId`: `Lobby`, `Garage`, `Records`, `Account`, `Connection`처럼 shell route id만 표현한다.
- `LobbyShellPageRoute`: route id, host element, optional nav button, shell title/state, on-enter callback을 담는 작은 route record.
- `LobbyShellPageRouter`: route collection을 받아 `Show(LobbyShellPageId pageId)`를 수행한다.
- `Shared.Ui.UitkElementUtility`: low-level VisualElement 조작만 맡는다. route registry나 feature-specific shell wording은 알지 않는다.
- `LobbyPageController`: 기존 public page open methods와 UseCase command routing을 유지한다.
- `LobbyUitkRuntimeAdapter`: 기존 public `ShowLobbyPage`, `ShowGaragePage`, `ShowRecordsPage`, `ShowAccountPage`, `ShowConnectionPage` surface는 유지하되 내부에서 router를 호출한다.

## Explicit Non-Goals

- Unity scene/prefab serialized references 변경.
- UXML/USS 구조 개편.
- Garage page controller, presenter, state 구조 변경.
- Records/Account/Connection page를 별도 feature controller로 승격.
- generic `Shared.Ui.UitkPageRouter<T>` 도입.
- route discovery, reflection, file naming convention 기반 자동 등록.
- Next.js의 server/client component 개념을 Unity type system에 그대로 매핑.
- SFX wiring, WebGL acceptance, Stitch reimport.

## Implementation Slices

### Slice 1 - Router Extraction

Type: AFK

작업:

- `Assets/Scripts/Features/Lobby/Presentation/LobbyShellPageRouter.cs` 추가.
- `LobbyUitkRuntimeAdapter`의 `SetPageVisibility`, `SetNavState`, `SetShell` 책임을 router로 이동.
- `Show*Page` public methods는 유지하고 내부만 `ShowPage(LobbyShellPageId.X)` 형태로 정리.
- `EnsureRecordsSurface`, `EnsureAccountSurface`, `EnsureConnectionSurface`, `EnsureGarageSurface`는 route enter callback으로 명시 등록.

Acceptance:

- `LobbyUitkRuntimeAdapter`에 route별 display/nav/shell text 분기 중복이 남지 않는다.
- `Show*Page` behavior는 기존 direct tests 기준으로 동일하다.
- route registration은 explicit construction이며 reflection/runtime lookup fallback이 없다.

### Slice 2 - Direct Tests

Type: AFK

작업:

- `LobbyShellPageRouterDirectTests` 또는 기존 `LobbyUitkRuntimeAdapterDirectTests`에 route show behavior를 추가한다.
- Lobby, Garage, Records, Account, Connection 전환 시 host display, selected nav class, shell title/state가 맞는지 확인한다.
- route enter callback이 필요한 surface clone을 한 번만 보장하는지 확인한다.

Acceptance:

- page route 전환이 adapter internals보다 public behavior 기준으로 검증된다.
- tests가 private field나 implementation order에 과하게 의존하지 않는다.

### Slice 3 - Documentation Sync

Type: AFK

작업:

- `docs/owners/design/ui_foundations.md`의 Presentation Naming/Current Runtime Targets에 shell routing helper 기준을 짧게 반영한다.
- helper와 layer를 혼동하지 않도록 `LobbyShellPageRouter`가 adapter 내부 helper임을 명시한다.

Acceptance:

- `docs/index.md` registry에서 이 plan을 closeout reference로 찾을 수 있다.
- old feature-local UITK element helper stale trace가 active code/docs에 없다.

## Validation

Mechanical validation:

- `rg -n "LobbyShellPageRouter|SetPageVisibility|SetNavState|SetShell|UitkElements" Assets/Scripts Assets/Editor docs/owners/design/ui_foundations.md`
- `dotnet build JG.slnx -v:minimal`
- Unity EditMode:
  - `Tests.Editor.LobbyUitkRuntimeAdapterDirectTests`
  - `Tests.Editor.LobbyPagePresenterDirectTests`
  - `Tests.Editor.ArchitectureGuardrailReflectionTests`
  - new/updated Lobby shell router direct tests
- `npm run --silent rules:lint`
- If Unity UI workflow policy applies to the changed files, run `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` and store evidence under `artifacts/unity/uitk-page-routing-refactor/`.

Manual/product acceptance:

- Not required for this plan by default. This is a behavior-preserving routing refactor.
- If route changes touch actual scene/prefab serialized references later, manual Unity smoke becomes required before closeout.

## Risks And Residuals

- Risk: router extraction may become a generic page framework. Guard: keep it Lobby-specific until a second real shell needs the same route model.
- Risk: route enter callbacks may hide surface creation side effects. Guard: callbacks must be named and registered explicitly in `LobbyUitkRuntimeAdapter`.
- Risk: tests may only assert internal route implementation. Guard: prefer visible host/nav/shell state assertions.
- Residual: A future broader Shell/Layout architecture can be considered after Lobby has at least one additional page or another feature shell with the same problem.

## Authoring Review

과한점 리뷰:

- Generic shared router, file routing, lifecycle interface, scene/prefab rewiring, and page framework work are explicitly out-of-scope.
- The plan does not redefine Clean Architecture, Unity UI authoring policy, or acceptance reporting rules; it links through upstream owner docs.

부족한점 리뷰:

- Owner, scope, non-goals, slices, acceptance, validation, risks, and residual handling are present.
- The plan was kept as a reference because the implementation is closed and the over/under abstraction decision may be useful for future Shell/Layout work.

plan rereview: clean - UITK page routing refactor scope, over/under abstraction boundary, validation, and lifecycle checked

## Closeout

Mechanical closeout: success.

- `LobbyShellPageRouter` was added as a Lobby-specific adapter-owned helper.
- `LobbyUitkRuntimeAdapter.Show*Page` public surface was preserved and now routes through explicit page ids.
- Route visibility, nav selected state, shell title/state, and one-time route surface clone behavior are covered by `LobbyUitkRuntimeAdapterDirectTests`.
- No Unity scene, prefab, UXML, USS, Garage controller, SFX, WebGL, or Stitch scope was changed by this routing pass.

Evidence:

- `dotnet build JG.slnx -v:minimal`
- Unity EditMode artifact: `artifacts/unity/uitk-page-routing-refactor/lobby-page-routing-editmode-tests.xml`
- Unity UI authoring policy artifact: `artifacts/unity/uitk-page-routing-refactor/unity-ui-authoring-workflow-policy.json`
- `npm run --silent rules:lint`

Historical artifact path note:

- The Unity UI authoring policy artifact is preserved as 2026-05-01 execution evidence and may contain pre-migration doc paths in `changedFiles`.
- Current owner lookup for that historical run should use `docs/index.md`; UI foundations, skill routing, skill trigger, non-Stitch handoff, progress, and this plan now live under the current owner tree.

Residual:

- No routing-refactor residual remains.
- Broader Shell/Layout architecture remains a future design option only if another shell develops the same route-growth problem.

owner impact:

- primary: `plans.uitk-page-routing-refactor`
- secondary: `design.ui-foundations`
- out-of-scope: scene/prefab serialized references, UXML/USS authoring, Garage page architecture, generic shared router framework

doc lifecycle checked:

- reference 유지. 이 plan은 implementation sequence와 over/under abstraction decision 기록으로 보존한다. 현재 실행 TODO는 닫혔고, 새 Shell/Layout expansion이 필요하면 새 owner 판단으로 시작한다.
