# Lobby Feature

## Responsibility

`Lobby`는 Photon 로비 연결 이후 방 생성/참가/팀 변경/준비/게임 시작 흐름과 로비 UI 렌더링을 소유한다.

## Entrypoints

* `LobbySetup.cs`
  scene-level wiring, scene EventBus 생성, Lobby/Photon/View 조립
* `Presentation/LobbyView.cs`
  로비 패널 전환, `Lobby/Garage` 탭 전환, 게임 시작 씬 로드 이벤트 발행
* `Infrastructure/Photon/LobbyPhotonAdapter.cs`
  Photon room/lobby 콜백과 게임 시작 네트워크 브리지

## Local Contract

* `LobbySetup`는 `LobbyView`, `LobbyPhotonAdapter`, `SceneErrorPresenter`, `SoundPlayer`를 inspector로 받아야 한다.
* `UnitSetup`, `GarageSetup`는 선택 참조다. 둘 다 있으면 로비 진입 시 함께 초기화하고, 없으면 로비 핵심 흐름만 유지한다.
* `LobbyView`는 `RoomListPanel`, `RoomDetailPanel`, `RoomListView`, `RoomDetailView` 참조가 필요하다.
* `LobbyView`는 `Lobby/Garage` 탭 버튼과 `GaragePageRoot`를 추가로 받을 수 있다. 참조가 있으면 같은 씬 안에서 페이지 전환을 처리한다.
* `RoomListView`는 `roomNameInput`, `capacityInput`, `displayNameInput`, `roomListContent`, `roomItemPrefab`, `createRoomButton`이 연결되어야 한다.
* `RoomDetailView`는 room/member 텍스트, member list content, member item prefab, leave/team/ready/start 버튼이 연결되어야 한다.
* `RoomDetailView`는 EventBus를 통해 `GarageInitializedEvent`, `RosterSavedEvent`를 받아 `Ready` 가능 여부를 갱신한다.

## Scene-owned Notes

* `CodexLobbyScene`는 Photon 연결용 `PhotonConnectionAdapter`, 로비 UI용 `Canvas`, 입력용 `EventSystem`을 씬 소유로 둔다.
* `CodexLobbyScene`는 상단 `Lobby/Garage` 탭과 `GaragePageRoot`를 씬 소유로 둔다.
* `GaragePageRoot`가 존재하면 `GaragePageController`, `GarageRosterListView`, `GarageUnitEditorView`, `GarageResultPanelView`, `UnitSetup`, `GarageSetup`, `GarageNetworkAdapter`도 같은 씬에 함께 존재해야 한다.
* `SceneErrorPresenter`의 banner/modal UI는 씬에 명시적으로 존재해야 하며 런타임 생성으로 대체하지 않는다.
* `SoundPlayer`는 로비 씬에서 생성되어 DDOL로 유지되며 이후 게임 씬에서 재초기화된다.
* room/member item prefab은 로비 씬 내부의 비활성 템플릿 오브젝트를 참조한다.
