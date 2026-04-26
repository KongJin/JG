# GameScene Flow Validation Closeout Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-flow-validation-closeout
> role: plan
> owner_scope: GameScene/BattleScene 실제 플레이 플로우 검증, blocker/mismatch closeout, runtime-smoke-clean 판정
> upstream: plans.progress, plans.game-scene-entry, plans.game-scene-agent-a-runtime-core, plans.game-scene-agent-b-hud-input-validation, plans.game-scene-phase5-multiplayer-sync, playtest.runtime-validation-checklist
> artifacts: `Assets/Scripts/Features/Lobby/`, `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Unit/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Enemy/`, `Assets/Scenes/BattleScene.unity`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 GameScene/BattleScene을 "코드가 있다"가 아니라 "플레이어가 시작해서 결과까지 도달할 수 있다"는 기준으로 닫기 위한 closeout 계획이다.
Phase별 구현 세부는 기존 Agent A/B/Phase5 문서가 소유하고, 이 문서는 actual flow acceptance와 blocker 분리만 소유한다.

---

## Current State

Pass:

- `compile-clean`과 `rules:lint`는 통과한다.
- 2026-04-26 actual UI path smoke에서 `LobbyScene -> CreateRoomButton -> RoomDetailPanel -> ReadyButton -> StartGameButton -> BattleScene` 진입이 console error 0으로 재현됐다.
- 같은 smoke에서 enemy contact damage -> core damage -> defeat -> `GameEndReportRequestedEvent`/analytics 로그까지 이어졌다.
- actual placement/summon stat smoke에서 `UnitSlot-0 -> ConfirmPlacementAtPlacementCenter -> ForceCoreDefeatForMcpSmoke` 순서로 `Summons: 1`, `Unit Kills: 1`, result contribution cards, Firebase stub count가 일치했다.
- diagnostic victory result smoke에서 `UnitSlot-0 -> ConfirmPlacementAtPlacementCenter -> ForceVictoryForMcpSmoke` 순서로 `Result: Victory`, `Summons: 1`, `Unit Kills: 1`, `Core HP: 1470/1500`, `거점 보존/압박 정리/기체 전개` 카드가 기록됐다.
- `WaveGameEndBridge`는 match-relative elapsed time과 reached wave를 전달한다.

Residual:

- 자연 final-wave clear로 victory event가 발생하는 loop evidence가 없다. 현재 pass는 diagnostic victory event smoke다.
- victory smoke 세션의 console error buffer에는 `GaragePageController.SyncChrome`의 Garage nav 경로 `NullReferenceException` 2건이 남아 있어, victory result report pass와 console error 0 closeout을 분리한다.
- 2-client sync와 late-join hydration은 Phase 5 residual이다.
- mobile HUD framing과 placement automation은 Agent B residual이다.

Out of scope:

- 밸런스 수치와 wave table 재설계
- GameScene HUD visual polish 자체
- Account/Garage WebGL save/load, Google login, Firebase 운영 검증
- Stitch source-to-Unity fidelity pass

---

## Findings

| ID | 상태 | Owner | Closeout |
|---|---|---|---|
| F1 Lobby room start UI | single-client pass | Lobby UX | 2-client room flow와 mobile framing만 residual |
| F2 GameEnd stats | single-client pass | Wave/GameEnd runtime | defeat, actual summon/kill, diagnostic victory result report 값 일치 |
| F3 BattleEntity late-join hydration | blocked/residual | Phase 5 + Agent A | entity id별 HP/position/dead state 수렴 증거 |
| F4 Enemy priority gameplay pressure | partial pass | Agent A | core unavailable, unit present, player fallback 조건 분리 |
| F5 Summon failure cost rollback | unverified risk | Agent A | spawn 실패가 impossible이거나 Energy rollback 보장 |
| F6 Victory loop | diagnostic result pass / natural loop residual | Agent A + Agent B result HUD | final wave clear -> victory overlay -> result report |

---

## Execution Order

1. Mechanical baseline
   - `tools/check-compile-errors.ps1` errors 0.
   - `npm run --silent rules:lint` pass.
   - smoke 전 active scene, Play Mode, compile 상태를 기록한다.

