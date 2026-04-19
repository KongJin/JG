# Set A Handoff - Lobby

> Accepted set date: 2026-04-19

## Accepted Screens

- Main baseline: `Tactical Hangar Lobby - Populated` (`be28b236edb64094878f680f2e4f5f42`)
- Supporting empty state: `Tactical Hangar Lobby` (`3b2f3bca917b42dea3fa7485f6e207ff`)
- Supporting overlay: `Create Operation Modal Overlay` (`07ca2d1148804194947b71557745d41b`)

## Intent

Lobby should read like a tactical matchmaking surface, not a Garage teaser and not a dashboard collage.
The player should first understand what rooms are open, then whether to create a room, then whether to jump to Garage.

## Reading Order

1. `LobbyHeaderCard`
2. `RoomsSectionCard`
3. `CreateRoomCard`
4. `GarageSummaryCard`

## CTA Priority

- Primary: `Join` on the highlighted room row when rooms exist
- Primary fallback: `Create Room` when the list is empty
- Secondary: other room row join actions
- Tertiary: `Open Garage`

`Create Room` must stay below the room list in perceived priority.
`GarageSummaryCard` must never outrank room actions.

## Covered States

- Empty room list with finished tactical empty state
- Populated room list with 3 to 4 readable room rows
- Create room overlay / modal state

## Unity Translation Targets

- Home shell root: `/Canvas/LobbyPageRoot`
- Matchmaking list container: `/Canvas/LobbyPageRoot/RoomListPanel`
- Main list header: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/ListHeaderRow`
- Main list surface: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface`
- Empty-state copy anchor: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface/EmptyStateText`
- Create room interaction path: `/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/RoomNameInput/Field`
- Create room action: `/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/CreateRoomButton`
- Garage summary card: `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard`
- Garage entry CTA: `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard/GarageTabButton`

## Translation Rules

- Keep the existing scene-owned shell and card stack order.
- Translate the Stitch density into card spacing, header emphasis, and row styling, not into absolute-position effects.
- Room rows should read as compact tactical operation cards, not as a table.
- The highlighted join candidate can be expressed through one stronger accent row, but the section itself still owns the strongest visual weight.
- Garage summary remains summary-only: status pill, short save state, and `Open Garage`.

## Validation Focus

- `Open Rooms` remains the first read at `390x844`
- Empty-state card never looks like a blank hole
- `CreateRoomCard` stays secondary
- `GarageSummaryCard` opens Garage through the existing smoke path
- No deep Garage editor content leaks back into Lobby

## Assumptions

- The populated Lobby variant is the baseline for day-to-day implementation because it carries the clearest matchmaking hierarchy.
- The empty-state Lobby remains a required fallback state, not a discarded concept.
