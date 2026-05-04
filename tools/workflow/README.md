# Workflow Helpers

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: tools.workflow-readme
> role: reference
> owner_scope: 반복 review/closeout helper 스크립트 사용법
> upstream: ops.document-management-workflow, ops.acceptance-reporting-guardrails
> artifacts: `tools/workflow/`

Small local scripts for the repeated review loop:

1. inspect current dirty scope,
2. choose the right validation pack,
3. flag generated evidence that captured unrelated work,
4. find the next simplification candidate.

These helpers do not define new repo policy. They only automate checks already described by the owner docs and existing validation scripts.

## Commands

```powershell
.\tools\workflow\Invoke-CloseoutPack.ps1 -PlanOnly
.\tools\workflow\Find-SimplificationCandidates.ps1
.\tools\workflow\Test-GeneratedArtifactScope.ps1
.\tools\workflow\Test-FeaturePresentationStructure.ps1
.\tools\workflow\Test-GaragePresentationStructure.ps1
npm run --silent feature:presentation:lint
npm run --silent garage:presentation:lint
.\tools\workflow\Get-NextWorkSuggestion.ps1
.\tools\workflow\Test-UnityCliRunTestsPreflight.ps1
.\tools\workflow\Invoke-UnityEditModeTests.ps1 -TestFilter Tests.Editor.PlacementAreaViewDirectTests -ResultName placement-area-view-direct-tests
```

Use `Invoke-CloseoutPack.ps1 -PlanOnly` before a mixed runtime/presentation closeout to see which checks would run.
Do not use the shared `unity-ui-authoring-workflow-policy.json` as closeout evidence when unrelated dirty files are present.

Do not call `Unity.exe -batchmode -runTests` directly while this repo is open in Unity Editor.
Use `Invoke-UnityEditModeTests.ps1`; it first checks `Library/EditorInstance.json` and blocks with `open-editor-owns-project` when the active editor already owns the project.
If the editor is open and MCP is available, prefer `tools\unity-mcp\Invoke-UnityMcpEditModeTests.ps1` for targeted EditMode tests. Its safe default is now to preserve the current Play Mode session and block instead of stopping the editor. Pass `-AllowPlayModeStop` only when the current verification lane intentionally owns that interruption.

Unity Editor, Play Mode, MCP UI policy, screenshots, and CLI EditMode tests are treated as one shared Unity resource.
`Invoke-UnityEditModeTests.ps1`, `Invoke-UnityMcpEditModeTests.ps1`, `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`, and MCP runtime smoke helpers acquire `Temp/UnityMcp/unity-resource.lock` before using that resource.
If another live process owns the lock, treat the workflow as `blocked: unity-resource-lock-held` instead of running a competing Unity operation.
