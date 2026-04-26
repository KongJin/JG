# Garage Mobile Scroll Code Cleanup Plan

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.garage-mobile-scroll-code-cleanup
> role: plan
> owner_scope: Garage 모바일 scroll 복구 이후 코드/이름/중복 정리 실행 순서와 acceptance
> upstream: plans.progress, plans.garage-mobile-scroll-recovery, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `Assets/Editor/SceneTools/GarageMobileScrollMigrationTool.cs`, `Assets/Editor/SceneTools/LobbySceneRuntimeAssemblyTool.cs`, `Assets/Scripts/Features/Garage/Presentation/*.cs`
>
> 생성일: 2026-04-26
> 근거: Garage 모바일 single scroll 복구 후 code review에서 발견한 naming drift, layout constant duplication, preview API cleanup residual

이 문서는 Garage 모바일 scroll 복구 후속 코드 정리만 다룬다.
UX 구조 복구와 evidence는 [`garage_mobile_scroll_recovery_plan.md`](./garage_mobile_scroll_recovery_plan.md)가 맡고, Set B visual fidelity 판단은 [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md)가 맡는다.

## 현재 상태

- `GarageMobileScrollMigrationTool`은 열린 `LobbyScene`을 single vertical scroll 구조로 마이그레이션했고, report artifact를 남긴다.
- `LobbySceneRuntimeAssemblyTool`은 destructive rebuild fallback에서 같은 scroll 구조를 만들도록 업데이트됐다.
- `GaragePageChromeController`는 `PreviewCard`와 `ResultPane`을 모바일 scroll flow에 상시 노출한다.
- `GarageUnitPreviewView`는 새 Input System `Pointer.current`를 사용해 legacy `UnityEngine.Input` 예외를 제거했다.
- 2026-04-26 cleanup pass에서 아래 후보를 정리했다:
  - `RequireRect`를 null-return fallback에서 분리하고 `FindRect`를 추가했다.
  - scroll layout 숫자를 migration tool과 runtime assembly helper의 local named constants로 정리했다.
  - `MobilePreviewTabButton` / `_mobilePreviewTabButton` 계열을 `MobileFirepowerTabButton` / `_mobileFirepowerTabButton` 계열로 정리했다.
  - `GarageUnitPreviewView.Render(..., GaragePanelCatalog catalog)`의 unused `catalog` 파라미터를 제거했다.
  - `GarageUnitPreviewView` 설명과 tooltip을 Nova1492 prefab mapping + primitive fallback 구조에 맞췄다.
  - `DestroyCurrentPreview()`가 destroy 후 `_currentPreviewRoot = null`을 설정하도록 정리했다.

## 목표

- runtime 동작을 바꾸지 않고 코드가 현재 역할을 더 솔직하게 말하게 한다.
- 작은 정리는 즉시 처리하고, serialized reference churn이 큰 정리는 별도 단계로 격리한다.
- scene/prefab serialized reference를 건드리는 정리는 MCP 또는 explicit migration route로만 수행한다.
- visual fidelity 판단과 code cleanup closeout을 섞지 않는다.

## 제외 범위

- Garage UI visual redesign
- Set B Stitch fidelity acceptance
- Account/Garage 저장 로직
- WebGL save/load 실기 검증
- 새 prefab 체계 도입
- destructive rebuild helper를 routine repair route로 승격

## 실행 순서

1. **safe code cleanup**
   - `GarageUnitPreviewView.Render`에서 unused `GaragePanelCatalog catalog` 파라미터를 제거하고 호출부를 함께 수정한다.
   - `DestroyCurrentPreview()`가 destroy 후 `_currentPreviewRoot = null`을 설정하도록 정리한다.
   - class summary와 tooltip을 현재 Nova1492 model mapping + primitive fallback 구조에 맞게 업데이트한다.

2. **migration tool naming cleanup**
   - `RequireRect`를 `FindRect`와 `RequireRect`로 분리한다.
   - 필수 노드는 `RequireRect`가 즉시 throw하게 하고, legacy fallback 대상만 `FindRect`로 읽는다.
   - 실패 메시지에는 parent path와 child name을 포함한다.

3. **layout constants cleanup**
   - `GarageMobileScrollMigrationTool` 안의 height/offset 값을 named constants로 추출한다.
   - `LobbySceneRuntimeAssemblyTool`에도 같은 의미의 named constants를 둔다.
   - 같은 상수를 cross-file global owner로 빼는 것은 이번 범위에서 제외한다. Editor helper 둘 사이의 과한 결합을 만들지 않는다.

4. **Firepower tab naming decision**
   - `_mobilePreviewTabButton`, `_mobilePreviewTabLabel`, `MobilePreviewTabButton`을 `Firepower` 계열로 리네임할지 판단한다.
   - 리네임할 경우 `FormerlySerializedAs` 또는 scene migration을 사용해 serialized reference를 보존한다.
   - 리네임이 불필요하게 scene churn을 키우면 이번 pass에서는 사용자-facing label과 docs residual만 유지한다.

