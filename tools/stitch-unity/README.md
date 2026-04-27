# Stitch Unity Source Utilities

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: tools.stitch-unity-readme
> role: reference
> owner_scope: Stitch source fact, contract, and presentation profile execution reference
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.stitch-to-unity-translation-guide
> artifacts: `tools/stitch-unity/`, `artifacts/stitch/`, `artifacts/unity/`

이 폴더는 Stitch source를 Unity 쪽 후보 구현으로 넘기기 전에 source facts, contract draft, presentation profile을 확인하는 실행 스크립트를 둔다.

현재 Lobby/Garage UI 기본 route는 UI Toolkit candidate surface다.

`source freeze -> source visual contract review -> UI Toolkit candidate surface -> preview scene/capture -> runtime replacement`

## Collect

LLM draft 전에 source facts를 확인한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\collectors\Collect-StitchSourceFacts.ps1 `
  -SurfaceId <surface-id> `
  -HtmlPath <source.html> `
  -ImagePath <source.png>
```

기본은 stdout JSON이다.
디버그 파일이 필요할 때만 `-OutputPath <path>`를 넘긴다.

## Validate

LLM이 만든 contract draft JSON은 후보 구현 전에 먼저 검사한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\validators\Test-StitchContractDraft.ps1 `
  -DraftPath <draft.json> `
  -SurfaceId <surface-id>
```

실패하면 `terminalVerdict = blocked`와 `blockedReason`을 출력하고 non-zero로 종료한다.

## Presentation Profile

Stitch source가 현재 presentation profile 생성 대상인지 확인한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\presentations\Generate-StitchPresentationProfile.ps1 `
  -SurfaceId set-b-garage-main-workspace `
  -CanGenerateOnly
```

필요하면 `-PresentationOutputPath <path>`로 JSON profile을 남긴다.
기존 `-TargetAssetPath` 인자는 contract compatibility 용도이며, 새 UI 구현의 기본 산출물 경로로 보지 않는다.

## Outputs

- source facts: stdout 또는 `-OutputPath`
- contract validation: stdout JSON
- presentation profile: stdout 또는 `-PresentationOutputPath`
- UI candidate evidence: `Assets/UI/UIToolkit/`, preview scene, `artifacts/unity/*.png|*.json`

## Notes

새 Stitch 기반 UI 작업은 UI Toolkit candidate surface와 preview capture로 검토한다.

## Checks

```powershell
npm run --silent rules:lint
```
