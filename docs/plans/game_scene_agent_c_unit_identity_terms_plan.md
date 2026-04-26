# GameScene Agent C Unit Identity And Terms Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.game-scene-agent-c-unit-identity-terms
> role: plan
> owner_scope: Agent C가 맡는 기체 애착, 로스터 슬롯 정체성, 기체 전적 태그, 유저-facing 용어/카피 정리 작업
> upstream: plans.progress, design.game-design, design.world-design, design.unit-module-design, plans.game-scene-agent-a-result-belonging, plans.operation-record-world-memory, plans.game-scene-agent-b-hud-input-validation
> artifacts: `Assets/Scripts/Features/Garage/`, `Assets/Scripts/Features/Unit/Presentation/`, `Assets/Scripts/Features/Wave/Presentation/WaveEndView.cs`, `Assets/Prefabs/Features/Battle/`, `Assets/Prefabs/Features/Result/`, `docs/design/game_design.md`, `docs/design/world_design.md`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 세 에이전트 분업 중 Agent C가 맡는 기체 정체성 계획이다.
Agent C의 목표는 유닛을 단순 조합 템플릿이 아니라 플레이어가 계속 데리고 다니는 `기체`로 읽히게 만들고, 전투/차고/결과 화면의 말을 저항 SF 세계관 안에서 한 목소리로 정리하는 것이다.

핵심 문장:

> 내가 만든 기체 조합이, 다른 사람의 조합과 맞물려, 이 거점을 5분 더 버티게 했다.

---

## Agent C Scope

Agent C가 소유한다:

- 로스터 슬롯별 자동 콜사인 또는 기체명 표시
- 기체별 대표 전적 태그 표시: 최장 전선 유지, 최다 재소환, 코어 근접 차단, 위기 순간 생존
- 기체 정체성 view model: 슬롯 번호, 프레임명, 모듈 조합, 역할 태그, 대표 기록
- Garage 로스터/편집/프리뷰 표면에서 `Unit`보다 `기체`로 읽히는 카피
- 전투 HUD와 결과 표면에서 A/B가 소비할 유저-facing 용어 기준
- `승리/패배`, `웨이브`, `유닛` 같은 기존 카피를 `버텨냈다/방어 실패`, `공세/침공`, `기체` 쪽으로 정리하는 pass
- `world_design.md`, `game_design.md`에 필요한 용어 기준 반영 제안과 최소 문서 갱신

Agent C가 소유하지 않는다:

- Agent A의 GameEnd 통계 생성, 전투 결과 계산, contribution card scoring
- Agent B의 최근 작전 기록 5개 저장, 프로필/차고 작전 기록 목록 UI
- battle runtime state, summon 판정, energy, wave/core authoritative 로직
- 결과 화면 layout, HUD placement preview, 모바일 입력 UX
- 성능 강화, 랜덤 성장, 희귀도, 기체 능력치 보너스
- Nova1492 원전 고유명사의 공개 배포 권리 판단

Primary code paths:

- `Assets/Scripts/Features/Garage/Domain/`
- `Assets/Scripts/Features/Garage/Application/`
- `Assets/Scripts/Features/Garage/Infrastructure/`
- `Assets/Scripts/Features/Garage/Presentation/`
- `Assets/Scripts/Features/Unit/Presentation/UnitSlotView.cs`
- `Assets/Scripts/Features/Unit/Presentation/UnitSlotsContainer.cs`
- `Assets/Scripts/Features/Wave/Presentation/WaveEndView.cs`

Serialized/UI paths are edited only after the copy and view model contract is stable:

- `Assets/Prefabs/Features/Battle/`
- `Assets/Prefabs/Features/Result/`
- Lobby/Garage scene or prefab roots touched by Garage roster and preview surfaces

---

## Agent A / B Contract

Agent A owns 이번 판 결과 (`game_scene_agent_a_result_belonging_plan.md`):

- Agent A provides match-end stats or a `GameEndSummary` equivalent.
- Agent C may request per-unit fields such as summons, survival duration, core-near blocks, kills, support saves, and crisis survival.
- Agent C does not calculate authoritative combat events from presentation state.

Agent B owns 누적 작전 기록 (`operation_record_world_memory_plan.md`):

- Agent B stores and displays recent operation records.
- Agent C may derive or display per-unit tags from B's operation records when those records include unit identifiers.
- Agent C does not own the recent operation list, profile operation archive, or storage policy for operation records.

Agent C owns 기체/용어 정체성:

- Agent C defines the display vocabulary and the unit identity surface that A/B can reuse.
- Agent C supplies copy labels and tag naming so A's result cards and B's operation records use the same language.
- Agent C avoids changing A/B data ownership. If a required field is missing, C leaves a handoff request instead of inventing hidden lookup or duplicate storage.
- Agent B HUD/input remains a separate UI lane. C only hands off labels or updates C-owned presentation strings; it does not take over HUD layout or placement UX.

Conflict boundary:

- A: 이번 판 결과
- B: 누적 기록
- C: 기체 정체성 and user-facing terminology
- HUD/input Agent B: battle HUD layout, input, preview, and UI smoke

---

## Product Decisions For MVP

- `Unit` remains acceptable as an internal code/domain word.
- User-facing copy prefers `기체`.
- `Core` remains user-facing when it means the defended base device.
- `Garage/차고` remains user-facing for assembly and sortie preparation.
- `Wave` remains internal; user-facing candidates are `공세`, `침공`, or `접근`.
- `Victory` copy should move toward `버텨냈다`.
- `Defeat` copy should move toward `거점 붕괴` or `방어 실패`.
- MVP automatic callsigns do not need manual rename.
- MVP stat tags do not affect gameplay power.
- First implementation should prefer deterministic labels derived from roster slot and loadout over new account schema.

Example first-pass labels:

| Current/Internal | User-facing target | Note |
|---|---|---|
| Unit | 기체 | Garage, battle slots, result copy |
| Wave | 공세 | HUD/result; keep `Wave` in code |
| Victory | 버텨냈다 | Result headline |
| Defeat | 거점 붕괴 / 방어 실패 | Use context: headline vs stat row |
| Kill count | 제거 | Enemy pressure language |
| Summon | 출격 / 재출격 | Garage/result may use 출격, internal remains summon |

---

## Execution Plan

### Phase C1. Copy And Term Inventory

- Search current UI strings for `Unit`, `Wave`, `Victory`, `Defeat`, `승리`, `패배`, `유닛`, `웨이브`, `소환`.
- Split strings into internal logs, developer diagnostics, and user-facing UI copy.
- Confirm which user-facing strings belong to Agent A result surfaces, Agent B operation-record surfaces, HUD/input surfaces, and Garage identity surfaces.
- acceptance: C has a small copy inventory and an edit list that does not require touching A's runtime logic or B's layout ownership.

### Phase C2. Terminology Baseline In Design Owners

- Add or refine a short user-facing terminology section in the design owner docs if the current docs are not enough.
- Keep product tone in `world_design.md`: 저항 SF, 조합으로 버틴다, 군사 용어 허용, 명령 수행보다 협력 방어.
- Keep gameplay scope in `game_design.md`: internal `Unit/Wave` can remain, player-facing labels prefer `기체/공세`.
- Do not duplicate the full glossary in this plan after the design owners are updated.
- acceptance: A/B/C can point to the same design owner for copy direction.

### Phase C3. Automatic Callsign Contract

- Define a deterministic callsign for each roster slot without requiring manual rename.
- MVP format can be slot-based plus frame/module identity, for example `A-03 가디언` or `G-02 중장갑 가디언`.
- Callsign should be stable across a session and readable in Garage, battle slots, and result/record summaries.
- Avoid adding cloud persistence for callsigns unless manual rename is explicitly added later.
- acceptance: the same saved roster produces the same visible callsigns after reload.

### Phase C4. Unit Identity View Model

- Extend or wrap existing Garage presentation view models so a roster slot can expose:
  - callsign
  - frame name
  - role label
  - module shorthand
  - one representative service tag
- Keep the view model presentation-friendly; do not push UI wording into domain entities unless it is identity data, not layout text.
- Use existing `GarageRoster.UnitLoadout` and computed `UnitSpec` where possible.
- acceptance: Garage roster/preview can display a unit as a named 기체 without changing combat stats.

### Phase C5. Service Tag Source Contract

- Define the minimum per-unit data needed for tags:
  - longest frontline hold
  - most redeployed unit
  - core-near blocks
  - crisis survival
- Prefer consuming Agent A's match summary and Agent B's operation records instead of creating a separate event pipeline.
- If A/B data lacks per-unit identifiers, leave a handoff request for `unitSlotIndex` or `loadoutKey` in the summary contract.
- acceptance: each planned tag has a source owner and fallback state, such as `기록 대기중`, without fake stats.

### Phase C6. MVP Service Tag Display

- Show at most one or two representative tags per roster slot in Garage.
- Tags should describe remembered behavior, not power:
  - `최장 전선 유지 42초`
  - `코어 근접 차단 31회`
  - `최다 재출격 기체`
  - `위기 순간 생존`
- If no history exists, use neutral identity copy such as role/module tags instead of empty achievement language.
- acceptance: a player can identify "this is the 기체 that did X" in Garage without any stat bonus.

### Phase C7. Battle, Result, And Record Copy Handoff

- Provide Agent A with replacement labels for result contribution cards.
- Provide operation-record Agent B with replacement labels for recent operation summaries.
- Provide HUD/input Agent B with replacement labels for battle HUD surfaces when those surfaces are in that UI lane.
- Update low-risk presentation strings directly only where C owns the view or where the owning A/B lane has completed its integration pass.
- Result headline direction:
  - success: `버텨냈다`
  - failure: `방어 실패` or `거점 붕괴`
