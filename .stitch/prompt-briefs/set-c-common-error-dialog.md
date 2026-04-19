# Set C Prompt Brief — Common Error Dialog

**Project ID:** `11729197788183873077`
**Base Screen:** `Login / Connection Loading Overlay`
**Base Screen ID:** `056724f23ac54729903db6fdecd1eab1`
**Date:** `2026-04-19`

## Goal

Create a reusable common error dialog style for system failures and retry states.
It should feel like the same tactical system language as the loading overlay, but now shifted into a readable failure state with retry and back options.

## Required Reading Order

1. `Error Title`
2. `Short failure reason`
3. `Primary recovery action`
4. `Secondary dismiss / back action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Same tactical hangar system language as loading overlay
- Error dialog must be readable and calm, not loud or chaotic
- Retry action should be clear
- Secondary dismiss action should be quieter
- No generic OS alert styling
- No giant red failure screen

## Target Edit Prompt

```md
Transform this login / connection loading overlay into a common tactical error dialog state for a failed system connection.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical hangar, compact, serious, readable
- Background: Keep the dimmed lobby context behind the dialog
- Error emphasis: restrained danger red only for small warning accents, not the whole surface
- Recovery emphasis: Command Blue (#5EB6FF) or Signal Orange (#F59E0B) for the primary retry action
- Typography: Space Grotesk for title, IBM Plex Sans for body
- Copy language: Korean-first

STRUCTURE:
1. Preserve the dimmed background context
2. Replace the loading state with a compact error dialog
3. Show a short Korean error title
4. Show 1 to 2 short lines explaining connection failure or data sync failure
5. Add one primary retry action
6. Add one secondary close/back action

EDIT GOALS:
- Make the dialog feel reusable for common error handling
- Keep it compact and unambiguous
- Show the user the next recovery action immediately
- Preserve the tactical product language instead of using generic app failure patterns
- Use restrained red as a warning accent, not as a full-surface fill

ANTI-PATTERNS:
- No full red background
- No generic browser error styling
- No neon red glow
- No long troubleshooting text
- No equal-priority buttons
```
