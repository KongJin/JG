# 네이밍 규칙

---

## 기본 규칙

| 타입 | 패턴 | 예시 |
|------|------|------|
| **Entity** | 접미사 없음 | `Lobby`, `Room`, `RoomMember` |
| **UseCase** | `XxxUseCase` 또는 통합 | `CreateRoomUseCase`, `JoinRoomUseCase`, `LobbyUseCases` |
| **NetworkEventHandler** | `XxxNetworkEventHandler` | `LobbyNetworkEventHandler`, `PlayerNetworkEventHandler` |
| **Port 인터페이스** | `IXxxRepository`, `IXxxPort` | `ILobbyRepository`, `IPlayerNetworkCommandPort` |
| **Event** | 과거 시제 + `Event` | `LobbyUpdatedEvent`, `GameStartedEvent` |
| **EventBus** | `IEventPublisher`, `IEventSubscriber`, `EventBus` | `Shared/EventBus/`에 |
| **Adapter** | `XxxAdapter` | `LobbyPhotonAdapter`, `ClockAdapter` |
| **View** | `XxxView` | `LobbyView`, `RoomListView` |
| **InputHandler** | `XxxInputHandler` | `LobbyInputHandler` |

---

## Type naming safety

feature namespace 이름과 같은 short type (`Unit`, `Player`, `Wave` 등)을 bare identifier로 타입처럼 쓰지 않습니다.

**예시:**
- `Features.Unit` namespace
- `Features.Unit.Domain.Unit` type

이 경우 아래를 따릅니다:
- same feature 안에서는 `Unit`, `Player`, `Wave` 같은 이름을 bare identifier 타입처럼 쓰지 않음
- **alias를 필수 기본값**으로 사용: `using UnitSpec = Features.Unit.Domain.Unit;`
- alias가 없으면 fully-qualified name 사용

이 규칙은 compile-clean을 위한 구조 규칙입니다.
