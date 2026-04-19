# Set D Prompt Brief — Unit Stats Popup

**Project ID:** `11729197788183873077`
**Base Screen:** `Refined Battle HUD - Tactical Command`
**Base Screen ID:** `03af04196bfa4615b06d4284c66cf1f8`
**Date:** `2026-04-19`

## Goal

Create a compact `unit stats popup` for the battle HUD that helps the player understand the selected unit without interrupting the summon loop.
It should feel like a tactical inspection card, not a full modal takeover.

## Required Reading Order

1. `Selected Unit Identity`
2. `Key Tactical Stats`
3. `Current Energy / Summon Context`
4. `Placement / close affordance`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Preserve the battle HUD as the primary layer
- Popup must be compact and non-blocking
- Stats should be short and icon/number driven
- Command bar remains visible
- Avoid giant tooltip or encyclopedia-like card

## Target Edit Prompt

```md
Add a compact unit stats popup to this refined tactical Battle HUD for the currently selected summon unit.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical battlefield HUD, compact, readable, command-oriented
- Background: Preserve the existing HUD and battlefield context
- Primary Accent: Signal Orange (#F59E0B) for current selected summon state
- Secondary Accent: Command Blue (#5EB6FF) for informational focus and stat labels
- Typography: Space Grotesk for short tactical headings, IBM Plex Sans for stat rows
- Copy language: Korean-first

STRUCTURE:
1. Keep the full HUD visible with the selected summon slot
2. Add a compact popup panel near the selected slot or just above the command bar
3. In the popup show:
   - unit name
   - role tag
   - summon cost
   - attack style
   - range
   - durability
   - mobility or anchor radius
4. Keep the popup readable with short rows, icons, and numbers

EDIT GOALS:
- Help the player confirm what this unit does before placing it
- Keep the summon loop fast and readable
- Make the popup feel like an extension of the command HUD, not a separate screen
- Preserve visibility of the command bar and energy state
- Avoid long descriptions and focus on tactical utility

ANTI-PATTERNS:
- No full-screen stats page
- No dense encyclopedia panel
- No generic fantasy card UI
- No neon or decorative filler panels
```
