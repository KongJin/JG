# Lobby Feature

멀티플레이 로비 기능. 방 생성/입장/퇴장, 팀 변경, 레디, 게임 시작을 담당한다.

## 책임

- 방 목록 조회 및 표시
- 방 생성/입장/퇴장
- 방 **난이도 프리셋** (Room `CustomProperties`: `difficultyPreset` int 0 Normal / 1 Easy / 2 Hard) — 방 생성 시 `LobbyPhotonAdapter`가 설정, Wave 피처는 읽기만 ([state_ownership.md](../../../../agent/state_ownership.md))
- 방 내 팀 변경, 레디 상태 토글
- 게임 시작 조건 검증 및 시작 트리거

## 이벤트 흐름

### 명령 경로 (로컬 → 네트워크)

```
LobbyView (UI 입력)
  → LobbyUseCases.CreateRoom / JoinRoom / LeaveRoom / ChangeTeam / SetReady / StartGame
    → ILobbyRepository로 현재 상태 검증
    → ILobbyNetworkCommandPort (LobbyPhotonAdapter)
      → Photon API 호출 (CreateRoom, CustomProperties 등)
```

### 콜백 경로 (네트워크 → UI)

```
Photon 콜백 (OnCreatedRoom, OnJoinedRoom, OnPlayerEnteredRoom 등)
  → LobbyPhotonAdapter → ILobbyNetworkCallbackPort Action 호출
    → LobbyNetworkEventHandler
      → ILobbyRepository 업데이트 (도메인 상태 반영)
      → EventBus 이벤트 발행:
          LobbyUpdatedEvent, RoomUpdatedEvent, GameStartedEvent 등
        → LobbyView가 구독하여 UI 갱신
```

### 핵심 설계: 이벤트 드리븐

UseCase는 커맨드만 발사하고 끝낸다.
Photon 콜백 해석과 도메인 상태 업데이트는 `LobbyNetworkEventHandler`가 처리한다.

### 에러 처리

로비는 동기/비동기 실패를 모두 씬 공통 에러 UI로 수렴시킨다.

| 에러 종류 | 처리 방식 |
|---|---|
| 동기 유효성 에러 (방 이름 중복 등) | UseCase가 `Result.Failure` 반환 → Presentation이 `UiErrorRequestedEvent(Banner)` 발행 |
| 비동기 네트워크 에러 (방 입장 실패 등) | `LobbyNetworkEventHandler`가 `UiErrorRequestedEvent(Banner)` 발행 |
| 프로그래밍 오류 (Inspector 미연결 등) | `Debug.LogError`만 남기고 UI에는 자동 노출하지 않음 |

## 네트워크 동기화

| 데이터 | 방식 | 용도 |
|---|---|---|
| 팀, 레디, 닉네임 | `CustomProperties` (상태 동기화) | 늦게 입장한 유저에게 자동 동기화 |
| 게임 시작 | `RaiseEvent` (이산 이벤트) | 전체 방 알림 |
| 방 입퇴장 | Photon 자체 콜백 | 멤버 변동 감지 |

`LobbyPhotonAdapter`가 `ILobbyNetworkCommandPort`(송신)와 `ILobbyNetworkCallbackPort`(수신)을 모두 구현한다.

## 도메인 모델

- **Lobby**: 방 컬렉션 관리 (aggregate root)
- **Room**: 멤버 관리, 팀/레디 상태
- **RoomMember**: Id, DisplayName, Team, IsReady
- **LobbyRule**: 방 생성/게임 시작 조건 검증

주의: `Lobby` 클래스명이 `Features.Lobby` 네임스페이스와 충돌하므로
다른 레이어에서 사용 시 `using DomainLobby = Features.Lobby.Domain.Lobby;` alias 필요.

## Photon ↔ 도메인 매핑

| Photon | 도메인 |
|---|---|
| `PhotonNetwork.CurrentRoom.Name` | `Room.Id.Value` |
| `CustomProperties["roomDisplayName"]` | `Room.Name` |
| `CustomProperties["difficultyPreset"]` | `Room.DifficultyPresetId` |
| `Room.MaxPlayers` | `Room.Capacity` |
| `MasterClientId` | `Room.OwnerId` |
| `Player.CustomProperties["memberId"]` | `RoomMember.Id.Value` |
| `Player.CustomProperties["displayName"]` | `RoomMember.DisplayName` |
| `Player.CustomProperties["team"]` | `RoomMember.Team` |
| `Player.CustomProperties["isReady"]` | `RoomMember.IsReady` |

## JG_LobbyScene (씬 계약)

- **MCP/로컬 반복 테스트**: `RoomNameInput`·`DisplayNameInput`·`CapacityInput`에 씬 저장 시 기본 문자열이 들어가 있어야 `CreateRoom` 검증(방 이름 2글자 이상 등)을 타이핑 없이 통과할 수 있다. 프로덕션 빌드에서 비우려면 별도 정책으로 조정한다.
- **난이도 (선택)**: `RoomListView`에 `TMP_Dropdown _difficultyDropdown`을 연결하면 옵션 순서가 **0=Normal, 1=Easy, 2=Hard**여야 한다. 미연결이면 항상 Normal(0). 드롭다운 값은 **그대로** `LobbyUseCases.CreateRoom`에 넘기며, 0~2 밖이면 `LobbyRule.ValidateDifficultyPreset`에서 실패한다(뷰에서 중복 클램프하지 않음). `RoomItemView`·`RoomDetailView`의 난이도 텍스트 필드는 선택 사항(미연결 시 비워 둠).

## Bootstrap

- **LobbyBootstrap** (씬 오브젝트, 피처 루트에 위치): `SceneErrorPresenter` → LobbyPhotonAdapter → LobbyNetworkEventHandler → LobbyUseCases → LobbyView 순서로 조립
- `SceneLoaderAdapter`는 plain C# class이므로 Inspector에서 연결하지 않고 `Awake()`에서 직접 생성한다
- `LobbyView`는 에러 텍스트를 직접 관리하지 않고, 씬 공통 `SceneErrorPresenter`가 배너를 렌더링한다
- Inspector 연결 필드는 `[Required, SerializeField]`로 선언해 씬 저장 시 누락을 검증한다

## 레이어 메모

- **Presentation**: `RoomListView`와 `RoomDetailView`는 `GameObjectPool` (`Shared/Runtime/Pooling`)을 사용하여 리스트 아이템을 재활용한다. `Initialize()`에서 풀을 생성하고, `Render()` 시 `Rent()`, `Clear()` 시 `Return()`으로 GC 압박을 줄인다. 풀에서 꺼낸 아이템은 `GetComponent`로 View를 얻는다 (런타임 풀 인스턴스라 Inspector wiring 불가 — Runtime Lookup Policy 허용 예외).

## 피처 간 의존

- **독립적**: 다른 피처에 의존하지 않음
- **Shared**: EventBus, DomainEntityId, Result, IClockPort, `UiErrorRequestedEvent`, `GameObjectPool`
