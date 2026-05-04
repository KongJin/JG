# Set A Prompt Brief - Lobby Matchmaking Flow Refresh

**Project ID:** `11729197788183873077`
**Date:** `2026-05-04`
**Status:** accepted source freeze for state-specific Lobby surfaces

## Goal

Refresh the mobile Lobby so it reads first as a matchmaking console, not a sparse form screen.

The accepted Lobby flow is split into four source-frozen surfaces:

1. `Matchmaking Lobby` - populated room list baseline
2. `Matchmaking Lobby (Empty State)` - no active rooms baseline
3. `Create Operation Modal Overlay` - room creation overlay
4. `Room Detail Panel - Selected State` - selected room escalation before join

## Accepted Source Freeze

### Populated Lobby

- Screen: `Matchmaking Lobby`
- Screen ID: `d64845cd097a4a30b8fb2e1fb4435347`
- Local export: `.stitch/designs/set-a-matchmaking-lobby-populated-v2.{html,png}`

### Empty-State Lobby

- Screen: `Matchmaking Lobby (Empty State)`
- Screen ID: `0e5a4d83630e479eb81eb1d7463dfac5`
- Local export: `.stitch/designs/set-a-matchmaking-lobby-empty-state-v2.{html,png}`

### Create Room Overlay

- Screen: `Create Operation Modal Overlay`
- Screen ID: `d308a69f1e684a1189b3681671bac049`
- Local export: `.stitch/designs/set-a-create-operation-modal-overlay-v2.{html,png}`

### Selected Room Detail

- Screen: `Room Detail Panel - Selected State`
- Screen ID: `f716ca6e17f84ba7bd9338838cf43752`
- Local export: `.stitch/designs/set-a-room-detail-panel-selected-v2.{html,png}`

## Required Reading Order

1. `Open Rooms / Room List`
2. `Create Room`
3. `Garage Summary Entry`

Selected room sub-flow:

1. `Selected Room Row`
2. `Room Detail Dossier`
3. `Primary Join Action`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first copy
- Dark tactical hangar atmosphere
- Room list is the strongest Lobby surface
- Empty state must look finished and actionable
- Create room stays secondary and overlay-driven
- Garage summary stays visible but quieter than room actions
- Selected room state must make join intent obvious
- Avoid marketing-site layouts
- Avoid generic SaaS form rows
- Avoid large dead black gaps
- Avoid purple accents or neon hologram gimmicks

## Current Design Judgment

- Use the empty-state lobby as the primary baseline when evaluating overall Lobby hierarchy.
- Use the populated lobby as density and metadata reference for active room rows.
- Use the selected-room detail panel as the preferred escalation path before room join.
- Use the create-room overlay as the preferred creation flow instead of a large inline form block.

## Unity Translation Notes

- Treat these exports as source artifacts only.
- Do not use this brief as a generator input contract.
- When moving to Unity candidate work, prepare per-surface in-memory manifest, unity-map, and presentation contract from the accepted source freeze.
