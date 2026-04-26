# GameScene Agent A Runtime Systems Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-agent-a-runtime-core
> role: plan
> owner_scope: Agent A가 맡는 GameScene/BattleScene 전투 런타임 규칙, BattleEntity, Wave/Core, 동기화 baseline 안정화 작업
> upstream: plans.progress, plans.game-scene-entry, plans.game-scene-phase5-multiplayer-sync, design.game-design
> artifacts: `Assets/Scripts/Features/Player/GameSceneRoot.cs`, `Assets/Scripts/Features/Unit/Domain/`, `Assets/Scripts/Features/Unit/Infrastructure/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Enemy/`, `Assets/Scripts/Features/Combat/`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 두 에이전트 분업 중 Agent A가 맡는 전투 런타임 시스템 계획이다.
Agent A의 목표는 플레이어 입력 표면과 분리된 실제 전투 규칙을 안정화해, 소환 유닛이 앵커 반경 안에서 자동 교전하고 적이 `core -> unit -> player` 우선순위로 압박하며 `wave -> core damage -> victory/defeat` loop가 재현되는 상태를 만드는 것이다.

현재 repo 기준 전투 runtime 코드는 `GameSceneRoot` 이름을 쓰지만, 실제 통합 대상은 `BattleScene` 계열로 남아 있다.
이 문서에서 `GameScene`은 `BattleScene` 전투 런타임 lane과 같은 범위로 본다.

---

## Agent A Scope

Agent A가 소유한다:

- 앵커 반경 기반 자동 교전 규칙
- `BattleEntity` 이동, 공격, 타겟 선택, 사망/비활성 상태 전이
- enemy target priority: `core -> unit -> player`
- wave/core/victory/defeat baseline smoke를 가능하게 하는 런타임 상태와 이벤트
- late-join `BattleEntity`, `Energy`, `Wave` hydration
- player avatar를 전투 유닛으로 둘지, commander/base target으로 낮출지에 대한 런타임 계약 정리
- `UnitSpec`, `BattleEntity`, `WaveState`, `Energy`, `Core HP` 상태와 이벤트 보장

Agent A 소유 파일:

- `Assets/Scripts/Features/Player/GameSceneRoot.cs`
- `Assets/Scripts/Features/Unit/Domain/`
- `Assets/Scripts/Features/Unit/Infrastructure/`
- `Assets/Scripts/Features/Wave/`
- `Assets/Scripts/Features/Enemy/`
- `Assets/Scripts/Features/Combat/`

Agent A만 수정하는 경계:

- `GameSceneRoot.cs`
- `WaveSetup.cs`
- `BattleEntityPrefabSetup.cs`

Agent A가 소유하지 않는다:

- HUD layout, slot/card styling, preview visual hierarchy
- `*View`, `*InputHandler`, `*Container`, `PlacementAreaView`
- `docs/playtest/runtime_validation_checklist.md`의 GameScene 전용 UX/checklist 확장
- `tools/unity-mcp/`의 placement/HUD smoke helper
- legacy Skill UI/리소스 격리 판단 문서화
- 밸런스 수치, wave table 난이도 재설계

---

## Agent B Contract

Agent A는 아래 런타임 계약을 보장한다:

- `UnitSpec`은 소환 가능 유닛의 실제 비용, 앵커 반경, 공격 사거리, 예상 소환 위치 계산에 필요한 값을 제공한다.
- `BattleEntity`는 현재 위치, HP/dead state, owner, combat target 등록 상태, 공격 가능 여부를 일관되게 노출한다.
- `WaveState`는 wave index, active/countdown/spawn 상태, victory/defeat 전환 기준을 이벤트나 상태로 제공한다.
- `Energy`는 current/max/regen/spend 결과와 insufficient/sufficient 판정을 같은 player id 기준으로 발행한다.
- `Core HP`는 damage, danger threshold, destroyed/victory/defeat 이벤트를 중복 없이 발행한다.

