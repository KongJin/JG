# Set C Prompt Brief — Room Detail Panel

**Project ID:** `11729197788183873077`
**Base Screen:** `Tactical Hangar Lobby - Populated`
**Base Screen ID:** `be28b236edb64094878f680f2e4f5f42`
**Date:** `2026-04-19`

## Goal

Create the in-room entry state for Lobby by adding a `Room Detail Panel` to the populated lobby flow.
This should feel like a focused tactical inspection surface for the selected room, not a full page replacement and not a generic modal.

## Required Reading Order

1. `Selected Room Card`
2. `Room Detail Panel`
3. `Primary Join / Ready Action`
4. `Secondary Back / Close Action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Same tactical hangar visual language as Set A
- Detail panel is in-room flow specific, distinct from the lobby home shell
- Main flow should not be overwhelmed by the panel
- Join / ready action must be clear
- Secondary dismiss / back action must be quieter
- No generic white modal or desktop sidebar styling

## Target Edit Prompt

```md
Transform this populated Tactical Hangar Lobby into a selected-room state with a tactical Room Detail Panel for a co-op PvE match.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical hangar, compact, practical, readable
- Background: Hangar Canvas (#111827)
- Primary Accent: Signal Orange (#F59E0B) for the strongest join or ready action
- Secondary Accent: Command Blue (#5EB6FF) for room status, selected focus, and supporting information
- Typography: Space Grotesk for headers and section labels, IBM Plex Sans for body copy
- Copy language: Korean-first

STRUCTURE:
1. Preserve the existing populated lobby as the base context
2. Make one room card clearly selected
3. Add a Room Detail Panel as a mobile-appropriate tactical panel or bottom sheet linked to the selected room
4. Inside the detail panel show:
   - room name
   - squad occupancy
   - mission or difficulty status
   - short tactical description
   - player slot/readiness information
   - one primary join/ready action
   - one secondary back or close action

EDIT GOALS:
- Make the panel feel like an in-room flow surface, not a separate app screen
- Keep the selected room context obvious
- Maintain strong hierarchy: selected room and detail panel first, other room rows quieter
- Keep the panel compact, high-density, and useful
- Distinguish primary and secondary actions clearly
- Preserve the dark tactical hangar language and Korean system-like labels
- Avoid long paragraphs; favor short labels, tags, and state rows

ANTI-PATTERNS:
- No generic SaaS side panel
- No oversized hero treatment
- No neon glow or purple accent
- No loud full-screen takeover unless necessary
- No long explanatory copy
```
