# Enemy Feature

적 엔티티 생성, AI, 전투 통합, 접촉 데미지.

## 먼저 읽을 규칙

- 전역 구조, scene contract 체크리스트: [architecture.md](../../../../agent/architecture.md)
- 크로스 피처 포트 소유권: [port_ownership.md](../../../../docs/design/port_ownership.md)

## 씬 계약 (적 프리팹)

### 필수 Inspector 참조 (EnemySetup — Resources/EnemyCharacter.prefab)

| 필드 | 타입 | 용도 |
|---|---|---|
| `_networkAdapter` | `EnemyNetworkAdapter` | Photon 네트워크 |
| `_aiAdapter` | `EnemyAiAdapter` | 이동 AI |
| `_view` | `EnemyView` | 시각 표현 |
| `_healthBarPrefab` | `GameObject` | 체력바 프리팹 |
| `_contactDetector` | `EnemyContactDamageDetector` | 접촉 데미지 |
| `_entityIdHolder` | `EntityIdHolder` | DomainEntityId 식별 |

### 프리팹 요구사항

- PhotonView, EnemySetup, EnemyNetworkAdapter, EnemyAiAdapter
- EnemyView, EnemyContactDamageDetector, EntityIdHolder
- Collider (trigger) + Rigidbody (kinematic)

### 초기화 순서

```
1. Master: EnemySpawnAdapter.SpawnEnemy() → PhotonNetwork.Instantiate
2. Master: EnemySetup.Initialize(eventBus, combatBootstrap, data, playerQuery, coreQuery)
3. Non-Master: EnemyArrived static event → fallback Initialize()
```

### 네트워크 권한

| 행위 | 권한자 | 동기화 |
|---|---|---|
| 스폰 | Master | `PhotonNetwork.Instantiate` |
| AI 이동 | Master | `IPunObservable` 위치 동기화 |
| 투사체 데미지 | 투사체 소유자 | 기존 Combat RPC |
| 접촉 데미지 | Master | `CombatBootstrap.ApplyDamage` → RPC |
| 소멸 | Master | `PhotonNetwork.Destroy` |
