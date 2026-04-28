# GameScene Phase 5 Multiplayer Sync Plan

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: plans.game-scene-phase5-multiplayer-sync
> role: plan
> owner_scope: GameScene/BattleScene Phase 5 멀티플레이 동기화 smoke와 blocker closeout 실행 계획
> upstream: plans.progress, playtest.runtime-validation-checklist
> artifacts: `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Unit/`, `Assets/Scripts/Features/Wave/`, `Assets/Scenes/BattleScene.unity`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 `GameScene` Phase 5의 실제 멀티플레이 검증을 이어받는 실행 계획이다.
`progress.md`의 Phase 5 완료 표기는 code path 기준이며, 이 문서의 목표는 2-client smoke로 late-join, BattleEntity sync, Energy sync, wave state sync를 pass 또는 구체적 blocker로 닫는 것이다.

현재 코드와 문서에서는 `GameScene`이라는 lane 이름을 쓰지만 실제 전투 씬 에셋은 `BattleScene.unity`다. 이 계획에서 `GameScene`은 `BattleScene` 전투 런타임 범위를 뜻한다.

---

## Scope

Primary owner:

- GameScene/BattleScene runtime sync lane
- `GameSceneRoot`, player/energy/unit/wave runtime wiring
- Photon 기반 BattleEntity HP, position, dead state 동기화
- late-join 복구와 master switch 중복 실행 검증

Secondary owner:

- GameScene UI/UX lane은 검증 중 UI 조작 blocker가 발견될 때만 handoff를 받는다.
- scene/prefab serialized 변경이 필요하면 한 writer가 preflight evidence를 남긴 뒤 순차 처리한다.

Out of scope:

- HUD layout, placement UX visual polish, slot/card styling
- 밸런스 수치, wave table 난이도 재설계
- Account/Garage WebGL save/load와 Google linking 검증
- Phase 3 runtime initialization을 새로 설계하는 작업

---

## Current State

- Phase 0~9의 완료 표기는 주로 code path 기준이다.
- `progress.md` 기준 현재 남은 GameScene 리스크는 direct EditMode 실행 확인, mobile drag/input framing, multiplayer sync smoke다.
- 기존 상위 계획은 Phase 5를 "Multiplayer Sync Smoke"로 잡아두었고, 이 문서는 그 Phase 5만 실행 가능한 단위로 분리한다.
- 2026-04-27 single-client Lobby actual UI path -> BattleScene -> placement center confirm -> natural victory flow는 `artifacts/unity/game-flow/game-scene-natural-victory-flow-closeout.json`에서 `success: true`, `newErrorCount: 0`으로 통과했다. 이 결과는 Phase 5의 multiplayer acceptance가 아니라 2-client 전 baseline이다.
- 2-client host/join runner 또는 별도 client orchestration helper는 현재 repo-local toolset에서 확인되지 않았다. actual multiplayer acceptance는 `blocked: two-client runner unavailable`로 남기고, single-client pass를 Phase 5 success로 확장하지 않는다.
- 2026-04-27 Phase 5 preflight는 `artifacts/unity/game-flow/game-scene-phase5-preflight.json`에 남겼다. MCP는 `LobbyScene`, Play Mode off, compile idle이고, WebGL build는 존재하지만 repo-local 2-client runner candidate가 없어 `terminalVerdict: blocked`, `blockedReason: two-client runner unavailable`이다.

---

## Execution Plan

### Phase 5.1 - Preflight And Target Lock

- Unity가 Play Mode가 아닌지, compile 상태가 안정적인지 확인한다.
- Lobby load target, build settings, direct run target이 같은 `BattleScene`을 가리키는지 확인한다.
- 현재 dirty worktree를 확인하고 presentation 파일이나 unrelated artifact를 덮어쓰지 않는다.
- scene/prefab mutation이 필요하면 Unity MCP preflight로 active scene, object path, serialized reference를 먼저 확인한다.

Acceptance:

- 검증 대상 scene과 client role이 명확하다.
- 코드 문제, scene contract 문제, UI/input 문제를 나눠 기록할 준비가 되어 있다.

### Phase 5.2 - Single Client Baseline

- direct `BattleScene` 실행에서 local player, camera, combat target, Energy, Unit/Garage spec, Wave setup이 한 번씩 연결되는지 본다.
- Lobby -> BattleScene 진입 경로에서 Room properties의 roster/difficulty가 유지되는지 확인한다.
- summon 1회가 BattleEntity 생성, combat target 등록, UnitPositionQuery 등록으로 이어지는지 확인한다.
- wave start -> enemy spawn -> combat -> victory/defeat loop가 single client에서 먼저 깨지지 않는지 확인한다.

Acceptance:

- single client baseline이 통과하거나, multiplayer smoke 전에 막는 blocker가 분리된다.
- summon 1회에 `UnitSummonCompletedEvent` 계열 반응이 중복 실행되지 않는다.

### Phase 5.3 - Two Client Session Setup

- master client와 non-master client를 같은 room, 같은 roster, 같은 difficulty 조건으로 띄운다.
- 각 client의 role, player id, room property, initial scene load 순서를 기록한다.
- stale console error와 이번 실행 error를 timestamp 기준으로 분리한다.

Acceptance:

- 두 client가 같은 session을 공유한다.
- master/non-master 초기화 순서가 로그나 evidence로 구분된다.

### Phase 5.4 - Late Join Hydration

