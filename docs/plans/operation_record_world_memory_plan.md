# Operation Record / World Memory Plan

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: plans.operation-record-world-memory
> role: plan
> owner_scope: 최근 작전 기록, 세계 기억, Lobby/Garage 기록 표시 작업
> upstream: plans.progress, design.game-design, design.world-design, plans.account-system
> artifacts: `Assets/Scripts/Features/Player/`, `Assets/Scripts/Features/Wave/`, `Assets/Scripts/Features/Account/`, `Assets/Scripts/Features/Garage/`, `Assets/UI/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

목표는 보상 테이블을 늘리는 것이 아니라, 한 판이 끝난 뒤 플레이어가 "내가 이 세계에서 실제로 몇 번 버텼고, 어떤 흔적을 남겼는지"를 Lobby/Garage에서 다시 볼 수 있게 하는 것이다.

---

## Scope

이 문서가 소유한다:

- 최근 작전 기록 5개 저장 모델과 읽기 API
- 전투 종료 시 작전 기록 생성
- 기록 필드의 최소 정규화: 성공/실패, 생존 시간, 도달 공세, 코어 잔여 체력, 주요 기여 기체, 주요 압박 요약
- 계정/로컬 저장 경로 연결
- Lobby 또는 Garage 한쪽에 최근 기록 요약 표시
- 기록 저장 실패가 전투 종료나 Lobby 복귀를 막지 않는 fallback 처리
- 기록 검증용 direct test와 WebGL/Play Mode smoke 항목

이 문서가 소유하지 않는다:

- 결과 화면의 팀 기여 카드 생성과 UI 배치
- `GameEndSummary` 또는 동급 전투 종료 요약의 authoritative 계산
- 기체 콜사인, 기체별 전적 태그, 유닛 애착 시스템
- 저항 SF 용어/카피 SSOT 자체
- Firestore 인증/계정 복구의 미완료 backlog
- Wave/enemy/core 전투 판정 변경

Primary boundary:

- 전투 결과 runtime은 이번 판 결과와 팀 기여 요약을 만든다.
- 이 문서는 그 결과를 누적 작전 기록으로 저장하고 Lobby/Garage에 보여준다.
- 유저-facing 용어와 기체 정체성 기준은 `design.game-design`과 `design.world-design`을 따른다.

---

## MVP Record Shape

최근 5회 작전 기록은 아래 정보를 우선한다.

| 필드 | MVP 기준 | 비고 |
|---|---|---|
| `operationId` | 로컬 생성 timestamp 기반 ID | 서버 고유 ID는 후순위 |
| `endedAtUnixMs` | 종료 시각 | 정렬 기준 |
| `result` | `held` 또는 `baseCollapsed` | 화면 문구는 design owner 용어 기준을 따른다 |
| `survivalSeconds` | 실제 플레이 시간 | 현재 `GameEndEvent.PlayTimeSeconds`부터 시작 |
| `reachedWave` | 도달 공세/침공 단계 | 내부 wave 용어는 UI에서 직접 노출하지 않는다 |
| `coreHealthPercent` | 종료 시 코어 잔여율 | 전투 결과 summary에 없으면 `unknown` 허용 |
| `summonCount` | 소환 횟수 | 현재 event에서 확보 가능 |
| `unitKillCount` | 제거 수 | 현재 event에서 확보 가능 |
| `primaryRosterUnits` | 주요 기체 1~2기 | 초기에는 roster slot/frame/module 조합에서 산출 |
| `pressureSummary` | 주요 압박 요약 | enemy role data가 없으면 reached wave 기반 짧은 fallback |

MVP는 "최근 5회만 보여준다"를 기준으로 한다.
통산 전적, 상세 필터, 서버 랭킹, 시즌 기록, 리플레이 저장은 이번 scope가 아니다.

---

## Data Contract

이 문서는 전투 runtime을 다시 계산하지 않고 종료 요약을 소비한다.

1. 단기 시작점은 현재 `GameEndEvent`의 `IsVictory`, `ReachedWave`, `PlayTimeSeconds`, `SummonCount`, `UnitKillCount`다.
2. `coreHealthPercent`, 팀 기여 카드, enemy pressure, 주요 기여 기체 같은 확장값은 전투 결과 runtime이 `GameEndSummary` 또는 동급 DTO로 제공할 때 연결한다.
3. 확장값이 없을 때는 기록을 만들되 해당 필드는 `unknown` 또는 보수적 fallback으로 남긴다.
4. 저장 모델은 presentation 문구가 아니라 정규화된 값과 짧은 label key를 저장한다.

Acceptance:

- 기록 생성은 GameEnd 이벤트 1회당 1회만 일어난다.
- 같은 판의 victory/defeat 전환 중복 이벤트가 있어도 최근 기록이 중복 저장되지 않는다.
- 저장 모델은 결과 화면 UI 텍스트에 직접 의존하지 않는다.

---

## Execution Plan

### Phase 1. Current Result Contract Audit

- `GameEndEvent`, `GameEndAnalytics`, `WaveGameEndBridge`, `WaveEndView`가 현재 어떤 값을 보유하는지 확인한다.
- `CoreObjectiveSetup`에서 종료 시 코어 잔여율을 안전하게 읽을 수 있는지 확인한다.
- Garage roster handoff와 Lobby/Garage 표시 경로에서 주요 기체명을 얻을 수 있는지 확인한다.
- acceptance: 즉시 사용 가능 필드와 전투 결과 owner 제공이 필요한 필드가 분리된다.

### Phase 2. Operation Record Domain And Port

- `OperationRecord`와 최근 기록 컬렉션을 작은 domain model로 만든다.
- 최대 5개 유지, 최신순 정렬, schema version, unknown field 처리 기준을 둔다.
- 저장/로드 port는 Account/Garage의 기존 port 패턴을 따른다.
- acceptance: 순수 domain/direct test에서 추가, 정렬, 5개 초과 trimming, duplicate guard가 통과한다.

### Phase 3. Local Persistence First

- 첫 구현은 로컬 JSON fallback을 기준으로 최근 5회 기록을 저장한다.
- Firestore 연결은 `Account/Garage` 경로와 충돌하지 않게 별도 문서 또는 stats 확장 후보로 검토한다.
- WebGL에서 로컬 persistence가 제한되면 Account lane의 기존 WebGL 저장 검증과 묶어 blocked/residual로 남긴다.
- acceptance: 로그인/Firestore가 없어도 최근 기록은 로컬에서 저장/복원된다.

### Phase 4. Account Persistence Bridge

- 계정 세션이 있을 때 Firestore 저장을 시도하되, 실패해도 로컬 기록과 Lobby 복귀는 유지한다.
- 기존 `PlayerStats` 통산값과 최근 작전 기록은 역할을 분리한다.
- Firestore schema는 최근 5회 작전 기록만 저장하고, 장기 분석/랭킹용 누적 로그로 확장하지 않는다.
- acceptance: Firestore 성공 시 계정 재진입에서 최근 5회가 복원되고, 실패 시 local fallback reason이 로그로 남는다.

### Phase 5. Lobby/Garage Summary Surface

- Lobby 또는 Garage 한쪽에 최근 작전 기록 요약을 표시한다.
- 요약은 보상/랭킹보다 "버틴 흔적"이 먼저 읽히게 한다.
- 첫 표시는 3줄 내외로 제한하고, 상세 모달이 필요하면 후순위로 둔다.
- 예시 문구는 design owner의 용어 기준을 따르되, 임시 기준은 `버텨냈다`, `거점 붕괴`, `코어 잔여율`, `주요 기체`를 사용한다.
- acceptance: 모바일 세로 Lobby/Garage에서 기록 요약이 기존 roster/save/settings 흐름을 가리지 않는다.

### Phase 6. Result Flow Integration

- 전투 종료 -> 기록 생성 -> 저장 시도 -> 결과 화면/Lobby 복귀 흐름이 끊기지 않게 연결한다.
- 결과 화면 기여 카드와 작전 기록은 같은 source summary를 소비하지만, 서로의 UI 상태에 의존하지 않는다.
- acceptance: victory와 base collapse 양쪽에서 기록이 1건 추가되고 최근 5회 표시가 갱신된다.

### Phase 7. Validation And Closeout

- direct tests: record trimming, duplicate guard, serialization, load fallback.
- Play Mode smoke: GameEnd 발생 후 record 생성, Lobby/Garage 표시.
- WebGL smoke: local restore, 계정 세션이 있을 때 Firestore restore 시도.
- docs: 실제 acceptance가 바뀌면 `progress.md`를 짧게 갱신한다.
- acceptance: mechanical pass와 실제 UI/저장 smoke가 분리되어 보고된다.

---

## Current Implementation Evidence

2026-04-26 implementation pass:

- Code path: `OperationRecord`, `RecentOperationRecords`, `OperationRecordFactory`, `SaveOperationRecordUseCase`, `OperationRecordGameEndHandler`, `OperationRecordJsonStore`가 추가됐다.
- Code path: `GameSceneRoot`가 `GameEndReportRequestedEvent`를 소비해 최근 작전 기록을 local JSON으로 저장하는 handler를 조립한다.
- Code path: `GarageSetup`이 local 최근 작전 기록을 읽고 `GaragePagePresenter`가 기존 Garage result stats text에 최근 작전 2줄 요약을 표시한다.
- Code path: `FirestoreRestPort`가 `operations/recent` 문서 저장/로드 포트를 구현했고, `GarageSetup`은 local record를 먼저 표시한 뒤 계정 세션이 있으면 cloud/local 최근 5회를 merge해서 local과 Firestore에 다시 저장한다.
- Direct test coverage: `OperationRecordDirectTests`가 최근 5회 trimming, duplicate replacement, report-to-record mapping, JSON roundtrip, same-session duplicate guard를 검증하도록 추가됐다.
- Direct test coverage: `GaragePagePresenterDirectTests`가 최근 작전 요약 표시와 기록 없음 fallback을 검증하도록 확장됐다.
- Direct test coverage: `OperationRecordDirectTests`와 `FirestoreMapperDirectTests`가 cloud/local merge와 Firestore raw-json mapper roundtrip을 검증하도록 확장됐다.
- Compile validation: `tools/check-compile-errors.ps1` pass with `ERRORS: 0`, `WARNINGS: 0`.
- Docs validation: `npm run --silent rules:lint` pass.
- UI workflow validation: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` pass.
- Play Mode smoke: `LobbyScene -> BattleScene` defeat flow produced `recent_operation_records.json` under `LocalLow/DefaultCompany/MakeSD` with one `baseCollapsed` record: reached wave 1, core 0%, survival about 45s.
- Play Mode UI smoke: reopening Lobby/Garage and invoking `/LobbyCanvas/LobbyGarageNavBar/GarageTabButton` showed active Garage stats text with `최근 작전: 거점 붕괴 | 공세 1 | 코어 0%` and `기록 1/5 | 작전 시간 0:45`.
- Console validation during UI smoke: recent MCP console errors count was 0.
- Mobile visibility follow-up: operation summary copy was compressed to one line and Garage chrome initialization now guards pre-`Initialize` state, so Garage tab activation no longer throws `SyncChrome` NRE in the checked path.
- Mobile visibility follow-up: `MobileSaveStateText` can now receive the operation summary when its normal save/status text would otherwise be empty.
- Validation after follow-up: `tools/check-compile-errors.ps1` pass with `ERRORS: 0`, `WARNINGS: 0`; `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1` passed.
- Validation after Firestore bridge: `tools/check-compile-errors.ps1` pass with `ERRORS: 0`, `WARNINGS: 0`; `npm run --silent rules:lint` pass.
- MCP account bridge smoke: Lobby Play Mode reused persisted anonymous session, loaded local operation records, saw Firestore `operations/recent` missing, then wrote the merged recent record document successfully.
- MCP mobile UI smoke: Garage tab activation showed `MobileSaveStateText` with `작전 2/5: 버텨냄 | 공세5 | 코어46% | 0:42 | 제거27`; GameView screenshot proof was captured at `artifacts/unity/operation-record-garage-summary-mobile-smoke.png`; recent MCP console errors count was 0.

