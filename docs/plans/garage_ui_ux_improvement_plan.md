# Garage UI/UX Recovery Plan

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: plans.garage-ui-ux-improvement
> role: plan
> owner_scope: Set B Garage Stitch-to-Unity recovery의 현재 범위와 acceptance 기준
> upstream: plans.progress, design.ui-foundations, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab`, `artifacts/unity/set-b-garage-main-workspace-pipeline-result.json`, `artifacts/unity/set-b-garage-main-workspace-scene-capture.png`
>
> 생성일: 2026-04-13
> 근거: 2026-04-25 Set B recovery audit

이 문서는 Set B Garage만 다룬다.
기준은 `source freeze -> compiled contract -> translation -> SceneView capture -> visual judgment` 한 줄이다.
Set B는 Set A 진입 전 범용 루프의 reference/sample surface로만 읽고, `.stitch/designs` copied source를 active source owner처럼 재정의하지 않는다.
Set A의 새 기준은 별도 전용 helper가 아니라 `source html/png -> source facts -> contract draft -> validate -> translate/generate -> capture -> verdict` 루프에서 pass 또는 blocked verdict를 남기는 것이다.

## Target

- surface: `set-b-garage-main-workspace`
- source convenience/reference: `.stitch/designs/set-b-garage-main-workspace.html/png`
- prefab: `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab`
- evidence: `artifacts/unity/set-b-garage-main-workspace-pipeline-result.json`
- capture: `artifacts/unity/set-b-garage-main-workspace-scene-capture.png`

## Command

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -WriteJsonArtifacts
```

이 명령 하나가 source를 읽고, contract를 만들고, prefab을 갱신하고, 캡처까지 남긴다.

## Done

- Set B source 이름을 `set-b-garage-main-workspace`로 맞췄다.
- screen별 전용 contract file 없이 parser route로 닫았다.
- 화면 분류용 수동 기준을 active path에서 제거했다.
- `summary-card` 의미가 compiled contract와 translation path에 다시 들어왔다.
- `GaragePageRoot.prefab`와 SceneView capture 경로가 current route에 맞춰졌다.

## Open

- source PNG와 SceneView capture를 눈으로 비교해서 visual fidelity를 닫아야 한다.
- runtime save/settings 검증은 shared Account/Garage validation lane에서 본다.

## Acceptance

- source와 prefab/capture가 같은 화면으로 보인다.
- pipeline result가 성공 또는 blocked reason을 한 파일에 담는다.
- capture가 현재 prefab을 기준으로 fresh하다.
- visual mismatch가 있으면 polish가 아니라 translation mismatch로 기록한다.
