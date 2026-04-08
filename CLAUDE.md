# CLAUDE.md

이 문서는 이 레포의 엔트리포인트다.
규칙을 다시 길게 설명하지 않고, 어떤 작업이면 어디로 가야 하는지만 안내한다.

## 먼저 읽기

1. 전역 구조, 레이어, 포트, 씬 계약 체크리스트는 `/agent/architecture.md`에서 시작한다.
2. `/Assets/Scripts/Features/<Name>/` 또는 `/Assets/Scripts/Shared/`를 수정하면, 먼저 `/agent/architecture.md`, `/agent/anti_patterns.md`, `/agent/event_rules.md`를 확인하고 그다음 해당 feature의 `*Setup.cs` / `*Bootstrap.cs`와 실제 코드 경로를 읽는다.
3. 구현 중 금지 사항, Bootstrap 예외, runtime lookup 예외 판단은 `/agent/anti_patterns.md`를 따른다.
4. 이벤트 체인 설계, 이벤트 vs 직접 호출 판단은 `/agent/event_rules.md`를 따른다.
5. 전역 초기화 순서와 late-join 전제는 별도 README가 아니라 각 feature의 `*Setup.cs` / `*Bootstrap.cs`, 씬/프리팹 직렬화 계약, 관련 전역 규칙 문서를 함께 읽고 판단한다.
6. 네트워크 상태 키와 동기화 채널은 **해당 키를 실제로 쓰는 코드**를 기준으로 본다. cross-feature로 읽을 때도 쓰기 소유 피처의 Application/Infrastructure 경로를 먼저 본다.
7. Unity MCP나 에디터 자동화는 `/docs/ops/unity_mcp.md`를 먼저 읽는다.
8. 대규모 기계적 치환 전에는 대상 패턴, 제외 대상, 검증 방법을 먼저 정하고 문서 의미가 바뀌면 규칙 문서도 같이 갱신한다.

## 작업별 진입 경로

| 작업 | 먼저 볼 문서 |
|---|---|
| 피처 경계, 폴더, 레이어, 포트, 네이밍 | `/agent/architecture.md` |
| Bootstrap 책임, EventHandler 위치, runtime lookup 예외 | `/agent/anti_patterns.md` |
| 이벤트 체인 설계, 이벤트 vs 직접 호출 판단 | `/agent/event_rules.md` |
| 전역 초기화 순서, late-join, scene 간 조립 순서 | `/Assets/Scripts/Features/<Name>/<Name>Setup.cs`, `/Assets/Scripts/Features/<Name>/<Name>Bootstrap.cs` 및 관련 씬 루트 |
| CustomProperties 키, 쓰기 소유권, 동기화 채널 선택 | 해당 키를 실제로 쓰는 `Application/Infrastructure` 코드 |
| 로컬 씬 wiring, lookup 예외, 프리팹/씬 계약 | 해당 feature의 `*Setup.cs` / `*Bootstrap.cs`, 씬/프리팹 직렬화 참조, `/agent/anti_patterns.md` |
| Shared에 둘 수 있는 것과 없는 것 | `/agent/architecture.md`, `/agent/anti_patterns.md`, `Assets/Scripts/Shared/**` |
| Unity MCP 엔드포인트와 테스트 SOP | `/docs/ops/unity_mcp.md` |
| 수치, MVP 기준, 디자인 문장 | `/docs/design/game_design.md` |
| 유닛/모듈 설계 SSOT | `/docs/design/unit_module_design.md` |
| Unit Feature 분리 설계 | `/docs/design/unit_feature_separation.md` |
| 문서 소유권과 SSOT 운영 원칙 | `/agent/work_principles.md` |

---

## 문서 폴더 구조

| 폴더 | 목적 |
|---|---|
| `/agent/` | AI 에이전트 규칙/컨텍스트 (불변, 자주 참조) |
| `/docs/design/` | 설계 SSOT (시스템의 "무엇, 왜" — 장기 보존) |
| `/docs/plans/` | 구현 계획 ("어떻게, 언제" — 완료 후 아카이브 가능) |
| `/docs/ops/` | 배포·빌드·운영 (Firebase, WebGL 최적화, CI/CD) |
| `/docs/discussions/` | 토론/의사결정 기록 (배경, 대안, 결정 근거) |
| `/docs/playtest/` | 플레이테스트 템플릿/기록

---

## 충돌 시 우선순위

1. `/agent/architecture.md` (folders, features, dependencies, layers, naming, ports)
2. `/agent/anti_patterns.md` (금지 패턴, 예외 판단, 리팩터링 교훈)
3. `/agent/event_rules.md` (이벤트 체인 방향, 깊이 제한, 이벤트 vs 직접 호출 판단)
4. `/docs/design/game_design.md` (게임 컨셉 SSOT — 코드 책임과 기획 방향이 충돌하면 이 문서가 우선)
5. 해당 feature의 `*Setup.cs` / `*Bootstrap.cs`와 실제 씬/프리팹 계약 (로컬 wiring, lookup 예외, 네트워크 키 실제 사용 경로)
6. `/agent/work_principles.md` (문서 운영, SSOT 소유권, 응집도 원칙)

---

## 진행 상황 (Game Scene Entry)

**마지막 업데이트**: 2026-04-08

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Bootstrap, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | 🟨 기본 구축 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (3슬롯) |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy → Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | 🟨 일부 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀 |

### 미완료 TODO
- UnitSlotView 드래그 앤 드롭 배치 (현재 클릭만)
- UnitSlotsContainer 로테이션 완성 (6개 중 3개 표시 전환)
- Phase 5 완성 — Late-join, BattleEntity 상태 동기화
- UnitCatalog에서 실제 스펙 조회 (`CreateTemporaryUnitSpec()` 대체)
- WaveEndView → 실제 Lobby 씬 전환 (현재 `PhotonNetwork.LeaveRoom()`만)
- Firebase Analytics 연동

### 주요 변경 사항 (최근 커밋)
- 포트 소유권: `port_ownership.md` → `architecture-diagram.md` 통합 (Mermaid 의존성 그래프)
- Player: Mana → Energy 변경, IPlayerSpecProvider 도입, InitializeLocal/Remote 분리
- Garage: 슬롯 5→6 확장, GetLocalPlayerRoster, RestoreGarageRosterUseCase
- Unit: 소환 시스템, 슬롯 UI, ComputePlayerUnitSpecsUseCase
- Wave: Victory/Defeat 통계, Lobby 복귀 버튼
- Agent 문서 5개 삭제 (game_design.md, state_ownership.md, initialization_order.md, firebase_hosting.md, webgl_optimization.md)