Residuals after this pass:

- Unity EditMode test execution은 Unity Editor가 이미 열려 있어 CLI test route가 `open-editor-owns-project`로 blocked됐다. 테스트 코드는 compile clean까지만 확인됐다.
- WebGL build/player 환경에서 `operations/recent` 저장·복원 smoke는 Account/Garage residual로 남아 있다.

---

## UI Copy Guardrails

이 문서는 용어 SSOT가 아니다.
다만 design owner 기준이 잠기기 전까지 이 구현은 아래 임시 guardrail을 따른다.

- `승리`보다 `버텨냈다`를 우선한다.
- `패배`보다 `거점 붕괴` 또는 `방어 실패`를 우선한다.
- `웨이브`는 내부 용어로 두고, 유저-facing은 `공세`, `침공`, `접근` 후보를 따른다.
- `유닛`은 내부 용어로 두고, 유저-facing은 `기체`를 우선한다.
- 카피 문자열은 저장 데이터에 박지 않고 UI surface/binding 단계에서 변환한다.

---

## Blocked / Residual Handling

- 전투 종료 요약에 코어 잔여율이나 pressure summary가 없으면 MVP 기록은 현재 event 필드로 먼저 만들고, 누락 필드는 residual로 남긴다.
- design owner 용어 기준이 아직 없으면 임시 카피로 표시하되, 저장 schema에 임시 문구를 고정하지 않는다.
- Firestore 계정 경로가 WebGL에서 아직 불안정하면 로컬 최근 5회까지만 acceptance로 닫고 계정 동기화는 `Account/Garage` residual로 남긴다.
- Lobby/Garage visual lane이 Set B final judgment와 충돌하면 기록 표시 prefab 준비까지만 닫고 scene integration을 residual로 남긴다.
- 저장 실패는 전투 결과 흐름을 막지 않는다. 저장 실패 UI를 새로 크게 만들지 않고 로그와 다음 진입 복원 실패로 구분한다.

