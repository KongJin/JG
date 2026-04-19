# 진행 상황 (Game Scene Entry)

> **마지막 업데이트**: 2026-04-19

## 상태 주석

- 중요: Phase 0~9의 `완료` 표기는 주로 코드 경로 기준이다.
- 2026-04-18 확인 시 실제 `Assets/Scenes/GameScene.unity` 씬 에셋은 삭제된 상태였고, 플레이 가능한 전투 씬 완성도와 문서 표기가 어긋나 있었다.
- 현재는 새 hand-authored `GameScene.unity`와 최소 `BattleEntity.prefab`을 다시 만들었고, build settings 등록과 required-field audit까지 복구했다.
- `CodexLobbyScene -> GameScene` end-to-end summon smoke는 통과했다.
- click summon 기준 `GameScene` wave/core/victory loop는 다시 통과했고, 현재 남은 핵심 리스크는 placement drag/drop 자동화와 멀티플레이 동기화 smoke다.

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Setup, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | ✅ 완료 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (드래그+클릭), 로테이션 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy → Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | ✅ 완료 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster, BattleEntity late-join HP sync |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀, Firebase Analytics |
| Phase 7: 배치 시스템 완성 | ✅ 완료 | PlacementArea, PlacementAreaView, 드래그 피드백, 영역 검증, MaterialFactory, ErrorView |
| Phase 8: Energy 재생 증가 곡선 | ✅ 완료 | EnergyRegenCurve (시간 기반 60s→180s, 3→5/s), EnergyRegenCurveConfig, TickRegen wiring |
| Phase 9: 네트워크 완성 | ✅ 완료 | BattleEntityPhotonController (IPunObservable HP/pos/dead sync), BattleEntityDespawnAdapter, WaveEndView 통계 |
| Phase 10: 계정 시스템 | 🟨 복구 진행 중 | Firestore/Garage 저장·로드·삭제·재시도 핵심 경로는 연결됐고, 남은 과제는 WebGL 실기 검증과 설정 동기화, 계정 UX 마무리 |
| Phase 11: Google 로그인 | 🟨 실동작 검증 전 | Google linking 경로 코드는 존재하지만 UID 유지와 WebGL 실기 동작은 아직 검증되지 않음 |

## 미완료 TODO

- GameScene rebuild: placement area drag/drop, wave start, core victory/defeat loop 검증
- GameScene rebuild: 멀티플레이 smoke로 late-join, BattleEntity sync, Energy sync 확인
- Phase 9: 실제 멀티플레이어 smoke 테스트 (late-join, BattleEntity sync, Energy sync)
- Phase 10: Firebase Console 설정 (API Key, Project ID, Firestore DB 생성)
- Phase 10: 설정 Firestore 동기화 마무리 (저장 UI, language 소비 경로)
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: Garage save/load WebGL 실기 확인 후속 1회 더 재현
- Phase 10: Garage 수동 저장 UX 2차 폴리시 (슬롯 카드/결과 패널/계정 카드 완성도)
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명→Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업 메모

- `CodexLobbyScene` 로비/Garage 대시보드 리팩터링 2차: 시각 polish와 상호작용 smoke 보강 필요
- `CodexLobbyScene` mobile polish 2차: `Open Rooms` empty-state hierarchy와 Garage save dock first-screen visibility는 계속 sanity check 대상
- `CodexLobbyScene` Lobby shell은 `LobbyHeaderCard -> RoomsSectionCard -> CreateRoomCard -> GarageSummaryCard` 구조로 재정리했고, 다음 polish는 mobile-first Garage 단일 구조의 시각 밀도 튜닝 중심으로 이어가기
- Lobby/Garage UI layout SSOT는 `CodexLobbyScene.unity`와 관련 prefabs만 유지하고, 코드-driven builder/rebuild 경로는 재도입하지 않기
- 열린 `CodexLobbyScene.unity` 디스크 덮어쓰기는 금지하고, 복구는 기본값으로 MCP repair를 사용하기
- Lobby/Garage 검증 레이어 재배치 기준: `contract -> EditMode/unit tests -> 얇은 smoke`
- `GarageReadyFlow`는 필수 회귀 gate가 아니라 optional supervised smoke로 유지하고, Ready/Save 규칙은 EditMode 테스트로 계속 이동
- 외부 디자인 시안 도입 기준: `Stitch`를 기본 생성 도구로 두고, 실제 반영은 Unity MCP와 scene contract 기준으로 번역
- Garage UI 레이아웃 SSOT: [`ui_foundations.md`](../design/ui_foundations.md)
- Garage UI 상세 계획: [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md)
- GameScene 진입 계획: [`game_scene_entry_plan.md`](./game_scene_entry_plan.md)
- GameScene UI/UX 상세 계획: [`game_scene_ui_ux_improvement_plan.md`](./game_scene_ui_ux_improvement_plan.md)
- GameScene UI authoring 기본 경로도 `GameScene.unity` / 관련 prefabs 대상 MCP repair를 사용하고, code-driven builder/rebuild는 재도입하지 않기
- 계정 시스템 상세 계획: [`account_system_plan.md`](./account_system_plan.md)
- 기술부채 감축 실행 계획: [`tech_debt_reduction_plan.md`](./tech_debt_reduction_plan.md)
- WebGL 실기 체크리스트: [`webgl_smoke_checklist.md`](./webgl_smoke_checklist.md)
- 다음 세션 시작점: `CodexLobbyScene -> GameScene` summon + wave/core victory smoke 통과 상태에서 placement drag/drop automation contract와 multiplayer sync를 이어서 검증하거나, `game_scene_ui_ux_improvement_plan.md` 기준으로 전투 HUD/소환 UX 재설계를 시작

### 최근 변경 사항

### 2026-04-19

- done: Garage desktop/mobile 분기 제거
  - done: `GaragePageController`의 breakpoint, responsive controller, desktop host 복원 로직 제거
  - done: Garage shell을 mobile-first 단일 구조로 고정하고, 런타임에서 항상 `GarageMobileStackRoot` + `MobileSaveDock` 기준으로 배치
  - done: `GarageUnitEditorView`, `GaragePartSelectorView`의 responsive typography/state를 mobile 기본값으로 단순화
  - done: contract/docs에서 `_responsiveRoot`, `_desktopContentRoot`, `_desktopSlotHost`, desktop/mobile 이중 레이아웃 설명 제거

