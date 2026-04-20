# Set A Prompt Brief — Lobby Main

**Project ID:** `11729197788183873077`
**Target Screen:** `Tactical Hangar Lobby`
**Screen ID:** `3b2f3bca917b42dea3fa7485f6e207ff`
**Date:** `2026-04-19`

## Goal

Refine the Lobby main screen so it reads as a **mobile-first tactical matchmaking surface**.
The room list must dominate the first read, create room stays secondary, and the garage entry remains visible without overpowering room actions.

This brief maps to the accepted Set A supporting empty-state screen and the local export `set-a-lobby-main.{html,png}`.

## Required Reading Order

1. `LobbyHeaderCard`
2. `RoomsSectionCard`
3. `CreateRoomCard`
4. `GarageSummaryCard`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Dark tactical hangar atmosphere
- Strong hierarchy before decoration
- Room list is the first thing the player reads
- Create room is secondary, never hero-like
- Garage summary is tertiary and compact
- Empty room state must look complete, not blank
- Avoid marketing-site layouts
- Avoid generic SaaS card grids
- Avoid purple or neon glow aesthetics
- Do not add deep garage editor controls to the lobby

## Target Edit Prompt

```md
Refine this existing mobile Tactical Hangar Lobby screen into a stronger tactical hangar lobby for a co-op PvE build defense game.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical sci-fi hangar, compact, practical, readable
- Background: Hangar Canvas (#111827)
- Primary Accent: Signal Orange (#F59E0B) for the main room action and strongest CTA
- Secondary Accent: Command Blue (#5EB6FF) for focus, supporting actions, and informational emphasis
- Typography: Space Grotesk for headers and section labels, IBM Plex Sans for body copy
- Copy language: Korean-first
- Cards: compact layered tactical panels, low wasted space, finished empty states

PAGE STRUCTURE:
1. Header card for title, party or profile status, and compact tactical context
2. Main rooms section card that clearly dominates the screen and becomes the first visual read
3. Secondary create room card that is useful but quieter than the rooms section
4. Compact garage summary card near the lower portion of the screen with ready or draft state and an Open Garage action

EDIT GOALS:
- Make the room list surface the clearest and largest functional area on the page
- If the room list is empty, show a polished empty state card with a short Korean title, one short helper line, and a clear action to create or refresh rooms
- Make create room feel like a secondary tactical utility card, not a hero banner
- Keep the garage summary visible but tertiary: compact summary, status, and one CTA only
- Make the whole screen feel like matchmaking first, garage second
- Keep visual density compact and intentional, not sparse
- Use at most two strong accent zones in the viewport
- Favor clean vertical mobile flow over equal card grids
- Preserve one unified tactical hangar language and avoid generic app dashboard patterns

CONTENT DIRECTION:
- Short Korean system-like labels
- Clear room count or status area near the rooms section
- Empty state must feel finished, framed, and useful rather than like a missing list

ANTI-PATTERNS:
- No desktop-first layout
- No marketing hero composition
- No bright neon glows or purple accents
- No oversized garage promo area
- No empty black placeholder box
- No long explanatory copy
```
