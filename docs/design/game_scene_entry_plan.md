# 게임 씬 진입 계획 (Game Scene Entry Plan)

> **마지막 업데이트**: 2026-04-07
> **기획 결정 반영 완료** — 아래 "기획 결정 사항" 섹션 참조

이 문서는 GameScene 진입 시점에 해야 할 작업들을 정의한다.
현재 `GameSceneRoot.cs`는 **Player 중심**의 초기화만 하고 있으며,
**Unit/Garage** 기반 소환 시스템으로 전환하면서 대대적인 재구성이 필요하다.

---

## 기획 결정 사항

사용자와의 논의를 통해 확정된 기획 사항이다. 구현의 기준이 된다.

### 에너지 시스템
| 항목 | 결정 |
|---|---|
| **자원 이름** | `Mana` → `Energy`로 명칭 변경 |
| **용도** | 유닛 소환 전용 자원 (Skill 시스템 통합으로 단일화) |
| **재생** | 고정 속도 + **게임 경과 시간에 따라 점점 증가** (페이싱 조절) |
| **초기값** | **가장 저렴한 유닛 1회 소환 가능** 수준 |

### Unit 슬롯 (Clash Royale 스타일)
| 항목 | 결정 |
|---|---|
| **Garage 편성** | **최대 6개** |
| **인게임 슬롯** | **3개 표시, 6개 로테이션** (Clash Royale 방식) |
| **UI 표시** | 유닛 아이콘 + 이름 + 소환 비용 + **소환 가능 상태 시각 표시** (쿨타임/에너지 부족) |
| **입력 방식** | **클릭 + 드래그 앤 드롭 둘 다 지원** |

### 배치
| 항목 | 결정 |
|---|---|
| **배치 구역** | **고정 영역** (아군 진영 내 지정 구역) |
| **시각화** | **배치 가능 영역 하이라이트 표시** |

### Unit 스펙 계산
| 항목 | 결정 |
|---|---|
| **계산 주체** | **각 클라이언트 자체 계산** (동일 입력 → 동일 출력, 네트워크 절약) |

### BattleEntity
| 항목 | 결정 |
|---|---|
| **상태 동기화** | **PhotonView + RPC**, **플레이어 각자 상태 권한** (Owner 기반) |
| **재소환** | **즉시** (에너지 있으면 바로, 쿨다운 없음) |

### Skill 시스템
| 항목 | 결정 |
|---|---|
| ** Skill** | **유닛의 공격 형태로 통합** (Skill 슬롯 제거, Skill 독립 Feature화) |
| **Skill 선택** | **Unit 선택으로 대체** (GameStartEvent 조건에서 Skill 선택 제거) |

### 게임 시작
| 항목 | 결정 |
|---|---|
| **GameStartEvent 조건** | **조건 없음, 바로 시작** (싱글/멀티 테스트 용이) |

### 코어
| 항목 | 결정 |
|---|---|
| **코어 구조** | **팀 공용 코어 1개** |

### Enemy AI
| 항목 | 결정 |
|---|---|
| **타겟팅** | **공격 범위 내 오브젝트 우선**, 첫 방향은 코어 |

### 테스트
| 항목 | 결정 |
|---|---|
| **환경** | **멀티플레이어 고려** (처음부터 네트워크 염두) |

---

## 현재 상태 분석

### GameSceneRoot가 현재 하는 일
1. EventBus 생성
2. 로컬 플레이어 스폰 (PlayerCharacter prefab)
3. Player 초기화 (Health, Mana, Status, Combat)
4. Combat 초기화
5. ProjectileSpawner, ZoneSetup 초기화
6. Skill 선택 (pre-selection)
7. Skill 선택 완료 후 Wave 초기화

### GameSceneRoot가 **아직** 하지 않는 일
- GarageRoster 복원 (편성 데이터 로드)
- Unit 스펙 계산 (ComposeUnitUseCase 호출)
- Unit 슬롯 UI 생성 (3개 표시 + 6개 로테이션)
- 소환 시스템 초기화
- Energy 시스템 (기존 Mana를 Energy로 변경 필요)
- Enemy/Wave와 Unit 스펙의 연결

### Unit Feature 현재 상태
- `Features/Unit/` 구조 완료 (Domain, Application, Infrastructure)
- `UnitBootstrap.cs`, `UnitSetup.cs` 스켈레톤 존재
- `ComposeUnitUseCase` 구현됨
- `UnitCompositionProvider` 구현됨
- **아직 GameScene에 통합되지 않음**

