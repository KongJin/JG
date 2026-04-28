# Garage SetB UI Toolkit Port Report

- Status: runtime acceptance evidence captured for the active LobbyScene UITK route
- Source: `.stitch/designs/set-b-garage-main-workspace.{html,png}`
- UXML: `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml`
- USS: `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss`
- PanelSettings: `Assets/Settings/UI/GarageSetBPanelSettings.asset`
- Preview scene: `Assets/Scenes/GarageSetBUitkPreview.unity`
- Capture: `artifacts/unity/garage-setb-uitoolkit-preview.png`
- Capture size: 390x844

## Scope

- This pass does not replace `GaragePageRoot.prefab`.
- This pass adds a LobbyScene runtime UITK route with real Garage data, interaction smoke, and RenderTexture-backed Nova model preview evidence.
- The original static UI Toolkit translation remains the visual baseline; Phase 1-4 entries below describe the runtime bridge added after that baseline.

## Preserved SetB Reading Order

1. Current slot summary + slot selector
2. Part focus bar
3. Focused editor
4. Preview + summary
5. Persistent save dock

## Known Gaps

- The legacy uGUI Garage compatibility surface has not been removed.
- Final WebGL save/load/settings/accessibility acceptance remains in the shared `Account/Garage` validation lane.
- Legacy sample Garage part IDs are mapped to promoted Nova catalog entries so the runtime route can use Unity-imported Nova models and XFI alignment data.
- Iconography uses text placeholders instead of Material Symbols or project icon assets.

## Visual Review

- First-read hierarchy is close to SetB: slot strip, part focus, focused editor, preview, and persistent save dock are all visible in one mobile capture.
- Density is substantially closer to the Stitch source than the current uGUI Garage mismatch capture.
- Remaining polish risk: the blueprint preview/status area is slightly compressed near the save dock and should be tuned before replacing the active Garage screen.

## 2026-04-28 Phase 1 Binding Contract

- Added stable UXML `name` attributes for runtime-owned values/actions only.
- Binding map: `artifacts/unity/garage-setb-uitk-binding-map.json`
- Static parse check: 68 named UXML elements, 8 binding groups, 0 missing binding names.
- SceneView staging capture: `artifacts/unity/garage-setb-uitk-phase1-binding-capture.png`
- GameView visual capture: `artifacts/unity/garage-setb-uitk-phase1-binding-gameview.png`
- UI authoring policy: `artifacts/unity/unity-ui-authoring-workflow-policy.json`
- Compile checks: MCP compile/reload settled; `tools/check-compile-errors.ps1` returned 0 errors and 0 warnings.
- Runtime replacement was not performed in this phase.

## 2026-04-28 Phase 2 Runtime Adapter Start

- Added `GarageSetBUitkSurface` to query the Phase 1 binding names and render the stable subset of existing Garage presenter view models into the UITK surface.
- Added `GarageSetBUitkRuntimeAdapter` as the serialized `UIDocument` bridge. It exposes slot, part focus, save, and settings events without scene lookup.
- `GaragePageController` now optionally forwards existing `GarageSlotViewModel`, `GarageEditorViewModel`, and `GarageResultViewModel` output into the UITK adapter when one is wired.
- Added direct editor tests for UXML binding/render behavior: `Assets/Editor/DirectTests/GarageSetBUitkSurfaceDirectTests.cs`.
- Simplified the adapter after the initial pass: removed the one-off render model wrapper and deferred speculative stat, attachment, and preview text mapping until those have a real presentation contract.
- Compile checks: `tools/check-compile-errors.ps1` returned 0 errors and 0 warnings; MCP compile/reload settled.
- UI authoring policy: `artifacts/unity/unity-ui-authoring-workflow-policy.json` returned success for the scoped Phase 2 changed files.
- EditMode test execution is blocked while the project is open in Unity Editor: `open-editor-owns-project`.
- Scene replacement and 3D preview RenderTexture hosting were not performed in this phase.

## 2026-04-28 Phase 3/4 Preview + Scene Contract

- Extracted shared 3D assembly logic into `GarageUnitPreviewAssembly`; the uGUI preview view and UITK preview renderer now use the same XFI/alignment-aware part placement path.
- Added `GarageSetBUitkPreviewRenderer` for RenderTexture-backed 3D unit preview hosting. It has no primitive/bounds fallback; incomplete loadouts or missing Unity model assets leave the placeholder path hidden.
- Added `GarageSetBUitkSurface.SetPreviewTexture(...)` and direct coverage for the runtime preview image toggle.
- Updated `GaragePanelCatalogFactory` to resolve preview models from promoted Nova Unity data. If serialized preview prefab references are null in Editor, it falls back to `NovaPartVisualCatalog.modelPath` and loads the already-imported `Assets/Art/Nova1492/GXConverted` OBJ asset.
- Wired `GarageSetBUitkPreview.unity` through MCP with `GarageSetBUitkRuntimeAdapter`, `GarageSetBUitkPreviewSceneDriver`, and `GarageSetBPreviewCamera`.
- Runtime evidence: `artifacts/unity/garage-setb-uitk-phase3-4-runtime-preview.png`; hierarchy verification showed `GarageSetBPreviewCamera/PreviewRoot` with frame, firepower, and mobility mesh children.
- Active Garage/uGUI replacement was not performed in this phase.

## 2026-04-28 Phase 5 Runtime Acceptance

- Verdict: `success`.
- Wired `LobbyScene` through MCP with `/GarageSetBUitkDocument` (`UIDocument`, `GarageSetBUitkRuntimeAdapter`, `GarageSetBUitkPageController`) and `/GarageSetBPreviewCamera` (`GarageSetBUitkPreviewRenderer`).
- Added a SetB UITK page controller initialized by `GarageSetup`, so the surface renders real Garage state without relying on the legacy `GaragePageController` reference.
- Added explicit Nova catalog aliases for the legacy sample loadout IDs so `frame_bastion/fire_scatter/mob_burst`, `frame_striker/fire_pulse/mob_vector`, and `frame_relay/fire_rail/mob_treads` resolve to promoted Nova Unity model assets and XFI alignment data.
- Runtime smoke evidence: `artifacts/unity/garage-setb-uitk-phase5-runtime-smoke.json`.
- Runtime capture: `artifacts/unity/garage-setb-uitk-phase5-runtime-acceptance.png` at 390x844.
- Interaction smoke: slot select reached slot 1, part focus reached `Firepower`, settings toggled on, and save action routed to the UITK controller.
- Preview evidence: `GarageSetBPreviewCamera/PreviewRoot` contained `datan_common_body1_sz_206b7fcc`, `datan_common_arm1_sz_5fce3a7e`, and `datan_common_legs1_rdrn_46fa96e7` mesh children.
- Console evidence: no new errors after entering the UITK Garage route.
- Validation: `tools/check-compile-errors.ps1` returned 0 errors and 0 warnings after the runtime route and alias changes.