5. **verification**
   - compile check를 먼저 실행한다.
   - Unity가 열려 있으면 script reload wait 후 Play Mode Lobby -> Garage smoke를 반복한다.
   - `MobileContentRoot`가 active `ScrollRect`이고 `MobileBodyHost`가 content인지 재확인한다.
   - console error/exception 0, AudioListener 경고 0을 확인한다.
   - Unity UI authoring workflow policy와 `npm run --silent rules:lint`를 실행한다.

## Acceptance

- cleanup 후 compile errors/warnings 0이다. Status: met.
- `GarageUnitPreviewView.Render` 호출부와 시그니처가 unused parameter 없이 일치한다. Status: met.
- `DestroyCurrentPreview()`가 preview root 상태를 명확히 정리한다. Status: met.
- migration tool에서 missing required node는 즉시 읽기 쉬운 에러로 실패한다. Status: met.
- layout 값은 magic number가 아니라 local named constant로 읽힌다. Status: met.
- Firepower tab naming은 either renamed cleanly or residual로 명시된다. Status: met; field and scene object renamed to Firepower.
- Play Mode Lobby -> Garage smoke에서 console error 0, AudioListener 경고 0이다. Status: met.
- Unity UI authoring workflow policy와 `npm run --silent rules:lint`가 통과한다. Status: met.

## Blocked / Residual 처리

- serialized field/object rename이 scene reference를 불안정하게 만들면 `Firepower tab naming residual`로 남기고, safe cleanup만 닫는다.
- visual capture가 Set B source와 다르게 보여도 이 plan의 success로 처리하지 않는다. 그 판단은 Set B visual fidelity plan으로 넘긴다.
- Unity MCP가 unavailable이면 scene mutation은 blocked로 두고 code-only cleanup과 compile/rules verification만 분리한다.

## 검증 명령

- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- Unity MCP compile wait / Play Mode Lobby -> Garage smoke
- Unity MCP `Tools/Validate Required Fields`
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- `npm run --silent rules:lint`

## 2026-04-26 Cleanup Pass

- Renamed serialized fields with `FormerlySerializedAs`:
  - `_mobilePreviewTabButton` -> `_mobileFirepowerTabButton`
  - `_mobilePreviewTabLabel` -> `_mobileFirepowerTabLabel`
- Re-ran `GarageMobileScrollMigrationTool`; scene object path is now `MobileFirepowerTabButton`, and old `MobilePreviewTabButton` is absent.
- Removed unused preview `catalog` parameter.
- Split migration helper lookup into optional `FindRect` and required `RequireRect`.
- Added local layout constants in the migration tool and destructive runtime assembly helper.
- Verification:
  - `tools/check-compile-errors.ps1`: errors 0, warnings 0
  - Unity MCP `Tools/Validate Required Fields`: executed, no new console errors
  - Play Mode Lobby -> Garage smoke: console error/exception/assert 0
  - AudioListener warning filter: 0
  - `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`: pass
  - `npm run --silent rules:lint`: pass

## 문서 재리뷰

- 새 문서 생성 판단: 기존 scroll recovery plan은 UX 구조 복구와 evidence owner이고, 이번 작업은 code cleanup 실행 순서와 serialized rename risk가 중심이라 별도 active plan으로 둔다.
- 과한점 리뷰: Unity UI authoring 규칙, script design 규칙, visual fidelity 기준을 새로 정의하지 않고 기존 owner 문서에 위임했다. 새 hard-fail이나 새 artifact gate를 추가하지 않았다.
- 부족한점 리뷰: 현재 상태, 목표, 제외 범위, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 반영: `Firepower tab naming`을 즉시 rename과 residual 두 경로로 나누고, Set B visual fidelity는 out-of-scope로 분리했다.
- 수정 후 재리뷰: obvious 과한점/부족한점 없음.
- 2026-04-26 cleanup closeout rereview: cleanup 결과와 verification만 추가했고, visual fidelity/Account/WebGL owner를 끌어오지 않았다. 완료된 단기 plan이므로 reference 상태로 내린다.
- owner impact: primary `plans.garage-mobile-scroll-code-cleanup`; secondary `plans.progress`, `docs.index`, `plans.garage-mobile-scroll-recovery`, `ops.unity-ui-authoring-workflow`; out-of-scope `plans.garage-ui-ux-improvement`, Account/Garage persistence, WebGL validation.
- doc lifecycle checked: cleanup pass가 닫혔으므로 이 문서는 reference로 내린다. 기존 `garage_mobile_scroll_recovery_plan.md`는 scroll recovery evidence owner로 유지하고 대체/삭제하지 않는다.
- plan rereview: clean