- Result stat direction:
  - `공세`, `침투 차단`, `전선 유지`, `코어 내구도`, `재출격`
- acceptance: result, operation record, and Garage copy no longer reads as generic RPG ranking language.

### Phase C8. Validation And Handoff

- Run compile validation if code changes are made.
- Run `npm run --silent rules:lint` for docs changes.
- For UI/prefab changes, follow Unity UI authoring workflow and capture evidence as required by the relevant UI plan.
- Leave A/B handoff notes for missing fields instead of blocking C closeout on unrelated runtime/storage work.
- acceptance: copy and identity changes are mechanically clean, and any missing contribution/history data has an owner.

---

## Validation

Docs:

- `npm run --silent rules:lint`

Code, if C changes C#:

- C# compile clean
- reflection or direct tests for callsign generation and service tag selection if new public API is added

UI, if C changes prefab/scene surfaces:

- Unity UI authoring workflow policy check
- Garage roster/preview smoke
- battle slot label smoke
- result copy smoke after Agent A integration
- operation record copy smoke after Agent B integration

Evidence expectation:

- mechanical pass and actual player-facing acceptance are reported separately.
- Missing A/B source data is `blocked` or `residual`, not faked by presentation code.
- Logs and internal diagnostics do not need to be fully localized in the same pass.

---

## Closeout Criteria

Agent C closeout requires:

- User-facing terminology baseline is documented in design owners or explicitly handed off.
- Garage and battle/result copy use `기체`, `공세/침공`, `버텨냈다`, `방어 실패/거점 붕괴` direction where C-owned surfaces are touched.
- Roster slots can display deterministic callsigns or a clearly blocked owner reason exists.
- Garage can show representative identity/service tags or a source-data residual is assigned to A/B.
- No gameplay power, upgrade, or random growth is introduced by the identity layer.
- A/B/C ownership remains separated: result data, operation storage, identity/copy.

---

## Blocked / Residual Handling

- If per-unit match stats do not exist, C records the missing `unitSlotIndex`/`loadoutKey`/metric fields as Agent A result handoff.
- If recent operation records do not store unit identifiers, C records the missing record fields as operation-record Agent B handoff.
- If Garage persistence schema would need expansion for manual rename, leave manual rename out of MVP and keep callsigns deterministic.
- If UI copy lives in an Agent A result view, operation-record Agent B surface, or HUD/input Agent B prefab mid-edit, C provides copy constants/handoff and waits for the owning integration pass.
- If public release naming rights for Nova1492-specific terms are unclear, C keeps structure and tone but avoids locking product-facing proper nouns as release-ready.

---

## Lifecycle

- active 전환 이유: 사용자가 세 에이전트 분업에서 Agent C의 기체 애착/용어 체계 작업을 별도 실행 계획으로 요청했고, A/B active plan과 충돌하지 않는 owner boundary가 필요하다.
- reference 전환 조건: deterministic callsign, service tag display or clear source-data residual, and terminology/copy handoff are closed.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다. 실제 현재 포커스가 바뀌면 `plans.progress`도 짧게 갱신한다.

---

## 문서 재리뷰

- 과한점 리뷰: 새 메타 성장 시스템, manual rename, 새 클라우드 스키마, A/B runtime/storage 구현을 C plan에 넣지 않았다.
- 부족한점 리뷰: owner, scope, 제외 범위, A/B 계약, 실행 순서, validation, acceptance, blocked/residual 처리를 포함했다.
- 수정 후 재리뷰: 용어 표는 C 작업 시작 기준만 남기고, 최종 glossary owner는 `design.world-design`/`design.game-design`으로 유지했다.
- 반복 재리뷰 반영: obvious 과한점/부족한점 없음.
- 2026-04-26 A/B 실행 문서 정렬 후 과한점 리뷰: Agent A result plan과 operation-record Agent B plan을 upstream/contract에 직접 연결했고, HUD/input Agent B는 UI lane으로만 남겨 C가 layout 작업을 가져오지 않게 했다.
- 2026-04-26 A/B 실행 문서 정렬 후 부족한점 리뷰: C가 어떤 A/B 산출물을 기다리는지, result/record/HUD copy handoff 대상이 누구인지 명시했다.
- 2026-04-26 A/B 실행 문서 정렬 후 재리뷰: plan rereview: clean.
- owner impact: primary `plans.game-scene-agent-c-unit-identity-terms`; secondary `design.world-design`, `design.game-design`, `design.unit-module-design`, `plans.game-scene-agent-a-result-belonging`, `plans.operation-record-world-memory`, `plans.game-scene-agent-b-hud-input-validation`, `docs.index`; out-of-scope Agent A result scoring, Agent B operation record storage, HUD/input layout, Account release/legal gates.
- doc lifecycle checked: 새 active plan으로 등록한다. A/B active plan은 대체하지 않고 병렬 owner로 유지하며, C closeout 뒤 reference 전환 후보로 본다.
- plan rereview: clean
