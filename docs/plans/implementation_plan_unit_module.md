# Implementation Plan — Unit & Module System v2

이 문서는 `unit_module_design.md`, `module_data_structure.md`, `garage_ui_design.md`를 실제 Unity 코드로 구현하는 순서와 마일스톤을 정의한다.

**상위 계획 의존 문서**:
- `../design/game_design.md` — 게임 디자인 SSOT
- `implementation_plan_redesign_execution.md` — 병렬 씬 전략 및 컷오버 순서
- `../../agent/architecture.md` — Feature-first Clean Architecture 규칙
- `../../agent/state_ownership.md` — CustomProperties 소유권 맵

---

## 전제: 상위 계획과의 정렬

이 문서는 다음 상위 결정사항을 준수한다.

### 1. 병렬 씬 전략

- 기존 `JG_GameScene`을 직접 수정하지 않는다.
- 새 전투 씬 `JG_BattleScene_Redesign.unity`를 병렬로 생성한다.
- 로비의 시작 경로를 새 씬으로 전환 후, 기존 씬은 아카이브한다.
- 컷오버 전까지는 "구형 씬 = 구형 규칙", "신규 씬 = 신규 규칙"으로 완전 분리한다.

### 2. Feature-first Clean Architecture

```
Assets/Scripts/Features/Garage/
├── Domain/                          ← Unity/Photon 의존 금지
├── Application/                     ← Unity/Photon 의존 금지, UseCase/Port/Event
├── Presentation/                    ← MonoBehaviour, View, InputHandler
├── Infrastructure/                  ← ScriptableObject, Photon, Persistence
├── GarageSetup.cs                   ← Composition root (의존성 주입)
└── GarageBootstrap.cs               ← Scene-level wiring (EventBus 주입)
```

- `GarageSetup`과 `GarageBootstrap`은 feature 루트에 배치 (`Bootstrap/` 폴더 금지).
- EventBus는 씬 Bootstrap이 생성하고 Garage에 주입한다. Garage는 자체 EventBus를 생성하지 않는다.
- Domain 레이어는 Unity API, Photon API, 파일 IO를 사용하지 않는다.

### 3. 네트워크 동기화 필수

- GarageRoster는 로컬 저장(JSON) + 네트워크 직렬화(CustomProperties)를 둘 다 지원한다.
- 각 플레이어의 편성은 CustomProperties를 통해 룸에 동기화된다.
- late-join 시 CustomProperties에서 편성 복구.
- host migration 시에도 편성 데이터 유지.

### 4. 레이어 경계 엄수

- `AnchorBehaviour`는 Unity 컴포넌트이므로 **Presentation**에 배치 (Domain이 아님).
- `EnergySystem`은 계산 로직(Domain) + 틱/동기화(Infrastructure) + UI(Presentation)로 분리.
- Domain은 순수 C#만 사용. Unity 타입은 Infrastructure/Presentation에 배치.

---

## 아키텍처: Garage Feature 구조

### 전체 구조

```
Assets/Scripts/Features/Garage/
├── GarageSetup.cs                      ← Composition root
├── GarageBootstrap.cs                  ← Scene-level wiring
├── Domain/
│   ├── Unit.cs                         ← 전투 유닛 엔티티 (순수 C#)
│   ├── UnitComposition.cs              ← 조합 검증 + 스탯 계산 (순수 C#)
│   ├── CostCalculator.cs               ← 비용 공식 (순수 C#)
│   ├── GarageRoster.cs                 ← 편성 데이터 (순수 C#)
│   └── PassiveEffect.cs                ← 패시브 효과 도메인 (순수 C#)
├── Application/
│   ├── Ports/
│   │   ├── IGarageNetworkPort.cs       ← 편성 네트워크 동기화 포트
│   │   └── IGaragePersistencePort.cs   ← 저장/불러오기 포트
│   ├── InitializeGarageUseCase.cs      ← 차고 초기화
│   ├── ComposeUnitUseCase.cs           ← 유닛 조합 계산
│   ├── ValidateRosterUseCase.cs        ← 편성 유효성 검증
│   ├── SaveRosterUseCase.cs            ← 편성 저장
│   └── GarageEvents.cs                 ← 도메인 이벤트 정의
├── Presentation/
│   ├── GarageScreen.cs                 ← 차고 화면 메인 컨트롤러
│   ├── UnitFrameCard.cs                ← 프레임 선택 카드 UI
│   ├── ModuleSelector.cs               ← 모듈 선택 UI
│   ├── StatsDisplay.cs                 ← 실시간 스탯 표시
│   ├── CompositionValidator.cs         ← 금지조합 경고 UI
│   ├── RosterSlot.cs                   ← 편성 슬롯 UI
│   ├── AnchorBehaviour.cs              ← MonoBehaviour: 이동범위 기반 교전
│   └── SummonInputHandler.cs           ← 소환 입력 처리
└── Infrastructure/
    ├── UnitFrameData.cs                ← ScriptableObject: 프레임
    ├── FirepowerModuleData.cs          ← ScriptableObject: 상단 모듈
    ├── MobilityModuleData.cs           ← ScriptableObject: 하단 모듈
    ├── PassiveTraitData.cs             ← ScriptableObject: 고유 특성
    ├── ModuleCatalog.cs                ← 카탈로그 SO
    ├── GarageNetworkAdapter.cs         ← Photon CustomProperties 동기화
    └── GarageJsonPersistence.cs        ← JSON 저장/불러오기 구현
```

