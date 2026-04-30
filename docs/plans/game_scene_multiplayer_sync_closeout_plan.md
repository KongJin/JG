# GameScene Multiplayer Sync Closeout Plan

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.game-scene-multiplayer-sync-closeout
> role: plan
> owner_scope: GameScene Phase 5/9 2-client multiplayer sync acceptance, late-join hydration, BattleEntity/Energy/WaveState smoke
> upstream: playtest.runtime-validation-checklist, ops.acceptance-reporting-guardrails
> artifacts: `artifacts/unity/game-flow/multiplayer/`

이 문서는 GameScene multiplayer sync acceptance만 소유한다.
Single-client actual flow, placement input, result HUD, direct EditMode execution은 별도 actual-flow active owner가 소유하며, 현재 경로는 `plans.progress`와 `docs.index`에서 확인한다.

## Current Judgment

- Phase 5/9 code path는 있으나 2-client acceptance는 아직 닫히지 않았다.
- 현재 repo-local 2-client runner가 없어 `blocked: two-client runner unavailable` 상태다.
- Single-client GameScene pass, direct EditMode tests, UI smoke를 multiplayer success로 확장하지 않는다.

## Acceptance

| Item | Required evidence | Closeout |
|---|---|---|
| Late-join hydration | Joiner가 기존 BattleEntity HP/dead/position state를 entity id 기준으로 복구 | success / blocked / mismatch |
| BattleEntity sync | Host/joiner에서 HP, dead state, position이 같은 전투 상태로 수렴 | success / blocked / mismatch |
| Energy sync | Host/joiner에서 Energy/Mana state가 같은 기준으로 관측 | success / blocked / mismatch |
| Wave state sync | Host/joiner에서 current wave, spawned enemy, victory/defeat state가 같은 기준으로 관측 | success / blocked / mismatch |

## Execution Rule

- 수동 2-client session 또는 repo-local runner 중 하나를 먼저 확보한다.
- UI/input blocker와 runtime sync mismatch를 분리해 기록한다.
- 실행 환경이 없으면 `blocked`, 실행했지만 상태가 다르면 `mismatch`, 비교가 끝나고 기준과 맞으면 `success`로 남긴다.

## Validation

- Manual 2-client session 또는 runner evidence.
- Runtime validation checklist의 Phase 5 항목은 절차 reference로만 사용한다.
- 필요한 경우 `artifacts/unity/game-flow/multiplayer/` 아래에 session log, screenshot, runner result를 남긴다.

## Residual

- 2-client runner 또는 수동 session 확보가 남아 있다.
- Late-join, BattleEntity, Energy, Wave state sync 비교가 남아 있다.

owner impact:

- primary: `plans.game-scene-multiplayer-sync-closeout`
- secondary: `plans.progress`, `playtest.runtime-validation-checklist`
- out-of-scope: single-client actual flow, placement input, result HUD, direct EditMode execution, Photon/tooling capability expansion

doc lifecycle checked:

- active 유지. 2-client acceptance가 success/blocked/mismatch로 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean
