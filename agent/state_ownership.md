# /agent/state_ownership.md

## CustomProperties 소유권 규칙

### 원칙

하나의 CustomProperties 키는 **정확히 하나의 피처만 쓰기(write) 권한**을 갖는다.
다른 피처는 읽기(read)만 허용된다. 이 규칙은 anti_patterns.md의 "Dual state" 금지를 네트워크 계층까지 확장한 것이다.

---

### 현재 CustomProperties 소유권 맵

#### Player CustomProperties (Photon Player)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `hp` | Player | `PlayerNetworkAdapter.SyncHealth()` | 현재 HP |
| `maxHp` | Player | `PlayerNetworkAdapter.SyncHealth()` | 최대 HP |
| `mana` | Player | `PlayerNetworkAdapter.SyncMana()` | 현재 Mana |
| `maxMana` | Player | `PlayerNetworkAdapter.SyncMana()` | 최대 Mana |
| `lifeState` | Player | `PlayerNetworkAdapter.SyncLifeState()` | 0=Alive, 1=Downed, 2=Dead |

#### Lobby CustomProperties (Photon Player / Room)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `memberId` | Lobby | `PhotonPlayerPropertyManager` | 멤버 식별 |
| `displayName` | Lobby | `PhotonPlayerPropertyManager` | 닉네임 |
| `team` | Lobby | `PhotonPlayerPropertyManager` | 팀 |
| `isReady` | Lobby | `PhotonPlayerPropertyManager` | 레디 상태 |
| `roomDisplayName` | Lobby | `PhotonPlayerPropertyManager` | 방 이름 |

#### Skill CustomProperties (Photon Player)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `skillsReady` | Skill | `SkillNetworkAdapter.SyncSkillsReady()` | 스킬 선택 완료 여부 |

#### Garage CustomProperties (Photon Player)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `garageRoster` | Garage | `GarageNetworkAdapter.SyncRoster()` | JSON 직렬화 편성 데이터 (3~5기 유닛 조합). **읽기:** Battle Feature (전투 진입 시 복원). late-join 시 `OnPlayerPropertiesUpdate`에서 복구. |
| `garageReady` | Garage | `GarageNetworkAdapter.SyncReady()` | 편성 완료 여부 (bool). **읽기:** Lobby Feature (시작 버튼 활성화 조건). |

#### Room CustomProperties (Photon Room — 로비 단계, Lobby 전용 쓰기)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `difficultyPreset` | Lobby | `LobbyPhotonAdapter.CreateRoom` (`LobbyPhotonConstants.DifficultyPresetKey`) | 난이도 프리셋 `int`: 0 Normal, 1 Easy, 2 Hard. **읽기:** Wave 피처 `RoomDifficultyReader` / `WaveRoomPropertyKeys.DifficultyPreset` (문자열 동일). 게임 시작 후 변경하지 않음. |

#### Wave CustomProperties (Photon Room)

| 키 | 소유 피처 | 쓰기 위치 | 용도 |
|----|-----------|-----------|------|
| `waveIndex` | Wave | `WaveNetworkAdapter.SyncWaveState()` | 현재 웨이브 인덱스 |
| `waveState` | Wave | `WaveNetworkAdapter.SyncWaveState()` | 현재 웨이브 상태 (WaveState enum int) |
| `countdownEnd` | Wave | `WaveNetworkAdapter.SyncWaveState()` | 카운트다운 종료 시각 (ServerTimestamp ms). Countdown 상태에서만 유의미 |

---

### 새 피처가 상태를 동기화해야 할 때

#### 1. 기존 키에 쓰기가 필요한 경우

**금지.** 해당 키의 소유 피처를 통해 간접 수정한다.

```
❌ Status 피처가 직접 CustomProperties["hp"]를 수정
✅ Status 피처가 Player 도메인의 Heal() 호출 → Player가 SyncHealth()
```

#### 2. 새 키가 필요한 경우

1. 이 문서의 소유권 맵에 키를 추가한다.
2. 쓰기는 소유 피처의 NetworkAdapter에서만 수행한다.
3. 읽기(Hydrate 포함)는 다른 피처도 가능하지만, 해당 피처의 README에 명시한다.

#### 3. 여러 피처가 같은 값을 변경해야 하는 경우

도메인 엔티티를 소유한 피처가 **유일한 쓰기 경로**를 제공한다.
다른 피처는 이벤트나 UseCase 호출을 통해 변경을 요청한다.

```
예: HP 변경
- Combat: DamageAppliedEvent 발행 → PlayerDamageEventHandler가 Player.TakeDamage() 호출
- Status: HealTick → Player.Heal() 호출 (UseCase 경유)
- Player: 도메인 상태 변경 후 SyncHealth() 실행 — 유일한 쓰기 경로
```

---

### 동기화 채널 선택 기준

| 데이터 특성 | 채널 | 예시 |
|-------------|------|------|
| 매 프레임 변경 (연속) | `OnPhotonSerializeView` | Position, Rotation |
| 현재 상태 (late-join 필요) | `CustomProperties` | HP, Mana, LifeState |
| 특정 시점 이벤트 (일회성) | `RPC` | Jump, Damage, Rescue |

**하나의 데이터에 두 채널을 동시에 사용하지 않는다.** 예: HP를 RPC로도 보내고 CustomProperties로도 보내면 불일치가 생긴다.

---

### 금지 사항

- 소유 피처가 아닌 곳에서 `SetCustomProperties` 호출
- 같은 키를 두 피처가 쓰기
- 하나의 데이터를 두 채널(RPC + CustomProperties)로 동시 동기화
- CustomProperties 키를 문서화하지 않고 추가
