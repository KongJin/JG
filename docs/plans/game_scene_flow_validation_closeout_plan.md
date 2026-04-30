# GameScene Flow Validation Closeout Plan

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.game-scene-flow-validation-closeout
> role: plan
> owner_scope: GameScene/BattleScene single-client actual flow, placement input, result HUD, direct EditMode blocker/mismatch closeout
> upstream: playtest.runtime-validation-checklist
> artifacts: `artifacts/unity/game-flow/actual-flow/`

이 문서는 GameScene/BattleScene actual flow의 직접 실행 기준만 소유한다. 세부 구현 규칙은 관련 feature code와 runtime validation checklist를 따른다.
Phase 5/9 2-client multiplayer sync acceptance는 별도 multiplayer active owner가 소유하며, 현재 경로는 `plans.progress`와 `docs.index`에서 확인한다.

## Current State

- Single-client actual UI path baseline은 pass 상태다: Lobby room create -> ready/start -> BattleScene, placement center confirm, natural victory/defeat/result overlay까지 `newErrorCount: 0` evidence가 있다.
- Summon rollback, enemy priority, drag/drop direct test asset은 추가됐고 compile-clean은 통과했지만, 2026-04-29 targeted EditMode 실행은 `open-editor-owns-project`로 blocked다.
- Victory overlay mismatch는 `WaveEndView` visible overlay/CTA toggle 수정으로 닫았다.
- Phase 5 multiplayer acceptance는 별도 multiplayer active owner로 분리했다. 이 문서는 single-client flow와 direct execution blocker만 닫는다.

## Findings

| ID | 상태 | Owner | Closeout |
|---|---|---|---|
| F1 Lobby room start UI | single-client pass | Lobby UX | 2-client room flow는 multiplayer owner에서 확인 |
| F2 GameEnd stats/result HUD | single-client pass / checklist residual | Wave/GameEnd runtime | actual player-flow checklist 갱신 필요 |
| F3 Summon rollback | test asset added / execution blocked | Summon/Energy runtime | EditMode execution이 `open-editor-owns-project` 해소 후 필요 |
| F4 Enemy priority | test asset added / execution blocked | Battle runtime | core/unit/player fallback direct test execution 필요 |
| F5 Placement drag/drop | center-confirm pass / direct execution blocked | GameScene UI/UX | drag/drop direct test execution 필요 |

## Validation

- Compile/static baseline: Unity compile check 또는 targeted EditMode preflight.
- Actual flow smoke: Lobby actual UI path 기반 placement, victory, defeat, result overlay.
- Direct tests: `SummonUnitUseCaseDirectTests`, `UnitSlotInputHandlerDirectTests`, `GameSceneRuntimeSystemsDirectTests`.

## Closeout

- `success`: direct tests 실행 evidence와 actual flow smoke가 있고 single-client flow 기준과 맞는다.
- `blocked`: Unity Editor ownership, direct test 실행 환경 부재처럼 actual flow acceptance를 판정할 수 없는 이유가 남는다.
- `mismatch`: smoke는 실행됐지만 placement, result HUD, summon/enemy priority 결과가 기준과 다르다.

owner impact:

- primary: `plans.game-scene-flow-validation-closeout`
- secondary: `plans.progress`
- split scope: Phase 5/9 2-client multiplayer sync moved out to the multiplayer active owner registered in `docs.index`

doc lifecycle checked:

- 이 문서는 active 유지. Direct execution과 multiplayer residual이 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean
