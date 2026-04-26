# Workflow Helpers

> 마지막 업데이트: 2026-04-26
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
.\tools\workflow\Invoke-AgentPassReview.ps1 -Agent B
.\tools\workflow\Invoke-AgentPassReview.ps1 -Agent C
.\tools\workflow\Invoke-CloseoutPack.ps1 -PlanOnly
.\tools\workflow\Find-SimplificationCandidates.ps1
.\tools\workflow\Test-GeneratedArtifactScope.ps1
.\tools\workflow\Get-NextWorkSuggestion.ps1
.\tools\workflow\Test-UnityCliRunTestsPreflight.ps1
.\tools\workflow\Invoke-UnityEditModeTests.ps1 -TestFilter Tests.Editor.PlacementAreaViewDirectTests -ResultName placement-area-view-direct-tests
```

Use `Invoke-AgentPassReview.ps1 -RunCloseoutPack` only when the current dirty worktree is scoped enough that running generated artifact and Unity policy checks will not overwrite another lane's evidence.

For parallel Agent A/B/C work, run closeout with the matching `-Agent` value so Unity UI policy evidence is written to an agent-scoped artifact such as `artifacts/unity/unity-ui-authoring-workflow-policy-b.json`.
Do not use the shared `unity-ui-authoring-workflow-policy.json` as closeout evidence when another lane has dirty files.

Do not call `Unity.exe -batchmode -runTests` directly while this repo is open in Unity Editor.
Use `Invoke-UnityEditModeTests.ps1`; it first checks `Library/EditorInstance.json` and blocks with `open-editor-owns-project` when the active editor already owns the project.
