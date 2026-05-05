# BattleScene Flow Validation Closeout Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.battle-scene-flow-validation-closeout
> role: plan
> owner_scope: BattleScene/BattleScene single-client actual flow, placement input, result HUD, direct EditMode blocker/mismatch closeout
> upstream: playtest.runtime-validation-checklist
> artifacts: `artifacts/unity/game-flow/actual-flow/`

이 문서는 BattleScene/BattleScene actual flow의 직접 실행 기준만 소유한다. 세부 구현 규칙은 관련 feature code와 runtime validation checklist를 따른다.
Phase 5/9 2-client multiplayer sync acceptance는 별도 active plan 없이 [`runtime_validation_checklist.md`](../../owners/validation/runtime_validation_checklist.md)가 소유한다.

## Current State

- Single-client actual UI path baseline은 pass 상태다: Lobby room create -> ready/start -> BattleScene, placement center confirm, natural victory/defeat/result overlay까지 `newErrorCount: 0` evidence가 있다.
- Summon rollback, enemy priority, drag/drop direct tests는 2026-04-30 in-editor MCP test route로 실행됐고 16/16 pass다: `artifacts/unity/game-flow/actual-flow/direct-editmode-tests.xml`.
- Victory overlay mismatch는 `WaveEndView` visible overlay/CTA toggle 수정으로 닫았다.
- Phase 5 multiplayer acceptance는 runtime validation checklist로 분리했다. 이 문서는 single-client flow와 direct execution blocker만 닫는다.
- 남은 closeout blocker는 result HUD actual player-flow checklist이며, direct/EditMode pass만으로 이 blocker를 success 처리하지 않는다.

## Findings

| ID | 상태 | Owner | Closeout |
|---|---|---|---|
| F1 Lobby room start UI | single-client pass | Lobby UX | 2-client room flow는 runtime validation checklist에서 확인 |
| F2 GameEnd stats/result HUD | single-client pass / checklist residual | Wave/GameEnd runtime | actual player-flow checklist 갱신 필요 |
| F3 Summon rollback | direct EditMode pass | Summon/Energy runtime | `SummonUnitUseCaseDirectTests` pass |
| F4 Enemy priority | direct EditMode pass | Battle runtime | `BattleSceneRuntimeSystemsDirectTests` pass |
| F5 Placement drag/drop | direct EditMode pass | BattleScene UI/UX | `UnitSlotInputHandlerDirectTests` pass |

## Validation

- Compile/static baseline: Unity compile check 또는 targeted EditMode preflight.
- Actual flow smoke: Lobby actual UI path 기반 placement, victory, defeat, result overlay.
- Direct tests: `SummonUnitUseCaseDirectTests`, `UnitSlotInputHandlerDirectTests`, `BattleSceneRuntimeSystemsDirectTests`; latest evidence `artifacts/unity/game-flow/actual-flow/direct-editmode-tests.xml` has 16 total, 16 passed, 0 failed.

## Closeout

- `success`: direct tests 실행 evidence와 actual flow smoke가 있고 single-client flow 기준과 맞는다.
- `blocked`: Unity Editor ownership, direct test 실행 환경 부재처럼 actual flow acceptance를 판정할 수 없는 이유가 남는다.
- `mismatch`: smoke는 실행됐지만 placement, result HUD, summon/enemy priority 결과가 기준과 다르다.

owner impact:

- primary: `plans.battle-scene-flow-validation-closeout`
- secondary: `plans.progress`
- split scope: Phase 5/9 2-client multiplayer sync는 별도 multiplayer active plan 없이 runtime validation checklist로 추적한다

doc lifecycle checked:

- 이 문서는 active 유지. Direct execution은 닫혔고, actual player-flow checklist residual이 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - single-client closeout and multiplayer residual split checked