2. Actual UI single-client flow
   - Lobby actual UI path로 room create -> ready -> start를 재확인한다.
   - custom method로 room start를 직접 호출하지 않는다.

3. Combat result smoke
   - actual placement/input 경로로 unit summon을 수행한다.
   - defeat path와 victory path를 분리해 result HUD/analytics 값을 확인한다.
   - summon count와 kill count는 실제 이벤트가 있었을 때 0이면 mismatch다.

4. Runtime contract tests
   - enemy target priority를 core alive, core unavailable, unit present, player fallback으로 나눈다.
   - summon failure path에서 Energy silent-consume 여부를 고정한다.

5. Multiplayer closeout
   - host/client session에서 BattleEntity, Energy, WaveState를 비교한다.
   - late-join은 entity id 기준 hydration 증거가 없으면 blocked로 남긴다.

6. Runtime checklist update
   - `docs/playtest/runtime_validation_checklist.md`에 actual player flow와 custom diagnostic smoke를 분리한다.

---

## Validation Commands

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- `npm run --silent rules:lint`
- `git diff --check`
- Unity MCP `/health`, `/console/errors`, `/scene/hierarchy`, `/ui/get-state`, `/ui/invoke`
- Phase 5 two-client smoke helper, if available

---

## Closeout Criteria

이 계획은 아래가 모두 충족되기 전에는 `reference`로 내리지 않는다.

- Actual UI path로 Lobby room create -> ready -> start -> BattleScene 진입이 된다. 2026-04-26 single-client pass.
- Single-client defeat path와 diagnostic victory result path가 재현된다. defeat pass, diagnostic victory result pass, 자연 final-wave victory loop residual.
- Result HUD와 analytics report의 play time, reached wave, summon count, kill count가 실제 이벤트와 일치한다. actual summon/kill smoke pass.
- console error 0 closeout은 smoke 직전/직후 buffer를 분리해 재확인한다. 현재 console buffer의 Garage nav `NullReferenceException`은 Account/Garage lane residual이다.
- BattleEntity/Energy/Wave 2-client sync가 pass하거나 blocker owner가 명확하다.
- late-join hydration이 pass하거나 blocker owner가 명확하다.
- runtime validation checklist가 custom invoke diagnostic과 actual player flow를 분리한다.

---

## Blocked / Mismatch Handling

- UI가 보이지 않거나 버튼이 눌리지 않으면 Agent B 또는 Lobby UX blocker로 남긴다.
- UI는 정상인데 summon, Energy, BattleEntity, wave/core event가 실패하면 Agent A runtime blocker로 남긴다.
- 2-client 환경이 준비되지 않으면 `blocked: multiplayer environment unavailable`로 남기고 single-client pass를 success로 확장하지 않는다.
- 통계 값이 실제 이벤트와 다르면 `mismatch`로 남기고 overlay 표시 성공과 분리한다.
- custom invoke로만 통과한 경로는 actual flow success로 보고하지 않는다.

---

## Lifecycle

- active 전환 이유: GameScene cold review에서 compile/single-client smoke와 actual flow acceptance 사이의 차이가 확인됐다.
- reference 전환 조건: closeout criteria가 pass 또는 명확한 owner의 blocked/residual로 이관되어 이 문서가 직접 실행 기준이 아니게 된다.
- 전환 시 갱신: `docs.index`, `plans.progress`, 관련 Agent A/B/Phase5 plan의 residual 상태를 함께 맞춘다.

---

## 문서 재리뷰

- 과한점 리뷰: 실행 로그 상세와 owner별 구현 절차를 제거하고, residual closeout 기준만 유지했다.
- 부족한점 리뷰: current state, findings, execution order, validation, closeout, blocked/mismatch handling은 남겼다.
- doc lifecycle checked: cross-owner actual-flow closeout plan으로 active 유지. diagnostic victory result와 actual summon/kill stat은 pass로 반영했고, 자연 final-wave victory loop와 2-client/mobile residual은 이 문서가 계속 추적한다.
- plan rereview: residual - diagnostic victory result와 actual summon/kill stat은 닫혔지만, 자연 final-wave victory loop, console error 0 재확인, 2-client sync, mobile HUD framing은 남아 있다.
