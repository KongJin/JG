# Set A Prompt Brief — Lobby Room List Populated

**Project ID:** `11729197788183873077`
**Base Screen:** `Tactical Hangar Lobby`
**Base Screen ID:** `3b2f3bca917b42dea3fa7485f6e207ff`
**Date:** `2026-04-19`

## Goal

Create a populated room-list state from the approved Lobby direction while preserving the same tactical hierarchy.
The room list remains the dominant surface, but now it should show several readable joinable rooms instead of the polished empty state.

## Required Reading Order

1. `LobbyHeaderCard`
2. `RoomsSectionCard`
3. `CreateRoomCard`
4. `GarageSummaryCard`

## Non-Negotiable Rules

- Keep the same visual language as the empty-state lobby
- Do not redesign the whole page
- Replace the empty-state treatment with a populated tactical room list
- Keep create room secondary
- Keep garage summary tertiary and compact
- Keep Korean-first copy
- Keep two-accents-max discipline

## Target Edit Prompt

```md
Keep this Tactical Hangar Lobby layout and visual language, but change the main rooms section from an empty state to a populated room list state.

REQUIRED:
- Preserve the same mobile-first 390x844 tactical hangar structure
- Preserve the same header, create room card, and compact garage summary hierarchy
- The rooms section must remain the dominant visual area

ROOM LIST STATE:
- Show 3 to 4 joinable room entries inside the main rooms section
- Each room row should feel like a compact tactical operation card
- Include short Korean labels for room name, squad size, readiness, and a join action
- Make one room feel highlighted as the clearest immediate join candidate
- Keep the list highly readable and compact, not like a generic admin table
- Include a small room count or matchmaking status near the section header

VISUAL RULES:
- Dark tactical hangar surfaces
- Signal Orange (#F59E0B) only for the strongest join or create action
- Command Blue (#5EB6FF) for supportive status or focus accents
- Strong hierarchy before decoration
- No neon glow
- No equal grid cards
- No long filler copy

DO NOT:
- Promote the garage card above the room list
- Turn create room into a hero section
- Use generic SaaS table styling
```