### Garage Feature 현재 상태
- `Features/Garage/` 구조 완료
- `GarageBootstrap.cs`, `GarageSetup.cs` 존재
- `IUnitCompositionPort` 정의됨
- **Lobby에서만 사용 가능, GameScene 연동 없음**

---

## 게임 씬 진입 시 해야 할 일 리스트

### Phase 0: 씬 진입 전 (Lobby → Game 전환)

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P0-1 | GarageRoster 직렬화 | Garage | 🟥 필수 | `garageRoster`를 Room CustomProperties에 저장 (최대 6개 UnitLoadout) |
| P0-2 | Room 진입 시 GarageRoster 동기화 | Garage | 🟥 필수 | CustomProperties에서 `UnitLoadout[]` 읽기 |
| P0-3 | Unit 스펙 계산 트리거 | Unit | 🟥 필수 | 게임 시작 시 한 번, 각 클라이언트 자체 계산 |

### Phase 1: GameScene 초기화 (Start)

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P1-1 | EventBus 생성 | GameSceneRoot | 🟥 필수 | 기존과 동일, `new EventBus()` |
| P1-2 | UnitBootstrap 초기화 | Unit | 🟥 필수 | `UnitBootstrap.Initialize(eventBus)` |
| P1-3 | GarageBootstrap 초기화 | Garage | 🟥 필수 | `GarageBootstrap.Initialize(eventBus, compositionPort, catalog)` |
| P1-4 | GarageRoster 복원 | Garage | 🟥 필수 | CustomProperties에서 `UnitLoadout[]` 읽어서 `GarageRoster` 복원 (최대 6개) |
| P1-5 | Unit 스펙 계산 | Unit | 🟥 필수 | `ComposeUnitUseCase.Execute()`로 각 Loadout → `Unit[] specs` 변환 (클라이언트 자체 계산) |
| P1-6 | Unit 스펙 저장 | GameSceneRoot | 🟥 필수 | 계산된 `Unit[]`(6개)을 플레이어별로 저장, 소환 시스템에서 참조 |
| P1-7 | Energy 시스템으로 변경 | Player | 🟥 필수 | Mana → Energy 명칭 변경, 재생 로직 적용 |

### Phase 2: 소환 시스템 구축

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P2-1 | UnitSlot UI (3슬롯) | Presentation | 🟥 필수 | 3개 표시 슬롯, 아이콘+이름+비용+상태 표시 |
| P2-2 | 로테이션 시스템 | Presentation | 🟥 필수 | 6개 중 3개씩 표시, 다음/이전 전환 |
| P2-3 | Energy 재생 틱 | Player | 🟥 필수 | 고정 재생 + 게임 시간에 따른 증가 곡선 |
| P2-4 | 소환 요청 UseCase | Unit/Application | 🟥 필수 | `SummonUnitUseCase` — 에너지 차감, BattleEntity 생성 요청 |
| P2-5 | 소환 입력 (클릭+드래그) | Presentation | 🟥 필수 | 슬롯 클릭 → 배치 영역 하이라이트 → 배치 |
| P2-6 | BattleEntity 스폰 | Combat/Unit | 🟥 필수 | PhotonNetwork.Instantiate로 BattleEntity 생성 (Owner 기반 RPC) |

### Phase 3: Wave/Enemy와 Unit 연결

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P3-1 | WaveBootstrap 초기화 | Wave | 🟥 필수 | GameStartEvent 조건 제거, 바로 시작 |
| P3-2 | Enemy 스폰 시스템 | Enemy | 🟥 필수 | 기존 유지, WaveTable 기반 |
| P3-3 | Enemy → Unit Combat 연결 | Combat | 🟥 필수 | Enemy가 Unit(플레이어 소환물)도 타겟팅 |
| P3-4 | CoreObjective 방어 목표 | Wave | 🟥 필수 | 기존 유지, 코어 HP 관리 |

### Phase 4: 재소환 시스템

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P4-1 | BattleEntity 파괴 감지 | Combat | 🟥 필수 | `UnitDiedEvent` 발행 |
| P4-2 | 재소환 즉시 실행 | Unit | 🟥 필수 | 에너지 차감 + 동일 UseCase 재사용 (쿨다운 없음) |
| P4-3 | 재소환 UI 피드백 | Presentation | 🟨 중요 | 에너지 부족/가능 상태 표시 |

