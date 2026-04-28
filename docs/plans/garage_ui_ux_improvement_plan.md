# Garage UI/UX Recovery Plan

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: plans.garage-ui-ux-improvement
> role: plan
> owner_scope: Set B Garage Stitch-to-UI Toolkit recovery의 현재 범위와 acceptance 기준
> upstream: plans.progress, design.ui-foundations, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `Assets/UI/UIToolkit/GarageSetB/`, `Assets/Scenes/GarageSetBUitkPreview.unity`, `artifacts/unity/garage-setb-uitoolkit-preview.png`
>
> 생성일: 2026-04-13
> 근거: 2026-04-25 Set B recovery audit

이 문서는 Set B Garage만 다룬다.
기준은 `source freeze -> source visual contract -> UI Toolkit candidate -> preview capture -> visual judgment` 한 줄이다.
Set B는 Set A 진입 전 범용 루프의 reference/sample surface로만 읽고, `.stitch/designs` copied source를 active source owner처럼 재정의하지 않는다.
Set A의 새 기준은 별도 전용 helper가 아니라 `source html/png -> source facts -> contract draft -> validate -> UI Toolkit candidate -> preview capture -> verdict` 루프에서 pass 또는 blocked verdict를 남기는 것이다.

## Target

- surface: `set-b-garage-main-workspace`
- source convenience/reference: `.stitch/designs/set-b-garage-main-workspace.html/png`
- UI Toolkit candidate:
  - `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml`
  - `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss`
- preview scene: `Assets/Scenes/GarageSetBUitkPreview.unity`
- capture: `artifacts/unity/garage-setb-uitoolkit-preview.png`

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\stitch-unity\presentations\Generate-StitchPresentationProfile.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -CanGenerateOnly
```

이 명령은 source가 presentation profile 대상인지 확인한다.
후보 구현과 캡처는 UI Toolkit candidate surface와 preview scene에서 별도 evidence로 남긴다.

## Done

- Set B source 이름을 `set-b-garage-main-workspace`로 맞췄다.
- screen별 전용 contract file 없이 parser route로 닫았다.
- 화면 분류 기준은 source facts와 contract loop에서 확인한다.
- `summary-card` 의미가 compiled contract와 translation path에 다시 들어왔다.
- Set B UI Toolkit candidate와 preview scene이 current route다.

## Open

- runtime save/settings 검증은 shared Account/Garage validation lane에서 본다.
- UI Toolkit pilot surface exists for Set B visual comparison:
  - `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml`
  - `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss`
  - `Assets/Scenes/GarageSetBUitkPreview.unity`
  - `artifacts/unity/garage-setb-uitoolkit-preview.png`
- The pilot is static and does not replace runtime Garage UI until a separate runtime binding/replacement pass is accepted.

## Lifecycle

- reference 전환 이유: Set B Garage visual fidelity verdict는 `mismatch`로 기록됐고, 이후 source 후보 개선은 closeout되어 `plans.progress`, `design.ui-foundations`, Unity/Stitch evidence artifacts로 이관됐다. Runtime binding/replacement gap은 `garage_setb_uitk_runtime_gap_plan.md`가 소유한다.
- 남은 save/load/settings/accessibility 검증은 shared `Account/Garage` lane으로만 남는다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

## Visual Fidelity Verdict

- verdict: `mismatch`
- judgedAt: 2026-04-25
- source: `.stitch/designs/set-b-garage-main-workspace.png`
- compared captures:
  - `artifacts/unity/set-b-garage-main-workspace-scene-capture.png`
  - `artifacts/unity/lobby-scene-garage-tab-polished.png`
- mechanical status: structure and semantic block verification passed, but mechanical pass is not acceptance.
- acceptance status: source and current Unity captures do not yet read as the same final screen.

Mismatch reasons:

- source header/summary chrome is clipped or missing in Unity captures.
- source card framing, icon treatment, border contrast, and compact dark-panel hierarchy are not preserved.
- source `slot selector -> focus bar -> editor -> preview -> save dock` flow exists structurally, but current LobbyScene Garage capture reduces the page to roster plus focused editor and hides the preview/summary/save-dock relationship.
- source final preview has a framed blueprint/cockpit read with progress treatment; Unity capture reads as sparse placeholder text/empty space.
- source primary CTA is compact and persistent; Unity pipeline capture shows an oversized dock, while latest LobbyScene capture does not show it in the same first-read composition.

Closeout note:

- This is a translation mismatch, not a polish-only residual.
- Do not mark Set B Garage visual fidelity as accepted until a fresh capture matches the source screen's first-read hierarchy, card density, preview treatment, and persistent save action.
- 2026-04-27 UI Toolkit pilot capture improves first-read hierarchy but remains evidence for a candidate route, not acceptance of the active runtime Garage.

Plan rereview:

- 과한점: no new owner rules were added; UITK pilot evidence is recorded as candidate evidence only.
- 부족한점: active runtime replacement, state binding, 3D preview integration, and fresh Lobby/Garage flow acceptance remain explicitly open.
- 2026-04-28 lifecycle cleanup 재리뷰: 과한점은 mismatch verdict 문서를 계속 active owner로 유지하지 않고 후속 owner로 이관했다. 부족한점은 source 개선과 runtime gap owner를 Lifecycle에 명시해 해소했다.
- doc lifecycle checked: active plan에서 reference visual mismatch 기록으로 전환한다.
- plan rereview: clean

## Acceptance

- source와 UI Toolkit candidate capture가 같은 화면으로 보인다.
- candidate evidence가 성공 또는 blocked reason을 한 파일에 담는다.
- capture가 현재 UXML/USS와 preview scene을 기준으로 fresh하다.
- visual mismatch가 있으면 polish가 아니라 translation mismatch로 기록한다.
