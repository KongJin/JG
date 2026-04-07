# Garage Feature

차고(Garage) 피처는 전투 전 유닛 편성 및 모듈 조합을 담당한다.
로비 씬(`JG_LobbyScene.unity`) 내부에서 동작하며, 편성 결과는 CustomProperties를 통해 룸 전체에 동기화된다.

## 먼저 읽을 규칙

- 전역 구조, 레이어, 포트 위치: [architecture.md](../../../../agent/architecture.md)
- 구현 금지 사항과 Bootstrap 역할 분리: [anti_patterns.md](../../../../agent/anti_patterns.md)
- Garage CustomProperties 소유권: 이 문서의 `### 네트워크 동기화`
- 유닛/모듈 설계 SSOT: [unit_module_design.md](../../../../docs/design/unit_module_design.md)
- 데이터 구조 정의: [module_data_structure.md](../../../../docs/design/module_data_structure.md)
- Unit Feature 분리 설계: [unit_feature_separation.md](../../../../docs/design/unit_feature_separation.md)

## 이 피처의 책임

- 편성 목록 관리 (`GarageRoster` — UnitLoadout[] ID 조합 저장)
- Unit Feature를 통한 조합 검증 (`ComposeUnitUseCase` 호출)
- 편성 데이터 로컬 저장 (JSON 보조 캐시)
- 편성 데이터 네트워크 동기화 (Photon Player CustomProperties)
- late-join 시 편성 데이터 복구

## Unit Feature와의 관계

Unit 스펙 계산(`UnitComposition`, `CostCalculator`, `Unit` 엔티티)은 **Unit Feature**가 담당한다.
Garage는 Unit의 `ComposeUnitUseCase`를 호출하여 조합 결과를 얻는다.

| Garage의 역할 | Unit과의 관계 |
|---|---|
| 편성 목록 관리 (`GarageRoster`) | `UnitLoadout` (frameId, moduleId들) 저장 |
| 조합 요청 | `UnitBootstrap.Setup.ComposeUnit` 호출 |
| 유효성 검증 | Unit.Feature의 `UnitComposition.Validate` 사용 |

## 로컬 계약

### 레이어 구조

```
Assets/Scripts/Features/Garage/
├── GarageSetup.cs                      ← Composition root (의존성 주입)
├── GarageBootstrap.cs                  ← Scene-level wiring (EventBus 주입)
├── Domain/
│   └── GarageRoster.cs                 ← 편성 데이터 구조 (순수 C#)
├── Application/
│   ├── Ports/
│   │   ├── IGarageNetworkPort.cs       ← 편성 네트워크 동기화 포트
│   │   └── IGaragePersistencePort.cs   ← 저장/불러오기 포트
│   ├── InitializeGarageUseCase.cs      ← 차고 초기화
│   ├── ValidateRosterUseCase.cs        ← 편성 유효성 검증
│   ├── SaveRosterUseCase.cs            ← 편성 저장 + 네트워크 동기화
│   └── GarageEvents.cs                 ← 도메인 이벤트 정의
├── Presentation/                       ← (Phase 3에서 구현)
└── Infrastructure/
    ├── RosterValidationProvider.cs     ← 조합 검증 제공 (Unit.ModuleCatalog 사용)
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

Garage는 로비 씬에서 초기화된다. (이 문서의 초기화 순서 섹션 참조)

```
1. LobbyBootstrap가 EventBus 생성
2. UnitBootstrap.Initialize(eventBus) 호출
   - UnitCompositionProvider 생성 (IUnitCompositionPort 구현)
   - ComposeUnitUseCase 생성
3. GarageBootstrap.Initialize(eventBus, compositionPort, unitCatalog) 호출
   - RosterValidationProvider 생성 (unitCatalog 기반)
   - ComposeUnitUseCase 생성 (compositionPort 기반)
4. InitializeGarageUseCase.Execute() → 이전 편성 복원
```

### late-join / reconnect behavior

- late-join 플레이어가 로비에 입장하면, 기존 플레이어들의 `garageRoster` / `garageReady`가 이미 CustomProperties에 설정되어 있다.
- `GarageNetworkAdapter`가 `OnPlayerPropertiesUpdate` 콜백에서 자동으로 캐시 복구.
- late-join 플레이어 자신의 편성은 `InitializeGarageUseCase`가 로컬 JSON에서 복원 시도.

## Bootstrap

- **GarageBootstrap** (`ServiceRoot/GarageBootstrap` 또는 로비 씬의 적절한 위치):
  - `[Required, SerializeField]`로 `GarageNetworkAdapter` 연결
  - `Initialize(eventBus, compositionPort, unitCatalog)` 호출:
    - `compositionPort`: Unit이 제공하는 `IUnitCompositionPort` 구현
    - `unitCatalog`: Unit의 `ModuleCatalog` (Scene에서 `[SerializeField]`로 연결)
  - `OnDestroy()`에서 `Cleanup()` 호출
- **GarageSetup** (순수 C# class):
  - UseCases 생성 및 Port 연결
  - `DisposableScope`로 EventBus 구독 수명 관리
  - `ComposeUnitUseCase`는 `IUnitCompositionPort`로 생성

## 피처 간 의존

- **Unit**: 조합 계산 (`ComposeUnitUseCase` 호출)
- **Lobby**: 로비 씬에서 초기화됨. 로비의 EventBus를 공유.
- **Shared**: EventBus, DomainEntityId, Result, DisposableScope, JsonObjectPool
- **Battle (Phase 4)**: 편성 데이터를 `garageRoster` CustomProperties로 공급