### Phase 5: 네트워크 동기화

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P5-1 | Late-join 플레이어 처리 | Garage | 🟥 필수 | Room 진입 시 GarageRoster 복원 + Unit 스펙 재계산 |
| P5-2 | BattleEntity 상태 동기화 | Combat | 🟥 필수 | Owner 기반 RPC (HP, 위치, 사망) |
| P5-3 | 소환 RPC | Unit | 🟨 중요 | Photon RPC로 소환 알림 (Master 검증 없음, 각자 권한) |

### Phase 6: 게임 종료

| # | 작업 | 담당 | 우선순위 | 상세 |
|---|---|---|---|---|
| P6-1 | Wave Victory/Defeat 처리 | Wave | 🟥 필수 | 기존 유지 |
| P6-2 | 게임 종료 이벤트 | GameSceneRoot | 🟥 필수 | 결과 화면 전환 |
| P6-3 | 전적/통계 기록 | Analytics | 🟨 중요 | playtime, wave 달성, 소환 횟수 등 |

---

## 우선순위 작업 계획 (상세)

### 🟥 Phase 0+1: Unit 스펙 계산 파이프라인 구축

**목표**: GameScene 진입 시 GarageRoster → Unit[] specs 변환 완료

#### Step 1: GameSceneRoot에 Unit/Garage 통합

```
GameSceneRoot.cs 수정:
1. [SerializeField] UnitBootstrap _unitBootstrap;
2. [SerializeField] GarageBootstrap _garageBootstrap;
3. Start()에서 UnitBootstrap.Initialize(eventBus)
4. Start()에서 GarageBootstrap.Initialize(eventBus, unitBootstrap.CompositionPort, unitBootstrap.Catalog)
5. GarageRoster 복원 (CustomProperties에서 읽기, 최대 6개)
6. ComposeUnitUseCase로 Unit[] specs 계산 (클라이언트 자체 계산)
7. Skill 선택 로직 제거 → Unit 준비 완료로 대체
```

**세부 작업**:

| 파일 | 작업 | 비고 |
|---|---|---|
| `GameSceneRoot.cs` | UnitBootstrap, GarageBootstrap 필드 추가 | Inspector 연결 |
| `GameSceneRoot.cs` | `_unitBootstrap.Initialize(eventBus)` | Start() 초기 |
| `GameSceneRoot.cs` | `_garageBootstrap.Initialize(...)` | Unit 초기화 후 |
| `GameSceneRoot.cs` | `RestoreGarageRosterFromRoom()` | CustomProperties 읽기 |
| `GameSceneRoot.cs` | `ComputeUnitSpecs()` | ComposeUnitUseCase 호출 (자체 계산) |
| `GameSceneRoot.cs` | `Unit[] _myUnitSpecs` 필드 추가 | 계산 결과 저장 (최대 6개) |
| `GameSceneRoot.cs` | Skill 관련 코드 제거 | SkillSetup, pre-selection 제거 |

#### Step 2: GarageRoster 네트워크 복원

| 파일 | 작업 | 비고 |
|---|---|---|
| `GarageBootstrap.cs` | `InitializeFromRoomProperties()` 메서드 추가 | CustomProperties 읽기 |
| `GarageNetworkAdapter.cs` | `LoadRosterFromCustomProperties()` | Photon API 호출 |
| `InitializeGarageUseCase.cs` | UnitLoadout[] → GarageRoster 변환 | UseCase 수정 (최대 6개) |

#### Step 3: Unit 스펙 계산 실행

| 파일 | 작업 | 비고 |
|---|---|---|
| `GameSceneRoot.cs` | `ComputeUnitSpecs(UnitLoadout[])` | Loadout → Unit 변환 |
| `ComposeUnitUseCase.cs` | `Execute()` 호출 | 각 Loadout별로 (최대 6회) |
| `GameSceneRoot.cs` | `Unit[] _myUnitSpecs` 저장 | 결과 보관 |
| `Unit` | `UnitSpecComputedEvent` 발행 | 계산 완료 알림 (선택) |

**완료 조건**:
- [ ] GameScene 진입 시 GarageRoster 복원 완료 (최대 6개)
- [ ] Unit[] specs 계산 완료 (클라이언트 자체 계산)
- [ ] 다른 Feature에서 Unit 스펙 참조 가능
- [ ] Skill 선택 로직 제거 완료

