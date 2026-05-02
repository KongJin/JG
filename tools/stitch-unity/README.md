# Stitch Unity Source Utilities

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: tools.stitch-unity-readme
> role: reference
> owner_scope: Stitch source fact extraction and UI Toolkit handoff reference
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.unity-ui-authoring-workflow
> artifacts: `tools/stitch-unity/`, `artifacts/stitch/`, `artifacts/unity/`

이 폴더는 Stitch source를 Unity UI Toolkit 후보 구현으로 넘기기 전에 source facts를 확인하는 실행 스크립트를 둔다.

현재 Lobby/Garage UI 기본 route는 UI Toolkit candidate surface다.

`source freeze -> source visual contract review -> UI Toolkit candidate surface -> preview scene/capture -> runtime replacement`

The old Stitch-to-UGUI/TMP translation pipeline is disabled. Scripts that used to generate or validate Unity component contracts now fail closed with `blockedReason = legacy-ugui-translator-disabled`.

## Collect

LLM draft 전에 source facts를 확인한다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\stitch-unity\collectors\Collect-StitchSourceFacts.ps1 `
  -SurfaceId <surface-id> `
  -HtmlPath <source.html> `
  -ImagePath <source.png>
```

기본은 stdout JSON이다.
디버그 파일이 필요할 때만 `-OutputPath <path>`를 넘긴다.

## Disabled Legacy Routes

The following scripts are kept only as compatibility entrypoints for callers that still reference old paths. They do not produce accepted artifacts:

- `drafts/New-StitchOverlayDraftFromSourceFacts.ps1`
- `presentations/Generate-StitchPresentationProfile.ps1`
- `presentations/Resolve-StitchPresentationContract.ps1`
- `validators/Test-StitchContractDraft.ps1`
- `engine/StitchUnityCommon.ps1` translation/preflight functions

## Outputs

- source facts: stdout 또는 `-OutputPath`
- UI candidate evidence: `Assets/UI/UIToolkit/`, preview scene, `artifacts/unity/*.png|*.json`

## Notes

새 Stitch 기반 UI 작업은 UI Toolkit candidate surface와 preview capture로 검토한다. UGUI/TMP component contract, RectTransform layout, Canvas hierarchy, or TextMeshPro presentation-contract generation are not valid routes for this repo.

## Checks

```powershell
npm run --silent rules:lint
```
