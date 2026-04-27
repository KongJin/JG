# Non-Stitch UI Stitch Reimport Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.non-stitch-ui-stitch-reimport
> role: plan
> owner_scope: Stitch source freeze가 없는 Unity-native 또는 mixed UI surface를 Stitch에서 다시 만든 뒤 Unity prefab/scene contract로 재import하는 실행 순서
> upstream: plans.progress, design.ui-reference-workflow, ops.stitch-data-workflow, ops.stitch-to-unity-translation-guide, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/stitch/`, `artifacts/unity/`, `Assets/Prefabs/Features/`, `Assets/Resources/*.prefab`

이 문서는 현재 Unity에 직접 만든 UI와 Stitch-derived UI가 섞인 상태를 정리하기 위한 migration plan이다.
새 규칙은 만들지 않고, 기존 기준인 `Stitch source freeze -> execution contracts -> Unity translation`과 `prefab-first reset` route를 따른다.

한 줄 목표:

`Stitch source freeze가 없는 player-facing UI는 먼저 Stitch에서 accepted screen을 만든 뒤, Unity prefab/scene contract로 다시 가져온다.`

## Primary Owner / Scope

- primary owner: `plans.non-stitch-ui-stitch-reimport`
- secondary owners: `plans.progress`, `ops.stitch-data-workflow`, `ops.unity-ui-authoring-workflow`, relevant feature presentation code
- out-of-scope: Stitch/Unity workflow rule 개정, translator capability 확장, gameplay prefab 구조 변경, non-UI model preview asset 대량 정리

## 현재 판단

- `SetA/SetC/SetD/SetE` 이름을 가진 prefab들은 이미 Stitch-derived lane으로 본다.
- `GaragePageRoot.prefab`과 `LobbyPageRoot.prefab`은 Stitch lane과 scene/manual authoring이 섞였을 수 있으므로, source freeze와 current prefab/capture를 다시 대조한다.
- `Assets/Resources`의 전투/스킬 UI prefab은 historical native UI일 가능성이 높고, 재생성 후보로 먼저 inventory 한다.
- Nova1492 generated preview model prefab은 UI source migration 대상이 아니라 Garage content asset으로 본다.

## Surface Inventory

| surface | current asset | initial class | migration action |
|---|---|---|---|
| Garage main workspace | `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab` | mixed / Set B residual | 기존 Set B source freeze와 current prefab mismatch를 먼저 닫고, settings/account subpanel은 별도 Stitch source 후보로 분리한다 |
| Lobby main shell | `Assets/Prefabs/Features/Lobby/Root/LobbyPageRoot.prefab` | mixed / Set A candidate | current source freeze와 scene override를 대조한 뒤, missing native regions만 Stitch source로 다시 만든다 |
| Account settings overlay | scene-owned inside `LobbyScene.unity` / Garage bindings | reimported / policy residual | Stitch source freeze와 LLM-authored draft를 통해 prefab-first reimport/capture까지 통과했다. Runtime scene wiring과 workflow policy의 new-prefab guard residual은 아직 남아 있다 |
| Skill bar HUD | `Assets/Resources/SkillBarCanvas.prefab` | native candidate | GameScene HUD source와 통합할지, 독립 `Set D` skill-bar source로 만들지 먼저 결정한다 |
| Start skill selection | `Assets/Resources/StartSkillSelectionCanvas.prefab` | native candidate | modal/selection overlay source를 Stitch에서 만들고 Battle scene integration target을 정한다 |
| Player health HUD | `Assets/Resources/PlayerHealthHudView.prefab` | native candidate | 현재 `SetDGameSceneHudFullRoot`와 중복 여부를 확인하고, 남는 기능만 Stitch source로 재작성한다 |
| Enemy health bar / damage number | `Assets/Resources/EnemyHealthBar.prefab`, `Assets/Resources/DamageNumber.prefab` | gameplay feedback candidate | screen UI가 아니라 world-space feedback이면 low priority로 두고, visual consistency 필요 시 별도 micro-surface로 다룬다 |

## 실행 순서

1. **Inventory audit**
   - UI prefab과 scene-owned UI를 `already Stitch-derived`, `mixed`, `native candidate`, `not UI / generated asset`으로 분류한다.
   - 각 candidate마다 current asset, owning scene/prefab, presentation script, existing source freeze 여부를 기록한다.

2. **Stitch source creation**
   - native candidate마다 현재 기능과 required states를 짧은 prompt brief로 만든다.
   - Stitch에서 한 surface당 accepted baseline 하나만 고정한다.
   - source freeze는 `artifacts/stitch/<project>/<screen>/screen.html`, `screen.png`, `meta.json`으로 남긴다.

3. **Contract readiness**
   - source freeze에서 in-memory `screen manifest`, `unity-map`, `presentation-contract`를 준비한다.
   - `presentation-contract.extractionStatus = resolved`가 아니면 active translation success로 보지 않는다.
   - 기존 translator capability 밖이면 해당 surface는 `blocked`로 남기고 capability expansion을 별도 lane으로 분리한다.

4. **Unity reimport**
   - 기존 prefab patch가 아니라 `generate-or-patch` 또는 `prefab-first reset` 기본값으로 본다.
   - scene-owned UI는 먼저 baseline prefab으로 뽑을 수 있는지 판단하고, scene wiring은 마지막에 갱신한다.
   - presentation code는 state render, event, data binding만 담당하게 유지한다.

5. **Integration and evidence**
   - preflight, translation, SceneView capture 또는 route-specific capture, pipeline result를 남긴다.
   - compile/reload 후 `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`를 실행한다.
   - runtime smoke가 필요한 HUD/overlay는 mechanical translation과 actual acceptance를 분리해 판정한다.

## Acceptance

- non-Stitch UI candidate마다 `Stitch source freeze 있음`, `Unity reimport 됨`, `blocked`, `not applicable` 중 하나로 분류되어 있다.
- reimport된 surface는 source freeze와 current prefab/capture가 같은 first-read hierarchy로 보인다.
- required serialized reference가 prefab/scene contract에 연결되어 있다.
- presentation code가 geometry, typography, color literal을 새 owner처럼 갖지 않는다.
- pipeline artifact와 workflow policy evidence가 fresh하다.
- 기존 native prefab을 대체한 경우 runtime smoke 또는 scene-specific verification에서 console error가 없다.

## Blocked / Residual 처리

- Stitch에서 아직 만들지 않은 화면은 `blocked: missing-source-freeze`가 아니라 `pending-source-creation`으로 둔다.
- translator가 지원하지 않는 grammar면 `blocked: capability-expansion-required`로 남기고, 해당 확장은 이 plan의 reimport success로 섞지 않는다.
- world-space combat feedback처럼 full-screen UI가 아닌 surface는 low priority residual로 두고, visual language 통일이 필요할 때만 Stitch micro-surface로 승격한다.
- scene override 때문에 prefab source와 runtime view가 달라지면, visual fidelity mismatch와 scene integration mismatch를 분리해 기록한다.

## 2026-04-26 First Pass: Account Settings Overlay

- surfaceId: `set-c-account-settings-overlay`
- Stitch project/screen: `11729197788183873077` / `bb7b179274c04e85b313b726245da446`
- source freeze:
  - `artifacts/stitch/11729197788183873077/bb7b179274c04e85b313b726245da446/screen.html`
  - `artifacts/stitch/11729197788183873077/bb7b179274c04e85b313b726245da446/screen.png`
  - `artifacts/stitch/11729197788183873077/bb7b179274c04e85b313b726245da446/meta.json`
- no-hardcoding draft probe: `Temp/StitchDraftRoute/set-c-account-settings-overlay-draft-no-hardcoding.json`

Status:

- Stitch source creation: passed
- source freeze fetch: passed
- no-hardcoding draft validation: blocked as expected for heuristic draft
- LLM-authored source-derived draft validation: passed (`Temp/StitchDraftRoute/set-c-account-settings-overlay-llm-draft-v2.json`)
- Unity prefab-first translation: passed
- capture review: passed mechanically, with `artifacts/unity/set-c-account-settings-overlay-scene-capture.png`
- compile check: passed, errors 0 / warnings 0
- docs/rules lint: passed
- workflow policy: blocked by `new-prefab-blocked`; capability/onboarding mix is explicit and must be closed in a separate policy lane or by declared prefab reset recognition

Artifacts:

- Unity prefab: `Assets/Prefabs/Features/Account/Independent/SetCAccountSettingsOverlayRoot.prefab`
- Unity map declaration: `.stitch/contracts/mappings/set-c-account-settings-overlay.json`
- pipeline result: `artifacts/unity/set-c-account-settings-overlay-pipeline-result.json`
- review capture: `artifacts/unity/set-c-account-settings-overlay-scene-capture.png`

No-hardcoding result:

- Script-side/manual text overrides are not allowed in the reimport path.
- `New-StitchOverlayDraftFromSourceFacts.ps1` now leaves heuristic output at `presentation.extractionStatus = pending-source-derivation`, so validator blocks translation until a real LLM/source-derived draft resolves it.
- The accepted Unity import uses the LLM-authored draft as contract input; the translator script does not contain Account Settings literals.

Residual:

- Workflow policy still reports `new-prefab-blocked` for `Assets/Prefabs/Features/Account/Independent/SetCAccountSettingsOverlayRoot.prefab`; this is a policy/declared reset lane residual, not a translator failure.
- Runtime integration into `LobbyScene` / Garage settings is not started.
- Visual fidelity is acceptable for first-read hierarchy but still needs final polish against the Stitch source if this becomes the production settings overlay.

## 검증 명령

- `powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 -SurfaceId <surface-id> -WriteJsonArtifacts`
- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- route-specific Play Mode smoke when the surface is runtime-critical
- `npm run --silent rules:lint`

## 문서 재리뷰

- 과한점 리뷰: 새 Stitch/Unity 규칙, 새 hard-fail, 새 artifact schema를 만들지 않고 기존 owner 문서의 루프를 실행 순서로만 참조한다.
- 부족한점 리뷰: owner/scope, 현재 판단, surface inventory, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 수정 후 재리뷰: 이미 Stitch-derived인 `SetA/SetC/SetD/SetE`를 재작업 대상으로 묶지 않고, mixed/native candidate만 migration 대상으로 좁혔다.
- 반복 재리뷰 반영: generated Nova1492 preview model prefab과 gameplay prefab 구조 변경은 제외 범위로 분리했다.
- 2026-04-26 first pass 후 재리뷰: Account settings overlay는 Stitch source freeze만 active evidence로 유지한다. Script-side/manual 값을 섞은 debug prefab/capture는 제거했고, no-hardcoding draft probe는 validator blocked 상태로 남긴다.
- owner impact: primary `plans.non-stitch-ui-stitch-reimport`; secondary `plans.progress`, `docs.index`; out-of-scope `ops.stitch-data-workflow`, `ops.unity-ui-authoring-workflow` 규칙 개정.
- doc lifecycle checked: 새 active plan으로 등록한다. 이전 Stitch inventory plan은 lifecycle cleanup으로 제거했고, 이 plan은 native/mixed UI migration closeout 뒤 reference 전환 후보로 본다.
- plan rereview: Account settings overlay now has source freeze, source-derived LLM draft, prefab-first Unity reimport, pipeline artifact, and capture evidence. Residual is workflow policy new-prefab approval plus runtime scene wiring.