---

### 🟥 Phase 2: 소환 시스템 기본 구축

**목표**: 플레이어가 Energy로 Unit을 소환할 수 있다 (Clash Royale 스타일)

#### Step 1: Energy 시스템 (Player Feature)

| 파일 | 작업 | 비고 |
|---|---|---|
| `Player/Domain/Player.cs` | `Mana` → `Energy`로 필드명 변경 | MaxEnergy, CurrentEnergy |
| `Player/Domain/EnergyRegen.cs` | 시간 기반 재생 로직 | **게임 경과 시간에 따라 증가 곡선** |
| `Player/Application/EnergyRegenUseCase.cs` | 재생 UseCase | |
| `Player/Presentation/EnergyBarView.cs` | UI (기존 ManaBarView 수정) | Energy 표시 |
| `Player/Infrastructure/EnergyRegenTicker.cs` | MonoBehaviour 틱 | |

#### Step 2: UnitSlot UI (3슬롯 + 6개 로테이션)

| 파일 | 작업 | 비고 |
|---|---|---|
| `Unit/Presentation/UnitSlotView.cs` | 슬롯 UI MonoBehaviour | 아이콘+이름+비용+상태 표시 |
| `Unit/Presentation/UnitSlotInputHandler.cs` | 클릭+드래그 입력 처리 | |
| `Unit/Presentation/UnitSlotsContainer.cs` | **3개 표시, 6개 로테이션** | Clash Royale 스타일 |
| `Unit/Presentation/UnitRotationControls.cs` | 다음/이전 전환 UI | |

#### Step 3: SummonUnitUseCase

| 파일 | 작업 | 비고 |
|---|---|---|
| `Unit/Application/SummonUnitUseCase.cs` | 신규 생성 | Energy 차감, BattleEntity 생성 요청 |
| `Unit/Application/Ports/ISummonExecutionPort.cs` | 포트 정의 | BattleEntity 생성 추상화 |
| `Unit/Infrastructure/SummonPhotonAdapter.cs` | 포트 구현 | PhotonNetwork.Instantiate |

#### Step 4: BattleEntity

| 파일 | 작업 | 비고 |
|---|---|---|
| `Unit/Domain/BattleEntity.cs` | 신규 생성 | 가변 상태 (HP, 위치, 상태) |
| `Unit/Presentation/BattleEntityView.cs` | MonoBehaviour | 시각 표현 |
| `Unit/Presentation/BattleEntitySetup.cs` | Composition root | 스폰 시 초기화 |
| `Unit/Infrastructure/BattleEntityPhotonController.cs` | **Owner 기반 RPC** | 플레이어 각자 상태 권한 |

**완료 조건**:
- [ ] Energy 시스템 동작 (Mana → Energy 변경)
- [ ] UnitSlot UI 표시 (3슬롯 + 6개 로테이션)
- [ ] 슬롯 클릭/드래그 → 배치 영역 하이라이트 → 소환 실행
- [ ] BattleEntity 생성 및 필드 배치 (Owner 기반 RPC)

---

### 🟥 Phase 3: Wave/Enemy와 Unit 통합

**목표**: Wave가 바로 시작되고 Enemy가 Unit(소환물)과 전투

#### Step 1: WaveBootstrap 조건 변경

| 파일 | 작업 | 비고 |
|---|---|---|
| `GameSceneRoot.cs` | Skill 선택 제거 → Unit 준비 완료 후 Wave 초기화 | |
| `WaveBootstrap.cs` | `GameStartEvent` 발행 조건 **제거** | 바로 시작 |
| `WaveBootstrap.cs` | `TryStartGame()`에서 skillsReady 확인 제거 | |

#### Step 2: Combat에 Unit 타겟 추가

| 파일 | 작업 | 비고 |
|---|---|---|
| `CombatBootstrap.cs` | `RegisterTarget(DomainEntityId, ICombatTargetProvider)` | Unit도 타겟 등록 |
| `Combat/Domain/` | `UnitCombatTargetProvider` | Unit용 Provider |
| `Enemy/` | Enemy AI가 Unit도 타겟팅 | **공격 범위 내 오브젝트 우선**, 첫 방향 코어 |

#### Step 3: CoreObjective 연결

