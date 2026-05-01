# Non-Stitch UI Stitch Import Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.non-stitch-ui-stitch-reimport
> role: plan
> owner_scope: Stitch source freeze가 없는 Unity-native/mixed UI의 source-freeze routing, UI Toolkit candidate handoff, owner split gate
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/unity/account-sync-uitk-preview-gameview.png`, `artifacts/unity/connection-reconnect-uitk-preview-gameview.png`, `artifacts/unity/account-sync-runtime-lobby-shell.png`, `artifacts/unity/connection-reconnect-runtime-lobby-shell.png`, `artifacts/unity/account-connection-uitk-candidate-report.md`, `artifacts/unity/account-connection-uitk-workflow-policy.json`, `artifacts/unity/account-connection-runtime-replacement-policy.json`

이 문서는 native/mixed UI surface를 source freeze와 UI Toolkit candidate route로 넘기는 handoff만 소유한다. Stitch/UITK 운영 규칙 본문은 upstream owner 문서를 따른다.

## Current Judgment

- Active route는 `Stitch source freeze -> UI Toolkit candidate surface -> isolated preview capture/report -> runtime replacement owner handoff`다.
- Account/Sync와 Connection/Reconnect는 source candidate, UI Toolkit candidate, Lobby runtime shell visibility까지 확보했다. WebGL/account/cloud acceptance는 `plans.progress` WebGL account residual과 WebGL smoke checklist에서 확인한다.
- `SetA/SetC/SetD/SetE` prefabs/captures는 historical Stitch-derived evidence로만 본다.
- Runtime feedback prefabs under `Assets/Prefabs/RuntimeFeedback/`는 screen UI replacement 대상이 아니라 world-space feedback compatibility surface다.

## Surface Inventory

| Surface | Current state | This plan owns | Handoff owner |
|---|---|---|---|
| Account/Sync | source candidate + UITK candidate + runtime shell visibility | source/candidate handoff status | `plans.progress` WebGL account residual |
| Connection/Reconnect | source candidate + UITK candidate + runtime shell visibility | source/candidate handoff status | reconnect/cloud product owner |
| Battle HUD / Skill selection | BattleScene scene-owned UI, no accepted source freeze yet | source-freeze required marker | BattleScene UI/runtime owner |
| Player/Enemy health, damage number | RuntimeFeedback compatibility prefab | route exclusion marker | runtime feedback owner if visual consistency work opens |

## Execution Rule

- Stitch source가 없으면 먼저 accepted screen/source freeze를 만든다.
- `presentation-contract.extractionStatus = resolved` 전에는 active translation success로 보지 않는다.
- translator capability 밖이면 `blocked: capability-expansion-required`로 남기고 이 plan의 reimport success로 섞지 않는다.
- preview capture/report, runtime visibility, product acceptance를 같은 success로 묶지 않는다.

## Validation

- Source facts/draft validation: `tools/stitch-unity` collector/validator route.
- UITK candidate check: isolated preview scene and GameView capture.
- Runtime check: current host scene에서 surface visibility와 product acceptance를 분리하고, product acceptance는 `plans.progress` residual 또는 새 product owner가 열릴 때 그쪽으로 넘긴다.
- Policy lint: `npm run --silent rules:lint`.

## Residual

- Battle HUD와 skill-selection UI는 source freeze 전까지 native candidate다.
- Account/Connection product acceptance는 `plans.progress` WebGL account residual과 reconnect/cloud follow-up route에 남는다.
- Runtime feedback visual consistency는 필요할 때 별도 micro-surface로 다시 연다.

owner impact:

- primary: `plans.non-stitch-ui-stitch-reimport`
- secondary: `plans.progress`, `design.ui-foundations`
- out-of-scope: Stitch/UITK policy 규칙 개정, translator capability expansion, code/API 변경

doc lifecycle checked:

- active 유지. 남은 native/mixed candidate가 source freeze/candidate/verdict로 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - source/candidate handoff and product residual routing checked