- done: mobile Garage redesign implementation 시작점 반영
  - done: `GaragePageController` 모바일 흐름을 `Edit/Preview/Summary` 탭 전환에서 `slot first -> single scroll body -> fixed save dock` 구조로 전환
  - done: 모바일 탭 바를 `Frame / Weapon / Mobility` 포커스 바로 재사용하고, 모바일에서는 선택한 부위 selector 하나만 크게 노출하도록 `GarageUnitEditorView`/`GaragePartSelectorView` responsive state 추가
  - done: 저장 완료 후 모바일 scroll body를 상단으로 복귀시키고, inline save는 모바일에서 숨기고 `MobileSaveDock`만 메인 CTA로 유지
  - done: `GarageSlotItemView` 상태 라벨 가시성 강화, `CodexLobbySceneContract` sentinel에 `MobileBodyScrollContent` 추가, `ui_foundations.md` 모바일 Garage 계약 갱신
  - verify: `scene/verify-codex-lobby-contract` success, canonical page-switch smoke success (`warningCount = 3`, `errorCount = 0`), settings overlay smoke success (`errorCount = 0`)
  - evidence: `artifacts/unity/lobby-garage-page-switch-result.json`, `artifacts/unity/garage-settings-smoke-result.json`
  - note: 자동 smoke 캡처는 현재 desktop GameView 기준이라 모바일 390x844 시각 검증은 후속 전용 캡처가 필요

- note: mobile Garage redesign implementation brief를 외부 메모로 정리했고 현재 저장 위치는 `C:\Users\SOL\Downloads\PLAN.md`
- note: session compact handoff - 다음 구현은 `mobile Garage only` 범위로 진행
  - current: Lobby 상단 `TopGlow`와 `LobbyHeaderCard`는 제거했고, `CodexLobbyScene` contract와 page-switch smoke는 최근 기준 통과 상태
  - decision: 모바일 Garage는 `slot first -> single scroll body -> fixed save dock` 구조로 재설계하고, 기존 mobile `Edit / Preview / Summary` 탭 기본 흐름은 폐기
  - decision: 첫 화면은 슬롯 섹션 우선 노출, 스크롤 시 슬롯은 위로 사라져도 됨, 저장 후에는 자동으로 상단 슬롯 영역으로 복귀
  - decision: 본문은 `부위 선택 -> 상위 3~5개 파츠 카드 -> 전체보기 bottom sheet -> dual preview(편집 부위 / 최종 유닛)` 순서로 번역
  - next: `GaragePageController` 모바일 분기와 `CodexLobbyScene` mobile hierarchy를 위 계약 기준으로 함께 수정하고, 새 mobile refs/sentinels가 생기면 `CodexLobbySceneContract`와 `ui_foundations.md`까지 동기화
  - verify: `contract verify -> page-switch smoke -> mobile first-screen / save-dock / save-return-to-top` 순서로 재검증