---

## Closeout Criteria

- 전투 종료 때마다 최근 작전 기록이 1건 생성된다.
- 최근 기록은 최대 5개만 유지되고 최신순으로 복원된다.
- 성공/실패, 생존 시간, 도달 공세, 소환/제거 수, 주요 기체 후보가 표시된다.
- 코어 잔여율과 pressure summary는 실제 source가 있으면 표시되고, 없으면 unknown/residual로 분리된다.
- Lobby 또는 Garage에서 최근 기록 요약이 모바일 세로 기준으로 보인다.
- 로컬 저장/복원 direct test가 통과한다.
- 계정/Firestore 저장은 성공하거나, WebGL/Account blocker가 owner와 함께 남는다.
- 결과 화면 기여 카드, 기체 전적 태그, 용어 SSOT와 owner가 섞이지 않는다.

---

## Lifecycle

- reference 전환 이유: 최근 5회 작전 기록 생성, local-first 저장, Garage 표시, Firestore bridge code path, Play Mode/Garage mobile smoke evidence가 남았고, 남은 EditMode 실행과 WebGL player restore는 blocked/residual owner가 명확하다.
- 남은 residual owner: Unity EditMode test route는 `open-editor-owns-project` 해소 뒤 확인하고, WebGL player Firestore restore는 shared `Account/Garage` validation lane에서 본다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

