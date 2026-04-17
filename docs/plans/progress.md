# 진행 상황 (Game Scene Entry)

> **마지막 업데이트**: 2026-04-17

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

- Phase 9: 실제 멀티플레이어 smoke 테스트 (late-join, BattleEntity sync, Energy sync)
- Phase 10: Firebase Console 설정 (API Key, Project ID, Firestore DB 생성)
- Phase 10: 설정 Firestore 동기화 마무리 (저장 UI, language 소비 경로)
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: 계정 카드 wiring smoke + 계정 삭제 WebGL 실기 확인
- Phase 10: Garage save/load WebGL 실기 확인
- Phase 10: Garage save/load smoke 자동화 보강 (실제 draft dirty + PATCH 캡처)
- Phase 10: Garage 수동 저장 UX 2차 폴리시 (슬롯 카드/결과 패널/계정 카드 완성도)
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명→Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업 메모

- `CodexLobbyScene` 로비/Garage 대시보드 리팩터링 2차: 시각 polish와 상호작용 smoke 보강 필요
- Garage UI 레이아웃 SSOT: [`ui_foundations.md`](../design/ui_foundations.md)
- Garage UI Figma handoff 계획: [`figma_ui_system_plan.md`](./figma_ui_system_plan.md)
- Garage UI 상세 계획: [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md)
- 계정 시스템 상세 계획: [`account_system_plan.md`](./account_system_plan.md)
- 기술부채 감축 실행 계획: [`tech_debt_reduction_plan.md`](./tech_debt_reduction_plan.md)
- WebGL 실기 체크리스트: [`webgl_smoke_checklist.md`](./webgl_smoke_checklist.md)
- 다음 세션 시작점: `tools/webgl-smoke/garage-save-load-smoke.cjs`가 Garage draft를 실제로 바꾸도록 입력 경로를 보강하고 `garage/roster` PATCH를 다시 캡처

### 최근 변경 사항

### 2026-04-17

- done: WebGL 익명 세션 지속성 복구 1차 완료
  - done: `FirebaseAuthRestAdapter`가 WebGL에서 `localStorage + PlayerPrefs`를 함께 사용하도록 보강하고 restore/persist 로그 추가
  - done: `Assets/Plugins/WebGL/AccountStorage.jslib` 추가 - 브라우저 `localStorage` 직접 읽기/쓰기/삭제 브리지 도입
  - done: `Assets/WebGLTemplates/JG/index.html`에 `autoSyncPersistentDataPath: true` 반영 - WebGL 파일 저장 동기화 기본값 보강
  - done: fast WebGL rebuild + Firebase preview 재배포 후 브라우저 저장소에서 `account.auth.*` 키 실제 생성 확인
  - done: 같은 브라우저 `reload`에서 anonymous UID 유지 확인 (`dzKQAAyYrTad865ky4a2Yy3Hc3K3 -> same UID`)
  - note: 기존 blocker였던 "reload 후 새 anonymous account 생성"은 해소됨
- blocked: Garage save/load WebGL smoke 2차 실행
  - done: 익명 세션 복구가 적용된 preview에서 `tools/webgl-smoke/garage-save-load-smoke.cjs` 재실행
  - done: smoke 재실행 결과 `reload` 후에도 같은 UID(`8Bkwj5acimag8gM2K19FiC1uRPM2`) 유지 확인
  - blocked: 자동 클릭/저장 입력이 Garage draft를 실제 dirty 상태로 만들지 못해 `garage/roster` PATCH가 여전히 발생하지 않음
  - blocked: 현재 smoke 실패 원인은 인증이 아니라 입력 자동화 정확도 부족
  - evidence: `artifacts/webgl/garage-save-load-smoke-result.json`, `garage-save-load-before.png`, `garage-save-load-after-save.png`, `garage-save-load-after-reload.png`
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
  - done: `docs/plans/figma_ui_system_plan.md` 추가 — Garage-first Figma 실행 체크리스트
  - done: 기존 Garage 계획 문서에서 새 SSOT 문서 참조 추가
- done: Figma remote MCP 설치 및 OAuth 인증 완료 (`codex mcp add figma --url https://mcp.figma.com/mcp`, `codex mcp login figma`)
- done: 대상 Figma 파일 접근 권한 확인 (`Page 1`, node `0:1`)
- done: Figma 운영 방식을 Starter 기준 `3페이지 staged`로 전환 (`Foundations / Components / Garage`, handoff는 Garage 섹션 + 레포 문서)
- done: 로컬 `usfigma` skill 제거 — 직접 Figma MCP 프롬프트 기반으로 운영 전환
- blocked: 스캐폴드 생성 시도 직후 Figma MCP Starter 호출 한도 도달
- blocked: 최소 범위 재시도 (`Page 1` 조회 및 `Foundations` 이름 변경`)도 동일한 Starter MCP 호출 한도에서 실행 전 차단
- blocked: 계정 전환 이후에도 파일 권한/seat/Starter MCP 호출 한도 문제로 Figma 기반 실행 계획 지속 불가
- done: `figma_ui_system_plan.md` 상태를 `보류`로 전환
- next: Figma 연결 전제 없이 Garage UI를 코드와 문서 SSOT 기준으로 계속 정리할지 판단

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
  - done: Garage/Lobby README 추가 — 씬 소유권, 저장 계약, 이벤트 흐름 SSOT화
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
  - done: `tools/unity-mcp/Invoke-GarageManualSmoke.ps1` 추가 - Garage 수동 smoke 캡처 플로우 스크립트화
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
