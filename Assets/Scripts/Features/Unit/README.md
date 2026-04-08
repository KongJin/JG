# Unit Feature

유닛 스펙 정의, 조합 검증, 비용 계산을 담당하는 독립 피처다.
Garage가 조합을 요청하고, Player/Enemy/Combat이 전투 중에 소비한다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, 포트 위치: [architecture.md](../../../../agent/architecture.md)
- 구현 금지 사항과 Bootstrap 역할 분리: [anti_patterns.md](../../../../agent/anti_patterns.md)
- 게임 디자인 SSOT: [game_design.md](../../../../docs/design/game_design.md)
- 유닛/모듈 설계 SSOT: [unit_module_design.md](../../../../docs/design/unit_module_design.md)
- 데이터 구조 정의: [module_data_structure.md](../../../../docs/design/module_data_structure.md)
- Unit Feature 분리 설계: [unit_feature_separation.md](../../../../docs/design/unit_feature_separation.md)

## 이 피처의 책임

- `Unit` 도메인 엔티티 정의 (불변, 순수 C#)
- `UnitComposition` 조합 검증 + 스탯 계산 (순수 C#)
- `CostCalculator` 소환 비용 자동 계산 (순수 C#)
- `Module` 타입 안전 제공 (`ModuleType`, `ModuleId`, `ModuleStats`)
- `ComposeUnitUseCase` 조합 계산 UseCase
- ScriptableObject 데이터 카탈로그 (`ModuleCatalog`, `UnitFrameData`, `FirepowerModuleData`, `MobilityModuleData`, `PassiveTraitData`)

## 로컬 계약

### 레이어 구조

```
Assets/Scripts/Features/Unit/
├── UnitSetup.cs                          ← Composition root (의존성 주입)
├── UnitBootstrap.cs                      ← Scene-level wiring (EventBus 주입)
├── Domain/
│   ├── Unit.cs                           ← 전투 유닛 엔티티 (순수 C#, 불변)
│   ├── UnitComposition.cs                ← 조합 검증 + 스탯 계산 (순수 C#)
│   ├── CostCalculator.cs                 ← 비용 공식 (순수 C#)
│   ├── PassiveEffect.cs                  ← 패시브 효과 도메인 (enum + 값 객체)
│   └── Module.cs                         ← 모듈 타입 안전 (ModuleType, ModuleId, ModuleStats)
├── Application/
│   ├── ComposeUnitUseCase.cs             ← 유닛 조합 계산
│   └── Ports/
│       └── IUnitCompositionPort.cs       ← 조합 데이터 조회 포트
├── Infrastructure/
│   ├── UnitCompositionProvider.cs        ← IUnitCompositionPort 구현
│   ├── ModuleCatalog.cs                  ← SO 카탈로그
│   ├── UnitFrameData.cs                  ← ScriptableObject: 프레임
│   ├── FirepowerModuleData.cs            ← ScriptableObject: 상단 모듈
│   ├── MobilityModuleData.cs             ← ScriptableObject: 하단 모듈 (anchorRange 포함)
│   └── PassiveTraitData.cs               ← ScriptableObject: 고유 특성
└── Presentation/                         ← (미구현, 전투 Feature가 사용)
```

### 조합 검증 규칙 (UnitComposition.Validate)

유닛은 다음 조건 중 하나를 만족해야 한다:

1. `이동범위 ≥ 4m` (근접형 — 적을 쫓아갈 수 있음)
2. `사거리 ≥ 6m` (원거리형 — 제자리에서 화력)
3. `이동범위 ≥ 3m AND 사거리 ≥ 4m` (하이브리드형)

이를 만족하지 않으면 금지조합이다.

### 소환 비용 계산 (CostCalculator)

```
기본 비용 = (HP × 0.02) + (공격력 × 0.5) + (공격속도 × 3.0) + (사거리 × 2.0) + (이동범위 × 1.5)
분산 페널티 = 표준편차(각 스탯의 가중치 적용된 값) × 0.3
최종 비용 = round(기본 비용 + 분산 페널티 + 고유특성 보정값)
범위: 15 ~ 80
```

### 앵커 반경 (AnchorRange)

앵커 반경은 **기동 모듈(Mobility Module)** 이 결정한다.
`MobilityModuleData.anchorRange` 필드에 저장되며, `ModuleStats.AnchorRange`를 통해 계산에 사용된다.

### CustomProperties 소유권

- `battle_hp`는 Unit이 소유하는 Room `CustomProperties` 키다.
- `battle_dead`는 Unit이 소유하는 Room `CustomProperties` 키다.
- 쓰기 주체는 `BattleEntityPhotonController.SyncState()`를 호출하는 owner 인스턴스이며, 전투 상태 변경 시 authoritative write를 수행한다.
- 다른 클라이언트와 다른 피처는 이 키들을 read-only로만 소비한다.
- 목적은 전투 중 BattleEntity 상태 동기화와 late-join 시 상태 복원의 근거를 남기는 것이다.

## Bootstrap

- **UnitBootstrap** (`ServiceRoot/UnitBootstrap` 또는 로비 씬의 적절한 위치):
  - `[Required, SerializeField]`로 `ModuleCatalog` 연결
  - `Initialize(eventBus)` 호출받아 Infrastructure 어댑터 + UnitSetup 조립
  - `OnDestroy()`에서 `Cleanup()` 호출
- **UnitSetup** (순수 C# class):
  - `UnitCompositionProvider` 생성 (ModuleCatalog 기반)
  - `ComposeUnitUseCase` 생성 + 포트 주입
  - `DisposableScope`로 EventBus 구독 수명 관리

## 초기화 순서

```
1. LobbyBootstrap가 EventBus 생성
2. UnitBootstrap.Initialize(eventBus) 호출
   - UnitCompositionProvider 생성
   - ComposeUnitUseCase 생성
3. GarageBootstrap.Initialize(eventBus) 호출
   - UnitBootstrap.Setup.ComposeUnit 을 GarageSetup에 전달
4. InitializeGarageUseCase.Execute() → 이전 편성 복원
```

## 피처 간 의존

- **Garage**: Unit을 조합 (ComposeUnitUseCase 호출)
- **Player** (미래): Unit.Domain 읽기 전용 (전투 유닛 스펙 참조)
- **Enemy** (미래): Unit.Domain 읽기 전용
- **Combat** (미래): Unit.Domain 읽기 전용
- **Shared**: EventBus, DomainEntityId, Result, DisposableScope

## 포트 소유권

`IUnitCompositionPort`는 **Garage가 정의**하고(소비자), **Unit Infrastructure가 구현**한다(제공자).
Garage는 Unit의 `ComposeUnitUseCase`를 직접 호출한다.

```
Garage/Application/Ports/IUnitCompositionPort.cs   ← Garage가 정의 (소비자)
Unit/Infrastructure/UnitCompositionProvider.cs      ← Unit이 구현 (제공자)
Garage → Unit.ComposeUnitUseCase.Execute()           ← Garage가 소비
```
