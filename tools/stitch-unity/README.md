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

`source freeze -> compiled/draft contract -> validation/preflight -> translation -> SceneView capture -> pipeline result`

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

LLM draft JSON을 이미 만들었다면 같은 entry에 `-DraftPath`를 넘긴다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -DraftPath <draft.json> `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab> `
  -WriteJsonArtifacts
```

## Inputs

- source: `.stitch/designs/<surface-id>.html`
- image: `.stitch/designs/<surface-id>.png`
- target prefab path: `-TargetAssetPath`

## Collect

LLM draft 전에 source facts만 확인할 수 있다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\collectors\Collect-StitchSourceFacts.ps1 `
  -SurfaceId <surface-id> `
  -HtmlPath <source.html> `
  -ImagePath <source.png> `
  -TargetAssetPath <target.prefab>
```

기본은 stdout JSON이다.
디버그 파일이 필요할 때만 `-OutputPath <path>`를 넘긴다.

## Validate

LLM이 만든 contract draft JSON은 translation 전에 먼저 검사한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\validators\Test-StitchContractDraft.ps1 `
  -DraftPath <draft.json> `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab>
```

실패하면 `terminalVerdict = blocked`와 `blockedReason`을 출력하고 non-zero로 종료한다.
`Invoke-StitchSurfaceTranslation.ps1 -DraftPath`도 같은 검사를 먼저 실행한다.

## Outputs

- prefab: `-TargetAssetPath`에 적힌 Unity prefab
- result: `artifacts/unity/<surface-id>-pipeline-result.json`
- capture: `artifacts/unity/<surface-id>-scene-capture.png`

`pipeline-result.json` 안에 preflight, translation, review capture 상태를 같이 담는다.
별도 preflight/translation JSON은 기본 evidence로 만들지 않는다.

## Notes

기본 실행은 source에서 contract를 준비하고, draft 실행은 `-DraftPath`에서 받은 contract를 검증한 뒤 같은 translator로 넘긴다.
실패한 실행은 pipeline result에 reason을 남긴다.
capture는 visual fidelity review용이며 runtime 검증 증거와는 분리한다.

## Checks

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\presentations\Generate-StitchPresentationProfile.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -CanGenerateOnly
```

```powershell
npm run --silent rules:lint
```
