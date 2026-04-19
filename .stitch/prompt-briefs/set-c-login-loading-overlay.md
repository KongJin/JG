# Set C Prompt Brief — Login Loading Overlay

**Project ID:** `11729197788183873077`
**Base Screen:** `Tactical Hangar Lobby`
**Base Screen ID:** `3b2f3bca917b42dea3fa7485f6e207ff`
**Date:** `2026-04-19`

## Goal

Create a global login/loading overlay that fits the same tactical hangar UI language.
It should feel like a system connection/authentication state, not a generic spinner popup and not a full new page.

## Required Reading Order

1. `Loading State Title`
2. `Current Connection / Authentication Status`
3. `Progress or system activity indicator`
4. `Secondary wait/cancel guidance if needed`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Same tactical hangar language as Lobby
- Overlay should pause interaction without visually shouting over the whole app
- Loading should feel integrated into the command system
- No generic circular spinner on black
- No long explanation blocks

## Target Edit Prompt

```md
Design a login / connection loading overlay on top of this Tactical Hangar Lobby screen for a mobile co-op PvE game.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical hangar, compact, practical, system-like
- Background: Keep the existing lobby visible under a dark scrim
- Primary Accent: Command Blue (#5EB6FF) for connection state and system activity
- Secondary Accent: Signal Orange (#F59E0B) only for critical system emphasis if needed
- Typography: Space Grotesk for headers, IBM Plex Sans for body
- Copy language: Korean-first

STRUCTURE:
1. Preserve the lobby as dimmed background context
2. Add a centered or lower-centered loading overlay panel
3. Show a short Korean loading title related to login / connection / command sync
4. Show 2 to 3 short status lines such as profile sync, command channel check, room data load
5. Use a tactical progress treatment such as segmented bars, scanning lines, pulse blocks, or blueprint-like progress markers
6. If a secondary action exists, keep it minimal and quiet

EDIT GOALS:
- Make the overlay feel like system initialization inside the same product
- Keep it readable, compact, and calm
- Communicate that the user is waiting for authentication or data sync
- Avoid generic app-loader patterns
- Make the background context still visible enough that this reads as an overlay, not a separate page

ANTI-PATTERNS:
- No generic spinner-only modal
- No white sheet dialog
- No neon glow or purple accents
- No giant full-screen takeover with no context
- No long copy paragraphs
```
