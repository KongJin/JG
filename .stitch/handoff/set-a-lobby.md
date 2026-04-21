# Set A Handoff - Lobby

> Accepted set date: 2026-04-19

## Accepted Screens

- Main baseline: `Tactical Hangar Lobby - Populated` (`be28b236edb64094878f680f2e4f5f42`) -> `set-a-lobby-populated.{html,png}`
- Supporting empty state: `Tactical Hangar Lobby` (`3b2f3bca917b42dea3fa7485f6e207ff`) -> `set-a-lobby-main.{html,png}`
- Supporting overlay: `Create Operation Modal Overlay` (`07ca2d1148804194947b71557745d41b`) -> `set-a-create-room-modal.{html,png}`
- Non-baseline project screen: `Matchmaking Lobby` (`bbb4345a636148bc98ea94c76b5f8c29`) remains a project-side legacy candidate only and should not be used as the current Lobby baseline or Unity translation source

Baseline labeling note:
`set-a-lobby-main` is a local export filename for the supporting empty-state screen, not the implementation baseline.
For implementation decisions, follow the role labels above first.

## Intent

 Lobby should read like a tactical matchmaking surface, not a Garage teaser and not a dashboard collage.
 The player should first understand what rooms are open, then whether to create a room, then whether the roster looks ready.

## Reading Order

1. `LobbyHeaderCard`
2. `RoomsSectionCard`
3. `CreateRoomCard`
4. `GarageSummaryCard`

## Screen Block Map

- `LobbyHeaderCard`
  - Purpose: establish scene identity and current matchmaking posture
  - Must survive in Unity as a short orientation block, not as a hero banner
- `RoomsSectionCard`
  - Purpose: main work surface for scanning available rooms and choosing where to enter
  - Must survive in Unity as the visually dominant card and the first clearly readable block
- `CreateRoomCard`
  - Purpose: fallback action when no good room exists, plus direct create flow entry
  - Must survive in Unity as a compact action card below the room list
- `GarageSummaryCard`
  - Purpose: roster readiness summary only
  - Must survive in Unity as a quiet summary footer, never as a peer workspace

## CTA Priority Matrix

- Primary default CTA: `Join` on the strongest highlighted room row
- Primary fallback CTA: `Create Room` when there are no joinable room rows
- Secondary CTA: `Join` on non-highlighted room rows
Priority rules:

- `RoomsSectionCard` owns the first actionable read.
- `Create Room` is allowed to feel available, but never more urgent than joining an existing room.

## CTA Priority

- Primary: `Join` on the highlighted room row when rooms exist
- Primary fallback: `Create Room` when the list is empty
- Secondary: other room row join actions
`Create Room` must stay below the room list in perceived priority.
`GarageSummaryCard` must never outrank room actions.

## Covered States

- Empty room list with finished tactical empty state
- Populated room list with 3 to 4 readable room rows
- Create room overlay / modal state

## Unity Translation Targets

- Home shell root: `/Canvas/LobbyPageRoot`
- Shared nav root: `/Canvas/LobbyGarageNavBar`
- Shared nav Garage tab: `/Canvas/LobbyGarageNavBar/GarageTabButton`
- Matchmaking list container: `/Canvas/LobbyPageRoot/RoomListPanel`
- Main list header: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/ListHeaderRow`
- Main list surface: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface`
- Empty-state copy anchor: `/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface/EmptyStateText`
- Create room interaction path: `/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/RoomNameInput/Field`
- Create room action: `/Canvas/LobbyPageRoot/RoomListPanel/CreateRoomCard/CreateRoomButton`
- Garage summary card: `/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard`

## Translation Rules

- Keep the existing scene-owned shell and card stack order.
- Translate the Stitch density into card spacing, header emphasis, and row styling, not into absolute-position effects.
- Room rows should read as compact tactical operation cards, not as a table.
- The highlighted join candidate can be expressed through one stronger accent row, but the section itself still owns the strongest visual weight.
- Garage summary remains summary-only: status pill, short save state, and no route CTA.
- If space becomes tight on mobile, preserve `RoomsSectionCard` and reduce decorative header weight before shrinking room-row readability.

## Validation Focus

- `Open Rooms` remains the first read at `390x844`
- Empty-state card never looks like a blank hole
- `CreateRoomCard` stays secondary
- `GarageSummaryCard` stays summary-only and does not own page routing
- No deep Garage editor content leaks back into Lobby

## Assumptions

- The populated Lobby variant is the baseline for day-to-day implementation because it carries the clearest matchmaking hierarchy.
- The empty-state Lobby remains a required fallback state, not a discarded concept.
- The Lobby succeeds when the player can answer "which room should I join?" before noticing Garage entry.
