# Set D Prompt Brief — Battle HUD Baseline

**Project ID:** `11729197788183873077`
**Target Screen:** `Battle HUD - Tactical View`
**Screen ID:** `bf3d08890f2d4a4e98f81c25e14d6073`
**Date:** `2026-04-19`

## Goal

Refine the battle HUD into a clear **mobile tactical command screen** where the player immediately understands `select -> place -> summon`.
The first read should be the unit command area, current energy, core pressure, and wave state, not decorative combat noise.

## Required Reading Order

1. `Summon Command Bar`
2. `Current Energy`
3. `Placement Feedback / Selected Unit State`
4. `Core HP`
5. `Wave / Countdown`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Dark tactical battlefield HUD
- Player fantasy is command, not avatar action
- Unit slot selection must be the clearest interaction
- Energy and placement state must be readable at a glance
- Core HP must feel like the team objective
- Avoid effect-heavy shooter HUD styling
- Avoid generic mobile MOBA UI patterns

## Target Edit Prompt

```md
Refine this existing Battle HUD into a stronger tactical command HUD for a mobile co-op PvE build defense game.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical battlefield HUD, compact, readable, command-oriented
- Background: Preserve battlefield context while making UI hierarchy clearer
- Primary Accent: Signal Orange (#F59E0B) for selected summon action or high-priority command emphasis
- Secondary Accent: Command Blue (#5EB6FF) for selected unit state, placement feedback, and supportive system info
- Typography: Space Grotesk for tactical headers and labels, IBM Plex Sans for short body/supporting text
- Copy language: Korean-first

HUD STRUCTURE:
1. Top left or upper area: compact Wave and countdown status
2. Top right: Core HP promoted into a clear team objective card
3. Bottom command zone: 3 summon slots as the main tactical control bar
4. Near the command zone: current energy and recovery flow, tightly linked to summon affordance
5. Battlefield overlay feedback: selected unit, placement valid/invalid state, and summon guidance

EDIT GOALS:
- Make the summon bar the face of the GameScene HUD
- Make it immediately clear which unit can be selected and whether it is affordable right now
- Show current energy as a strong, readable combat resource, not a tiny side stat
- Make placement feedback feel tied to the selected command state
- Make Core HP feel like the shared mission objective
- Reduce prototype feeling and unify the HUD into one tactical language
- Keep text minimal: icons, numbers, short Korean status labels
- Favor tap-first clarity over drag-heavy complexity

CONTENT DIRECTION:
- Short Korean system-like labels
- Strong selected slot state
- Clear cannot-afford / invalid-placement feedback
- Clear wave pressure and core danger signal

ANTI-PATTERNS:
- No MOBA shop-like HUD clutter
- No generic FPS crosshair-centric overlay
- No neon purple glow
- No decorative sci-fi filler panels
- No long combat text blocks
```