| 파일 | 작업 | 비고 |
|---|---|---|
| `CoreObjectiveBootstrap.cs` | 기존 유지 | 팀 공용 코어 1개 |
| `WaveBootstrap.cs` | `_coreObjective.RegisterCombatTarget(_combatBootstrap)` | 기존 유지 |

**완료 조건**:
- [ ] Unit 준비 완료 시 Wave 바로 시작 (조건 없음)
- [ ] Enemy가 Unit 타겟팅 (공격 범위 내 우선)
- [ ] 코어 방어 메커니즘 동작

---

### 🟨 Phase 4+: 재소환, 네트워크, 종료

**목표**: 완전한 게임 루프 완성

#### Phase 4: 재소환

| 파일 | 작업 | 비고 |
|---|---|---|
| `Unit/Application/UnitDiedHandler.cs` | UnitDiedEvent 구독 | 파괴 감지 |
| `Unit/Presentation/UnitSlotView.cs` | 재소환 버튼 활성화 | 에너지 충분 시 **즉시** |
| `SummonUnitUseCase.cs` | 재소환도 동일 로직 | 재사용 (쿨다운 없음) |

#### Phase 5: 네트워크 동기화

| 파일 | 작업 | 비고 |
|---|---|---|
| `Unit/Infrastructure/SummonPhotonAdapter.cs` | Owner 기반 RPC | 소환 알림 (각자 권한) |
| `Unit/Infrastructure/BattleEntityPhotonController.cs` | BattleEntity 상태 동기화 | HP, 위치, 사망 (Owner 기반) |
| `GameSceneRoot.cs` | Late-join 처리 | Unit 스펙 재계산 |

#### Phase 6: 게임 종료

| 파일 | 작업 | 비고 |
|---|---|---|
| `WaveBootstrap.cs` | Victory/Defeat 처리 | 기존 유지 |
| `GameSceneRoot.cs` | 결과 화면 | |
| `Analytics` | 전적 기록 | |

---

## 의존성 순서도

```
Phase 0 (Lobby)
  ↓
Phase 1 (GameScene 초기화) ─── Unit 스펙 계산 파이프라인
  ↓
Phase 2 (소환 시스템) ──────── Energy + UnitSlot + Summon
  ↓
Phase 3 (Wave/Enemy 통합) ──── 게임 루프 완성
  ↓
Phase 4 (재소환)
Phase 5 (네트워크 동기화)
Phase 6 (게임 종료)
```

---

## 주의사항

### 기존 코드 유지 vs 변경
- **Player Feature**: Mana → Energy로 명칭 변경, Skill 시스템은 Unit 공격으로 통합
- **Wave/Enemy Feature**: 기존 구조 유지, Unit 타겟팅 추가, GameStartEvent 조건 제거
- **Combat Feature**: Player + Unit 동시 지원으로 확장
- **Skill Feature**: 독립 Feature로 분리, Unit 공격 시스템에 통합

### Unit Feature 독립성
- Unit은 Garage를 모름 (단방향 의존)
- Unit은 Player/Wave/Enemy를 모름
- GameSceneRoot가 wiring 책임

### 네트워크 결정 사항
- Unit 스펙 계산: **각 클라이언트 자체 계산** (동일 입력 → 동일 출력)
- BattleEntity 상태: **Owner 기반 RPC** (플레이어 각자 상태 권한)
- 소환: **각자 권한** (Master 검증 없음)

### Clash Royale 레퍼런스
- 3개 표시 슬롯 + 6개 로테이션
- Energy 기반 소환 (쿨다운 없음, 에너지 충분하면 즉시)
- 배치 구역 하이라이트 + 클릭/드래그 둘 다 지원

---

## 다음 액션

1. **Phase 1 시작**: GameSceneRoot에 Unit/Garage 통합 + Skill 제거
2. **Inspector 연결**: `UnitBootstrap`, `GarageBootstrap` GameSceneRoot에 추가
3. **CustomProperties 키 정의**: `garageRoster` 직렬화 포맷 (최대 6개 UnitLoadout)
4. **Energy 시스템 설계**: 재생 곡선 (시간에 따른 증가율) 구체화
5. **UnitSlot UI 프로토타입**: 3슬롯 + 로테이션 컨트롤 구현
6. **배치 구역 정의**: 고정 영역 범위 + 하이라이트 시각화

이 문서는 작업 진행하면서 계속 업데이트된다.
