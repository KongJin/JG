# GameScene Agent A Result Belonging Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-agent-a-result-belonging
> role: plan
> owner_scope: Agent A가 맡는 GameScene/BattleScene 전투 결과 기여 카드, 소속감 피드백, 이번 판 결과 요약 계약
> upstream: plans.progress, design.game-design, design.world-design, plans.game-scene-flow-validation-closeout, plans.game-scene-agent-a-runtime-core
> artifacts: `Assets/Scripts/Features/Player/Application/`, `Assets/Scripts/Features/Player/Application/Events/`, `Assets/Scripts/Features/Wave/Presentation/WaveEndView.cs`, `Assets/Scripts/Features/Combat/Application/Events/`, `Assets/Scripts/Shared/Gameplay/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 3-agent 분업 중 Agent A가 맡는 **전투 결과 / 소속감** 계획이다.
Agent A의 목표는 한 판이 끝났을 때 "누가 1등인가"보다 **내 조합과 팀 조합이 어떤 압박을 해결했는가**를 보여주는 것이다.

세계관과 톤은 [`world_design.md`](../design/world_design.md)를 따른다.
결과 화면의 감정은 회복감이 아니라 `조합으로 버틴 저항`이다.

---

## Goal

전투 결과 화면에서 아래 문장이 즉시 읽히게 한다.

> 내가 만든 기체 조합이, 다른 플레이어의 조합과 맞물려, 이 거점을 5분 더 버티게 했다.

첫 구현은 `팀 기여 카드 3장`을 목표로 한다.
기여 카드는 누적 기록이 아니라 **이번 판 결과**만 표현한다.

---

## Scope

Agent A가 소유한다:

- `GameEndReportRequestedEvent`에 이번 판 결과 기여 요약을 싣는 계약
- `GameEndAnalytics` 또는 동급 session analyzer에서 전투 이벤트를 집계하는 흐름
- 결과 화면의 랭킹/KD 중심 표시를 기여 카드 중심 표시로 바꾸는 최소 구현
- `WaveEndView`가 기여 카드를 텍스트로 표시하는 MVP 결과 표면
- 결과 카드의 카피: `버텨냈다`, `거점 보존`, `자리 지킴`, `압박 정리`, `기체 전개`

Agent A가 소유하지 않는다:

- 최근 작전 기록 저장, 계정/Firestore/로컬 저장, 누적 히스토리 UI: Agent B
- 기체 콜사인, 기체별 장기 전적 태그, 용어 전체 리네임: Agent C
- 전투 HUD, 배치 프리뷰, 모바일 HUD visual polish: Agent B HUD/input plan
- BattleEntity 이동/타겟팅/웨이브 baseline 자체: 기존 Agent A runtime plan
- PvP 결과, 랭킹, 시즌/업적, 보상 경제

공유 경계:

- Agent A는 **이번 판 결과 데이터**만 만든다.
- Agent B는 Agent A가 만든 결과 데이터 중 저장할 값을 골라 **누적 작전 기록**으로 저장한다.
- Agent C는 결과 카드에 쓰일 유저-facing 용어와 기체 정체성 카피를 후속으로 정리한다.

---

## Current State

2026-04-26 구현 시작 시 확인한 기준:

- `GameEndAnalytics`는 결과 카드 구현 전에는 `UnitSummonCompletedEvent`, `EnemyDiedEvent`, `GameEndEvent`를 구독했다.
- 현재 결과 카드 구현은 feature cycle을 피하기 위해 소환 집계 소스로 `Shared.Gameplay.BattleUnitDeployedEvent`를 사용한다.
- 현재 결과 리포트는 `summonCount`, `unitKillCount`, `reachedWave`, `playTimeSeconds`, `isVictory`만 가진다.
- `WaveEndView`는 결과, 도달 Wave, 플레이 시간, 소환 횟수, 처치 횟수, K/D 비율을 텍스트로 보여준다.
- `DamageAppliedEvent`는 `targetId`, `damage`, `remainingHealth`, `isDead`, `attackerId`를 가진다.
- `EnemyDiedEvent`는 `killerId`를 갖지만, 현재 enemy combat target path에서 `default`로 발행될 수 있어 첫 구현의 신뢰 소스로 쓰지 않는다.

따라서 첫 pass는 `DamageAppliedEvent`와 `BattleUnitDeployedEvent`를 중심으로 attribution을 만들고,
부족한 attacker id가 발견되면 damage math를 바꾸지 않는 범위에서 attacker id 전달만 보강한다.

---

## Result Card Contract

새 계약은 기존 `GameEndReportRequestedEvent`를 확장하는 방식으로 둔다.
별도 저장 이벤트는 만들지 않는다.

추가 타입:

- `ResultContributionCard`
- `ResultContributionKind`

`ResultContributionCard` 필드:

- `Kind`: 카드 종류
- `Title`: 짧은 제목
- `Body`: 결과 화면 본문 한 줄
- `PrimaryValue`: 정렬이나 강조에 쓰는 숫자
- `OwnerId`: 특정 플레이어 귀속이면 player id, 팀 카드면 empty/default
- `UnitId`: 특정 battle entity 또는 unit spec 귀속이면 id, 없으면 empty/default
- `IsTeamCard`: 팀 전체 카드 여부

`ResultContributionKind` MVP 값:

- `HoldPosition`: 아군 기체가 받은 피해/자리 지킴
- `ClearPressure`: 적 처치/피해량으로 압박 정리
- `KeepCoreAlive`: 거점 잔여 체력/거점 보존
- `DeployUnits`: 소환 횟수로 기체 전개

`GameEndReportRequestedEvent` 추가 필드:

- `ResultContributionCard[] ContributionCards`
- `float CoreRemainingHealth`
- `float CoreMaxHealth`

생성 규칙:

- 항상 최대 3장만 표시한다.
- 데이터가 부족하면 팀 카드로 fallback한다.
- 같은 종류 카드가 여러 개라면 `PrimaryValue`가 큰 카드만 남긴다.
- `KeepCoreAlive`는 core hp가 1% 이상 남았을 때만 후보로 만든다. 0% 붕괴 결과에서 `거점 보존`을 주장하지 않는다.
- MVP에서 개인 순위표는 만들지 않는다.

---

## Contribution Aggregation

새 session analyzer는 Application 계층에 둔다.
이름 후보는 `GameEndContributionAnalyzer`로 고정한다.

구독 이벤트:

- `BattleUnitDeployedEvent`
- `EnemySpawnedEvent`
- `DamageAppliedEvent`
- `GameEndEvent`

보유 상태:

- `battleEntityId -> playerId`
- `enemyIds`
- `unitDamageDealtByOwner`
- `unitDamageTakenByOwner`
- `enemyKillsByOwner`
- `summonCountByOwner`
- `coreRemainingHealth`
- `coreMaxHealth`

집계 규칙:

- 소환 완료 시 shared gameplay event로 battle entity와 owner/player를 매핑한다.
- 적 spawn 시 enemy id를 enemy set에 등록한다.
- `DamageAppliedEvent.TargetId`가 enemy set에 있고 `AttackerId`가 known battle entity면 해당 owner의 `damage dealt`를 누적한다.
- 같은 damage event가 `IsDead == true`이면 해당 owner의 enemy kill을 1 올린다.
- `DamageAppliedEvent.TargetId`가 known battle entity면 해당 owner의 `damage taken`을 누적한다.
- `DamageAppliedEvent.TargetId`가 objective core id면 core remaining health를 갱신한다.
- `attackerId`가 비어 있거나 known battle entity가 아니면 개인 귀속 없이 team total에만 반영한다.

core id / max hp 주입:

- analyzer가 scene object를 직접 찾지 않는다.
- `GameSceneRoot` 또는 현재 bootstrap owner가 objective core id와 max hp를 생성자 인자로 넘긴다.
- core id가 없으면 `KeepCoreAlive` 카드는 `core hp unknown` fallback 문구를 쓰고 blocker로 보고한다.

---

## Card Copy MVP

결과 headline:

- 승리: `버텨냈다`
- 패배: `거점 붕괴`

카드 카피 기본형:

- `자리 지킴`: `아군 기체가 피해 {value}을 받아 거점으로 향한 압박을 붙잡았습니다.`
- `압박 정리`: `침공 기체 {value}기를 정리했습니다.`
- `거점 보존`: `거점 내구도 {percent}%로 마지막 공세를 넘겼습니다.` 단, 0%면 숨긴다.
- `기체 전개`: `전장에 기체 {count}기를 투입했습니다.`

표시 원칙:

- `K/D 비율`은 결과 화면 첫 표면에서 제거한다.
- `처치 횟수`는 단독 랭킹처럼 보이지 않게 `압박 정리` 카드 안에서만 쓴다.
- 플레이어 이름 표시가 아직 안정적이지 않으면 `팀` 기준 문구로 보여준다.
- 숫자 0인 카드는 숨긴다. 모든 카드가 0이면 `이번 기록을 수집하지 못했다` 대신 기본 결과 요약만 표시한다.

---

## Execution Plan

### Phase A1. Result Contract Audit

- 현재 `GameEndReportRequestedEvent`, `GameEndAnalytics`, `WaveEndView`, `DamageAppliedEvent`, `BattleUnitDeployedEvent` 사용처를 확인한다.
- `GameEndReportRequestedEvent` 확장으로 깨지는 constructor call을 파악한다.
- `DamageAppliedEvent.AttackerId`가 unit attack path에서 채워지는지 확인한다.
- acceptance: 기여 카드 생성을 위해 필요한 source event와 missing attribution이 분리된다.

### Phase A2. Event Contract Extension

- `ResultContributionKind`와 `ResultContributionCard`를 Player Application Events 근처에 추가한다.
- `GameEndReportRequestedEvent`에 contribution cards와 core hp 필드를 추가한다.
- 기존 constructor call은 optional/fallback 파라미터로 유지해 compile break를 줄인다.
- acceptance: 기존 기본 결과 통계 소비자는 contribution card가 없어도 동작한다.

### Phase A3. Session Contribution Analyzer

- `GameEndContributionAnalyzer`를 추가한다.
- analyzer는 event subscription만으로 이번 판 데이터를 모은다.
- attacker id 누락이 있으면 damage 계산을 바꾸지 않고 attack source id 전달만 보강한다.
- `EnemyDiedEvent.KillerId`는 첫 pass attribution source로 쓰지 않는다.
- acceptance: summon, enemy damage, unit damage taken, core damage가 각각 카드 후보로 집계된다.

### Phase A4. GameEnd Report Composition

- `GameEndAnalytics`가 기존 summon/kill count와 contribution analyzer 결과를 함께 보고한다.
- 카드 정렬은 `KeepCoreAlive` 1장 + `HoldPosition`/`ClearPressure`/`DeployUnits` 중 상위 2장으로 고정한다.
- victory/defeat 모두 같은 card pipeline을 탄다.
- acceptance: victory/defeat report 모두 최대 3장 contribution cards를 가진다.

### Phase A5. Result Surface MVP

- `WaveEndView`의 `statsText`를 결과 카드 중심 텍스트로 바꾼다.
- MVP는 새 prefab 구조를 만들지 않고 기존 text field를 사용한다.
- `Victory!`/`Defeat!` headline은 `버텨냈다`/`거점 붕괴`로 바꾼다.
- acceptance: 결과 화면에서 K/D 비율보다 기여 카드가 먼저 읽힌다.

### Phase A6. Test And Smoke

- EditMode 또는 direct test에서 card generation pure logic을 검증한다.
- single-client defeat smoke에서 contribution cards가 표시되는지 확인한다.
- victory smoke는 flow closeout plan과 함께 판정한다.
- 2-client attribution은 Phase 5 sync가 닫히기 전까지 `residual`로 남길 수 있다.
- acceptance: compile clean, docs lint, single-client result card smoke pass 또는 blocker owner 명확화.

---

## 2026-04-26 Implementation Note

적용됨:

- `ResultContributionCard`, `ResultContributionKind`를 추가했다.
- `GameEndReportRequestedEvent`에 `ContributionCards`, `CoreRemainingHealth`, `CoreMaxHealth`를 추가했다.
- Unit -> Player -> Unit feature cycle을 피하기 위해 `Shared.Gameplay.BattleUnitDeployedEvent`를 추가했고, `SummonUnitUseCase`가 기존 `UnitSummonCompletedEvent`와 함께 발행한다.
- `GameEndContributionAnalyzer`를 추가해 `BattleUnitDeployedEvent`, `EnemySpawnedEvent`, `DamageAppliedEvent`에서 이번 판 카드 후보를 집계한다.
- `GameSceneRoot`가 objective core id/max hp를 `GameEndAnalytics`에 주입한다.
- `WaveEndView`는 `Victory!/Defeat! + K/D` 대신 `버텨냈다/거점 붕괴 + 기여 카드`를 표시한다.
- direct test에 contribution card generation 경로를 추가했다.
- actual battle smoke에서 0% core defeat에 `거점 보존` 카드가 생성되는 문제를 발견했고, core hp 0%에서는 `KeepCoreAlive` 카드를 숨기도록 보정했다.
- repeatable MCP smoke를 위해 `GameSceneRoot.ForceCoreDefeatForMcpSmoke()`를 `UNITY_EDITOR` 전용으로 추가했다. 이 훅은 `CombatSetup.ApplyDamage`를 통해 objective core에 lethal damage를 넣어 기존 GameEnd pipeline을 태운다.
- repeatable victory result smoke를 위해 `GameSceneRoot.ForceVictoryForMcpSmoke()`를 `UNITY_EDITOR` 전용으로 추가했다. 이 훅은 `WaveVictoryEvent`를 발행해 기존 `WaveEndView`와 `WaveGameEndBridge -> GameEndReportRequestedEvent` pipeline을 태운다.

검증 상태:

- `tools/check-compile-errors.ps1`: pass, errors 0 / warnings 0.
- `tools/rule-harness/write-feature-dependency-report.ps1`: pass, layerViolationCount 0, hasCycles false.
- `npm run --silent rules:lint`: pass.
- `Invoke-McpCompileRequestAndWait`: pass, Unity bridge health clean after compile.
- post-compile console summary: error 0 / warning 0.
- `Invoke-UnityEditModeTests.ps1 -TestFilter Tests.Editor.GameSceneRuntimeSystemsDirectTests`는 현재 Unity Editor가 프로젝트를 열고 있어 `open-editor-owns-project` preflight로 blocked다.
- LobbyScene Play Mode 진입/정지: pass. console error 0 기준에서 남은 warning은 Firestore stats/settings missing document, Photon dev region, SoundPlayer `DontDestroyOnLoad` root 경고다.
- Lobby actual UI path `CreateRoomButton -> ReadyButton -> StartGameButton`: pass, `BattleScene` 로드 확인.
- BattleScene actual result smoke: pass. single-client defeat path에서 `WaveEndOverlay/ResultPanel` 활성화, `GameEndReportRequestedEvent` 로그, `Card 1` 로그 확인.
- 0% core defeat 보정 actual smoke: pass. `Core HP: 0/1500` 결과에서 `Card 1: 작전 참여`가 기록되고 `거점 보존` 카드는 생성되지 않았다.
- actual placement/summon stat smoke: pass. Lobby actual UI path로 BattleScene에 진입한 뒤 `UnitSlot-0` 클릭, `ConfirmPlacementAtPlacementCenter`, `ForceCoreDefeatForMcpSmoke` 순서로 실행했다. 결과 로그에서 `Summons: 1`, `Unit Kills: 1`, `Card 1: 압박 정리`, `Card 2: 기체 전개`, Firebase stub `summon_count:1`, `unit_kill_count:1`을 확인했다.
- victory result smoke: pass. Lobby actual UI path로 BattleScene에 진입한 뒤 `UnitSlot-0` 클릭, `ConfirmPlacementAtPlacementCenter`, `ForceVictoryForMcpSmoke` 순서로 실행했다. 결과 로그에서 `Result: Victory`, `Summons: 1`, `Unit Kills: 1`, `Core HP: 1470/1500`, `Card 1: 거점 보존`, `Card 2: 압박 정리`, `Card 3: 기체 전개`, Firebase stub `is_victory: True`, `summon_count:1`, `unit_kill_count:1`을 확인했다.
- victory smoke 세션의 console error buffer에는 `GaragePageController.SyncChrome`의 Garage nav 경로 `NullReferenceException` 2건이 남아 있었다. stack이 Garage page 전환 경로라 Agent A result pipeline blocker로 보지 않고, Account/Garage lane residual로 분리한다.
- direct test 실행만 Unity Editor ownership으로 blocked다.

---

## Test Cases

필수 테스트:

- summon 0, kill 0, core damage only: `KeepCoreAlive`만 표시되거나 기본 결과 fallback.
- one unit deals enemy killing damage: `ClearPressure` 카드 생성.
- one unit receives damage: `HoldPosition` 카드 생성.
- multiple contribution candidates: 최대 3장, 정렬 안정.
- defeat path: `거점 붕괴` headline과 contribution cards 동시 표시.
- victory path: `버텨냈다` headline과 contribution cards 동시 표시.
- missing attacker id: 개인 귀속 없이 team total fallback, exception 없음.

수동 smoke:

- Lobby actual UI path로 BattleScene 진입.
- 한 번 이상 기체 소환.
- actual 자연 defeat는 적이 코어를 때리게 두고, 반복 가능한 MCP smoke는 `ForceCoreDefeatForMcpSmoke`로 defeat path를 만든다.
- 결과 화면에서 카드 문구가 잘리지 않는지 확인.
- console error 0 확인.
- 0% core defeat에서 `거점 보존` 카드가 표시되지 않는지 확인.

---

## Acceptance

Agent A closeout 기준:

- 결과 화면이 `Victory!/Defeat! + K/D` 중심이 아니라 `버텨냈다/거점 붕괴 + 기여 카드` 중심으로 읽힌다.
- `GameEndReportRequestedEvent`가 이번 판 contribution cards를 전달한다.
- card source는 session event aggregation이며, 결과 화면이 직접 전투 오브젝트를 탐색하지 않는다.
- card가 없거나 attribution이 불완전해도 결과 화면이 깨지지 않는다.
- single-client defeat path에서 최소 1개 이상의 카드가 표시된다.
- core hp 0% 패배 결과는 `거점 보존` 카드로 표현하지 않는다.
- victory result path는 pass다. 자연 final-wave victory loop는 `game_scene_flow_validation_closeout_plan.md` owner의 residual로 남는다.
- Agent B가 저장할 수 있는 이번 판 요약 데이터가 `GameEndReportRequestedEvent`에 있다.

---

## Blocked / Residual Handling

- `DamageAppliedEvent.AttackerId`가 unit damage에서 끝까지 비어 있으면 `blocked: damage attribution source missing`으로 남기고, damage math 변경 없이 source id wiring만 별도 runtime fix로 처리한다.
- core id/max hp를 bootstrap에서 안전하게 넘길 수 없으면 `KeepCoreAlive`는 fallback하고, core hp 정확도는 Agent A runtime/core owner와 함께 본다.
- 2-client에서 owner attribution이 host/client마다 다르면 Phase 5 multiplayer sync residual로 이관한다.
- 결과 화면의 layout이 부족하면 Agent B HUD/input 또는 UI visual lane으로 넘기고, Agent A는 text data contract만 닫는다.
- 최근 작전 기록 저장 요구가 생기면 Agent B plan으로 넘긴다.

---

## Lifecycle

- active 전환 이유: 사용자 요청으로 Agent A의 `전투 결과 / 소속감` 작업을 별도 실행 plan으로 고정한다.
- reference 전환 조건: result contribution cards가 pass되거나, 남은 작업이 Agent B 기록 저장/Agent C 용어/visual polish로 이관된다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

---

## 문서 재리뷰

- 과한점 리뷰: Agent B의 누적 기록 저장, Agent C의 기체 애착/용어 리네임, Agent B HUD visual polish를 이 문서에서 제외했다.
- 부족한점 리뷰: owner, scope, current state, event contract, aggregation rule, execution phases, tests, acceptance, blocked/residual handling을 포함했다.
- 수정 후 재리뷰: 기존 Agent A runtime plan의 이동/타겟팅/wave baseline을 복제하지 않고, result contribution source가 필요한 최소 attribution만 이 plan에 남겼다.
- 구현 후 재리뷰: 실제 코드명 `HoldPosition`/`DeployUnits`와 결과 카피 `자리 지킴`/`기체 전개`에 맞춰 문서를 보정했고, Unit feature 직접 참조는 shared gameplay event로 분리해 feature cycle을 제거했다.
- owner impact: primary `plans.game-scene-agent-a-result-belonging`; secondary `design.world-design`, `design.game-design`, `plans.game-scene-flow-validation-closeout`, `plans.game-scene-agent-a-runtime-core`, `plans.game-scene-agent-b-hud-input-validation`; out-of-scope Agent B operation records, Agent C identity/copy full pass.
- doc lifecycle checked: compile/static/docs baseline, single-client defeat actual battle result smoke, 0% core 보정 smoke, actual placement/summon stat smoke, victory result smoke는 통과했다. EditMode test execution, 자연 final-wave victory loop, 2-client attribution은 아직 남아 active plan으로 유지한다. `progress.md`는 현재 포커스 SSOT 갱신이 필요할 때 별도 변경한다.
- plan rereview: residual - direct EditMode test는 open editor 때문에 blocked이고, 자연 final-wave victory loop와 2-client attribution은 flow closeout/Phase 5 lane에서 계속 본다. actual placement/summon stat smoke, 0% core 보정, victory result smoke는 actual/diagnostic smoke pass로 닫혔다.
