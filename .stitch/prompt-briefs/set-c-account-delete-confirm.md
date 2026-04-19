# Set C Prompt Brief — Account Delete Confirm

**Project ID:** `11729197788183873077`
**Base Screen:** `Tactical Unit Assembly Workspace`
**Base Screen ID:** `d440ad9223a24c0d8e746c7236f7ef27`
**Date:** `2026-04-19`

## Goal

Create a destructive `Account delete confirm` overlay in the Garage context.
It should feel like a serious tactical confirmation step inside the same product, not a generic system alert.

## Required Reading Order

1. `Deletion Warning Title`
2. `Short consequence summary`
3. `Primary destructive action`
4. `Secondary cancel / back action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Base Garage workspace remains visible under a dark scrim
- Dialog must feel integrated into the same tactical hangar system
- Destructive action is clearly separated from cancel
- Red is reserved for destructive emphasis only
- No generic OS alert box
- No long legal-style copy block

## Target Edit Prompt

```md
Design an account deletion confirmation overlay on top of this Tactical Unit Assembly Workspace screen.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical hangar, compact, practical, serious
- Background: Keep the Garage workspace visible under a dark scrim
- Primary destructive accent: restrained danger red (#EF4444 or similar) only for the delete action
- Secondary accent: muted dark surface or subtle Command Blue outline for cancel
- Typography: Space Grotesk for title, IBM Plex Sans for body
- Copy language: Korean-first

STRUCTURE:
1. Preserve the Garage workspace in the background
2. Add a centered or lower-centered confirmation dialog sized for mobile
3. Title should clearly indicate account deletion or account removal
4. Include 2 to 3 short Korean lines that explain the consequence in a concise way
5. Include one destructive primary action button
6. Include one clearly quieter cancel/back action

EDIT GOALS:
- Make the dialog feel weighty and serious but still clean and readable
- Separate destructive intent clearly from cancel intent
- Keep the copy short, system-like, and unambiguous
- Preserve the tactical hangar visual language instead of using generic app alert styling
- Make it obvious that this is a confirmation barrier before irreversible action

ANTI-PATTERNS:
- No white alert box
- No bright neon red glow
- No equal-weight buttons
- No long warning paragraph
- No playful or soft visual tone
```