Agent B는 그 상태를 읽고 조작하는 HUD/input/preview만 담당한다:

- 유닛 선택 후 배치 가능 영역 표시
- 앵커 반경, 공격 사거리, 예상 소환 위치 preview
- 에너지 부족/충분 상태 표시
- wave 압박, 적 역할 텔레그래프, core danger feedback
- 모바일 기준 slot select -> placement preview -> summon feedback 흐름

공통 serialized 변경 원칙:

- `BattleScene.unity`와 HUD prefab serialized 변경은 동시에 하지 않는다.
- 마지막 통합 pass에서 한 명만 scene/prefab wiring을 수행한다.
- Agent A는 runtime state/event 문제를 닫은 뒤 Agent B가 읽을 수 있는 handoff note를 남긴다.
- Agent B가 UI 입력은 정상인데 summon, Energy spend, BattleEntity spawn이 실패한다고 판단하면 Agent A runtime blocker로 되돌린다.

---

## Execution Plan

### Phase A1. Runtime Contract Audit

- `GameSceneRoot`, `WaveSetup`, `BattleEntityPrefabSetup`이 현재 scene/prefab contract에서 어떤 runtime state를 보장해야 하는지 확인한다.
- `UnitSpec`, `BattleEntity`, `Energy`, `WaveState`, `Core HP`가 Agent B preview/HUD가 읽을 수 있는 단일 source인지 점검한다.
- player avatar가 직접 전투 타겟인지, commander/base 역할의 낮은 우선순위 target인지 현재 코드 기준을 정리한다.
- acceptance: Agent B가 preview/HUD 구현에 필요한 runtime source와 아직 없는 source를 구분할 수 있다.

### Phase A2. Anchor Radius Auto Engagement

- 소환 유닛이 자기 앵커 반경 안에서만 자동으로 적을 찾고 교전하는지 확인한다.
- 앵커 이탈, target loss, dead/invalid target, cooldown 중 이동/공격 상태 전이를 정리한다.
- 유닛 공격 사거리와 앵커 반경이 `UnitSpec` 또는 BattleEntity 초기화 계약과 어긋나지 않게 한다.
- acceptance: 소환 유닛이 앵커 반경 안의 적을 자동 교전하고, 반경 밖 적에게 무한 추적하거나 idle lock에 빠지지 않는다.

### Phase A3. BattleEntity Movement, Attack, Target Selection

- `BattleEntity` 이동/공격 루프가 target query, combat registration, health/death state를 같은 기준으로 소비하는지 확인한다.
- target 선택이 local-only visual 상태나 Presentation view에 의존하지 않게 한다.
- enemy와 unit 모두 dead target, despawned target, late-joined proxy target을 안전하게 무시한다.
- acceptance: 유닛과 적이 최소 한 번씩 target acquire -> move/attack -> damage/death 또는 target switch 흐름을 재현한다.

### Phase A4. Enemy Priority: Core / Unit / Player

- enemy target priority를 `core -> unit -> player` 순서로 고정한다.
- core가 살아 있으면 core 압박을 우선하고, 교전 가능한 unit이 있으면 unit을 압박하며, player avatar는 commander/base fallback target으로 낮출지 계약을 결정한다.
- player avatar를 전투 주체로 유지해야 한다면 Agent B HUD와 충돌하지 않는 target role 이름을 정리한다.
- acceptance: enemy가 core, unit, player 후보가 동시에 있을 때 우선순위대로 target을 선택한다.

### Phase A5. Wave / Core / Victory / Defeat Baseline

- wave start, enemy spawn, core damage, wave clear, victory/defeat event가 한 세션에서 닫히는지 확인한다.
- core HP damage와 danger/destroyed event가 중복 발행되지 않게 한다.
- victory/defeat 판단이 HUD/result view 없이도 runtime event로 재현되는지 확인한다.
- acceptance: `wave -> core damage -> victory/defeat` loop가 single client smoke에서 pass하거나, blocker owner가 명확하다.