### 전투 Feature (별도, Phase 4에서 생성)

```
Assets/Scripts/Features/Battle/
├── BattleSetup.cs                      ← Composition root
├── BattleBootstrap.cs                  ← Scene-level wiring
├── Domain/
│   ├── EnergyCalculator.cs             ← 순수 에너지 계산
│   ├── Anchor.cs                       ← 순수 도메인 (이동범위, 교전 규칙)
│   └── BattleSession.cs                ← 전투 세션 상태
├── Application/
│   ├── SummonUnitUseCase.cs            ← 유닛 소환
│   ├── ApplyEnergyTickUseCase.cs       ← 에너지 틱 적용
│   └── BattleEvents.cs                 ← 전투 이벤트
├── Presentation/
│   ├── EnergyHUD.cs                    ← 에너지 UI
│   ├── UnitSlotHUD.cs                  ← 유닛 슬롯 UI
│   └── AnchorVisualizer.cs             ← 앵커 반경 Gizmos
└── Infrastructure/
    ├── EnergySync.cs                   ← Photon 에너지 동기화
    ├── UnitSpawner.cs                  ← 프리팹 생성 + 스탯 적용
    └── BattleNetworkAdapter.cs         ← 전투 상태 동기화
```

> **참고**: Battle Feature는 Phase 4에서 생성. 현재 문서는 Garage Feature에 집중하지만,
> Battle과의 경계는 미리 명시한다.

### 기존 시스템과의 관계

```
기존                          신규
─────────────────────────    ─────────────────────────
Features/Skill/        ───→   Features/Garage/ (점진적 마이그레이션)
Features/Player/       ───→   소환자 역할로 재정의 (Battle Feature)
Features/Wave/         ───→   연속 압박 구조로 개편 (Battle Feature)
Features/Combat/       ───→   유닛 소환/앵커 기반으로 전환 (Battle Feature)
```

---

## CustomProperties 소유권 맵 (추가)

`state_ownership.md`에 다음 항목을 추가한다.

### Garage CustomProperties (Photon Player)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `garageRoster` | Garage | `GarageNetworkAdapter.SyncRoster()` | JSON 직렬화 편성 데이터 |
| `garageReady` | Garage | `GarageNetworkAdapter.SyncReady()` | 편성 완료 여부 |

### 규칙

- 각 플레이어는 자신의 `garageRoster`만 쓰기 가능.
- 다른 플레이어의 편성은 읽기만 가능 (비교 표시용).
- late-join 시 `OnPhotonPlayerPropertiesChanged`에서 모두 복구.
- host migration 시 Room Property가 아닌 Player Property이므로 유지됨.

---

## 구현 Phase

### Phase 1: 도메인 모델 + Composition Root

**목표**: 유닛/모듈의 핵심 도메인 로직 + Garage Feature 조립 지점 정의

**작업 목록**:

| # | 작업 | 파일 | 설명 |
|---|---|---|---|
| 1.1 | GarageSetup 생성 | `GarageSetup.cs` | Composition root: 의존성 주입 책임 |
| 1.2 | GarageBootstrap 생성 | `GarageBootstrap.cs` | Scene-level wiring: EventBus 주입 |
| 1.3 | Unit 엔티티 생성 | `Domain/Unit.cs` | 프레임 + 모듈 조합 결과 저장 |
| 1.4 | UnitComposition 작성 | `Domain/UnitComposition.cs` | 조합 검증 + 스탯 계산 |
| 1.5 | CostCalculator 작성 | `Domain/CostCalculator.cs` | 가중치 + 분산 페널티 공식 |
| 1.6 | GarageRoster 작성 | `Domain/GarageRoster.cs` | 편성 데이터 구조 (네트워크 직렬화 지원) |
| 1.7 | PassiveEffect 작성 | `Domain/PassiveEffect.cs` | 패시브 효과 도메인 |
| 1.8 | 도메인 단위 테스트 | `Tests/Garage/Domain/` | 조합 검증, 비용 계산 테스트 |

**GarageSetup.cs 책임**:
```csharp
// GarageSetup.cs (Composition root)
// - Domain 객체 생성 (UnitComposition, CostCalculator)
// - Application UseCase 생성 (InitializeGarageUseCase 등)
// - Infrastructure 어댑터 생성 (GarageJsonPersistence 등)
// - Port 연결 (IGaragePersistencePort → GarageJsonPersistence)
// - Presentation에 UseCase 주입
// - 비즈니스 로직 직접 수행 금지
```

**GarageBootstrap.cs 책임**:
```csharp
// GarageBootstrap.cs (Scene-level wiring)
// - Scene의 EventBus를 GarageSetup에 주입
// - GarageScreen MonoBehaviour 참조 연결
// - ModuleCatalog ScriptableObject 로드
// - GarageSetup.Initialize() 호출
// - 씬 전환 시 정리 (GarageSetup.Cleanup())
```

**검증 기준**:
- [ ] `UnitComposition.Validate()` 금지조합 올바르게 감지
- [ ] `CostCalculator.Calculate()` 극한빌드 > 균형형 비용
- [ ] `GarageRoster.IsValid` 3~5기 체크
- [ ] GarageSetup/Bootstrap이 feature 루트에 위치
- [ ] Domain 레이어에 Unity/Photon 의존성 없음

---

### Phase 2: 데이터 카탈로그 + 네트워크 Port 정의

**목표**: ScriptableObject 기반 데이터 구조 + 샘플 데이터 + 네트워크 계약

**작업 목록**:

| # | 작업 | 파일 | 설명 |
|---|---|---|---|
| 2.1 | UnitFrameData SO | `Infrastructure/UnitFrameData.cs` | 프레임 기본 스탯 + 고유 특성 |
| 2.2 | FirepowerModuleData SO | `Infrastructure/FirepowerModuleData.cs` | 상단 모듈 데이터 |
| 2.3 | MobilityModuleData SO | `Infrastructure/MobilityModuleData.cs` | 하단 모듈 데이터 |
| 2.4 | PassiveTraitData SO | `Infrastructure/PassiveTraitData.cs` | 중단 고유 특성 |
| 2.5 | ModuleCatalog SO | `Infrastructure/ModuleCatalog.cs` | 전체 카탈로그 |
| 2.6 | GarageJsonPersistence | `Infrastructure/GarageJsonPersistence.cs` | JSON 저장/불러오기 구현 |
| 2.7 | IGarageNetworkPort | `Application/Ports/IGarageNetworkPort.cs` | 네트워크 동기화 인터페이스 |
| 2.8 | GarageNetworkAdapter | `Infrastructure/GarageNetworkAdapter.cs` | Photon CustomProperties 구현 |
| 2.9 | 샘플 데이터 생성 | `Assets/Data/Garage/` | 프레임 4종, 모듈 6종, 특성 4종 |

**네트워크 동기화 흐름**:

```
1. 플레이어 편성 완료 → SaveRosterUseCase 실행
2. SaveRosterUseCase가 IGaragePersistencePort.Save() + IGarageNetworkPort.SyncRoster() 호출
3. GarageNetworkAdapter가 GarageRoster를 JSON 직렬화 → CustomProperties["garageRoster"] 설정
4. 동시에 CustomProperties["garageReady"] = true
5. 다른 클라이언트에서 OnPhotonPlayerPropertiesChanged 콜백
6. GarageNetworkAdapter가 JSON 역직렬화 → 로컬 GarageRoster 복구
7. GarageScreen이 다른 플레이어 편성 상태 표시
```

