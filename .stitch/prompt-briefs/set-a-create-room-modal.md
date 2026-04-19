# Set A Prompt Brief — Create Room Modal

**Project ID:** `11729197788183873077`
**Base Screen:** `Tactical Hangar Lobby - Populated`
**Base Screen ID:** `be28b236edb64094878f680f2e4f5f42`
**Date:** `2026-04-19`

## Goal

Design the `Create Room` flow as a tactical modal overlay on top of the populated lobby.
The modal should feel integrated into the same hangar system, not like a generic app dialog.

## Non-Negotiable Rules

- Keep the lobby visible behind the overlay
- Modal interrupts clearly but does not become louder than the whole screen language
- Primary action is room creation
- Secondary action is cancel/close
- Korean-first copy
- Compact fields and clear labels
- No oversized wizard flow
- No generic white form dialog

## Target Edit Prompt

```md
Design the Create Operation / Create Room flow as a tactical modal overlay on top of this populated lobby screen.

STRUCTURE:
- Keep the current Tactical Hangar Lobby visible in the background with a subtle dark scrim
- Add a centered or lower-centered modal panel sized appropriately for mobile
- Modal title in Korean for room creation
- Include compact labeled controls for:
  - room name
  - squad size or player count
  - privacy or access mode
- Add one strong primary create action using Signal Orange (#F59E0B)
- Add one quieter cancel action

VISUAL RULES:
- Same dark tactical hangar language as the base page
- Space Grotesk headers, IBM Plex Sans body
- Compact layered panel, not a generic form sheet
- Clear field grouping and short Korean labels
- Keep it readable and practical for one-hand mobile use
- Avoid excessive helper copy

BEHAVIORAL FEEL:
- This is a focused auxiliary overlay, not a full-screen replacement
- The player should immediately understand the next step and how to dismiss it
- Destructive or secondary actions must be visually quieter than the create action

ANTI-PATTERNS:
- No desktop-style popup proportions
- No bright neon glow
- No large empty padding
- No long multi-step wizard
- No generic SaaS modal styling
```