### Phase A6. Late-Join Hydration And 2-Client Sync

- late-join 시 기존 `BattleEntity`의 HP, position, dead state가 복구되는지 본다.
- `Energy` current/regen/spend 상태가 joiner에서 다른 player id로 초기화되지 않는지 확인한다.
- `WaveState` active/countdown/spawn/result 상태가 host와 joiner에서 수렴하는지 확인한다.
- 2-client sync smoke의 상세 절차와 closeout 판정은 [`game_scene_phase5_multiplayer_sync_plan.md`](./game_scene_phase5_multiplayer_sync_plan.md)를 따른다.
- acceptance: 2-client sync smoke가 pass하거나 `blocked`/`mismatch`와 owner가 분리된다.

### Phase A7. Agent B Handoff And Final Integration Pass

- Agent A가 보장한 runtime state/event를 Agent B가 읽을 수 있게 짧은 handoff note를 남긴다.
- Agent B가 병렬로 준비한 preview/HUD/checklist와 충돌하는 serialized 변경이 있는지 확인한다.
- `BattleScene.unity` 또는 HUD prefab wiring은 Agent A/B 중 한 명이 마지막에 한 번만 수행한다.
- acceptance: Agent B가 `*View`, `*InputHandler`, `*Container`, `PlacementAreaView`를 수정할 때 runtime state owner를 다시 판단하지 않아도 된다.

---

## Recommended Order

1. Agent A가 앵커 전투와 wave/core baseline을 먼저 안정화한다.
2. Agent B는 병렬로 preview, HUD, validation checklist를 준비한다.
3. 마지막에 `BattleScene` scene wiring 통합 pass를 1회만 수행한다.
4. 그 뒤 2-client sync smoke와 모바일 HUD smoke를 함께 판정한다.

---

## Current Runtime Evidence

2026-04-26 Agent A pass:

- Code path: `BattleEntity` now keeps spawn-time `AnchorPosition`; movement and auto target query are constrained by anchor radius.
- Code path: enemy movement target resolution is `core -> unit -> player` by contract. `HostilePositionQuery` uses unit before player as the fallback after core.
- Direct test coverage: `GameSceneRuntimeSystemsDirectTests` covers spawn-anchor movement, enemy core priority, and unit-before-player fallback.
- Compile validation: `tools/check-compile-errors.ps1` pass with `ERRORS: 0`, `WARNINGS: 0`.
- Single-client runtime smoke: `LobbyScene -> Photon room -> BattleScene` reached `BattleScene` with console errors `0`.
- Runtime smoke: unit slot select plus placement-center confirm spawned `/RuntimeSpawnRoots/BattleEntities/BattleEntity(Clone)`.
- Runtime smoke: wave enemies spawned, core contact damage fired, and `WaveEndOverlay` reached `Defeat!` with console errors `0`.
- Runtime smoke: direct `BattleScene` without a Photon room remains blocked by the expected room contract and reports `[GameScene] You are not connected to a room`.

Residuals after this pass:

- 2-client sync smoke is not yet closed. Owner remains Phase 5 multiplayer sync plan unless a runtime state mismatch is found.
- `SetCRoomDetailPanelRoot` stayed inactive while `RoomDetailPanel` rendered; this is a Lobby/UX visibility blocker, not Agent A runtime.
- `rules:lint`, compile, and Unity single-client runtime smoke are clean after this pass.

---

## Validation

기본 검증:

- C# compile clean
- `npm run --silent rules:lint`
- scene/prefab serialized 변경 시 Unity MCP preflight와 관련 authoring workflow policy 확인

Runtime smoke:

- direct `BattleScene` single client smoke
- summon unit anchor radius auto engagement smoke
- enemy `core -> unit -> player` target priority smoke
- `wave -> core damage -> victory/defeat` baseline smoke
- late-join `BattleEntity`, `Energy`, `WaveState` hydration smoke
- 2-client sync smoke, or blocker owner note

Evidence expectation:

