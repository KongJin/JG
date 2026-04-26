# GameScene Flow Validation Closeout Plan

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: plans.game-scene-flow-validation-closeout
> role: plan
> owner_scope: GameScene/BattleScene 실제 플레이 플로우 검증, blocker/mismatch closeout, runtime-smoke-clean 판정
> upstream: plans.progress, plans.game-scene-entry, plans.game-scene-phase5-multiplayer-sync, plans.game-scene-ui-ux-improvement, playtest.runtime-validation-checklist
> artifacts: `Assets/Scripts/Features/Lobby/`, `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Unit/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Enemy/`, `Assets/Scenes/BattleScene.unity`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 GameScene/BattleScene을 "코드가 있다"가 아니라 "플레이어가 시작해서 결과까지 도달할 수 있다"는 기준으로 닫기 위한 closeout 계획이다.
Phase별 구현 세부는 기능 owner와 runtime/presentation 코드가 소유하고, 이 문서는 actual flow acceptance와 blocker 분리만 소유한다.

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
- mobile HUD framing과 placement automation은 GameScene UI/UX residual이다.

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
| F3 BattleEntity late-join hydration | blocked/residual | Phase 5 sync | entity id별 HP/position/dead state 수렴 증거 |
| F4 Enemy priority gameplay pressure | partial pass | Battle runtime | core unavailable, unit present, player fallback 조건 분리 |
| F5 Summon failure cost rollback | unverified risk | Summon/Energy runtime | spawn 실패가 impossible이거나 Energy rollback 보장 |
| F6 Victory loop | diagnostic result pass / natural loop residual | Flow closeout + result HUD | final wave clear -> victory overlay -> result report |

---

## Priority Board

BattleScene 미완성 시스템은 아래 순서로 닫는다.
기준은 "다음 검증을 믿을 수 있게 만드는가"이며, visual polish와 모델 교체는 actual runtime acceptance 뒤로 둔다.

| Priority | System | 왜 먼저/나중인가 | Done when |
|---|---|---|---|
| P0 | console error 0 재확인 | 최신 smoke의 에러 버퍼가 섞이면 이후 pass/fail 판정이 흐려진다. | smoke 직전 console clear 또는 timestamp 분리 후 actual UI path, summon, result smoke의 error 0이 확인된다. |
| P1 | 자연 final-wave victory loop | 현재 victory는 diagnostic event smoke다. 실제 wave clear로 승리가 나와야 BattleScene 기본 루프가 닫힌다. | final wave clear -> victory event -> result overlay -> report values가 custom force 없이 이어진다. |
| P1 | summon failure cost rollback | 배치/소환 실패 시 Energy가 조용히 빠지면 모든 전투 smoke가 신뢰를 잃는다. | spawn 실패가 scene contract상 불가능하거나, 실패 시 Energy rollback이 direct/runtime test로 고정된다. |
| P2 | enemy target priority pressure | defeat path는 통과했지만 unit/core/player fallback 조건이 분리 검증되어야 전투 압박이 의도대로 읽힌다. | core alive, core unavailable, unit present, player fallback 각각에서 target 선택이 기대와 맞는다. |
| P2 | placement automation contract | 실제 플레이는 가능하지만 automation이 불안정하면 회귀 확인이 느려진다. | slot select -> placement center tap/drag -> summon smoke가 stable helper로 반복 가능하다. |
| P3 | 2-client BattleEntity/Energy/Wave sync | single-client loop가 먼저 닫혀야 멀티 mismatch가 runtime 문제인지 flow 문제인지 분리된다. | host/joiner에서 entity HP/dead/position, Energy, wave state가 수렴하거나 구체적 blocker가 남는다. |
| P3 | late-join hydration / master switch | Phase 5 acceptance의 핵심이지만, 기본 2-client smoke 뒤에 봐야 원인 분리가 쉽다. | late joiner가 existing entity/Energy/wave state를 복구하고, master switch가 duplicate spawn/result를 만들지 않는다. |
| P4 | mobile HUD/input framing | 조작 UX는 중요하지만 runtime loop 판정 이후에 고치는 편이 덜 흔들린다. | 모바일 세로에서 HUD overlap 없이 slot, placement, result CTA가 실제 조작 경로로 보인다. |
| P5 | BattleScene Nova1492 combat model assembly | Garage preview alignment와 별개다. gameplay smoke가 안정된 뒤 visual/model replacement로 다룬다. | 전투 소환 유닛이 3-part model assembly를 쓰고 기존 combat collider/targeting/result smoke가 유지된다. |

---

## Difficult Work Strategy

이 작업은 한 번에 "BattleScene 완성"으로 열면 원인 분리가 어렵다.
실행 단위는 작게 자르고, 각 단위는 pass / blocked / mismatch 중 하나로만 닫는다.

### First Cut - actual victory loop

목표:

- 최신 실행 기준 console error 0을 확보한다.
- custom force 없이 final wave clear -> victory -> result overlay -> report values를 재현한다.

멈춤 조건:

- smoke 직후 console error가 있으면 victory 구현으로 넘어가지 않고 P0 blocker로 먼저 분리한다.
- final wave가 끝났는데 victory event가 오지 않으면 wave/end bridge owner 문제로 좁힌다.
- victory overlay는 뜨는데 report 값이 틀리면 result/report mismatch로 분리한다.