- done: CodexLobby mobile contract cleanup 2차
  - done: `Garage Settings overlay`를 별도 smoke(`Invoke-GarageSettingsOverlaySmoke.ps1`) 대상으로 분리하고, README/ui foundations에 auxiliary panel contract를 반영
  - done: `GaragePageController`, `RoomListView`, `RoomDetailView`, `RoomItemView`, `GarageSlotItemView`, `GarageUnitPreviewView`, `AccountSettingsView` 등 UI presentation 레이어 전반에서 `serialized self-member null-check`를 줄이고 `Required + capability helper` 기준으로 재정렬
  - done: `CodexLobbySceneContract` reference checks를 새 mobile contract(`SettingsButton`, `GarageSettingsOverlay`, `MobileSaveDock`, mobile tab/save labels`) 기준으로 보강
  - observed: canonical page-switch smoke 재통과 - `warningCount = 3`, `errorCount = 0`
  - evidence: `artifacts/unity/lobby-page-smoke-lobby-initial.png`, `artifacts/unity/lobby-page-smoke-garage.png`, `artifacts/unity/lobby-garage-page-switch-result.json`

- done: GameScene builderless authoring route 고정
  - done: `Assets/Editor/SceneTools/GameSceneBuilder.cs` 제거 - GameScene UI/HUD restyle의 기본 경로를 code-driven rebuild가 아니라 `GameScene.unity` / 관련 prefab 대상 MCP repair로 재고정
  - done: `game_scene_ui_ux_improvement_plan.md`와 `tools/unity-mcp/README.md`에 같은 원칙을 명시해 GameScene UI 작업도 builderless route로 고정
  - note: 기존 progress의 builder 관련 항목은 당시 복구 이력으로만 남기고, 이후 authoring 기준으로 재사용하지 않음

- done: CodexLobby mobile-first layout pass 1차
  - done: `CodexLobbyScene` Garage를 mobile host 구조(`GarageMobileStackRoot / MobileBodyHost / MobileSlotGrid / MobileSaveButton`)로 재정리하고, `GaragePageController`가 pre-authored desktop/mobile hosts 사이에서 pane parent 전환과 `Edit / Preview / Summary` 탭 전환만 담당하도록 보강
  - done: Lobby `CreateRoomCard` label/field 세로 흐름과 `GarageSummaryCard` 밀도를 다시 다듬어 390 폭에서 읽기 순서 안정화
  - done: canonical page-switch smoke 재검증 통과 - `warningCount = 2`, `errorCount = 0`
  - evidence: `artifacts/unity/lobby-page-smoke-lobby-initial.png`, `artifacts/unity/lobby-page-smoke-garage.png`, `artifacts/unity/lobby-garage-page-switch-result.json`
  - note: 모바일 Garage는 이제 `slot selector -> Edit/Preview/Summary -> save dock` 흐름으로 읽히지만, Preview/Summary 탭별 미세 간격과 1440 desktop visual sanity는 후속 polish 여지가 남음

- done: GameScene rebuild 1차 복구
  - done: `Assets/Scenes/GameScene.unity`를 새 hand-authored scene으로 재도입
  - done: `Assets/Resources/BattleEntity.prefab` 추가 - summon 경로가 Photon instantiate 할 수 있는 최소 battle entity prefab 복구
  - done: `Assets/Editor/SceneTools/GameSceneBuilder.cs` 추가 - GameScene, HUD, core objective, wave/unit/garage/combat setup, summon UI를 editor menu로 재생성 가능하게 정리
  - done: `ProjectSettings/EditorBuildSettings.asset`에 `GameScene` 재등록
  - done: `GameSceneRoot` contract 정리 - `_localPlayerSetup`을 runtime-arrived 참조로 낮추고, `CoreObjectiveSetup.InitializePlacementArea()` 호출 추가
  - done: Unity MCP `Tools/Codex/Build Game Scene` 실행 후 `GameScene` active/open 확인
  - done: Unity MCP `Tools/Audit Required Fields In Project` 통과 - `[RequiredAudit] All required fields are assigned across scenes and prefabs.`
  - observed: `GameScene` 단독 Play Mode에서는 expected warning `You are not connected to a room`만 확인됐고, missing reference/null wiring error는 0건
  - note: 아직 실제 room start 기반 summon PvE smoke와 멀티플레이 smoke는 남아 있음

- done: GameScene summon smoke 1차 성공
  - done: `GarageNetworkAdapter`가 새 씬 진입 시 기존 Photon player custom properties를 hydrate하도록 보강 - room roster handoff 누락으로 `Computed 0 unit specs`가 나오던 문제 복구
  - done: `InitializeGarageUseCase`가 restore한 committed roster를 Photon custom properties로 다시 sync하도록 보강 - manual save 없이도 room start 이후 GameScene이 같은 roster를 읽을 수 있게 정리
  - done: `PlayerSetup.LocalArrived` 타이밍을 Photon local instantiation 시점으로 교정 - `GameSceneRoot` local bootstrap이 실제로 실행되도록 복구
  - done: `WaveNetworkAdapter` match reset + `PlacementAreaMaterialFactory`/`GameSceneBuilder` shader fallback 정리 - pink ground와 이전 매치 wave state 잔재 제거
  - done: `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1` 추가 - lobby join, room create, ready, start game, summon click, battle entity 존재 확인, screenshot/report 생성을 한 번에 수행
  - observed: MCP summon smoke 실행 성공 - `CodexLobbyScene -> GameScene` 진입 후 `/HudCanvas/UnitSummonUi/SlotRow/UnitSlotTemplate(Clone)`에 `/ui/invoke`를 보내 `BattleEntity(Clone)` 생성 확인
  - observed: latest smoke 기준 `recentErrorCount = 0`, `[GameSceneRoot] Computed 3 unit specs for player player-1`, spawned unit path `/RuntimeRoot/UnitsRoot/BattleEntity(Clone)`
  - evidence: `artifacts/unity/game-scene-summon-smoke.png`, `artifacts/unity/game-scene-summon-smoke-result.json`
  - note: GameView 좌표 기반 `/input/click`, `/input/drag`는 아직 불안정해서 현재 stable automation contract는 `/ui/invoke` 기반 summon smoke다

- done: GameScene wave/core outcome smoke 2차 검증
  - done: `tools/unity-mcp/Invoke-GameScenePlacementWaveSmoke.ps1`를 추가/보강해 placement drag 시도, click summon fallback, wave/core polling, end overlay 관찰, screenshot/report 생성을 한 번에 수행
  - done: `BattleEntityPrefabSetup`에 master-only fallback auto-attack loop를 추가해 local summon entity가 enemy kill -> wave advance -> victory path를 실제로 밟도록 보강
  - observed: latest placement smoke 기준 `/input/drag`는 아직 `dragDidSummon = false`, `dragPlacementErrorText = "배치 영역 밖입니다!"`로 실패했고 automation contract 안정화가 남아 있음
  - observed: 같은 smoke에서 click summon 기준 `Wave 5/5`, `waveEndOverlayActive = true`, `outcomeResultText = "Victory!"`, `returnToLobbyButtonActive = true`, `recentErrorCount = 0` 확인
  - observed: `coreHpAfterWait = "1454 / 1500"`였고 recent logs에 `[FirebaseStub] game_end {"result":"Victory!"...}`와 `Summons: 2`, `Unit Kills: 27`가 남아 wave/core/end overlay loop가 끝까지 닫히는 것을 재확인
  - evidence: `artifacts/unity/game-scene-placement-initial.png`, `artifacts/unity/game-scene-placement-after-drag.png`, `artifacts/unity/game-scene-placement-final.png`, `artifacts/unity/game-scene-placement-wave-result.json`

- done: 구조 복잡도 / lifecycle seam 제거 1차
  - done: `AuthTokenProvider` 정적 우회를 제거하고 `FirebaseAuthRestAdapter -> FirestoreRestPort` injected session access로 교체
  - done: `FirestoreRestPort`, `FirebaseAuthRestAdapter`, `GaragePageController`의 helper/transport/mapping 책임을 별도 파일로 분리해 orchestration 위주로 축소
  - done: `PlayerSetup.LocalArrived/RemoteArrived`와 `EnemySetup.EnemyArrived` 정적 이벤트를 scene-local registry 기반 arrival 흐름으로 교체
  - done: 사용처가 없던 `BattleEntityArrived` 정적 seam 제거
  - done: `GameSceneRoot`에서 `SoundPlayer.Instance` 직접 참조를 제거하고 runtime audio host 조회를 `IAudioRuntimePort` 내부로 한정
  - note: `SoundPlayer` 자체는 아직 DDOL host로 남아 있어, scene-owned audio host로 완전 교체하는 후속 패스는 남아 있음

- done: 구조 복잡도 / lifecycle seam 제거 2차
  - done: `GameSceneRoot` 내부 helper(`garage bootstrap`, `player connector`, `audio bootstrap`)를 별도 파일로 이동해 scene root를 orchestration 중심으로 축소
  - done: `WaveSetup`의 enemy arrival fallback 배선을 `WaveEnemyArrivalCoordinator`로 분리
  - done: `SoundPlayerRuntimeConfig` resource asset과 `SoundPlayerRuntimeHostFactory`를 추가해 `GameScene`이 로비 생성 DDOL 없이도 자기 runtime audio host를 직접 올릴 수 있게 정리
  - note: `SoundPlayer`의 내부 수명주기 구현은 아직 DDOL 기반이므로, 완전한 scene-owned audio lifecycle까지는 후속 정리가 더 필요함

- done: 구조 복잡도 / lifecycle seam 제거 3차
  - done: `LobbyPhotonAdapter` 안에 함께 붙어 있던 pending state, room mapper, command validator, callback translator를 별도 파일로 분리
  - done: `LobbySetup` 내부 helper(`LobbyAccountBootstrapFlow`, `LobbySceneInitializationFlow`)를 파일 분리해 setup 본체를 orchestration 중심으로 축소
  - done: `SoundPlayer`에서 `static Instance`를 제거하고 runtime duplicate 검사를 scene scan 기반으로 변경
  - note: `SoundPlayer`는 더 이상 전역 인스턴스 프로퍼티를 노출하지 않지만, `DontDestroyOnLoad` 자체는 아직 유지 중이어서 lifecycle debt가 완전히 끝난 것은 아님

- done: Lobby/Garage UI polish 1차
  - done: `CodexLobbySceneBuilder`, `CodexLobbyGarageAugmenter` 기준으로 헤더/탭 밀도, Garage 슬롯 폭/높이, selector card, result CTA 카피를 1차 정리
  - done: `GaragePagePresenter`, `GarageSlotItemView`, `GaragePartSelectorView`, `GarageResultPanelView`에서 슬롯/결과 문구와 타이포를 더 짧고 읽기 쉽게 조정
  - done: MCP Lobby/Garage overview capture 재검증 통과 - `errorCount = 0`, `warningCount = 0`
  - evidence: `artifacts/unity/ui-overview-lobby.png`, `artifacts/unity/ui-overview-garage.png`, `artifacts/unity/ui-overview-report.json`
  - done: `GarageUnitPreviewView` lazy render-texture 보장, runtime camera render, inactive clone 활성화, camera framing/scale 조정으로 preview card 실제 렌더 복구
  - note: preview는 이제 실제로 보이지만 카드 안에서 더 크게 보이도록 하는 시각 polish는 후속 여지 있음
  - done: Lobby/Garage 구조를 dual-workspace dashboard에서 page switcher로 전환 - Lobby는 `Garage` 진입 버튼만, Garage는 `Back To Lobby` 복귀 버튼만 노출
  - evidence: `artifacts/unity/lobby-page-capture.png`, `artifacts/unity/garage-page-capture.png`
  - done: Unity MCP workflow stabilization 1차 - overview/manual capture 경로를 page-switcher 구조로 교정하고, `Invoke-LobbyGaragePageSwitchSmoke.ps1`를 추가해 Lobby-only -> Garage-only -> Lobby-only 전환을 한 번에 재현 가능하게 정리
  - done: `tools/unity-mcp/README.md`에 builder 변경 루틴(`Assets/Refresh -> Build Codex Lobby Scene -> scene/save -> Play Mode smoke`)과 stale editor assembly 진단 힌트를 명시
  - note: Unity MCP 관련 문서/스크립트에서 `TopTabs` 기본 경로 의존성을 제거해 현재 scene contract와 맞췄음
  - done: Lobby room-list visibility polish - `RoomListPanel`에 `Open rooms` count badge와 빈 상태 카피를 추가해 방이 없을 때도 목록 영역이 공백으로 보이지 않도록 정리
  - done: Unity MCP recurrence fix 1차 - `CodexLobbyScene.unity`를 Lobby/Garage UI의 최종 SSOT로 명시하고, `CodexLobbySceneContract`로 sentinel node/serialized ref 검증 경로 추가
  - done: dedicated verified rebuild route 추가 - `/scene/rebuild-codex-lobby`가 direct builder call + contract verify + scene save 상태를 구조화된 응답으로 돌려주도록 정리
  - done: `/scene/verify-codex-lobby-contract` 추가 - builder와 scene drift를 machine-readable contract report로 바로 확인 가능하게 정리
  - done: `/health`에 pending play action, pending age, last asset refresh, last script reload, last dedicated rebuild 상태 노출 추가
  - done: `play/start`, `play/stop` idempotent 처리와 pending action 힌트 보강 - 불필요한 ambiguous pending failure를 줄이도록 정리
  - done: `Invoke-CodexLobbyUiWorkflowGate.ps1` 추가 - compile/reload stabilization, verified rebuild, contract verify, page-switch smoke를 직렬 게이트로 묶음
  - note: 이제 Lobby/Garage UI 작업의 권장 검증 루틴은 generic `menu/execute`가 아니라 verified rebuild route + workflow gate 조합이다
  - done: MCP trim 1차 - `McpWorkflowState` 실험적 상태 캐시와 rebuild response의 중복 메타(`verified`, `compileStateBefore/After`, `manualOnly`)를 제거하고, gate는 성공 여부만으로 판정하도록 단순화
  - done: MCP trim 2차 - Lobby/Garage 기준 스크립트를 `workflow gate -> page-switch smoke -> feature smoke` 구조로 축소하고, overview/manual smoke 제거, helper 공용화, JSON 리포트 최소화
  - done: MCP validation ownership 재배치 - `CodexLobbySceneContract`와 required-field audit가 wiring/structure를 맡고, canonical smoke는 page activation과 scene transition만 보도록 기준 정리
  - done: `GarageReadyFlow`를 필수 regression gate에서 제외하고, Ready/Save 세부 판정은 EditMode reflection tests로 이동 시작
- done: 외부 UI 시안 워크플로우 2차 - `Stitch`를 기본 생성 도구로 재정의하고, `docs/design/ui_reference_workflow.md`를 Stitch-first 기준으로 갱신
  - done: `CodexLobby` builderless 전환 1차 - `CodexLobbySceneBuilder`와 `/scene/rebuild-codex-lobby` 의존을 제거하고, 공식 루프를 `contract verify -> workflow gate -> page-switch smoke`로 재고정
  - done: Unity MCP 기준을 scene/prefab direct repair로 통일 - Lobby/Garage layout 복구는 builder가 아니라 MCP scene/prefab 수정으로 수행
- done: open-scene disk-write guard 추가 - `Assert-McpNoOpenSceneDiskWrite` helper와 문서 규칙으로 열린 `CodexLobbyScene.unity` 외부 덮어쓰기를 SSOT 위반으로 명시
- done: Lobby scene UI/UX rework 1차 - `CodexLobbyScene`를 MCP로 직접 수정해 `LobbyHeaderCard / RoomsSectionCard / CreateRoomCard / GarageSummaryCard` 구조로 재편하고, Lobby-owned `LobbyGarageSummaryView`를 추가해 Garage 저장 상태와 `Open Garage` CTA를 로비 메인에 요약 노출
- done: canonical smoke/feature smoke path refresh - `GarageSummaryCard/GarageTabButton`과 `CreateRoomCard/*` 기준으로 page-switch smoke와 lobby->game summon smoke 기본 경로를 갱신

- done: Account delete WebGL smoke 1차 성공
  - done: `AccountSettingsView`에 development build 전용 `WebglSmokeDeleteAccount*` 엔트리포인트 추가 - browser automation이 `AccountCard`를 직접 두 단계 delete/confirm 호출할 수 있게 정리
  - done: `tools/webgl-smoke/account-delete-smoke.cjs` 추가 - Playwright 기반으로 delete confirm, Firestore delete, Firebase Auth delete, 재진입 UID 변경까지 자동 수집
  - done: 루트 `package.json` + `playwright` dev dependency + Chromium 설치 경로 정리 - `npm run webgl:smoke:account-delete -- <url>`로 smoke 실행 가능 상태 확보
  - done: fast WebGL rebuild + Firebase preview `https://projectsd-51439--qa-362a4g3j.web.app` 재배포 후 실기 검증 성공
  - observed: 초기 UID `HcJmvfZWE8YbSswRW5H9ka2aAZr1` -> 삭제 후 재진입 UID `5SSQLw6LZGX44AB9PSCFnwIpNTK2`로 변경 확인
  - observed: Firestore `accounts/{uid}` 하위 `profile / stats / settings / garage` delete `200` 4건, Firebase Auth `accounts:delete` `200`, localStorage auth 키 clear 확인
  - evidence: `artifacts/webgl/account-delete-smoke-result.json`, `account-delete-before.png`, `account-delete-after-delete.png`, `account-delete-after-reload.png`
  - note: WebGL runtime console에는 URP render-pass 관련 에러와 Photon dev-region 경고가 남지만, 이번 account delete flow 자체는 blocker 없이 완료됨
- done: 문서 탐색 인덱스 1차 정리
  - done: `docs/index.md` 추가 - `design / plans / playtest / ops / discussions` 문서 지도를 상태 규칙(`active / draft / paused / historical / reference`)과 함께 정리
  - done: 루트 엔트리포인트에 문서 전체 지도 링크 추가 - `docs/index.md`로 바로 내려갈 수 있게 연결
  - done: `codex_lobby_garage_panel_plan.md`, `tech_debt_review.md`, `discussion_unity.md`에 historical 표기 추가 - 현재 SSOT/active 문서와 혼동되지 않도록 정리
  - done: `game_scene_entry_plan.md`의 끊어진 전역 규칙 참조를 루트 엔트리포인트 기준으로 교정
- done: UI layout ownership reset 1차 구현
  - done: `LobbyView`, `GaragePageController`에서 runtime `RectTransform` anchor/size 보정 로직 제거 - view는 focus/visibility/render만 담당하도록 정리
  - done: `CodexLobbyGarageAugmenter`, `CodexLobbySceneBuilder`, `CodexLobbyAccountAugmenter`를 scene-owned layout 기준으로 재정비 - builder가 tab/button/canvas-group/account/preview/result wiring까지 직접 생성·연결하도록 복구
  - done: `CodexLobbyScene.unity`의 당시 대시보드 핵심 `RectTransform` 값을 scene SSOT 쪽으로 직접 정렬 - 이후 page-switcher 전환 전 기준으로 lobby/garage 주요 앵커를 런타임 보정 없이 읽히는 값으로 정리
  - done: `ui_foundations.md`와 코드 계약에 "scene owns layout / runtime geometry mutation 금지" 규칙 명시
  - done: 열린 Unity Editor에서 `Tools/Codex/Build Codex Lobby Scene` 재실행 후 overview capture 재검증 - scene-owned right rail(`Account -> Preview -> Result`)이 실제 Play Mode 캡처에 반영됨
  - done: 재검증 캡처 기준 `errorCount = 0`, `warningCount = 0`, benign warning 5건 유지
  - note: `artifacts/unity/ui-overview-garage.png`, `artifacts/unity/ui-overview-lobby.png` 기준으로 right rail 구조는 복구됐지만 preview 내용은 여전히 비어 있음
  - note: Unity MCP `/play/start`는 간헐적으로 timeout이 남아 이번 재검증은 `Edit/Play` 메뉴 경로로 우회 수행함 - bridge play queue 안정화 후속 필요
- done: Unity MCP UI overview capture 경로 추가
  - done: 당시 overview capture 스크립트 추가 - Play Mode 진입, 로그인 오버레이 대기, Lobby/Garage 스크린샷 2장, JSON 리포트 생성을 한 번에 수행
  - done: `tools/unity-mcp/McpHelpers.ps1`에 UI state summary helper와 `Get-McpConsoleSummary` 추가 - full `/ui/state` dump와 중복 로그를 줄인 요약 진단 경로 마련
  - done: 당시 수동 Garage smoke와 `Invoke-GarageReadyFlowSmoke.ps1`에 process-scope execution policy bypass 보강
  - done: `tools/unity-mcp/README.md`를 runtime automation 기준 문서로 보강하고 quick-start, output 계약, 새 helper/overview flow 반영
- done: Garage desktop layout policy 2차 조정
  - done: `ui_foundations.md` Desktop contract에 `AccountCard -> Preview -> Stats/Primary Action` 우측 레일 스택과 탭/타이틀 비침범 규칙 명시
  - done: `LobbyView` top tab anchor를 좌측으로 당기고 dashboard 세로 리듬을 완화해 account card와 탭 충돌을 줄임
  - done: `GaragePageController` desktop anchor를 재배치해 editor 폭을 키우고 right rail을 `account / preview / result`로 재구성
  - done: `GaragePageController`에서 `GarageUnitPreviewView.Initialize()` 호출 누락을 보강
  - note: 재캡처 기준 우측 레일 위계는 개선됐지만 preview 내용은 여전히 비어 보여, 다음 패스는 `GarageUnitPreviewView` 렌더 경로 자체 점검이 필요

### 2026-04-17

- done: Garage save/load WebGL smoke 3차 성공
  - done: `GaragePageController`에 development build 전용 `WebglSmoke*` 메서드 추가 - 브라우저에서 `SendMessage` 기반으로 슬롯 선택/부품 순환/저장을 직접 호출 가능하게 정리
  - done: `tools/webgl-smoke/garage-save-load-smoke.cjs`를 좌표 클릭 기반에서 Unity `SendMessage` 기반으로 전환
  - done: smoke가 슬롯 1~3에 완성 loadout을 채운 뒤 저장하도록 수정 - `ValidateRosterUseCase`의 `3~6기` 조건에 맞게 보강
  - done: QA preview `https://projectsd-51439--qa-362a4g3j.web.app`에서 Firestore `garage/roster` PATCH `200`과 reload 후 GET `200` 실기 확인
  - done: 저장 payload와 reload payload 일치 확인 (`saveAndReloadMatch = true`)
  - note: `artifacts/webgl/garage-save-load-smoke-result.json` 기준 저장 roster는 `frame_striker + fire_pulse + mob_vector` 3기 구성
  - note: WebGL console에는 TMP 한글 fallback 경고가 남음 - smoke blocker는 아니지만 폰트 자산 보강 후속 필요
- done: WebGL 익명 세션 지속성 복구 1차 완료
  - done: `FirebaseAuthRestAdapter`가 WebGL에서 `localStorage + PlayerPrefs`를 함께 사용하도록 보강하고 restore/persist 로그 추가
  - done: `Assets/Plugins/WebGL/AccountStorage.jslib` 추가 - 브라우저 `localStorage` 직접 읽기/쓰기/삭제 브리지 도입
  - done: `Assets/WebGLTemplates/JG/index.html`에 `autoSyncPersistentDataPath: true` 반영 - WebGL 파일 저장 동기화 기본값 보강
  - done: fast WebGL rebuild + Firebase preview 재배포 후 브라우저 저장소에서 `account.auth.*` 키 실제 생성 확인
  - done: 같은 브라우저 `reload`에서 anonymous UID 유지 확인 (`dzKQAAyYrTad865ky4a2Yy3Hc3K3 -> same UID`)
  - note: 기존 blocker였던 "reload 후 새 anonymous account 생성"은 해소됨
- resolved: Garage save/load WebGL smoke 2차 blocker
  - note: 원인은 인증이 아니라 smoke가 1슬롯만 채워 `ValidateRosterUseCase`의 `3~6기` 조건을 만족하지 못한 점이었음
- blocked: Garage save/load WebGL smoke 1차 실행
  - env: fast WebGL build `Build/WebGL`, Firebase preview `https://projectsd-51439--qa-362a4g3j.web.app`
  - done: `tools/webgl-smoke/garage-save-load-smoke.cjs` 추가 - Playwright 기반으로 WebGL 화면 캡처와 Firestore `garage/roster` 네트워크 이벤트 수집
  - done: preview 배포와 익명 로그인, 초기 `garage/roster` 404까지 실기 확인
  - blocked: 같은 브라우저 `reload`인데 익명 UID가 `0vBv6rHj4KOTts6B55u1P69m6Hr1 -> EYfpqMjpOEZfdjZ7rOmFNlxT5Y72`로 바뀜
  - blocked: `garage/roster` PATCH 저장 요청은 잡히지 않았고, smoke는 현재 "same-account reload" 전제에서 막힘
  - note: 우선 수정 포인트는 `FirebaseAuthRestAdapter` - 현재는 리로드 시 기존 refresh token/session 복구 없이 매번 새 anonymous sign-up을 수행하는 상태
  - evidence: `artifacts/webgl/garage-save-load-smoke-result.json`, `garage-save-load-before.png`, `garage-save-load-after-save.png`, `garage-save-load-after-reload.png`
- done: Lobby/Game scene Inspector wiring 1차 실검증
  - done: MCP `Tools/Validate Required Fields`로 활성 `CodexLobbyScene` required field 검증 통과
  - done: MCP `Tools/Audit Required Fields In Project`로 씬/프리팹 전체 required field audit 통과
  - done: `LobbySetup`, `AccountSetup`, `LoginLoadingView`, `LobbyView`, `GaragePageController`, `GarageResultPanelView`, `GarageUnitEditorView`, `GaragePartSelectorView` 핵심 직렬화 참조를 MCP `component/get`으로 교차 확인
  - done: Play Mode start/stop smoke에서 wiring 관련 콘솔 에러 0건 확인
  - note: 런타임 경고는 신규 익명 계정의 Firestore 문서 없음과 Photon development-region 경고뿐이었고, Inspector 누락과는 무관
- done: WebGL smoke 체크리스트 SSOT 추가
  - done: `Garage save/load`, `Account delete`, `Google linking`을 분리한 수동 실기 절차 문서화
  - done: 각 smoke에 기대 결과, 실패 시 수집 로그, 결과 기록 형식을 고정
- done: 기술부채 감축 실행 계획 문서화
  - done: 기술부채 우선순위를 `런타임 검증 -> WebGL smoke -> 테스트 -> async/tooling -> UX polish` 순서로 재정리
  - done: 모호했던 `Garage save/load/delete` 표현을 `Garage save/load`, `Account delete`, `Google linking` WebGL smoke로 분리
  - done: `progress.md` 다음 작업 메모와 용어를 새 plan 기준으로 정렬

- done: Lobby/Garage UI 런타임 탐색 anti-pattern 1차 제거
  - done: `GaragePageController`, `LobbyView`의 name-based `transform.Find(...)` 레이아웃 탐색 제거
  - done: `GarageResultPanelView`, `GarageUnitEditorView`, `GaragePartSelectorView`, `AccountSettingsView`의 `GetComponentInChildren<TMP_Text>()` fallback 제거
  - done: `ButtonStyles`를 명시적 label 주입 방식으로 변경해 공통 헬퍼의 숨은 UI 구조 의존성 제거
  - done: `CodexLobbyScene`에 새 직렬화 참조와 `CanvasGroup` wiring 반영
  - note: 씬 리로드 직후 MCP 응답 정지는 있었지만, 후속 same-day play-mode smoke로 wiring 관련 에러 0건을 재확인함
- done: Garage `AccountCard` MCP polish 1차
  - done: `AccountCard` 배경색을 Garage 카드 톤으로 정리해 상단 흰 블록 제거
  - done: VerticalLayoutGroup padding/spacing/content-size 설정 정리로 카드 높이와 버튼 밀도 안정화
  - done: MCP 재캡처로 개선 상태 확인 (`artifacts/unity/garage-accountcard-pass1b.png`)
  - note: `Rooms` 입력부 간격과 중앙 프리뷰 빈 상태는 다음 패스 대상
- done: Lobby `AccountCard` 최소 상호작용 wiring 1차 복구
  - done: `CodexLobbyScene`의 `AccountSettingsView`에 display name / logout / delete 버튼 참조 연결
  - done: `AccountCard`에 `AccountDisplayNameText`, `AccountLogoutButton`, `AccountDeleteButton` 추가
  - done: 별도 confirm dialog가 없는 현재 씬 구조에 맞춰 delete 버튼 2단계 inline confirm 로직 추가
  - note: 계정 카드 경로는 이제 씬에서 눌러볼 수 있지만, Editor/WebGL smoke는 아직 남아 있음
- done: 로그인 후 Firestore `UserSettings` 소비 경로 1차 연결
  - done: `LobbySetup`이 로그인 직후 로드한 `AccountData.Settings`를 보관하고 Lobby 초기화 시 적용하도록 정리
  - done: `SoundPlayer`에 `masterVolume` 적용 경로 추가 - 이후 재생되는 사운드가 계정 설정 볼륨을 따르도록 연결
  - note: 현재는 `masterVolume`만 실제 런타임 소비 중이며, `language`와 설정 저장 UI는 후속 작업이 필요
- done: `GarageRoster` Unity Test Runner coverage 확대
  - done: `Assets/Editor/Tests/GarageRosterReflectionTests.cs`에 normalize/clone/get-filled-loadouts 검증 추가
  - note: repo 루트 `Tests/`와 별도로, 실제 EditMode Test Runner에서 확인 가능한 반사 기반 회귀 방어선을 넓힘
- done: Garage draft -> Save -> Room Ready smoke 자동화 추가 및 실검증
  - done: `tools/unity-mcp/Invoke-GarageReadyFlowSmoke.ps1` 추가 - room name 입력, 방 생성, 빈 슬롯 자동 채움, draft dirty/save/ready 토글까지 한 번에 검증
  - done: MCP 실기 기준 `Need 1 more saved unit` -> `Ready` -> `Save Garage Draft` -> `Ready` -> `Cancel` 흐름 확인
  - done: `tools/unity-mcp/README.md`에 Ready flow smoke 실행법과 기대 결과 반영
  - note: 현재 계정/Garage core flow는 Editor 플레이모드에서 재현되며, 남은 핵심 리스크는 WebGL 실기와 Google linking 검증
- done: Account/Garage 실제 코드 리뷰 기반으로 계정 시스템 SSOT 문서 정정
  - done: `account_system_plan.md`를 "기능 추가 계획"에서 "복구 계획 SSOT"로 재작성
  - done: Phase 10/11 상태를 실제 코드 기준으로 낮추고 복구 TODO를 `progress.md`에 반영
  - note: 현재 계정 시스템은 골격 구현 상태이며 Garage Firestore 저장/복원, 삭제 REST 형식, 자동 재시도, 닉네임 cooldown, stale 테스트 복구가 남아 있음
- done: Garage-first Figma / handoff SSOT 문서 추가
  - done: `docs/design/ui_foundations.md` 추가 — Garage 레이아웃, 토큰, 컴포넌트, Unity 변환 규칙 SSOT
  - done: 기존 Garage 계획 문서에서 새 SSOT 문서 참조 추가

- done: Account/Garage 복구 1차 구현 완료
  - done: Firestore Garage 저장/로드를 실제 런타임 경로에 연결 (`FirestoreRestPort`, `InitializeGarageUseCase`, `SaveRosterUseCase`, `GarageSetup`)
  - done: Firebase Auth `accounts:delete`를 `idToken` body 형식으로 수정
  - done: 익명 로그인 자동 재시도(최대 3회)와 로그인 후 계정 로드 연결 (`LobbySetup`)
  - done: 닉네임 cooldown timestamp(`lastNicknameChangeUnixMs`) 저장 로직 도입
  - done: stale Garage 테스트를 현행 API 기준으로 정리하고 Unity Test Runner 진입점 추가
  - note: 컴파일 기준 핵심 복구 경로는 연결됐지만 WebGL 실기 검증은 아직 남아 있음

- done: Lobby/Garage UX 전면 리팩토링 1차 구현
  - done: Garage를 `draft + committed roster` 기반 수동 저장 모델로 전환 (`GaragePageState`, `GaragePageController`, `GaragePagePresenter`)
  - done: 로비와 Garage를 동시에 보이는 분할 대시보드형 레이아웃으로 재구성 (`LobbyView` 런타임 레이아웃 조정)
  - done: Ready eligibility를 `saved roster + unsaved changes 없음` 기준으로 재연결 (`RoomDetailView`, `GarageDraftStateChangedEvent`)
  - done: Garage/Lobby 씬 소유권, 저장 계약, 이벤트 흐름을 전역 SSOT와 코드 계약 기준으로 정리
  - done: MCP smoke 재검증 — compile 0, 플레이모드 Garage 캡처로 새 대시보드 레이아웃 반영 확인
  - note: 현재 캡처 기준으로 구조는 바뀌었지만 계정 카드와 카드 디테일은 추가 polish가 필요

### 2026-04-15

- done: Unity MCP 1차 자동화 경로 추가 - Play wait, UI 상태 모니터링, 비동기 진단 기반 마련
  - done: `PlayHandlers.cs`에 `/play/wait-for-play`, `/play/wait-for-stop` 추가
  - done: `UiStateMonitorHandlers.cs` 추가 - UI 활성/비활성 대기, 텍스트 대기, 컴포넌트 대기, 스크린샷 비교
  - done: AsyncMonitorHandlers.cs 추가 - 비동기 작업 추적 및 상태 모니터링
  - done: WebRequestMonitor.cs 추가 - UnityWebRequest 타임아웃 감지
  - done: FirebaseAuthRestAdapter.cs 개선 - 로그 추가, 타임아웃 감지
  - done: FirestoreRestPort.cs 개선 - 로그 추가, 타임아웃 감지
  - done: improved UI Handlers - 코드로 바인딩된 핸들러 호출 지원
  - done: improved Console Handlers - 실시간 로그 스트리밍 (SSE), 태그 필터링
  - done: improved GameObject Handlers - 컴포넌트 필드 값, 메서드 정보 포함
- note: 이 시점의 "완전 자동화" 표현은 과장으로 판명됨 - 이후 play/stop 메인 스레드 안정화와 route 정리가 추가로 필요
- new: Play Mode 대기 엔드포인트
- new: UnityWebRequest 타임아웃 감지 (30초)
- new: UI 요소 상태 대기 (활성/비활성, 텍스트, 컴포넌트)
- new: 실시간 로그 스트리밍 (SSE)
- new: 코드로 바인딩된 버튼 핸들러 직접 호출

### 2026-04-17

- done: Unity MCP를 `진단 + 수동 자동화` 도구로 재정의
  - done: `PlayHandlers.cs`를 공통 상태 스냅샷 기반으로 정리 - play start/stop/wait polling을 전부 메인 스레드 상태 조회로 통일
  - done: `screenshot/capture` 덮어쓰기/경로 검증 보강 - 기존 파일 재사용으로 인한 stale 성공을 차단
  - done: `UnityMcpBridge.cs` 등록부에서 legacy `ConsoleHandlers`, `UiHandlers`, `GameObjectHandlers` 중복 등록 제거
  - done: `tools/unity-mcp/server.js`에 stable Play/UI/screenshot MCP tools 추가
  - done: `tools/unity-mcp/McpHelpers.ps1`를 stable route 기준 helper로 정리
  - done: 당시 Garage 수동 smoke 캡처 플로우를 스크립트화
  - done: `tools/unity-mcp/README.md`, `docs/plans/mcp_improvement_plan.md`를 stable/manual/experimental 기준으로 갱신
- next: Unity Editor에서 3회 play start/stop smoke와 Garage smoke를 다시 실행해 새 stable 경로를 실검증

### 2026-04-14

### 2026-04-14

- done: MCP eval 엔드포인트 버그 수정 — `EditorApplication.isPlaying` 메인 스레드 이동 (`EvalHandlers.cs`, `Models.cs`)
- done: Garage UI Phase 1~4 일괄 개선 — `garage_ui_ux_improvement_plan.md` 기반
  - done: `ThemeColors.cs` 생성 — Garage 전용 색상 토큰 체계 (`Presentation/Theme/`)
  - done: `GarageResultPanelView` 개선 — 저장 버튼 명확화, 텍스트 대비도, Toast/로딩 통합
  - done: `GarageSlotItemView` 개선 — 호버 피드백 (IPointerEnterHandler), ThemeColors 적용
  - done: `GarageUnitEditorView` 개선 — Clear Slot 버튼 텍스트 명시화, ThemeColors
  - done: `GaragePartSelectorView` 개선 — 빈 값 텍스트-muted 색상, ThemeColors
  - done: `GaragePageController` — ResultPane Preview 요소 런타임 분리, 중복 SaveButton 제거
  - done: `GaragePagePresenter` — subtitle 개선 (auto-save 명확화)
  - done: `GaragePageController` — 부품 선택 시 토스트 피드백 (Frame/Firepower/Mobility 이름 표시)
  - done: `LobbyView` — 탭 활성 시각 개선 (왼쪽 3px 보더 자동 생성)
- next: Unity Inspector에서 직렬화 참조 검증 (`_saveButtonText`, `_saveButtonImage`, `_clearButtonText` 등)
- next: 플레이모드 smoke 테스트 — Garage 탭 → 저장 → 토스트 → 호버 확인

### 2026-04-12

- done: Google 로그인 기반 코드 추가 — `IAuthPort.SignInWithGoogle`, `FirebaseAuthRestAdapter.signInWithIdp`, `SignInWithGoogleUseCase`, `AccountSetup` wiring
- done: `AccountConfig.googleWebClientId` 필드 추가
- done: WebGL JS 브리지 추가 — `google.accounts.id` SDK 로드, `GoogleSignIn.jslib`, Unity callback 연결
- done: `AccountSettingsView`에 Google 로그인 버튼/콜백/상태 메시지 로직 추가
- done: Firebase Auth linking 요청 추가 — 기존 Firebase ID token 전달
- done: `CodexLobbyScene` GaragePageRoot에 `AccountSettingsView`, Google 버튼, 상태 텍스트, authType 텍스트 wiring 반영
- done: `account_system_plan.md`에 Phase 11 현재 상태 갱신
- next: WebGL 빌드로 UID 유지 여부 포함 실기 검증

### 2026-04-11

- done: Account Feature 골격 — Domain (Account, PlayerStats, UserSettings), Application Ports/UseCases, Infrastructure (FirebaseAuthRestAdapter, FirestoreRestPort), AccountSetup
- done: Presentation — LoginLoadingView (로딩 오버레이), AccountSettingsView (계정 설정)
- done: LobbySetup에 Account 통합 — 익명 로그인 → 성공 시 로비 진입
- done: SaveRosterUseCase 수정 — 로컬 JSON → Firestore 저장 + Photon 동기화 유지
- done: GarageSetup → IAccountDataPort 주입, SaveRoster async 전환
- done: GaragePageController → SaveRoster async 호출로 변경
- done: AuthTokenProvider — 당시 순환 의존성 방지용 정적 토큰 제공자였음
- note: 이 정적 seam은 2026-04-19에 injected session access로 대체됨
- next: Firebase Console 설정 (API Key, Project ID, Firestore DB)
- next: Unity Inspector에 AccountConfig, LoginLoadingView, AccountSettingsView 할당
- next: WebGL 빌드 smoke 테스트

### 2026-04-10

| 커밋 | 내용 |
|---|---|
| `b01947f` | chore: gate rule harness on feature dependency dag |
| `1ccab5e` | chore: expand rule harness code fix recipes |
| `ec3be02` | chore: strengthen rule harness feedback loop |
| `4dcd9e4` | fix: rename scenes (JG_ prefix removed), fix compile errors, update validator |
| `54447f2` | docs(rules): unify Korean/English mixed language in agent/*.md |
| `b897879` | fix(zone): move ZoneStatusPayload to Domain layer, break layer violation |
| `d295479` | fix(zone): break Status->Player->Zone->Status cycle with ZoneStatusPayload |
| `9444534` | feat(phase9): complete network sync — stats UI and drag ghost prefab |

### 2026-04-09

| 커밋 | 내용 |
|---|---|
| `2540305` | feat(analytics): add LogGameResult for Firebase game outcome tracking |
| `733a376` | feat(phase5): BattleEntity late-join state sync via instantiation data |
| `d53e90a` | feat(unit): replace CreateTemporaryUnitSpec with real UnitSpec, add Lobby scene transition |
| `9655638` | feat(unit): fix compilation errors, add drag-drop and rotation support |

### 2026-04-08

| 커밋 | 내용 |
|---|---|
| `7e72929` | docs: update session memo with Phase 6 completion |
| `9548e5c` | docs: add game scene entry progress to root entrypoint flow |
| `4a48f7f` | feat(phase6): add game end handling with stats analytics and lobby return |
| `7759b38` | chore: add ProjectileSetup.meta, new test directories, harness memory, .qwen |
| `e869c85` | refactor(lobby): minor cleanup, remove obsolete agent docs |
| `a344a33` | feat(unit): add summon system, unit slot UI, ComputePlayerUnitSpecsUseCase |
| `6cb2131` | refactor(player): replace Mana with Energy, add IPlayerSpecProvider, split InitializeLocal/Remote |
| `1949a98` | refactor(garage): add GetLocalPlayerRoster, expand unit slots to 6 |
| `754cde4` | docs: consolidate port ownership into architecture-diagram.md |

### 주요 변경 요약
- **포트 소유권**: `port_ownership.md` → `architecture-diagram.md` 통합 (Mermaid 의존성 그래프)
- **Player**: Mana → Energy 변경, IPlayerSpecProvider 도입, InitializeLocal/Remote 분리
- **Garage**: 슬롯 5→6 확장, GetLocalPlayerRoster, RestoreGarageRosterUseCase
- **Unit**: 소환 시스템, 슬롯 UI, ComputePlayerUnitSpecsUseCase
- **Wave**: Victory/Defeat 통계, Lobby 복귀 버튼
- **문서 정리**: obsolete agent 문서 일부 제거와 규칙 문서 참조 정리

## 전체 로드맵

자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
