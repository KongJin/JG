# Stitch Unity Surface Generator

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: tools.stitch-unity-readme
> role: reference
> owner_scope: Stitch Unity surface generator 실행 reference
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.stitch-to-unity-translation-guide
> artifacts: `tools/stitch-unity/`, `artifacts/stitch/`, `artifacts/unity/`

이 폴더는 Stitch source를 Unity prefab으로 옮기는 실행 스크립트다.
기본 흐름은 하나다.

`source freeze -> compiled contract -> translation -> SceneView capture -> pipeline result`

## Command

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId <surface-id> `
  -TargetAssetPath <unity-prefab-path> `
  -WriteJsonArtifacts
```

예시:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -TargetAssetPath Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab `
  -WriteJsonArtifacts
```

파일명이 surface id와 다르면 source를 직접 넘긴다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId set-x-name `
  -HtmlPath .stitch/designs/source.html `
  -ImagePath .stitch/designs/source.png `
  -TargetAssetPath Assets/Prefabs/Features/Feature/Root/FeaturePageRoot.prefab `
  -WriteJsonArtifacts
```

## Inputs

- source: `.stitch/designs/<surface-id>.html`
- image: `.stitch/designs/<surface-id>.png`
- target prefab path: `-TargetAssetPath`

## Outputs

- prefab: `-TargetAssetPath`에 적힌 Unity prefab
- result: `artifacts/unity/<surface-id>-pipeline-result.json`
- capture: `artifacts/unity/<surface-id>-scene-capture.png`

`pipeline-result.json` 안에 preflight, translation, review capture 상태를 같이 담는다.
별도 preflight/translation JSON은 기본 evidence로 만들지 않는다.

## Rule

- screen마다 전용 parser나 전용 contract file을 늘리지 않는다.
- script는 source에서 compiled contract를 만든 뒤 실행한다.
- 실패하면 보정하지 않고 pipeline result에 reason을 남긴다.
- capture는 visual fidelity review용이다. runtime 검증 증거가 아니다.

## Checks

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\presentations\Generate-StitchPresentationProfile.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -CanGenerateOnly
```

```powershell
npm run --silent rules:lint
```
