# Lobby Feature

## Ownership

- Scene root: `CodexLobbyScene/LobbyPageRoot`
- Scene wiring owner: `LobbySetup`
- Presentation owner: `LobbyView`

## Runtime Contract

- Lobby now behaves as a dashboard, not a page switcher.
- `LobbyPageRoot` and `GaragePageRoot` stay visible together during normal runtime.
- `Lobby / Garage` tabs are focus controls only. They should not hide either workspace.
- `RoomListPanel` and `RoomDetailPanel` still alternate inside the lobby column.

## Scene Contract

- `LobbyView` owns inspector-wired references for `TopTabs`, tab labels/borders, and page `CanvasGroup`s.
- Do not reintroduce runtime child traversal for tab text/border updates or runtime `CanvasGroup` injection for page focus.

## Ready Contract

- Ready depends on the saved Garage roster only.
- Unsaved Garage changes must block Ready and surface a nearby reason.
- `RoomDetailView` consumes both `GarageInitializedEvent`/`RosterSavedEvent` and `GarageDraftStateChangedEvent` to keep the button state accurate.
