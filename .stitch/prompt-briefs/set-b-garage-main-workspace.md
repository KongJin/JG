# Set B Prompt Brief — Garage Main Workspace

**Project ID:** `11729197788183873077`
**Target Screen:** `Garage / Unit Editor`
**Screen ID:** `1fe9da270421469b8838f1450cbbfc57`
**Date:** `2026-04-19`

## Goal

Refine the Garage screen into a stronger **mobile-first unit assembly workspace**.
The player should first read the current roster slots, then the focused editor, then preview and summary, while the save dock remains the clearest persistent action.

## Required Reading Order

1. `Current Slot Summary + Slot Selector`
2. `Part Focus Bar`
3. `Focused Editor`
4. `Preview + Summary`
5. `Save Dock`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Dark tactical hangar atmosphere
- Single scroll workspace, not fragmented dashboard panels
- Slot selector comes before deep part browsing
- Focused editor must dominate the active work area
- Preview and summary must look finished, not placeholder-like
- Save dock must be the clearest persistent CTA
- Settings and account surfaces remain auxiliary
- Avoid marketing-site layouts
- Avoid generic SaaS card grids
- Avoid purple or neon glow aesthetics

## Target Edit Prompt

```md
Refine this existing mobile Garage / Unit Editor screen into a stronger tactical unit assembly workspace for a co-op PvE build defense game.

DESIGN SYSTEM (REQUIRED):
- Platform: Mobile-first, 390x844 baseline
- Theme: Dark tactical hangar, compact, practical, command-oriented
- Background: Hangar Canvas (#111827)
- Primary Accent: Signal Orange (#F59E0B) for the main persistent save action
- Secondary Accent: Command Blue (#5EB6FF) for focus, selected controls, and informational emphasis
- Typography: Space Grotesk for headers and section labels, IBM Plex Sans for body copy
- Copy language: Korean-first
- Cards: layered tactical panels with low wasted space and clear hierarchy

PAGE STRUCTURE:
1. Top area with garage title, current roster context, and settings access kept compact
2. Slot selector as the first major read, showing 3 to 6 roster slots with strong selected / filled / empty distinction
3. Part focus bar for frame, weapon, and mobility selection
4. One dominant focused editor area for the currently selected part
5. Preview and summary blocks in the same vertical flow, both looking complete and informative
6. Persistent bottom save dock with the clearest action on the page

EDIT GOALS:
- Make the garage feel like a single continuous assembly workspace, not separate mini dashboards
- Make slot selection the first visual and interaction priority
- Ensure the focused editor is clearly the active work area after slot selection
- Keep preview and summary below the editor, readable as evaluation surfaces rather than empty placeholders
- Show unit role, attack style, range, durability, and mobility outcome in a short, readable way
- Make stat changes or build consequences feel immediately understandable
- Keep settings and account information auxiliary and visually quieter than the main workspace
- Make the save dock persistent, strong, and unmistakable as the primary CTA
- Keep the interface dense but calm, with strict alignment and no decorative clutter

CONTENT DIRECTION:
- Short Korean system-like labels
- Compact tactical tags and role labels
- Clear selected slot state
- Clear build result framing
- Empty slot or empty preview areas must still feel intentional and complete

ANTI-PATTERNS:
- No desktop-first split layout
- No oversized promo hero area
- No equal-width dashboard card grid
- No neon glows or purple accents
- No long explanation blocks
- No weak or hidden save action
```
