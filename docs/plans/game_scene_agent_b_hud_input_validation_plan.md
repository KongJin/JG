# GameScene Agent B HUD Input Validation Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-agent-b-hud-input-validation
> role: plan
> owner_scope: Agent B가 맡는 GameScene/BattleScene HUD, input, feedback, validation 작업
> upstream: plans.progress, plans.game-scene-entry, plans.game-scene-ui-ux-improvement, ops.unity-ui-authoring-workflow, tools.unity-mcp-readme
> artifacts: `Assets/Scripts/Features/Unit/Presentation/`, `Assets/Scripts/Features/Wave/Presentation/`, `Assets/Scripts/Features/Combat/Presentation/`, `Assets/Scripts/Shared/Ui/`, `Assets/Prefabs/Features/Battle/`, `Assets/Prefabs/Features/Result/`, `tools/unity-mcp/`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 두 에이전트 분업 중 Agent B가 맡는 전투 표면 계획이다.
Agent B의 목표는 Agent A가 안정화한 event/state를 플레이어가 읽고 조작할 수 있는 모바일 세로 HUD, placement input, feedback, smoke/validator 경로로 닫는 것이다.

현재 repo 기준 전투 runtime 코드는 `GameSceneRoot` 이름을 쓰지만, 실제 전투 scene/prefab 통합 대상은 `BattleScene` 계열로 남아 있다.
이 문서에서 `GameScene` HUD라고 부르는 범위는 `BattleScene` 전투 표면과 같은 lane으로 본다.

---

## Agent B Scope

Agent B가 소유한다:

- Unit slot HUD와 command dock 표시 상태
- Energy bar, cost affordance, 부족/충분 상태 표현
- placement area, placement preview, valid/invalid feedback, placement error view
- Wave HUD, Core health HUD, WaveEnd/result view
- damage numbers, summon feedback, button/sound hook 연결
- 모바일 터치 입력, tap placement 기본 경로, drag/drop 고급 입력 UX
- placement automation smoke와 UI/presentation validator 정리
- `Assets/Prefabs/Features/Battle/Root/SetDBattleHudBaselineRoot.prefab`
- `Assets/Prefabs/Features/Battle/Root/SetDGameSceneHudFullRoot.prefab`
- `Assets/Prefabs/Features/Result/Independent/SetEMissionVictoryOverlayRoot.prefab`
- `Assets/Prefabs/Features/Result/Independent/SetEMissionDefeatOverlayRoot.prefab`

Agent B가 소유하지 않는다:

- `GameSceneRoot.cs` orchestration과 scene-level runtime wiring
- Player spawn, Garage roster restore, Unit spec 계산, Energy domain/use case 구현
- Enemy spawn, Combat target 등록, CoreObjective victory/defeat 판단
- late-join, BattleEntity sync, Energy sync의 runtime source of truth
- 밸런스 수치, cost, wave table, enemy AI 재설계

Primary code paths:

- `Assets/Scripts/Features/Unit/Presentation/`
- `Assets/Scripts/Features/Wave/Presentation/`
- `Assets/Scripts/Features/Combat/Presentation/`
- `Assets/Scripts/Shared/Ui/`
- `tools/unity-mcp/` 검증 및 smoke helper

---

## Agent A Contract

- Agent A는 event와 state가 나온다는 것을 보장한다.
- Agent B는 그 event와 state를 사람이 읽고 조작할 수 있게 만든다.
- `GameSceneRoot.cs`는 Agent A만 수정한다. Agent B가 compile compatibility 때문에 필요를 발견하면 handoff note로 남기고 직접 수정하지 않는다.
- Presentation view와 HUD prefab은 Agent B가 수정한다. Agent A는 runtime blocker가 아닌 layout/styling 변경을 하지 않는다.
- `BattleScene.unity` serialized 변경은 동시에 하지 않는다. Agent A runtime wiring pass가 끝난 뒤, Agent B가 HUD prefab/scene integration pass를 한 번만 수행한다.
- 새 시스템을 만들기 전에 기존 Setup, EventBus, UseCase, Presentation 경계를 먼저 사용한다.

---

## Execution Plan

### Phase 1. HUD Contract Audit

- 현재 battle HUD prefab과 scene reference가 어떤 view를 연결하는지 확인한다.
- `UnitSlotView`, `UnitSlotsContainer`, `SummonCommandController`, `PlacementAreaView`, `PlacementErrorView`의 serialized refs와 runtime event 입력을 구분한다.
- `WaveHudView`, `CoreHealthHudView`, `WaveEndView`, `DamageNumberSpawner`가 Agent A runtime state를 hidden lookup 없이 소비하는지 확인한다.
- acceptance: missing reference, stale prefab path, runtime blocker, visual/layout issue가 서로 분리되어 기록된다.

### Phase 2. Mobile HUD Layout Pass

- 모바일 세로 기준으로 상단 `Wave/Core`, 하단 `Energy/Unit slots`, 중앙 `Placement feedback`의 시야 충돌을 정리한다.
- Unit slot은 선택 가능, 비용, cooldown 또는 disabled affordance가 한눈에 읽히게 한다.
- Energy bar는 현재량, cost 가능 여부, 부족 상태를 같은 command 영역에서 보여준다.
- acceptance: 모바일 세로 첫 화면에서 HUD끼리 겹치지 않고 `wave`, `core`, `energy`, `slot`이 즉시 구분된다.

### Phase 3. Placement Input And Feedback

