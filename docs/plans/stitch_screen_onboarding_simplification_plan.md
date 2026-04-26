# Stitch Screen Onboarding Simplification Plan

> 마지막 업데이트: 2026-04-26
> 상태: draft
> doc_id: plans.stitch-screen-onboarding-simplification
> role: plan
> owner_scope: 새 Stitch screen을 Unity prefab route로 가져올 때 반복 blocker를 줄이는 실행 계획
> upstream: plans.progress, plans.stitch-llm-contract-pipeline, plans.stitch-ui-ux-overhaul, ops.stitch-to-unity-translation-guide, ops.unity-ui-authoring-workflow
> artifacts: `Temp/StitchDraftRoute/*.json`, `.stitch/contracts/draft-templates/*.json`, `artifacts/unity/*-pipeline-result.json`, `artifacts/unity/*-scene-capture.png`

이 문서는 새 Stitch screen을 한 장씩 가져올 때 Set A에서 드러난 반복 blocker를 줄이기 위한 계획이다.
운영 규칙 본문은 `ops.stitch-to-unity-translation-guide`, `ops.unity-ui-authoring-workflow`, `ops.stitch-data-workflow`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

기준 루프는 그대로 유지한다.

```text
source html/png -> source facts -> contract draft -> validate -> translate/generate -> capture -> verdict
```

## Draft Triage

- 판정: draft 유지.
- 이유: template/validator/next-screen trial first pass는 끝났지만, 신규 prefab policy path가 residual로 남아 있다.
- active 전환 조건: 새 screen onboarding을 다시 열고 `new-prefab-blocked` policy 판단을 직접 닫는 세션에서 active로 올린다.
- reference 전환 조건: 신규 prefab policy 판단이 다른 owner 문서나 `plans.progress`로 이관되고, 이 문서가 runbook 기록으로만 남으면 reference로 내린다.

## Goal

- 다음 screen부터는 새 세션이 매번 같은 blocker를 디버깅하지 않게 한다.
- overlay/dialog와 workspace/page root의 draft 시작점을 분리한다.
- Unity MCP 500으로 늦게 터지는 구조 문제를 draft validation에서 먼저 잡는다.
- 새 prefab 생성이 의도된 경우 policy blocked를 예측 가능하게 만든다.
- set-specific helper나 screen별 parser 보강 없이 generic loop를 유지한다.

## Scope

- primary owner: `tools/stitch-unity` draft/validation route
- secondary owner: `docs/ops/stitch_to_unity_translation_guide.md`
- policy owner: `docs/ops/unity_ui_authoring_workflow.md`
- progress owner: `docs/plans/progress.md`

이 plan이 직접 다루는 것:

- draft 작성 기준 템플릿화
- workspace-root baseline 누락 방지
- `Image` + `TextMeshProUGUI` 혼합 노드 사전 차단
- 새 prefab policy guard를 통과 또는 명시 blocked로 남기는 절차
- 다음 screen runbook

범위 밖:

- `.stitch/designs/*` 이동/삭제
- source freeze 구조 변경
- set-specific SceneTool 또는 set-specific helper 재도입
- Set A visual polish 자체
- Unity UI workflow policy를 전체적으로 완화하는 변경
- runtime wiring correctness closeout

## Current Friction

Set A에서 확인된 반복 blocker:

| blocker | 언제 발생했나 | 다음 screen 영향 |
|---|---|---|
| workspace-root baseline 누락 | `set-a-lobby-populated` preflight | page/root screen마다 재발 가능 |
| `Image` + `TextMeshProUGUI` 혼합 노드 | Unity MCP component add 단계 | validator가 못 잡으면 Unity 단계에서 늦게 실패 |
| 신규 prefab policy guard | `LobbyPageRoot`, `SetACreateRoomModalRoot` 생성 후 workflow policy | 새 prefab 생성 screen마다 재발 가능 |
| capture visual fidelity 미완 | pipeline passed 이후 육안 판단 | mechanical pass와 acceptance가 섞일 위험 |

## Target Shape

새 screen은 아래 셋 중 하나로 먼저 분류한다.

| target kind | 시작 기준 | 필수 baseline |
|---|---|---|
| `overlay-root` | modal, dialog, error, confirm, loading overlay | full-screen root, scrim optional, fixed dialog/panel |
| `workspace-root` | lobby, garage, page-level workspace | `HeaderChrome`, `HeaderChrome/TitleGroup`, `MainScroll`, `MainScroll/Content`, `SaveDock` |
| unsupported | scene composition, runtime wiring, custom interaction이 먼저 필요한 screen | translation 전 blocked verdict |

draft authoring 기본값:

- 텍스트는 배경 노드와 섞지 않고 child `Label` 또는 text-only node로 둔다.
- `Image`가 필요한 노드는 container/background로만 둔다.
- `Button` label은 `Button/Label` child를 둔다.
- `workspace-root`는 baseline element와 required rect/property를 draft 시작 시 포함한다.
- shared bottom nav, global chrome, runtime owner가 다른 표면은 target root에 섞지 않고 note와 validation check로 남긴다.

## Phases

### Phase 1 - Draft Templates

상태: first pass implemented

- overlay-root draft skeleton을 만든다.
- workspace-root draft skeleton을 만든다.
- template은 active source owner가 아니라 draft 작성 편의 입력으로 둔다.
- Set A modal/lobby draft에서 검증된 구조만 template에 반영한다.

결과:

- `.stitch/contracts/draft-templates/overlay-root-draft.template.json`을 추가했다.
- `.stitch/contracts/draft-templates/workspace-root-draft.template.json`을 추가했다.
- 두 template 모두 placeholder를 가진 JSON convenience input이며, active source owner나 execution contract로 취급하지 않는다.

Acceptance:

- 새 overlay screen draft가 template에서 시작해 validator를 통과한다.
- 새 workspace-root screen draft가 baseline preflight blocker 없이 시작한다.
- template에 `.stitch/designs/*`를 active owner로 설명하지 않는다.

### Phase 2 - Validator Guard 보강

상태: first pass implemented

- presentation element 하나에 `Image`와 `TextMeshProUGUI`가 같이 있으면 validator가 blocked로 잡는다.
- `Button`에 label text가 직접 붙는 패턴 대신 child label을 요구하거나 warning으로 남긴다.
- workspace-root baseline required components/rect/properties를 validator 단계에서 더 읽기 쉬운 issue로 보여준다.

결과:

- `tools/stitch-unity/validators/Test-StitchContractDraft.ps1`가 `Image` + `TextMeshProUGUI` 혼합 노드와 button host text를 Unity MCP 호출 전에 blocked로 잡는다.
- `workspace-root` draft는 `HeaderChrome`, `HeaderChrome/TitleGroup`, `MainScroll`, `MainScroll/Content`, `SaveDock` baseline을 validator 단계에서 확인한다.
- 기존 `Set A/B/C` draft validation은 false positive 없이 통과했다.

Acceptance:

- mixed `Image` + `TextMeshProUGUI` draft는 Unity MCP 호출 전에 blocked 된다.
- blocked reason이 수정 가능한 host path를 포함한다.
- 기존 Set A/B/C passed draft는 false positive 없이 통과한다.

### Phase 3 - New Prefab Policy Path

상태: residual

- 새 prefab이 의도된 screen과 기존 prefab patch screen을 시작 전에 구분한다.
- 의도된 신규 prefab이면 policy가 읽을 수 있는 declared reset target 경로가 필요한지 결정한다.
- declared reset target 방식이 현재 no-per-surface-active-contract 기준과 충돌하면, policy result의 `new-prefab-blocked`를 expected residual로 보고한다.

결과:

- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1 -AllowCapabilityExpansion` 기준 capability/onboarding 혼합 blocker는 분리 확인했다.
- 남은 blocker는 신규 Set A/C/D/E prefab 생성에 대한 `new-prefab-blocked` policy guard다.
- 이번 onboarding은 사용자 요청 범위상 신규 prefab 생성이 의도됐지만, policy가 읽을 declared reset target은 아직 정리하지 않았다.

Acceptance:

- 새 prefab 생성이 silent success로 포장되지 않는다.
- policy가 passed인 경우와 blocked인 경우의 closeout 문구가 분리된다.
- workflow policy 자체를 전체 완화하지 않는다.

### Phase 4 - Next Screen Trial

상태: first pass implemented

- 아직 가져오지 않은 screen 1개를 골라 template route로 실행한다.
- source facts, draft validate, translate/generate, capture, verdict를 남긴다.
- 도중 blocker가 나오면 template/validator/policy path 중 어느 단계의 문제인지 분류한다.

결과:

- 다음 7개 surface를 generic overlay draft route로 실행했다: `set-c-login-loading-overlay`, `set-c-room-detail-panel`, `set-d-battle-hud-baseline`, `set-d-low-core-warning`, `set-d-unit-stats-popup`, `set-e-mission-defeat-overlay`, `set-e-mission-victory-overlay`.
- 7개 모두 draft validation, translation/generation, SceneView capture까지 pipeline `passed` verdict를 남겼다.
- Battle 보강으로 `.stitch/designs` 밖의 `GameScene HUD` source freeze도 `set-d-gamescene-hud-full` surface로 실행했고, same generic draft route에서 `passed` verdict를 남겼다.
- `tools/stitch-unity/drafts/New-StitchOverlayDraftFromSourceFacts.ps1`는 source facts에서 최소 overlay draft를 생성하는 generic helper다.

Acceptance:

- 새 screen 1개가 set-specific helper 없이 `passed`, `blocked`, 또는 `mismatch` verdict를 남긴다.
- Unity MCP 500 같은 late blocker가 재발하면 validator 보강 TODO로 환류한다.
- `progress.md`에는 결과와 남은 리스크만 짧게 반영한다.

## Next Session Runbook

1. `docs/index.md -> plans/progress.md -> this plan -> ops/stitch_to_unity_translation_guide.md` 순서로 읽는다.
2. surface id와 source `html/png` 존재를 확인한다.
3. source facts를 수집한다.
4. target kind를 `overlay-root`, `workspace-root`, `unsupported` 중 하나로 결정한다.
5. target kind에 맞는 draft template으로 시작한다.
6. semantic blocks, CTA priority, validation checks만 screen별로 채운다.
7. draft validator를 먼저 실행한다.
8. validator가 passed일 때만 translation/generation을 실행한다.
9. SceneView capture를 만든다.
10. pipeline result에 `passed`, `blocked`, 또는 `mismatch`를 남긴다.
11. Unity workflow policy 결과를 mechanical pass와 분리해서 보고한다.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\collectors\Collect-StitchSourceFacts.ps1 `
  -SurfaceId <surface-id>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\validators\Test-StitchContractDraft.ps1 `
  -DraftPath Temp\StitchDraftRoute\<surface-id>-draft.json `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -DraftPath Temp\StitchDraftRoute\<surface-id>-draft.json `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab> `
  -WriteJsonArtifacts
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
```

```powershell
npm run --silent rules:lint
```

## Residual Risks

- template이 너무 두꺼워지면 screen별 판단을 다시 숨긴다.
- validator가 visual fidelity를 보장하지는 못한다.
- 새 prefab policy path는 현재 policy guard와 no-per-surface-active-contract 기준 사이의 결정을 요구할 수 있다.
- `workspace-root` capture는 passed라도 visual fidelity final judgment가 별도로 필요할 수 있다.
