# Garage Feature

전투 전 유닛 편성 및 모듈 조합. 로비 씬에서 동작.

## 먼저 읽을 규칙

- 전역 구조, 레이어, 포트 위치: [architecture.md](../../../../agent/architecture.md)
- 포트 소유권 패턴: [architecture-diagram.md](../../../../docs/design/architecture-diagram.md#포트-소유권-패턴)

## 씬 계약 (JG_LobbyScene)

### CustomProperties 소유권

| 데이터 | 키 | 타입 | 소유권 |
|---|---|---|---|
| 편성 데이터 | `garageRoster` | JSON string | Garage (쓰기), 다른 피처 (읽기) |
| 편성 완료 | `garageReady` | bool | Garage (쓰기), 다른 피처 (읽기) |

### 초기화 순서

```
1. LobbyBootstrap: EventBus 생성
2. UnitBootstrap.Initialize(eventBus)
3. GarageBootstrap.Initialize(eventBus, compositionPort, unitCatalog)
4. InitializeGarageUseCase.Execute() → 이전 편성 복원
```

### Late-join / Reconnect

- late-join 시 `OnPlayerPropertiesUpdate`에서 기존 플레이어 `garageRoster` 자동 복구
- 자신의 편성은 `InitializeGarageUseCase`가 로컬 JSON에서 복원 시도