완료 기준:

- single-client actual UI path에서 defeat와 natural victory가 모두 재현된다.
- diagnostic victory smoke와 natural victory smoke가 문서와 evidence에서 구분된다.

### Second Cut - runtime contract hardening

목표:

- summon 실패 시 Energy silent-consume이 없음을 고정한다.
- enemy target priority를 core, unit, player fallback 조건으로 분리한다.

멈춤 조건:

- scene contract상 spawn failure가 실제로 가능하면 rollback 구현을 먼저 한다.
- enemy target priority가 테스트 없이 smoke 감각으로만 판단되면 P2를 닫지 않는다.

완료 기준:

- summon failure path가 direct/runtime test 또는 impossible contract로 고정된다.
- enemy pressure가 defeat smoke 한 종류가 아니라 조건별 검증으로 설명된다.

### Third Cut - repeatable automation

목표:

- slot select -> placement center -> summon -> result smoke를 반복 가능한 helper로 만든다.
- drag/drop이 흔들리면 tap placement를 기본 automation path로 먼저 닫는다.

멈춤 조건:

- runtime state는 정상인데 UI 조작만 실패하면 GameScene UI/UX lane으로 handoff한다.
- helper가 custom diagnostic invoke에 의존하면 actual player-flow success로 보고하지 않는다.

완료 기준:

- placement/summon smoke가 stale UI path나 임시 object name에 덜 의존한다.
- regression 확인이 수동 조작 없이 가능하다.

### Fourth Cut - multiplayer sync

목표:

- single-client 루프가 안정된 뒤 host/client에서 BattleEntity, Energy, Wave state를 비교한다.
- late join hydration과 master switch는 기본 2-client smoke 뒤에 본다.

멈춤 조건:

- 2-client 환경이 준비되지 않으면 success가 아니라 blocked로 남긴다.
- UI/input blocker와 runtime sync mismatch를 섞어 보고하지 않는다.

완료 기준:

- host/joiner state가 수렴하거나, mismatch 재현 절차와 owner가 남는다.
- late joiner가 existing entity/Energy/wave state를 복구하거나 구체적 blocker가 남는다.

### Fifth Cut - UX and model follow-up

목표:

- 모바일 HUD/input framing은 runtime loop와 automation이 안정된 뒤 다룬다.
- Nova1492 combat model assembly는 collider/targeting/result smoke를 유지하는 조건에서만 적용한다.

멈춤 조건:

- visual/model 작업이 runtime acceptance를 흐리면 P5를 보류한다.
- Garage preview alignment 성공을 BattleScene model assembly 성공으로 확장하지 않는다.

완료 기준:

- mobile HUD가 실제 조작 경로에서 읽힌다.
- 전투 소환 유닛이 3-part model assembly를 쓰면서 기존 combat smoke가 유지된다.

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

- UI가 보이지 않거나 버튼이 눌리지 않으면 GameScene UI/UX 또는 Lobby UX blocker로 남긴다.
- UI는 정상인데 summon, Energy, BattleEntity, wave/core event가 실패하면 runtime blocker로 남긴다.
- 2-client 환경이 준비되지 않으면 `blocked: multiplayer environment unavailable`로 남기고 single-client pass를 success로 확장하지 않는다.
- 통계 값이 실제 이벤트와 다르면 `mismatch`로 남기고 overlay 표시 성공과 분리한다.
- custom invoke로만 통과한 경로는 actual flow success로 보고하지 않는다.

---

## Lifecycle

- active 전환 이유: GameScene cold review에서 compile/single-client smoke와 actual flow acceptance 사이의 차이가 확인됐다.
- reference 전환 조건: closeout criteria가 pass 또는 명확한 owner의 blocked/residual로 이관되어 이 문서가 직접 실행 기준이 아니게 된다.
- 전환 시 갱신: `docs.index`, `plans.progress`, Phase 5 sync와 GameScene UI/UX residual 상태를 함께 맞춘다.

---

## 문서 재리뷰

- 과한점 리뷰: 실행 로그 상세와 owner별 구현 절차를 제거하고, residual closeout 기준만 유지했다.
- 부족한점 리뷰: current state, findings, execution order, validation, closeout, blocked/mismatch handling은 남겼다.
- doc lifecycle checked: cross-owner actual-flow closeout plan으로 active 유지. diagnostic victory result와 actual summon/kill stat은 pass로 반영했고, 자연 final-wave victory loop와 2-client/mobile residual은 이 문서가 계속 추적한다.
- 2026-04-26 priority board 재리뷰: 과한점은 새 owner plan을 만들지 않고 이 active closeout 문서 안의 실행 순서로 제한해 줄였다. 부족한점은 runtime loop, automation, multiplayer, HUD, BattleScene model assembly residual의 상대 순서를 추가해 해소했다.
- 2026-04-27 difficult work strategy 재리뷰: 과한점은 별도 plan 신설 없이 기존 active closeout 문서의 실행 전략 섹션으로 제한했다. 부족한점은 first cut, stop condition, done criteria를 추가해 큰 작업을 작은 acceptance 단위로 나누며 해소했다.
- plan rereview: residual - diagnostic victory result와 actual summon/kill stat은 닫혔지만, P0~P5 우선순위 항목은 아직 실행 전이다.
