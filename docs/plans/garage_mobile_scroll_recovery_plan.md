# Garage Mobile Scroll Recovery Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.garage-mobile-scroll-recovery
> role: plan
> owner_scope: LobbyScene Garage 모바일 본문을 `slot first -> single scroll body -> fixed save dock` 구조로 복구하는 실행 순서와 acceptance
> upstream: plans.progress, design.ui-foundations, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `Assets/Scenes/LobbyScene.unity`, `Assets/Scripts/Features/Garage/Presentation/*.cs`, `Assets/Editor/SceneTools/LobbySceneRuntimeAssemblyTool.cs`, `artifacts/unity/lobby-scene-garage-*.png`
>
> 생성일: 2026-04-26
> 근거: Garage 모바일 화면이 빡빡하지만 현재 `MobileBodyHost`가 실제 `ScrollRect` 아래에 있지 않아 vertical scroll 훅이 no-op에 가까운 상태

이 문서는 Garage 모바일 런타임 레이아웃의 scroll 구조 복구만 다룬다.
Set B Stitch visual fidelity 최종 판단은 [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md)가 맡고, 이 문서는 그 판단을 가능하게 만드는 runtime/mobile layout debt를 정리한다.

## 현재 상태

- `design.ui-foundations`의 Garage layout contract는 `Slot Selector -> single scroll body -> fixed Save Dock` 흐름을 요구한다.
- `LobbyScene`에는 `MobileContentRoot`, `MobileSlotHost`, `MobileBodyHost`, `MobileTabBar`, `MobileSaveDockRoot`가 있다.
- 2026-04-26 pass에서 `MobileContentRoot`를 active `ScrollRect`로 만들고, `MobileBodyHost`를 content로 연결했다.
- `MobileSlotHost`, `MobileTabBar`, `GarageUnitEditorView`, `PreviewCard`, `ResultPane`는 같은 `MobileBodyHost` vertical content 아래에 배치됐다.
- `MobileSaveDockRoot`는 shared Lobby/Garage nav 위의 fixed dock으로 올렸다.
- legacy `MainScroll`은 scene/runtime assembly helper에서 숨기는 대상이며, 현재 모바일 Garage의 실제 scroll owner로 쓰이지 않는다.
- `MobilePreviewTabButton` 이름은 3D preview 탭처럼 보이지만 실제 동작은 `MobilePartFocus.Firepower` 선택이다. 화면상 라벨은 `무장`이며, 3D `PreviewCard`는 모바일 scroll content 안에서 상시 노출된다.
- Nova1492 preview model mapping은 들어갔고, `PreviewCard` active-state는 `artifacts/unity/lobby-scene-garage-mobile-scroll-smoke.png` capture로 확인했다.

## 목표

- Garage 모바일 본문을 실제 `ScrollRect` 기반 single vertical scroll로 만든다.
- 슬롯 선택부, 파트 포커스 바, focused editor, 3D preview, summary가 같은 세로 흐름 안에서 읽히게 한다.
- 저장 dock은 scroll content 밖에서 하단 고정으로 유지한다.
- `프레임 / 무장 / 기동` 컨트롤은 page nav나 preview tab이 아니라 파트 포커스 바로 명확히 정리한다.
- Play Mode mobile capture에서 빡빡함, 겹침, bottom dock 가림, preview 미노출을 같은 evidence로 판단할 수 있게 한다.

## 제외 범위

- Account/Garage 저장 로직 재설계
- Firestore/WebGL 저장 실기 검증
- Set B Stitch source 자체 재작성
- 신규 대형 prefab 체계 도입
- hidden runtime lookup, scene repair fallback, code-driven rebuild route를 정답 경로로 재도입
- BattleScene, GameScene, multiplayer smoke

## 실행 순서

1. **현재 hierarchy와 reference audit**
   - Unity MCP로 `LobbyScene`의 `GaragePageRoot`, `MobileContentRoot`, `MobileSlotHost`, `MobileBodyHost`, `PreviewCard`, `MobileTabBar`, `MobileSaveDockRoot` active state와 component 구성을 확인한다.
   - `_mobileBodyHost`가 실제 `ScrollRect` 아래에 있는지, legacy `MainScroll`이 active scroll로 남아 있는지 확인한다.
   - `GaragePageController`, `GaragePageChromeController`, `GaragePageScrollController`의 serialized reference와 현재 scene reference가 어긋나지 않는지 확인한다.

2. **mobile scroll contract 확정**
   - target hierarchy는 `MobileScrollViewport -> MobileBodyScrollContent`를 기준으로 잡되, 기존 serialized reference가 깨지지 않도록 migration path를 먼저 정한다.
   - `MobileSlotHost`는 별도 고정 패널이 아니라 scroll content 상단으로 옮긴다.
   - focused editor, `PreviewCard`, `ResultPane` 또는 summary surface는 같은 scroll content 아래에 배치한다.
   - `MobileTabBar`는 파트 포커스 바로 유지하고, `MobileSaveDockRoot`는 scroll 밖 하단 고정으로 유지한다.

3. **scene/prefab authoring**
   - scene/prefab 변경은 Unity MCP route로 수행한다.
   - 필요한 경우 `LobbySceneRuntimeAssemblyTool`의 template 생성 경로도 같은 구조를 생성하도록 맞춘다. 단, helper를 runtime repair나 rebuild 기본 경로로 승격하지 않는다.
   - `ScrollRect`, viewport mask, content `RectTransform`, layout group, content size fitter 구성을 실제 모바일 해상도 기준으로 점검한다.

4. **presentation code 정리**
   - `GaragePageScrollController.ScrollBodyToTop`이 실제 scroll owner를 안정적으로 찾도록 serialized reference 또는 명확한 owner component를 사용한다.
   - `MobilePreviewTabButton`은 Firepower focus 용도와 이름이 어긋난다. 리네임이 가능하면 `MobileFirepowerTabButton` 계열로 정리하고, 당장 큰 serialized churn이 크면 코드 주석과 plan residual로 남긴 뒤 후속 리네임을 분리한다.
   - `PreviewCard`는 별도 preview tab이 아니라 scroll content 안의 3D preview 영역으로 다룬다.
   - `Features.Garage.Presentation`에는 상태 렌더, 이벤트, scroll reset 같은 thin orchestration만 남기고 geometry literal 확장은 scene/prefab authoring 쪽에 둔다.

5. **visual/runtime smoke**
   - Play Mode에서 Lobby -> Garage tab 전환 후 mobile viewport capture를 만든다.
   - 슬롯 변경, 파트 포커스 변경, 저장 실패/성공 path에서 scroll reset이 동작하는지 확인한다.
   - 3D preview가 scroll 흐름 안에서 보이는지, save dock이 content를 가리지 않는지 확인한다.
   - console error/warning, AudioListener 경고 재발, required-field validation을 함께 확인한다.

6. **evidence와 문서 갱신**
   - fresh capture를 `artifacts/unity/lobby-scene-garage-mobile-scroll-*.png` 계열로 남긴다.
   - `progress.md`에는 현재 상태와 남은 residual만 짧게 반영한다.
   - Set B visual fidelity 판단이 여전히 mismatch면 이 plan을 success로 포장하지 않고 `garage_ui_ux_improvement_plan.md` residual로 분리한다.

## Acceptance

- `MobileBodyHost` 또는 새 scroll content owner가 실제 active `ScrollRect` 아래에 있다. Status: met.
- `GaragePageScrollController.ScrollBodyToTop`이 slot select/save 후 vertical scroll을 상단으로 되돌린다. Status: structurally met by active parent `ScrollRect`; manual drag/reset proof remains optional follow-up.
- slot selector, part focus bar, focused editor, 3D preview, summary가 하나의 vertical reading flow로 capture된다. Status: partially met; top capture shows slot/focus/editor/preview, summary is lower in scroll.
- `MobileSaveDockRoot`는 scroll 밖 하단 고정 상태로 남고, scroll content의 마지막 영역을 가리지 않는다. Status: met in latest capture.
- `MobilePreviewTabButton`이라는 용어 혼선이 제거되거나, serialized churn을 피하기 위한 residual로 명시된다. Status: residual; serialized name remains, visual label is `무장`.
- Play Mode Lobby -> Garage smoke에서 console error 0, AudioListener 경고 0이다. Status: met.
- compile check, required-field validation, Unity UI authoring workflow policy, `npm run --silent rules:lint`가 통과한다. Status: met.