- host가 먼저 player, summon, wave state를 만든 뒤 두 번째 client가 join한다.
- late join client에서 기존 BattleEntity가 보이고, HP, position, dead state가 host 상태와 맞는지 확인한다.
- Energy current/regeneration 상태가 late join 이후 잘못 초기화되거나 다른 player id로 연결되지 않는지 확인한다.
- wave index, active state, countdown 또는 spawn 상태가 late join에서 같은 기준으로 복구되는지 확인한다.

Acceptance:

- late join BattleEntity sync가 pass 또는 구체적 blocker로 남는다.
- late join Energy sync가 pass 또는 구체적 blocker로 남는다.
- late join wave state sync가 pass 또는 구체적 blocker로 남는다.

### Phase 5.5 - Runtime Ownership And Master Switch

- owner client만 authoritative damage/state publish를 수행하는지 확인한다.
- remote client가 같은 damage/death event를 중복 publish하거나 duplicate despawn을 만들지 않는지 본다.
- master client switch가 발생해도 wave restart, duplicate enemy spawn, duplicate reward/result가 생기지 않는지 확인한다.

Acceptance:

- BattleEntity HP/position/dead state가 양쪽 client에서 수렴한다.
- master switch 이후에도 wave owner가 하나로 유지되거나, 현재 미지원이면 blocker로 기록된다.

### Phase 5.6 - Fix Or Handoff

- runtime event/state 문제는 runtime owner 파일 안에서 최소 수정한다.
- scene contract 문제는 실제 serialized owner를 확인한 뒤 한 writer만 수정한다.
- HUD/input 조작 문제는 GameScene UI/UX lane으로 handoff하고 Phase 5 success로 포장하지 않는다.
- multiplayer 자체를 실행할 수 없는 환경이면 `blocked: manual multiplayer validation required`로 남긴다.

Acceptance:

- pass, blocked, mismatch가 섞이지 않는다.
- 다음 사람이 이어받을 수 있는 evidence path, 재현 절차, 관련 로그가 남는다.

---

## Validation

기본 검증:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- `npm run --silent rules:lint`
- `npm run --silent unity:asset-hygiene` if new Unity assets or `.meta` files are added

Runtime smoke:

- direct `BattleScene` single client smoke
- Lobby -> BattleScene transition smoke. 2026-04-27 `artifacts/unity/game-flow/game-scene-natural-victory-flow-closeout.json` pass.
- Phase 5 preflight. 2026-04-27 `artifacts/unity/game-flow/game-scene-phase5-preflight.json` blocked with `two-client runner unavailable`.
- 2-client host/join smoke
- late-join hydration smoke
- master switch sanity check

Evidence expectation:

- console error는 latest run 기준으로 분리한다.
- mechanical pass와 actual multiplayer acceptance를 따로 보고한다.
- smoke를 실행하지 못하면 success가 아니라 blocked로 닫는다.

---

## Closeout Conditions

Phase 5 closeout은 아래 중 하나로만 닫는다.

- `success`: 2-client smoke에서 late-join, BattleEntity sync, Energy sync, wave state sync가 기준과 맞는다.
- `blocked`: 실행 환경, scene contract, Photon session setup, 또는 manual validation 부재 때문에 핵심 acceptance를 판정할 수 없다.
- `mismatch`: smoke는 실행했고 결과가 기준과 다르다. 이 경우 재현 절차와 owner lane을 남긴다.

최소 완료 기준:

- host와 joiner 모두 player registry와 wave state를 본다.
- 소환된 BattleEntity가 양쪽 client에 존재하고 HP/dead state가 수렴한다.
- late joiner가 기존 BattleEntity, Energy, wave state를 복구하거나 구체적 blocker를 남긴다.
- 한 번의 summon 또는 damage가 중복 event/reward/despawn으로 이어지지 않는다.
- compile-clean과 rules lint가 통과한다.

---

## Residual Handling

- Placement drag/drop 자동화는 Phase 5 smoke를 막는 summon contract 문제일 때만 이 문서에서 다룬다.
- direct `BattleScene` 실행에서 SoundPlayer가 없어서 나는 오디오 결함은 core runtime blocker로 보지 않는다.
- presentation 파일이 dirty이면 Phase 5 runtime 수정 중 덮어쓰지 않는다.
- multiplayer runner나 Photon 환경이 로컬에서 준비되지 않았으면 수동 검증 필요 blocker로 남기고, code path 완료와 single-client smoke 완료, actual 2-client smoke 완료를 분리한다.

---

## Handoff Notes

- Runtime -> UI/UX: runtime state/event가 정상인데 HUD 표시나 drag/drop 조작만 깨지면 GameScene UI/UX lane으로 넘긴다.
- UI/UX -> Runtime: UI 입력이 정상인데 summon, Energy spend, BattleEntity spawn이 실패하면 runtime blocker로 되돌린다.
- Progress update는 실제 Phase 5 acceptance 판정이 바뀐 경우에만 한다.

- 2026-04-27 rereview: 과한점은 single-client natural victory pass를 Phase 5 success로 확장하지 않고 baseline evidence로만 기록했다. 부족한점은 repo-local 2-client runner 부재를 blocked reason으로 명시해 actual multiplayer acceptance와 code path 완료를 분리했다.
- 2026-04-27 preflight 반영 재리뷰: 과한점은 WebGL build 존재와 single-client pass를 actual multiplayer success로 승격하지 않았다. 부족한점은 repo-local runner 후보 0개, MCP/editor/WebGL 상태, blocked artifact 경로를 Current State와 Validation에 남겨 해소했다.
- plan rereview: clean for document shape / residual for execution - 2-client runner or manual multiplayer session is still required.
