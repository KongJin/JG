# GameScene Agent A Runtime Core Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-agent-a-runtime-core
> role: plan
> owner_scope: Agent A가 맡는 GameScene/BattleScene 전투 런타임 core flow 안정화 작업
> upstream: plans.progress, plans.game-scene-entry, design.game-design, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Unit/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Combat/`, `Assets/Scripts/Features/Enemy/`, `Assets/Scenes/BattleScene.unity`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 두 에이전트 분업 중 Agent A가 맡는 전투 런타임 core flow 계획이다.
Agent A의 목표는 플레이어가 전투 씬에 들어와 유닛을 소환하고, wave/core/victory-defeat loop가 네트워크 조건에서도 깨지지 않는 상태를 만드는 것이다.

현재 repo 기준 전투 runtime 코드는 `GameSceneRoot` 이름을 쓰지만, 실제 씬 에셋은 `BattleScene.unity`가 존재한다.
문서와 코드에서 `GameScene`이라고 부르는 범위는 이 계획에서 `BattleScene` 전투 런타임과 같은 lane으로 본다.

---

## Agent A Scope

Agent A가 소유한다:

- `GameSceneRoot` orchestration과 scene-level runtime wiring
- local/remote `PlayerSetup` arrival, registry, camera follow, player combat target 등록
- Garage roster restore, Unit spec 계산, Energy port 연결
- `UnitSetup.InitializeBattleEntity`, `SummonPhotonAdapter`, `BattleEntitySetup` runtime 연결
- `CombatSetup`, `EnemySetup`, `WaveSetup`, `CoreObjectiveSetup` 간 core loop 연결
- late-join, BattleEntity HP/position/dead sync, Energy sync smoke
- runtime audio bootstrap과 game end analytics가 core loop를 깨지 않는지 확인

Agent A가 소유하지 않는다:

- HUD layout, visual hierarchy, slot/card styling, result overlay redesign
- `UnitSlotView`, `WaveHudView`, `WaveEndView`, `CoreHealthHudView`의 표시 UX 변경
- Stitch handoff, source-derived presentation, prefab visual fidelity
- 밸런스 수치, cost, wave table 난이도 재설계

Agent B와 겹치는 지점:

- Agent A는 Application event와 runtime state가 정확히 나오게 한다.
- Agent B는 그 event/state를 사람이 읽고 조작할 수 있게 만든다.
- scene/prefab serialized 변경은 동시 수정하지 않는다. runtime wiring이 필요하면 Agent A가 먼저 preflight evidence를 남기고, HUD/presentation hierarchy 변경은 Agent B가 이어받는다.

---

## Execution Plan

### Phase 1. Runtime Contract Audit

- 현재 active battle scene, build settings scene name, Lobby load target이 서로 같은 전투 씬을 가리키는지 확인한다.
- `GameSceneRoot` required serialized references를 audit하고, missing reference가 있으면 scene-owned contract 문제로 기록한다.
- `PlayerSceneRegistry`, `EnemySceneRegistry`, `GameSceneRuntimeSpawnRegistrar`가 runtime-spawned object arrival만 담당하고 hidden lookup/repair로 흐르지 않는지 확인한다.
- acceptance: direct BattleScene 실행과 Lobby -> battle 진입 경로에서 blocker가 코드 문제인지 scene contract 문제인지 분리된다.

### Phase 2. Player And Energy Bootstrap

- local player spawn 후 `CompleteLocalPlayerInitialization()` 순서가 `Status -> Player -> Combat -> Core -> Energy -> Projectile/Zone -> Unit/Garage -> Wave` 흐름을 유지하는지 확인한다.
- remote player arrival이 local initialization 전후 어느 순서로 와도 `ConnectPlayer`가 중복 등록하거나 누락하지 않게 한다.
- Energy regen, current energy, Energy UI event 발행이 Unit summon port와 같은 player id를 사용하는지 확인한다.
- acceptance: local player가 생성되고 camera/health/energy/combat target이 한 번씩만 연결된다.

### Phase 3. Unit Spec And Summon Core

- Room CustomProperties의 `garageRoster` restore 경로와 local fallback 경로를 확인한다.
- `ComputePlayerUnitSpecsUseCase` 결과가 비어 있을 때는 summon UI만 건너뛰고 wave/core runtime을 죽이지 않게 한다.
- `UnitSetup.InitializeBattleEntity`가 Energy port, Combat setup, UnitPositionQuery를 모두 받은 뒤에만 summon을 허용하게 한다.
- acceptance: 최소 1개 Unit spec으로 summon 요청이 BattleEntity 생성, combat target 등록, unit position query 등록까지 이어진다.

### Phase 4. Wave / Enemy / Core Loop

- `WaveSetup.Initialize`가 core objective 없이 진행되지 않게 유지하고, blocked reason을 명확히 한다.
- master client에서 wave start, enemy spawn, enemy arrival initialization, hostile target query가 Player + BattleEntity를 모두 대상으로 삼는지 확인한다.
- core combat target 등록과 core HP event가 victory/defeat 판단까지 이어지는지 확인한다.
- acceptance: wave start -> enemy spawn -> enemy/core combat -> victory or defeat -> WaveEnd event 흐름이 한 세션에서 닫힌다.

### Phase 5. Multiplayer Sync Smoke

- 2-client smoke에서 master/non-master 초기화 순서를 확인한다.
- late-join 시 BattleEntity HP/position/dead state, Energy state, wave state가 복구되는지 본다.
- `PhotonNetwork.IsMasterClient` 전환 시 wave restart나 duplicate spawn이 생기지 않는지 확인한다.
- acceptance: late-join, BattleEntity sync, Energy sync, wave state sync가 각각 pass 또는 구체적 blocker로 남는다.

### Phase 6. Closeout And Handoff To Agent B

- Agent A가 고친 runtime event/state 계약을 Agent B가 소비할 수 있게 짧은 handoff note를 남긴다.
- Agent B가 수정해야 할 HUD/input/presentation 문제를 runtime blocker와 섞지 않는다.
- `progress.md` 갱신은 실제 acceptance 상태가 바뀐 경우에만 한다.
- acceptance: Agent B가 `UnitSlot`, `Placement`, `WaveHud`, `CoreHud`, `WaveEnd`를 건드릴 때 runtime 상태 소유자를 다시 판단하지 않아도 된다.

---

## Implementation Guardrails

- `GameSceneRoot`와 `*Setup`은 wiring-only로 유지한다. gameplay decision, event handling, domain entity creation은 Application/UseCase로 내린다.
- 새 global lookup, `FindFirstObjectByType<*SceneRegistry>`, runtime `AddComponent<*SceneRegistry>` 경로를 만들지 않는다.
- runtime-spawned object arrival은 registry 또는 explicit bootstrap registration으로만 연결한다.
- Application layer에는 Unity/Photon API를 새로 넣지 않는다.
- Agent A는 Presentation view의 layout/styling을 수정하지 않는다. compile repair가 필요하면 가장 작은 API compatibility 수정만 한다.
- scene/prefab 변경이 필요하면 Unity MCP preflight로 active scene, component path, serialized refs를 먼저 확인한다.

---

## Validation

기본 검증:

- C# compile clean
- `npm run --silent rules:lint`
- `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` when scene/UI authoring surfaces changed

Runtime smoke:

- direct `BattleScene` play mode smoke
- Lobby -> battle scene transition smoke
- click summon smoke
- placement acceptance smoke, or blocked handoff to Agent B if the failure is UI/input-only
- wave/core/victory-defeat smoke
- 2-client multiplayer smoke for late-join, BattleEntity sync, Energy sync

Evidence expectation:

- mechanical pass와 actual acceptance를 분리한다.
- smoke가 실패하면 `blocked` 또는 `mismatch`로 남기고, Agent B scope 문제인지 Agent A runtime 문제인지 분리한다.
- stale console errors는 latest timestamp 기준으로 구분한다.

---

## Residual Risks

- `GameScene` 문서명과 실제 `BattleScene.unity` 에셋명이 섞여 있어 scene target mismatch가 다시 생길 수 있다.
- 현재 progress 기준 핵심 미완료는 placement drag/drop automation과 multiplayer sync smoke다. Agent A는 multiplayer sync를 우선 닫고, placement가 input/HUD 문제면 Agent B로 넘긴다.
- SoundPlayer는 Lobby에서 DDOL singleton으로 들어오는 경로와 direct BattleScene 실행 경로가 다르다. direct run에서 오디오가 없어도 core runtime failure로 처리하지 않는다.
- Agent A가 scene wiring을 고치고 Agent B가 HUD prefab을 동시에 고치면 serialized conflict가 날 수 있다. scene/prefab mutation은 한 명씩 순차 처리한다.
