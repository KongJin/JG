# Set E Prompt Brief — Mission Defeat Overlay

**Project ID:** `11729197788183873077`
**Base Screen:** `Mission Victory Summary`
**Base Screen ID:** `895e6c337c2d47da92a8e28d01ea2376`
**Date:** `2026-04-19`

## Goal

Create the defeat counterpart to the mission victory summary using the same tactical result-card language.
The screen should communicate failure clearly, keep the battlefield context alive, and present the next action immediately.

## Required Reading Order

1. `Defeat Result`
2. `Key Failure Stats`
3. `Primary Return Action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Same tactical result-card system as victory
- Battlefield remains dimly visible behind the overlay
- Failure state is serious and readable, not melodramatic
- Return to lobby remains the clearest next action
- No full red screen takeover

## Target Edit Prompt

```md
Transform this mission victory summary overlay into a mission defeat and summary overlay for a co-op PvE defense battle.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical result overlay, compact, readable, serious
- Background: Keep the battlefield dimmed behind the result card so it still feels like the battle just ended
- Warning Accent: restrained danger red for defeat/result emphasis only
- Secondary Accent: Command Blue (#5EB6FF) for stat cards and supporting result info
- Typography: Space Grotesk for result title and key numbers, IBM Plex Sans for labels
- Copy language: Korean-first

RESULT STRUCTURE:
1. Large defeat title or failed mission state
2. 3 to 4 compact result cards with big numbers and short labels
   - examples: final wave, core integrity at collapse, units deployed, command score
3. One clear primary CTA to return to lobby
4. Keep any secondary text minimal

EDIT GOALS:
- Make the result readable in 2 seconds
- Communicate failure clearly without turning the UI into a giant red warning screen
- Keep the same tactical card language as the victory overlay
- Prefer big numbers and short Korean labels over paragraphs
- Make Return To Lobby the clearest next step

ANTI-PATTERNS:
- No full bright or full red failure splash
- No long debrief paragraph
- No generic game-over screen disconnected from the HUD language
- No noisy visual effects
```