---

## 문서 재리뷰

- 과한점 리뷰: 세계관/용어 규칙 본문을 새로 소유하지 않고, 최근 작전 기록 실행 계획과 owner boundary만 정리했다.
- 부족한점 리뷰: owner, scope, 제외 범위, MVP record shape, 실행 순서, acceptance, validation, blocked/residual, lifecycle을 포함했다.
- 수정 후 재리뷰: HUD/input plan과 이름 충돌을 본문에서 제거했고, Firestore 확장을 MVP 필수로 만들지 않도록 local-first로 낮췄다.
- 반복 재리뷰 반영: obvious 과한점/부족한점 없음.
- owner impact: primary `plans.operation-record-world-memory`; secondary `plans.progress`, `docs.index`, `plans.account-system`, `design.game-design`, `design.world-design`; out-of-scope result card implementation, terminology/unit attachment implementation, Account recovery backlog.
- doc lifecycle checked: 새 active plan으로 등록한다. 이 문서는 작전 기록 lane만 소유한다.
- plan rereview: clean
- 2026-04-26 구현 증거 반영 후 과한점 리뷰: 최근 작전 기록 local-first code path와 residual만 추가했고, Firestore schema/UI layout/용어 규칙은 이 문서에서 새로 확정하지 않았다.
- 2026-04-26 구현 증거 반영 후 부족한점 리뷰: compile/docs lint 증거, EditMode test blocked reason, Lobby/Garage 표시와 Firestore/WebGL residual을 분리했다.
- 2026-04-26 구현 증거 반영 후 재리뷰: plan rereview: clean.
- 2026-04-26 Garage 요약 표시 반영 후 과한점 리뷰: 새 prefab/scene authoring이나 새 상세 모달을 만들지 않고 기존 result stats text에 최소 요약만 붙였다. Firestore, WebGL, design owner 용어 SSOT는 그대로 residual/out-of-scope다.
- 2026-04-26 Garage 요약 표시 반영 후 부족한점 리뷰: compile, docs lint, Unity UI workflow policy, direct test coverage, 실제 UI smoke 미판정 residual을 구분했다.
- 2026-04-26 Garage 요약 표시 반영 후 재리뷰: plan rereview: clean.
- 2026-04-26 Play Mode smoke 반영 후 과한점 리뷰: actual record 생성과 Garage text-state 표시 증거만 추가했고, screenshot/Firestore/WebGL을 success로 승격하지 않았다.
- 2026-04-26 Play Mode smoke 반영 후 부족한점 리뷰: EditMode test route blocked, screenshot capture 409, Firestore/WebGL restore residual을 남겨 mechanical evidence와 acceptance residual을 분리했다.
- 2026-04-26 Play Mode smoke 반영 후 재리뷰: plan rereview: clean.
- 2026-04-26 mobile visibility follow-up 과한점 리뷰: 기록 표시 카피와 Garage chrome null guard만 반영했고, scene/prefab layout acceptance나 Firestore/WebGL을 success로 승격하지 않았다.
- 2026-04-26 mobile visibility follow-up 부족한점 리뷰: screenshot visual proof가 아직 Lobby/inactive-root automation residual이라 acceptance residual로 남겼다.
- 2026-04-26 mobile visibility follow-up 재리뷰: plan rereview: clean.
- 2026-04-26 Firestore bridge follow-up 과한점 리뷰: 기존 Account/Garage Firestore raw-json document 패턴을 재사용했고, 통산 전적/랭킹/새 UI를 확장하지 않았다.
- 2026-04-26 Firestore bridge follow-up 부족한점 리뷰: code path와 mapper/merge 검증은 추가됐지만 WebGL 실계정 smoke와 모바일 screenshot acceptance는 residual로 남겼다.
- 2026-04-26 Firestore bridge follow-up 재리뷰: plan rereview: clean.
- 2026-04-26 MCP smoke follow-up 과한점 리뷰: 기존 Garage 모바일 dock과 Account Firestore 경로만 검증/보정했고, 새 UI surface나 WebGL acceptance로 범위를 넓히지 않았다.
- 2026-04-26 MCP smoke follow-up 부족한점 리뷰: Editor Play Mode 계정 저장과 모바일 screenshot proof는 닫혔지만 WebGL player restore와 Unity EditMode test route는 residual로 남겼다.
- 2026-04-26 MCP smoke follow-up 재리뷰: plan rereview: clean.
- 2026-04-28 lifecycle cleanup 재리뷰: 과한점은 새 실행 계획을 만들지 않고 완료 증거와 residual owner를 기준으로 reference 전환만 수행했다. 부족한점은 WebGL player restore와 EditMode 실행 blocked를 Lifecycle에 남겨 해소했다.
- doc lifecycle checked: active 실행 계획에서 reference closeout 기록으로 전환한다.
- plan rereview: clean
