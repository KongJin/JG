# Garage Feature

차고(Garage) 피처는 전투 전 유닛 편성 및 모듈 조합을 담당한다.
로비 씬(`JG_LobbyScene.unity`) 내부에서 동작하며, 편성 결과는 CustomProperties를 통해 룸 전체에 동기화된다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, 포트 위치: [architecture.md](../../../../agent/architecture.md)
- 구현 금지 사항과 EventHandler/Bootstrap 역할 분리: [anti_patterns.md](../../../../agent/anti_patterns.md)
- Garage CustomProperties 소유권: [state_ownership.md](../../../../agent/state_ownership.md)
- 유닛/모듈 설계 SSOT: [unit_module_design.md](../../../../docs/design/unit_module_design.md)
- 데이터 구조 정의: [module_data_structure.md](../../../../docs/design/module_data_structure.md)

## 이 피처의 책임

- 유닛 프레임 + 화력/기동 모듈 조합 계산
- 조합 유효성 검증 (금지조합 차단)
- 소환 비용 자동 계산 (가중치 + 분산 페널티)
- 편성 데이터 로컬 저장 (JSON 보조 캐시)
- 편성 데이터 네트워크 동기화 (Photon Player CustomProperties)
- late-join 시 편성 데이터 복구

## 로컬 계약

### 레이어 구조

```
Assets/Scripts/Features/Garage/
├── GarageSetup.cs                      ← Composition root (의존성 주입)
├── GarageBootstrap.cs                  ← Scene-level wiring (EventBus 주입)
├── Domain/
│   ├── Unit.cs                         ← 전투 유닛 엔티티 (순수 C#)
│   ├── UnitComposition.cs              ← 조합 검증 + 스탯 계산 (순수 C#)
│   ├── CostCalculator.cs               ← 비용 공식 (순수 C#)
│   ├── GarageRoster.cs                 ← 편성 데이터 구조 (순수 C#)
│   └── PassiveEffect.cs                ← 패시브 효과 도메인 (순수 C#)
├── Application/
│   ├── Ports/
│   │   ├── IGarageNetworkPort.cs       ← 편성 네트워크 동기화 포트
│   │   └── IGaragePersistencePort.cs   ← 저장/불러오기 포트
│   ├── InitializeGarageUseCase.cs      ← 차고 초기화
│   ├── ComposeUnitUseCase.cs           ← 유닛 조합 계산
│   ├── ValidateRosterUseCase.cs        ← 편성 유효성 검증
│   ├── SaveRosterUseCase.cs            ← 편성 저장 + 네트워크 동기화
│   └── GarageEvents.cs                 ← 도메인 이벤트 정의
├── Presentation/                       ← (Phase 3에서 구현)
└── Infrastructure/
    ├── UnitFrameData.cs                ← ScriptableObject: 프레임
    ├── FirepowerModuleData.cs          ← ScriptableObject: 상단 모듈
    ├── MobilityModuleData.cs           ← ScriptableObject: 하단 모듈
    ├── PassiveTraitData.cs             ← ScriptableObject: 고유 특성
    ├── ModuleCatalog.cs                ← 카탈로그 SO
    ├── GarageNetworkAdapter.cs         ← Photon CustomProperties 동기화
    └── GarageJsonPersistence.cs        ← JSON 저장/불러오기 구현
```

### 네트워크 동기화

| 데이터 | 방식 | CustomProperties 키 | 소유권 |
|---|---|---|---|
| 편성 데이터 | `CustomProperties` (Player) | `garageRoster` (JSON) | Garage (쓰기), 다른 피처 (읽기) |
| 편성 완료 | `CustomProperties` (Player) | `garageReady` (bool) | Garage (쓰기), 다른 피처 (읽기) |

**규칙**:
- 각 플레이어는 자신의 `garageRoster`만 쓰기 가능.
- 다른 플레이어의 편성은 읽기 전용 (비교 표시용).
- late-join 시 `OnPlayerPropertiesUpdate`에서 모두 복구.
- host migration 시 Player Property이므로 유지됨.

### 초기화 순서

Garage는 로비 씬에서 초기화된다. (`initialization_order.md` 참조)

```
1. LobbyBootstrap가 EventBus 생성
2. GarageBootstrap.Initialize(eventBus) 호출
3. GarageBootstrap이 Infrastructure 어댑터 생성
   - GarageJsonPersistence (로컬 저장)
   - GarageNetworkAdapter (네트워크 동기화)
   - CompositionDataProvider (SO 카탈로그 조회)
   - RosterValidationProvider (조합 검증)
4. GarageSetup.Initialize(ports, providers) 호출
5. InitializeGarageUseCase.Execute() → 이전 편성 복원
```

### late-join / reconnect behavior

- late-join 플레이어가 로비에 입장하면, 기존 플레이어들의 `garageRoster` / `garageReady`가 이미 CustomProperties에 설정되어 있다.
- `GarageNetworkAdapter`가 `OnPlayerPropertiesUpdate` 콜백에서 자동으로 캐시 복구.
- late-join 플레이어 자신의 편성은 `InitializeGarageUseCase`가 로컬 JSON에서 복원 시도.

## 도메인 모델

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

## Bootstrap

- **GarageBootstrap** (`ServiceRoot/GarageBootstrap` 또는 로비 씬의 적절한 위치):
  - `[Required, SerializeField]`로 `ModuleCatalog` 연결
  - `Initialize(eventBus)` 호출받아 Infrastructure 어댑터 + GarageSetup 조립
  - `OnDestroy()`에서 `Cleanup()` 호출
- **GarageSetup** (순수 C# class):
  - UseCases 생성 및 Port 연결
  - `DisposableScope`로 EventBus 구독 수명 관리

## 피처 간 의존

- **Lobby**: 로비 씬에서 초기화됨. 로비의 EventBus를 공유.
- **Shared**: EventBus, DomainEntityId, Result, DisposableScope, JsonObjectPool
- **Battle (Phase 4)**: 편성 데이터를 `garageRoster` CustomProperties로 공급
