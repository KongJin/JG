# Garage SetB UITK Runtime Gap Plan

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: plans.garage-setb-uitk-runtime-gap
> role: plan
> owner_scope: Garage SetB UI Toolkit pilot을 runtime Garage 후보로 승격하기 위한 binding, interaction, 3D preview, replacement gap 해결 순서
> upstream: plans.progress, plans.garage-ui-ux-improvement, design.ui-foundations, ops.unity-ui-authoring-workflow, ops.stitch-data-workflow, ops.acceptance-reporting-guardrails
> artifacts: `Assets/UI/UIToolkit/GarageSetB/`, `Assets/Scenes/GarageSetBUitkPreview.unity`, `Assets/Settings/UI/GarageSetBPanelSettings.asset`, `artifacts/unity/garage-setb-uitoolkit-preview.png`, `artifacts/unity/garage-setb-uitoolkit-port-report.md`

이 문서는 현재 static `GarageSetB` UI Toolkit pilot을 실제 Garage runtime replacement 후보로 만들 때 남은 gap과 실행 순서를 소유한다.
visual fidelity 배경은 `plans.garage-ui-ux-improvement`, 현재 source/evidence 판단은 `plans.progress`와 `design.ui-foundations`가 소유한다.

## Primary Owner / Scope

- primary owner: `plans.garage-setb-uitk-runtime-gap`
- secondary owners: `plans.garage-ui-ux-improvement`, `ops.unity-ui-authoring-workflow`, `ops.stitch-data-workflow`, Garage presentation/runtime scripts
- out-of-scope: Stitch source 재디자인, 새로운 UI authoring 규칙 추가, GameScene HUD migration, account/Firebase 저장 검증의 최종 acceptance, Nova1492 모델 대량 재분류

## Current Surface

| item | path | current role |
|---|---|---|
| UXML | `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml` | static candidate structure |
| USS | `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss` | candidate styling |
| PanelSettings | `Assets/Settings/UI/GarageSetBPanelSettings.asset` | 390x844 runtime panel baseline |
| Preview scene | `Assets/Scenes/GarageSetBUitkPreview.unity` | isolated candidate review scene |
| Capture/report | `artifacts/unity/garage-setb-uitoolkit-preview.png`, `artifacts/unity/garage-setb-uitoolkit-port-report.md` | visual evidence |

Current judgment:

- Candidate surface exists and reads in the intended order: slot strip, part focus, editor, preview/status, save dock.
- Active `LobbyScene` has a SetB UITK runtime route wired through `UIDocument`, `GarageSetBUitkRuntimeAdapter`, `GarageSetBUitkPageController`, and `GarageSetBUitkPreviewRenderer`.
- Runtime acceptance evidence is recorded in `artifacts/unity/garage-setb-uitk-phase5-runtime-smoke.json` and `artifacts/unity/garage-setb-uitk-phase5-runtime-acceptance.png`.
- Legacy sample Garage IDs are explicitly mapped to promoted Nova Unity catalog entries so the runtime preview uses Unity-imported Nova models and XFI alignment data.
- Existing uGUI Garage compatibility objects were not removed.
- Final WebGL save/load/settings/accessibility acceptance remains with the shared `Account/Garage` validation lane.

## Gap Resolution Plan

### Phase 1: Candidate Contract Stabilization

Goal: make the UXML bindable without turning layout data into runtime code.

Tasks:

- Add stable `name` attributes only to elements that runtime binding must query.
- Keep visual grouping in UXML/USS; do not move layout constants into C#.
- Define a small Garage SetB binding map for:
  - slot cards and labels
  - frame/firepower/mobility tabs
  - focused part title, description, badge, stats
  - attachment rows and empty states
  - preview image/texture host and status tags
  - save and settings buttons
- Re-capture the preview after UXML naming changes to ensure no visual regression.

Acceptance:

- Bindable element names exist for all runtime-owned values and actions.
- Static visual capture still matches the current candidate reading order.
- No new scene/prefab replacement happens in this phase.

### Phase 2: Runtime Presenter Adapter

Goal: connect existing Garage state to UITK without replacing the whole Garage flow yet.

Tasks:

- Add a UITK-specific adapter/controller that receives existing Garage view models and applies them to `VisualElement` references.
- Keep data ownership in existing Garage application/presentation models.
- Map slot selection, active part slot, focused part data, stat values, dirty/save state, loading state, and unavailable/error state.
- Add interaction callbacks for slot select, part tab select, save, settings open, and attachment selection if the current domain supports it.
- Avoid hidden scene lookup; wire `UIDocument`, camera/render texture references, and presenter dependencies through serialized scene/prefab contract or explicit setup.

Acceptance:

- UITK adapter can render at least one real Garage loadout from runtime data.
- Clicking slot/tab/save routes through existing Garage events or an explicit compatibility port.
- Static sample literals are removed or isolated behind editor/demo-only mode.

### Phase 3: 3D Preview Integration

Goal: replace the blueprint placeholder with the existing assembled unit preview behavior.

Options to evaluate:

- Preferred: render the existing Garage unit preview camera to a `RenderTexture` and display it in a UITK `VisualElement` background or image host.
- Alternative: keep a scene-owned 3D preview object beside the UIDocument and reserve the UITK panel for chrome/control only.

Tasks:

- Reuse current Nova1492 preview assembly path and XFI socket data.
- Keep preview camera/render texture lifecycle explicit.
- Preserve empty/loading/error preview states in UITK.
- Capture a real assembled unit in 390x844 and compare against the candidate composition.

Acceptance:

- Blueprint placeholder no longer represents final runtime preview.
- A real unit assembly can be shown without console errors.
- Preview failure falls back to a deliberate empty state, not bounds-derived or fake placement.

### Phase 4: Scene Integration Pass

Goal: test runtime replacement without breaking the existing Garage compatibility surface.

Tasks:

- Create or update a scene-owned UITK document route through MCP, not direct scene YAML editing.
- Keep current Garage runtime UI available until UITK runtime smoke passes.
- Decide the switch strategy:
  - feature flag / scene variant for comparison, or
  - explicit replacement pass after acceptance.
- Verify input focus, panel sorting, camera interaction, and save dock reachability on mobile aspect.

Acceptance:

- Active scene can show the UITK Garage candidate through a real `UIDocument`.
- Existing Garage state still loads and can be saved through the accepted route.
- Rollback path is clear until runtime acceptance is complete.

### Phase 5: Runtime Acceptance

Goal: separate mechanical translation pass from actual player-flow acceptance.

Validation commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
```

Required evidence:

- fresh `390x844` candidate or runtime capture
- workflow policy result
- console error check after entering Garage
- slot/tab interaction smoke
- save action smoke
- settings interaction smoke if settings is present in the UITK surface
- real 3D preview capture or explicit `blocked` reason

Acceptance:

- Source/candidate visual hierarchy remains recognizable after runtime data binding.
- Slot selection, part focus, save CTA, settings action, and preview state are all interactive or explicitly blocked.
- No new console errors are introduced by the UITK route.
- Existing uGUI Garage is not removed until UITK runtime path has acceptance evidence.

## Blocked / Residual Handling

- If visual fidelity is still a mismatch, do not start scene replacement; return to `plans.garage-ui-ux-improvement`.
- If UITK cannot display the RenderTexture path reliably, mark `blocked: preview-hosting-route` and keep 3D preview integration separate from state binding.
- If existing Garage presenter models do not expose required state, add a small explicit presentation port; do not query runtime children to reconstruct state.
- If save/load or settings WebGL behavior fails, route final acceptance to shared `Account/Garage` validation rather than closing this plan as success.
- If workflow policy blocks the route, record `blockedReason` and do not treat the pilot as runtime accepted.

## Closeout Criteria

- UITK Garage can run from real Garage data in a Unity scene.
- Static sample data is removed from runtime path or isolated as editor/demo-only.
- Real 3D preview route is accepted or explicitly blocked with a follow-up owner.
- Fresh capture/report and workflow policy evidence exist.
- Runtime acceptance is either `success`, `mismatch`, or `blocked` with the reason recorded.
- Existing runtime Garage compatibility surface is only retired after acceptance evidence exists.

## Lifecycle

- reference 전환 이유: runtime binding/replacement verdict is recorded as `success`, with evidence linked below.
- residual owner: shared `Account/Garage` lane owns WebGL save/load, settings interaction, and save action accessibility.
- compatibility note: existing uGUI Garage remains available until a later retirement pass explicitly owns removal.

## Closeout

- Verdict: `success`.
- Evidence: `artifacts/unity/garage-setb-uitk-phase5-runtime-smoke.json`, `artifacts/unity/garage-setb-uitk-phase5-runtime-acceptance.png`, `artifacts/unity/garage-setb-uitoolkit-port-report.md`.
- Runtime smoke covered: real Garage state render, slot select, part focus, settings action, save action route, 390x844 GameView capture, console error delta, and real 3D preview hierarchy.
- Preview hierarchy included promoted Nova Unity model mesh children: `body1_sz`, `arm1_sz`, and `legs1_rdrn`.
- Validation: `tools/check-compile-errors.ps1` returned 0 errors and 0 warnings after the final route changes.
- owner impact: primary `plans.garage-setb-uitk-runtime-gap` moved to reference; residual shared validation stays with `plans.progress` `Account/Garage`; visual/source background stays with `plans.garage-ui-ux-improvement` as reference.
- doc lifecycle checked: active -> reference; keep this plan as compact closeout evidence because it records the runtime acceptance route and residual owner handoff.
- plan rereview: clean

## Plan Rereview

- 과한점 리뷰: 새 UI 규칙, 새 artifact schema, 새 hard-fail을 만들지 않고 기존 UI Toolkit candidate route의 runtime gap만 소유한다.
- 부족한점 리뷰: owner/scope, current surface, phase order, acceptance, validation, blocked/residual, lifecycle을 포함했다.
- 수정 후 재리뷰: visual fidelity/source redesign은 기존 Set B visual plans에 남기고, 이 문서는 binding/replacement gap으로만 좁혔다.
- owner impact: primary `plans.garage-setb-uitk-runtime-gap`; secondary `plans.garage-ui-ux-improvement`, `plans.progress`, `ops.unity-ui-authoring-workflow`; out-of-scope `ops.stitch-data-workflow` 규칙 개정.
- doc lifecycle checked: 새 active plan으로 등록하고, runtime verdict 뒤 reference 전환 후보로 본다.
- plan rereview: clean