**생성할 샘플 데이터**:

```
Frames:
  - SO_Frame_Guardian.asset     (가디언: 철벽)
  - SO_Frame_Hunter.asset       (헌터: 추격 본능)
  - SO_Frame_Artist.asset       (아티스트: 집중 포격)
  - SO_Frame_Medic.asset        (메딕: 긴급 수리)

Firepower Modules:
  - SO_Fire_Single.asset        (단일탄)
  - SO_Fire_AoE.asset           (광유탄)
  - SO_Fire_Rapid.asset         (연사)

Mobility Modules:
  - SO_Mob_Armor.asset          (중장갑)
  - SO_Mob_Light.asset          (경량)
  - SO_Mob_Fixed.asset          (고정포대)

Traits:
  - SO_Trait_IronWall.asset     (철벽: 중)
  - SO_Trait_Pursuit.asset      (추격 본능: 중)
  - SO_Trait_FocusFire.asset    (집중 포격: 강)
  - SO_Trait_EmergencyRepair.asset (긴급 수리: 중)
```

**검증 기준**:
- [ ] ModuleCatalog에서 모든 데이터 조회 가능
- [ ] GarageJsonPersistence 저장/불러오기 정상 동작
- [ ] Inspector에서 데이터 생성/수정 가능
- [ ] GarageNetworkAdapter CustomProperties读写 테스트
- [ ] late-join 시 편성 데이터 복구 확인

---

### Phase 3: 차고 UI + 로비 편성 단계

**목표**: 편성 화면 구현 + 로비에 편성 완료 상태 표시

**상위 계획 정렬**: `implementation_plan_redesign_execution.md` Phase 1 참조.
로비에 편성 진입 흐름을 추가한다. 새 전투 씬 구현 전이다.

**작업 목록**:

| # | 작업 | 파일 | 설명 |
|---|---|---|---|
| 3.1 | GarageScreen | `Presentation/GarageScreen.cs` | 차고 화면 메인 컨트롤러 |
| 3.2 | UnitFrameCard | `Presentation/UnitFrameCard.cs` | 프레임 선택 카드 UI |
| 3.3 | ModuleSelector | `Presentation/ModuleSelector.cs` | 모듈 선택 드롭다운/팝업 |
| 3.4 | StatsDisplay | `Presentation/StatsDisplay.cs` | 실시간 스탯 바 + 비용 표시 |
| 3.5 | CompositionValidator | `Presentation/CompositionValidator.cs` | 금지조합 경고 UI |
| 3.6 | RosterSlot | `Presentation/RosterSlot.cs` | 편성 슬롯 UI (드래그 지원) |
| 3.7 | InitializeGarageUseCase | `Application/InitializeGarageUseCase.cs` | 차고 진입 시 초기화 |
| 3.8 | ComposeUnitUseCase | `Application/ComposeUnitUseCase.cs` | 조합 계산 → UI 업데이트 |
| 3.9 | SaveRosterUseCase | `Application/SaveRosterUseCase.cs` | 완료 시 저장 + 네트워크 동기화 |
| 3.10 | LobbyGaragePanel | `Presentation/LobbyGaragePanel.cs` | 로비 내 편성 상태 표시 패널 |
| 3.11 | GarageInputHandler | `Presentation/GarageInputHandler.cs` | 터치/클릭 입력 → UseCase 호출 |

**씬 위치**:
- **차고 UI는 로비 씬(`JG_LobbyScene.unity`)에 배치**한다.
- GarageSetup/GarageBootstrap은 로비 씬에서 초기화된다.
- 전투 씬과는 독립적이다.

**UI 구현 순서**:

```
1. GarageScreen 본 구조 (UI 루트, 버튼 연결)
2. UnitFrameCard (가로 스크롤 카드 리스트)
3. ModuleSelector (상/하단 슬롯, 드롭다운)
4. StatsDisplay (바 애니메이션, 비용 숫자)
5. CompositionValidator (경고 메시지)
6. RosterSlot (3~5기 슬롯, 드래그)
7. GarageInputHandler (입력 → UseCase 연결)
8. LobbyGaragePanel (룸 내 다른 플레이어 편성 상태 표시)
9. SaveRosterUseCase → 네트워크 동기화 확인
```

