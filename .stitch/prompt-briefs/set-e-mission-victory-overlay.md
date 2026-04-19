# Set E Prompt Brief — Mission Victory Overlay

**Project ID:** `11729197788183873077`
**Base Screen:** `Refined Battle HUD - Tactical Command`
**Base Screen ID:** `03af04196bfa4615b06d4284c66cf1f8`
**Date:** `2026-04-19`

## Goal

Create a `mission victory/summary overlay` that makes the end of battle feel decisive and readable on mobile.
The player should instantly understand the result, key performance numbers, and the next action.

## Required Reading Order

1. `Victory Result`
2. `Key Stats`
3. `Primary Return Action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Battlefield remains dimly visible behind the result overlay
- Result must feel strong but not verbose
- Large numbers and compact cards over long text
- Return to lobby is the clearest next action
- No generic RPG loot screen styling

## Target Edit Prompt

```md
Transform this tactical Battle HUD into a mission victory and summary overlay for a co-op PvE defense battle.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical result overlay, compact, readable, decisive
- Background: Keep the battlefield dimmed behind the result card so it still feels like the battle just ended
- Primary Accent: Signal Orange (#F59E0B) for the strongest confirmation / return action
- Secondary Accent: Command Blue (#5EB6FF) for performance cards and supporting result info
- Typography: Space Grotesk for result title and key numbers, IBM Plex Sans for labels
- Copy language: Korean-first

RESULT STRUCTURE:
1. Large victory title or success state
2. 3 to 4 compact result cards with big numbers and short labels
   - examples: wave reached, core remaining, units deployed, command efficiency
3. One clear primary CTA to return to lobby
4. Keep any secondary text minimal

EDIT GOALS:
- Make the result readable in 2 seconds
- Emphasize the emotional finish of a successful defense
- Prefer big numbers and short Korean labels over paragraphs
- Keep the overlay consistent with the tactical HUD language rather than switching to a different UI theme
- Make Return To Lobby the most obvious next step

ANTI-PATTERNS:
- No giant loot grid
- No long debrief paragraph
- No generic fantasy reward screen
- No full bright victory splash that ignores the battlefield context
```