- mechanical pass와 actual runtime acceptance를 분리한다.
- smoke 실패는 `blocked` 또는 `mismatch`로 남기고 Agent A runtime 문제인지 Agent B HUD/input 문제인지 분리한다.
- direct `BattleScene` 실행에서 audio singleton이 없어도 core runtime failure로 보지 않는다.

---

## Closeout Criteria

Agent A closeout은 아래 기준을 만족해야 한다:

- 소환 유닛이 앵커 반경 안에서 자동 교전한다.
- 적이 `core -> unit -> player` 우선순위대로 압박한다.
- `wave -> core damage -> victory/defeat` loop가 재현된다.
- late-join `BattleEntity`, `Energy`, `WaveState` hydration이 pass하거나 blocker가 명확하다.
- 2-client sync smoke가 pass하거나 blocker owner가 명확하다.
- Agent B가 소비할 `UnitSpec`, `BattleEntity`, `WaveState`, `Energy`, `Core HP` 상태와 이벤트 계약이 정리되어 있다.

---

## Blocked / Residual Handling

- HUD가 상태를 표시하지 못하지만 runtime event/state가 정상이라면 Agent B blocker로 넘긴다.
- UI 입력은 정상인데 summon, energy spend, BattleEntity spawn, wave/core event가 실패하면 Agent A blocker로 유지한다.
- Photon 또는 2-client 실행 환경 때문에 smoke를 못 돌리면 code path 완료와 actual sync acceptance를 분리해 `blocked`로 남긴다.
- player avatar commander/base 계약이 제품 판단을 요구하면 runtime code 변경 전에 `design.game-design` 또는 별도 design owner로 이관한다.
- scene/prefab serialized 충돌이 있으면 final integration pass 전까지 코드/runtime 준비만 닫고 wiring은 residual로 남긴다.

---

## Lifecycle

- active 전환 이유: 사용자가 Agent A runtime systems lane을 현재 실행 계획으로 재지정했고, anchor engagement, enemy priority, wave/core baseline, hydration smoke가 직접 acceptance로 올라왔다.
- reference 전환 조건: Agent A closeout 기준이 pass 또는 owner가 명확한 blocked/residual로 닫히고, 남은 작업이 Agent B HUD/input 또는 Phase 5 sync smoke 전용 plan으로 이관된다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

---

## 문서 재리뷰

- 과한점 리뷰: Agent B의 HUD/input/checklist 구현 세부를 가져오지 않고, Agent A runtime state/event 계약과 handoff 기준만 둔다.
- 부족한점 리뷰: owner, scope, 제외 범위, 소유 파일, Agent B 계약, 실행 순서, validation, acceptance, blocked/residual 처리를 포함했다.
- 수정 후 재리뷰: 기존 Phase 5 sync plan의 상세 절차를 복제하지 않고 2-client smoke 판정은 해당 plan으로 링크했다.
- 반복 재리뷰 반영: obvious 과한점/부족한점 없음.
- 2026-04-26 실행 증거 반영 후 과한점 리뷰: runtime evidence만 추가했고, Phase 5 sync 절차와 Agent B HUD 구현 세부는 복제하지 않았다.
- 2026-04-26 실행 증거 반영 후 부족한점 리뷰: compile, single-client runtime smoke, direct-scene blocker, 2-client residual, UX owner blocker를 분리했다.
- owner impact: primary `plans.game-scene-agent-a-runtime-core`; secondary `plans.progress`, `docs.index`, `plans.game-scene-phase5-multiplayer-sync`, `plans.game-scene-agent-b-hud-input-validation`; out-of-scope `ops.unity-ui-authoring-workflow`, Agent B presentation implementation, scene/prefab final wiring implementation.
- doc lifecycle checked: 기존 reference Agent A 문서를 현재 active plan으로 재활성화한다. 새 plan 파일은 만들지 않으며, Agent B active plan과 Phase 5 active plan은 세부 owner로 유지한다.
- plan rereview: clean