**검증 기준**:
- [ ] 프레임 선택 → 모듈 패널 업데이트
- [ ] 모듈 변경 → 스탯/비용 즉시 반영 (0.2초 이내)
- [ ] 금지조합 → 경고 표시, 편성 추가 불가
- [ ] 3기 이상 편성 → "완료" 버튼 활성화
- [ ] 완료 클릭 → JSON 저장 + CustomProperties["garageRoster"] 설정
- [ ] 다른 플레이어의 편성 상태가 로비에서 표시됨
- [ ] late-join 시 기존 플레이어 편성 데이터 복구

---

### Phase 4: 새 전투 씬 생성 + 소환/앵커 통합

**목표**: `JG_BattleScene_Redesign` 병렬 생성 + 편성 데이터 → 소환 → 앵커 교전

**상위 계획 정렬**: `implementation_plan_redesign_execution.md` Phase 2, 5 참조.
기존 `JG_GameScene`을 수정하지 않는다. 새 씬을 별도로 만든다.

**작업 목록**:

| # | 작업 | 파일 | 설명 |
|---|---|---|---|
| 4.1 | 새 씬 생성 | `Assets/Scenes/JG_BattleScene_Redesign.unity` | 빈 전투 씬 |
| 4.2 | BattleSetup 생성 | `Battle/BattleSetup.cs` | 전투 Composition root |
| 4.3 | BattleBootstrap 생성 | `Battle/BattleBootstrap.cs` | 전투 Scene-level wiring |
| 4.4 | BattleSession | `Battle/Domain/BattleSession.cs` | 전투 세션 상태 (에너지, 편성) |
| 4.5 | EnergyCalculator | `Battle/Domain/EnergyCalculator.cs` | 순수 에너지 계산 |
| 4.6 | EnergySync | `Battle/Infrastructure/EnergySync.cs` | Photon 에너지 동기화 |
| 4.7 | Anchor (Domain) | `Battle/Domain/Anchor.cs` | 순수 도메인: 이동범위, 교전 규칙 |
| 4.8 | AnchorBehaviour (Presentation) | `Battle/Presentation/AnchorBehaviour.cs` | MonoBehaviour: 실제 이동/교전 |
| 4.9 | UnitSpawner | `Battle/Infrastructure/UnitSpawner.cs` | 소환 시 프리팹 생성 + 스탯 적용 |
| 4.10 | SummonUnitUseCase | `Battle/Application/SummonUnitUseCase.cs` | 유닛 소환 UseCase |
| 4.11 | SummonInputHandler | `Battle/Presentation/SummonInputHandler.cs` | 소환 입력 처리 |
| 4.12 | EnergyHUD | `Battle/Presentation/EnergyHUD.cs` | 에너지 UI |
| 4.13 | UnitSlotHUD | `Battle/Presentation/UnitSlotHUD.cs` | 유닛 슬롯 UI |
| 4.14 | GarageToBattleFlow | `Application/GarageToBattleFlow.cs` | 편성 데이터 → 전투 전달 |

**Battle Feature 구조**:

```
Assets/Scripts/Features/Battle/
├── BattleSetup.cs                      ← Composition root
├── BattleBootstrap.cs                  ← Scene-level wiring
├── Domain/
│   ├── Anchor.cs                       ← 순수 도메인 (이동범위, 교전 규칙)
│   ├── EnergyCalculator.cs             ← 순수 에너지 계산
│   └── BattleSession.cs                ← 전투 세션 상태
├── Application/
│   ├── SummonUnitUseCase.cs            ← 유닛 소환
│   ├── ApplyEnergyTickUseCase.cs       ← 에너지 틱 적용
│   └── BattleEvents.cs                 ← 전투 이벤트
├── Presentation/
│   ├── EnergyHUD.cs                    ← 에너지 UI
│   ├── UnitSlotHUD.cs                  ← 유닛 슬롯 UI
│   ├── SummonInputHandler.cs           ← 소환 입력
│   └── AnchorBehaviour.cs              ← MonoBehaviour: 이동/교전
└── Infrastructure/
    ├── UnitSpawner.cs                  ← 프리팹 생성 + 스탯 적용
    ├── EnergySync.cs                   ← Photon 에너지 동기화
    └── BattleNetworkAdapter.cs         ← 전투 상태 동기화
```