## Blocked / Residual 처리

- Unity UI authoring policy가 신규 prefab 또는 builder route를 막으면, policy를 바꾸지 말고 scene/prefab authoring 범위로 다시 줄인다.
- scroll 구조는 맞지만 Set B source와 visual density가 아직 다르면 `mismatch`를 `garage_ui_ux_improvement_plan.md`에 남긴다.
- mobile capture를 만들 수 없으면 mechanical verification은 `blocked`로 남기고, compile/policy pass만으로 acceptance success를 말하지 않는다.
- serialized field 리네임이 과한 churn을 만들면 이름 정리는 후속 residual로 남기고, 실제 UX 혼선 제거를 먼저 닫는다.

## 2026-04-26 Implementation Pass

- Added `GarageMobileScrollMigrationTool` and applied it to `Assets/Scenes/LobbyScene.unity` through Unity MCP menu execution.
- Updated `LobbySceneRuntimeAssemblyTool` so future destructive rebuilds generate the same single-scroll mobile Garage structure.
- Changed `GaragePageChromeController` so `PreviewCard` and `ResultPane` stay active in the mobile scroll flow.
- Updated `GarageUnitPreviewView` to use the Input System `Pointer.current` path instead of legacy `UnityEngine.Input`; this was required once the preview card became visible.
- Evidence:
  - `artifacts/unity/garage-mobile-scroll-migration-report.md`
  - `artifacts/unity/lobby-scene-garage-mobile-scroll-smoke.png`
  - `artifacts/unity/unity-ui-authoring-workflow-policy.json`
- Residual:
  - serialized `MobilePreviewTabButton` naming remains for now; user-visible label is `무장`.
  - Set B visual fidelity final judgment still belongs to [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md).

## 검증 명령

- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- Unity MCP `Tools/Validate Required Fields`
- Unity MCP Play Mode Lobby -> Garage tab smoke
- Unity MCP GameView mobile capture
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- `npm run --silent rules:lint`

## 문서 재리뷰

- 새 문서 생성 판단: 기존 `garage_ui_ux_improvement_plan.md`는 Set B visual fidelity plan이고, 이번 작업은 runtime/mobile scroll 구조와 scene/presentation wiring 실행 순서가 필요해 별도 active plan으로 둔다.
- 과한점 리뷰: Unity UI authoring 규칙, Stitch translation 규칙, visual fidelity 기준을 새로 정의하지 않고 기존 owner 문서에 위임했다. 새 hard-fail이나 새 policy artifact를 추가하지 않았다.
- 부족한점 리뷰: 현재 상태, 목표, 제외 범위, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 반영: `MobilePreviewTabButton` 명칭 혼선과 serialized churn 가능성, Set B visual mismatch 분리, mobile capture blocked 처리를 추가했다.
- 수정 후 재리뷰: obvious 과한점/부족한점 없음.
- 2026-04-26 implementation update rereview: evidence와 residual만 추가했고, Unity UI authoring 규칙이나 Set B visual fidelity owner를 재정의하지 않았다.
- owner impact: primary `plans.garage-mobile-scroll-recovery`; secondary `plans.progress`, `docs.index`, `design.ui-foundations`, `ops.unity-ui-authoring-workflow`; out-of-scope `plans.garage-ui-ux-improvement`, Account/Garage persistence, WebGL validation.
- doc lifecycle checked: 새 문서는 active plan으로 등록한다. 기존 Set B Garage plan은 visual fidelity owner로 유지하고 대체/삭제하지 않는다.
- plan rereview: residual - serialized `MobilePreviewTabButton` naming remains to avoid broad serialized churn; Set B final visual fidelity remains separate.
