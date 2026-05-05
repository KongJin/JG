# Non-Stitch UI Stitch Import Plan

> 마지막 업데이트: 2026-05-01
> 상태: reference
> doc_id: plans.non-stitch-ui-stitch-reimport
> role: plan
> owner_scope: historical source/candidate handoff record and residual routing for Unity-native/mixed UI surfaces
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.unity-ui-authoring-workflow
> artifacts: none

이 문서는 native/mixed UI surface의 source/candidate handoff 기록과 residual route만 보존한다.
새 source freeze, UI Toolkit candidate, runtime replacement 실행은 upstream owner 문서와 새 작업 owner에서 다시 연다.

## Closeout

- Account/Sync와 Connection/Reconnect는 source candidate, UI Toolkit candidate, Lobby runtime shell visibility까지 확보했다. WebGL/account/cloud acceptance는 WebGL smoke checklist에서 확인한다.
- `SetA/SetC/SetD/SetE` prefabs/captures는 historical Stitch-derived evidence로만 본다.
- Runtime feedback prefabs under `Assets/Prefabs/RuntimeFeedback/`는 screen UI replacement 대상이 아니라 world-space feedback compatibility surface다.

## Residual Owner

| Surface | Residual | Owner |
|---|---|---|
| Account/Sync | WebGL/account/cloud product acceptance | WebGL smoke checklist |
| Connection/Reconnect | reconnect/cloud product acceptance | new reconnect/cloud product owner when opened |
| Battle HUD / Skill selection | accepted source freeze and candidate handoff not started | new BattleScene UI/runtime owner routed through `ops.stitch-data-workflow` and `ops.unity-ui-authoring-workflow` |
| Player/Enemy health, damage number | visual consistency only if needed | runtime feedback owner if visual consistency work opens |

## Evidence Links

- Historical flat Unity evidence is archived in `artifacts/unity/archive/flat-legacy-20260505.zip`; use the zip entry list only when a historical artifact is explicitly requested.
- Original evidence paths included `account-sync-uitk-preview-gameview.png`, `connection-reconnect-uitk-preview-gameview.png`, `account-sync-runtime-lobby-shell.png`, `connection-reconnect-runtime-lobby-shell.png`, `account-connection-uitk-candidate-report.md`, `account-connection-uitk-workflow-policy.json`, and `account-connection-runtime-replacement-policy.json`.

owner impact:

- primary: `plans.non-stitch-ui-stitch-reimport`
- secondary: `plans.progress`, `design.ui-foundations`
- out-of-scope: Stitch/UITK policy 규칙 개정, translator capability expansion, code/API 변경

doc lifecycle checked:

- reference 압축 보존. 이 문서는 현재 실행 owner가 아니며, 남은 residual은 WebGL smoke checklist 또는 새 UI/runtime owner로 다시 열린다.
- plan rereview: clean - reference compression, source/candidate handoff closeout, and residual routing checked