**편성 데이터 → 전투 플로우**:

```
1. 로비에서 "시작" 클릭 → 모든 플레이어 garageReady == true 확인
2. SceneManager.LoadScene("JG_BattleScene_Redesign")
3. BattleBootstrap 초기화:
   - BattleSetup 생성
   - EventBus 주입
   - ModuleCatalog 로드
4. GarageToBattleFlow가 CustomProperties["garageRoster"] 읽기
   - 각 플레이어의 GarageRoster 역직렬화
   - BattleSession에 편성 데이터 로드
5. EnergySync 초기화 (초기 에너지 설정)
6. EnergyHUD, UnitSlotHUD 표시
7. 전투 중:
   - EnergySync가 틱 기반 에너지 증가 → ApplyEnergyTickUseCase
   - 플레이어가 UnitSlotHUD에서 유닛 선택 → SummonInputHandler
   - SummonUnitUseCase 실행 → 에너지 차감 → UnitSpawner
   - UnitSpawner가 프리팹 생성 → AnchorBehaviour 컴포넌트에 스탯 적용
   - AnchorBehaviour가 이동범위 내에서 자동 교전
```

**앵커 레이어 분리**:

```
Domain/Anchor.cs              ← 순수 C#
  - MoveRange (float)
  - OriginPosition (Vector2 구조체, Unity.Vector2 아님)
  - IsValidPosition(Vector2)  ← 범위 내 체크
  - GetNearestValidTarget()   ← 교전 규칙

Presentation/AnchorBehaviour.cs  ← MonoBehaviour
  - [SerializeField] Anchor anchor ← Domain 참조
  - Update()에서 anchor.IsValidPosition() 호출
  - NavAgent 또는 직접 Transform 제어
  - 가장 가까운 적 찾아 교전
```

**검증 기준**:
- [ ] 로비 → 새 전투 씬 전환 정상 동작
- [ ] 편성 유닛이 전투에서 올바른 스탯으로 생성
- [ ] 소환 시 에너지 차감 확인
- [ ] 이동범위 내에서만 유닛 이동/교전
- [ ] 앵커 반경 시각화 (Gizmos)
- [ ] 2인 코옵 동시 소환 테스트
- [ ] 기존 `JG_GameScene` 수정 없음 확인

---

### Phase 5: Wave 연속 압박 구조 + 로비 컷오버

**목표**: 보상 선택 제거 + 연속 압박 구현 + 로비 시작 경로 새 씬으로 전환

**상위 계획 정렬**: `implementation_plan_redesign_execution.md` Phase 6, 7 참조.

**작업 목록**:

| # | 작업 | 파일 | 설명 |
|---|---|---|---|
| 5.1 | Wave 보상 선택 UI 제거 | Wave Feature | 기존 보상 선택 루프 삭제 |
| 5.2 | 연속 압박 스포너 | `Battle/Infrastructure/WaveSpawner.cs` | 적 조합/진입 관리 |
| 5.3 | CoreObjective | `Battle/Presentation/CoreObjective.cs` | 코어 HP/파괴 판정 |
| 5.4 | BattleResultScreen | `Battle/Presentation/BattleResultScreen.cs` | 성공/실패 결과 화면 |
| 5.5 | 로비 시작 경로 변경 | Lobby Feature | "시작" → 새 전투 씬 로드 |
| 5.6 | 컷오버 체크리스트 검증 | — | 하단 체크리스트 참조 |

**검증 기준**:
- [ ] 전투 흐름이 "압박 대응 지속 → 성공/실패 종료"로 읽힘
- [ ] 코어 방어 승패 동작
- [ ] 로비에서 2~4인 입장 후 편성 완료 상태 표시
- [ ] 시작 시 새 전투 씬으로 이동
- [ ] 결과 화면 후 로비 복귀 또는 재시작

---

### Phase 6: 기존 시스템 마이그레이션

**목표**: 구형 Skill/Deck/Combat 시스템을 정리

**작업 목록**:

| # | 작업 | 설명 |
|---|---|---|
| 6.1 | InitializeDeckUseCase 제거 | Garage 시스템으로 완전 전환 |
| 6.2 | SkillData 의존성 제거 | 신규 Unit/Module 데이터만 사용 |
| 6.3 | Player 직접 조작 제거 | 이동/점프/다운/구조 삭제 또는 비활성화 |
| 6.4 | Combat FF 제거 | 프렌들리 파이어 파이프라인 정리 |
| 6.5 | SkillData SO 아카이브 | `Assets/_Archive/SkillData/` 이동 |
| 6.6 | JG_GameScene 아카이브 | 컷오버 완료 후 삭제 후보 |

**마이그레이션 원칙**:
- 컷오버 전에는 기존 코드를 삭제하지 않는다.
- 컷오버 후에는 SSOT와 충돌하는 구형 규칙부터 제거한다.
- 신규 코드는 오직 Garage/Battle 시스템만 사용한다.

---

## 씬 컷오버 체크리스트

새 전투 씬을 기본 경로로 바꾸기 전에 아래를 만족해야 한다.
(`implementation_plan_redesign_execution.md` 정의에 Garage 항목 추가)

- [ ] 로비에서 2~4인 입장 후 편성 완료 상태가 보인다
- [ ] 각 플레이어의 `garageRoster` CustomProperties 동기화 확인
- [ ] late-join 시 기존 플레이어 편성 데이터 복구
- [ ] 시작 시 새 전투 씬(`JG_BattleScene_Redesign`)으로 이동
- [ ] 각 플레이어가 유닛 슬롯과 에너지를 가진다
- [ ] 공용 코어가 존재하고 패배 조건이 동작한다
- [ ] 적이 연속 압박을 만든다
- [ ] 유닛 소환 위치와 타이밍이 실제 판단이 된다
- [ ] 결과 화면 후 로비 또는 재시작 흐름이 정리된다
- [ ] 기존 `JG_GameScene` 수정 없음 확인

---

## 의존성 순서

```
Phase 1 (도메인 + Composition Root)
    ↓
Phase 2 (데이터 + 네트워크 Port)
    ↓
Phase 3 (차고 UI + 로비 편성 단계)     ← 로비 씬에서 동작
    ↓
Phase 4 (새 전투 씬 + 소환/앵커)       ← JG_BattleScene_Redesign
    ↓
Phase 5 (연속 압박 + 컷오버)
    ↓
Phase 6 (구형 시스템 정리)
```

**병렬 가능**:
- Phase 1과 Phase 2의 ScriptableObject 구조 정의는 병렬 가능
- Phase 3의 UI 와이어프레임은 Phase 1 완료 전에도 시작 가능 (목 데이터 사용)
- Phase 4의 Battle Feature 구조 정의는 Phase 3 중에도 시작 가능

---

## 리스크 및 대응

| 리스크 | 영향 | 대응 |
|---|---|---|
| 비용 공식 밸런스 불안 | 특정 빌드 사기화 | 가중치 조정 가이드 참조, 빠른 피드백 루프 |
| 모듈 조합 경우의 수 폭발 | 테스트 부담 | MVP 9조합으로 제한, 확장 시 별도 관리 |
| CustomProperties 직렬화 크기 초과 | 네트워크 지연 | GarageRoster JSON 크기 모니터링, 압축 고려 |
| late-join 복구 타이밍 이슈 | 편성 데이터 유실 | OnPhotonPlayerPropertiesChanged에서幂等성 보장 |
| 기존 Skill 의존성 정리 지연 | Phase 6 지연 | 점진적 아카이브, 신규 코드는 절대 참조 금지 |
| 앵커 기반 교전 AI 복잡성 | 구현 시간 초과 | 첫 버전은 "가장 가까운 적 공격"만 |
| 기존 JG_GameScene 실수 수정 | 구형/신규 충돌 | 컷오버 전까지 수정 금지 원칙 팀 공유 |

---

## 참고 문서

- `../design/game_design.md` — 게임 디자인 SSOT
- `unit_module_design.md` — 유닛/모듈 설계 SSOT
- `module_data_structure.md` — 데이터 구조 정의
- `garage_ui_design.md` — 차고 UI 기획
- `implementation_plan_redesign_migration.md` — 전체 마이그레이션 계획
- `implementation_plan_redesign_execution.md` — 병렬 씬 전략 및 실행 순서
- `../../agent/architecture.md` — Feature-first Clean Architecture 규칙
- `../../agent/state_ownership.md` — CustomProperties 소유권 맵
