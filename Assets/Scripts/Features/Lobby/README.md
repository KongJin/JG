# Lobby Feature

## Ownership

- Scene root: `CodexLobbyScene/LobbyPageRoot`
- Scene wiring owner: `LobbySetup`
- Presentation owner: `LobbyView`

## Runtime Contract

- `CodexLobbyScene.unity` is the final SSOT for Lobby/Garage hierarchy, layout, and serialized wiring.
- Lobby now behaves as a page switcher, not a dual-workspace dashboard.
- `LobbyPageRoot` and `GaragePageRoot` are mutually exclusive during normal runtime.
- Lobby shows a single `Garage` navigation button.
- Garage shows a single `Back To Lobby` navigation button.
- `RoomListPanel` and `RoomDetailPanel` still alternate inside the lobby page.
- `RoomListPanel` should always present an explicit room-list surface, room count badge, and empty-state copy when no Photon rooms are available.

## Scene Contract

- `CodexLobbySceneBuilder` is a repair/reseed tool. It is not the runtime SSOT for Lobby/Garage UI.
- `CodexLobbyScene` owns page geometry and navigation button placement as serialized scene layout.
- `LobbyView` owns inspector-wired references for navigation buttons, labels/borders, and page `CanvasGroup`s.
- Normal UI verification should read the scene contract and Play Mode captures, not infer truth from builder code alone.
- Do not make `LobbyView` the layout author again. Runtime code must not rewrite scene-owned `RectTransform` anchors, offsets, or sizes for Lobby/Garage placement.
- Do not reintroduce runtime child traversal for navigation button text/border updates or runtime `CanvasGroup` injection for page focus.
- The verified rebuild path is `POST /scene/rebuild-codex-lobby`; generic `menu/execute` is manual-only and non-authoritative for this feature.

### Sentinel Checklist

- `/Canvas/LobbyPageRoot`
- `/Canvas/GaragePageRoot`
- `/Canvas/LobbyPageRoot/RoomListPanel`
- `/Canvas/LobbyPageRoot/RoomListPanel/ListHeaderRow`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface`
- `/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface/EmptyStateText`
- `/Canvas/LobbyPageRoot/GarageTabButton`
- `/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton`

### Required Reference Checks

- `/LobbyView::LobbyView._lobbyPageRoot`
- `/LobbyView::LobbyView._garagePageRoot`
- `/LobbyView::LobbyView._roomListPanel`
- `/LobbyView::LobbyView._roomDetailPanel`
- `/LobbyView::LobbyView._roomListView`
- `/LobbyView::LobbyView._roomDetailView`
- `/LobbyView::LobbyView._lobbyPageCanvasGroup`
- `/LobbyView::LobbyView._garagePageCanvasGroup`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListContent`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomItemPrefab`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListCountText`
- `/Canvas/LobbyPageRoot/RoomListPanel::RoomListView._roomListEmptyStateText`

## Ready Contract

- Ready depends on the saved Garage roster only.
- Unsaved Garage changes must block Ready and surface a nearby reason.
- `RoomDetailView` consumes both `GarageInitializedEvent`/`RosterSavedEvent` and `GarageDraftStateChangedEvent` to keep the button state accurate.
