# Set D Prompt Brief — Low Core HP Warning

**Project ID:** `11729197788183873077`
**Base Screen:** `Refined Battle HUD - Tactical Command`
**Base Screen ID:** `03af04196bfa4615b06d4284c66cf1f8`
**Date:** `2026-04-19`

## Goal

Create a `Low Core HP` warning state that raises urgency without destroying the battle HUD hierarchy.
The player should still read the summon command bar first, but immediately understand that the shared objective is in danger.

## Required Reading Order

1. `Summon Command Bar`
2. `Low Core Warning`
3. `Current Energy`
4. `Core HP Critical State`
5. `Placement Feedback`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Preserve the tactical command HUD layout
- Core warning must be strong but not full-screen takeover
- Command bar remains the primary interaction area
- Warning uses restrained danger red or orange, not neon chaos
- Avoid panic-screen aesthetics

## Target Edit Prompt

```md
Keep this refined tactical Battle HUD layout, but transform it into a low-core critical warning state.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical battlefield HUD, compact, urgent, readable
- Background: Preserve battlefield and HUD structure
- Warning Accent: restrained danger red and danger orange for critical core state only
- Primary Command Accent: keep Signal Orange (#F59E0B) for summon interaction
- Secondary System Accent: keep Command Blue (#5EB6FF) for placement and selection feedback
- Typography: Space Grotesk for warning/header labels, IBM Plex Sans for compact supporting text
- Copy language: Korean-first

HUD STATE CHANGES:
1. Core HP card changes into a critical state with visibly low integrity
2. Add a short warning banner or tactical alert near the top such as core breach warning / critical defense state
3. Keep the command bar intact and readable
4. Keep energy readable and still linked to summon decision-making
5. Preserve placement feedback, but make the whole screen feel more urgent

EDIT GOALS:
- Make the danger to the shared core immediately visible
- Avoid turning the entire HUD into a giant warning overlay
- Keep the summon decision loop readable under pressure
- Use short, high-priority Korean warning labels
- Make the player feel urgency without losing control or readability

ANTI-PATTERNS:
- No full-screen red wash
- No neon flashing sci-fi chaos
- No hiding the command bar under the warning
- No long emergency text blocks
```