- tap placement를 기본 경로로 유지 또는 강화한다.
- drag/drop은 고급 입력으로 유지하되 preview, valid, invalid, cancel 상태가 같은 summon contract를 사용하게 한다.
- placement area 표시, ghost/preview 위치, error view, command feedback이 서로 다른 실패 메시지를 중복 출력하지 않게 정리한다.
- acceptance: 슬롯 선택 -> 배치 가능 영역 표시 -> tap 또는 drag 소환 -> 성공/실패 피드백까지 한 흐름으로 보인다.

### Phase 4. Wave, Core, Result Surface

- wave countdown/status와 core health를 전투 목표 HUD로 정리한다.
- core damage, low core warning, wave clear, victory/defeat 전환 중 HUD가 깨지거나 사라지지 않게 한다.
- `WaveEndView`와 result prefab은 결과, 핵심 수치, lobby 복귀 CTA를 우선순위대로 보여준다.
- acceptance: `Wave -> Core damage -> Victory/Defeat -> WaveEnd/result` 흐름에서 HUD와 결과 표면이 끝까지 유지된다.

### Phase 5. Feedback And Sound Hooks

- summon success, cannot afford, invalid placement, damage number를 화면 위치와 색으로 구분한다.
- button/summon sound hook은 existing audio runtime이 있을 때만 소비하고, direct scene 실행에서 오디오가 없어도 UI smoke를 막지 않게 한다.
- feedback은 짧은 상태 표시 중심으로 두고, 전투 중 긴 설명문을 늘리지 않는다.
- acceptance: 에너지 부족과 배치 실패가 텍스트를 다 읽지 않아도 입력 문맥 안에서 이해된다.

### Phase 6. Smoke And Validator Cleanup

- placement automation smoke가 실제 input path를 검증하는지 확인한다.
- HUD/presentation validator가 visual authoring을 Presentation code로 끌어들이지 않는지 확인한다.
- smoke가 실패하면 Agent A runtime blocker인지 Agent B input/HUD blocker인지 분리한다.
- acceptance: placement automation smoke가 통과하거나, blocker와 owner가 명확히 남는다.

### Phase 7. Integration Handoff

- Agent A core loop가 안정화된 뒤 HUD prefab과 `BattleScene.unity` integration을 한 번만 수행한다.
- scene/prefab 변경 후 fresh contract, screenshot 또는 smoke evidence를 갱신한다.
- 실제 acceptance가 바뀐 경우에만 `progress.md`를 짧게 갱신한다.
- acceptance: 직접 BattleScene 실행과 Lobby 진입 경로 모두에서 최소 HUD/input smoke 결과가 남는다.

---

## Validation

기본 검증:

- C# compile clean
- `npm run --silent rules:lint`
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- Presentation layout ownership validation when Presentation code changed

Runtime/UI smoke:

- direct `BattleScene` play mode HUD smoke
- Lobby -> battle scene transition smoke after Agent A integration
- slot select -> placement area visible -> summon feedback smoke
- cannot afford / enough energy state smoke
- wave/core/result HUD persistence smoke
- placement automation smoke, or blocked owner note

Evidence expectation:

- mechanical pass와 actual acceptance를 분리한다.
- screenshot/capture는 layout proof를 보조할 뿐, runtime smoke를 대체하지 않는다.
- stale console errors는 latest timestamp 기준으로 구분한다.

---

## Blocked / Residual Handling

- Agent A runtime event/state가 아직 없으면 Agent B는 mock runtime을 새로 만들지 않고 blocked owner를 Agent A로 남긴다.
- `BattleScene.unity` serialized integration이 Agent A 작업과 충돌하면 Agent B는 prefab/presentation 준비까지만 닫고 scene pass를 residual로 남긴다.
- Unity MCP가 placement automation을 안정적으로 실행하지 못하면 수동 smoke evidence와 blocker reason을 분리해 남긴다.
- visual fidelity가 Set D/E Stitch source와 맞지 않으면 polish residual이 아니라 translation/runtime integration mismatch로 기록한다.
- direct BattleScene 실행에서 audio singleton이 없어서 sound hook이 무음이어도 HUD/input acceptance 실패로 보지 않는다.

---

## Closeout Criteria

- 모바일 세로 기준 HUD가 겹치지 않는다.
- 슬롯 선택, 배치 가능 영역 표시, 소환 성공/실패 피드백이 한 흐름으로 보인다.
- 에너지 부족/충분 상태가 분명하다.
- wave/core/result HUD가 전투 흐름 끝까지 유지된다.
- placement automation smoke가 통과하거나 blocker와 owner가 명확하다.
- Agent A runtime blocker와 Agent B HUD/input blocker가 섞이지 않는다.

---

## 문서 재리뷰

- 과한점 리뷰: 새 authoring 규칙이나 hard-fail을 만들지 않고, Agent B 실행 범위와 handoff 기준만 정리했다.
- 부족한점 리뷰: owner, scope, 제외 범위, Agent A 계약, 실행 순서, acceptance, validation, blocked/residual 처리를 포함했다.
- 수정 후 재리뷰: 기존 `game_scene_ui_ux_improvement_plan.md`의 UX 방향을 대체하지 않고 Agent B 작업 handoff 문서로 역할을 좁혔다.
- 반복 재리뷰 반영: obvious 과한점/부족한점 없음.
- owner impact: primary `plans.game-scene-agent-b-hud-input-validation`; secondary `plans.progress`, `plans.game-scene-ui-ux-improvement`, `plans.game-scene-entry`, `docs.index`; out-of-scope `ops.unity-ui-authoring-workflow`, Agent A runtime implementation.
- doc lifecycle checked: 새 active plan으로 등록한다. 기존 GameScene UI/UX draft와 entry reference는 대체하지 않고 유지하며, Agent B closeout 뒤 reference 전환 후보로 본다.
- plan rereview: clean
