# 진행 상황 (Game Scene Entry)

> **마지막 업데이트**: 2026-04-12

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
| Phase 10: 계정 시스템 | 🟨 구현 진행 중 | Firebase REST API 기반 Account Feature 골격 완료, Lobby/Garage 통합 완료 |
| Phase 11: Google 로그인 | 🟨 코드 구현 완료 | SignInWithGoogle 포트/UseCase/REST/config, JS 브리지, UI 콜백, linking 요청 추가 완료. Inspector wiring/실기 테스트 미완료 |

## 미완료 TODO

- Phase 9: Unity Inspector wiring 검증 (`PlacementAreaView`, `DragGhostPrefab` 등 직렬화 참조 할당 확인)
- Phase 9: 실제 멀티플레이어 smoke 테스트 (late-join, BattleEntity sync, Energy sync)
- Phase 10: Firebase Console 설정 (API Key, Project ID, Firestore DB 생성)
- Phase 10: Unity Inspector에 AccountConfig, LoginLoadingView 할당 확인
- Phase 10: 설정 (볼륨, 언어) Firestore 동기화
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: 계정 삭제 기능 + UI
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명→Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업 메모

- `CodexLobbyScene` Garage UI 2차 리팩터링: `GaragePageController + subview` 구조 compile/runtime 안정화 진행 중
- Garage UI 상세 계획: [`codex_lobby_garage_ui_refactor_plan.md`](./codex_lobby_garage_ui_refactor_plan.md)
- 계정 시스템 상세 계획: [`account_system_plan.md`](./account_system_plan.md)
- Phase 10 계정 시스템 마무리와 Phase 11 Google 로그인 사용자 플로우 연결을 병행

### 최근 변경 사항

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
- done: AuthTokenProvider — 순환 의존성 방지용 정적 토큰 제공자
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
| `7e72929` | docs: update QWEN.md with Phase 6 completion |
| `9548e5c` | docs: add game scene entry progress to CLAUDE.md (SSOT) |
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
- **문서 정리**: obsolete agent 문서 일부 제거와 규칙 문서 참조 정리, Feature README 전량 삭제

## 전체 로드맵

자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
