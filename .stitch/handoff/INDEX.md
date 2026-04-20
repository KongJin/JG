# Stitch Handoff Index

> Last updated: 2026-04-20
> Master project: `JG UI Refresh - Lobby Garage GameScene`
> Project ID: `11729197788183873077`

This folder is the handoff layer between the Stitch concept pass and the Unity implementation pass.

Use these docs in this order:

1. Read this file to identify the accepted baseline per set.
2. Read `docs/ops/stitch_handoff_completeness_checklist.md` if you are writing, reviewing, or translating a handoff.
3. Read the matching set handoff doc for reading order, CTA priority, and Unity translation roots.
4. Implement in Unity as scene-owned layout, not as a literal Stitch clone.

## Accepted Baselines

Format:
`Role: Stitch title (screen id) -> local export mapping`

Role labels are authoritative.
If a local export filename looks misleading, follow the role label first and the filename second.

### Set A - Lobby

- Main baseline: `Tactical Hangar Lobby - Populated` (`be28b236edb64094878f680f2e4f5f42`) -> `set-a-lobby-populated.{html,png}`
- Supporting empty state: `Tactical Hangar Lobby` (`3b2f3bca917b42dea3fa7485f6e207ff`) -> `set-a-lobby-main.{html,png}`
- Supporting overlay: `Create Operation Modal Overlay` (`07ca2d1148804194947b71557745d41b`) -> `set-a-create-room-modal.{html,png}`
- Non-baseline project screen kept in Stitch only: `Matchmaking Lobby` (`bbb4345a636148bc98ea94c76b5f8c29`) is a legacy pre-baseline lobby candidate and is not part of the accepted local working set or Unity handoff route
- Handoff: [set-a-lobby.md](./set-a-lobby.md)

### Set B - Garage

- Main baseline: `Tactical Unit Assembly Workspace` (`d440ad9223a24c0d8e746c7236f7ef27`) -> `set-b-garage-main-workspace.{html,png}`
- Non-baseline project screen kept in Stitch only: `Garage / Unit Editor` (`1fe9da270421469b8838f1450cbbfc57`) is not part of the accepted local working set or Unity handoff route
- Handoff: [set-b-garage.md](./set-b-garage.md)

### Set C - Lobby / Account Overlays

- In-room detail: `Room Detail Panel - Selected State` (`e785bb1479da48de9037dbad91e16ddf`) -> `set-c-room-detail-panel.{html,png}`
- Loading: `Login / Connection Loading Overlay` (`056724f23ac54729903db6fdecd1eab1`) -> `set-c-login-loading-overlay.{html,png}`
- Destructive confirm: `Account Deletion Confirmation Overlay` (`b39c877f686d4ea19a2e0ed93e604fcc`) -> `set-c-account-delete-confirm.{html,png}`
- Error dialog: `System Connection Error Overlay` (`09d03272c8aa4e90978945b00763ba69`) -> `set-c-common-error-dialog.{html,png}`
- Handoff: [set-c-overlays.md](./set-c-overlays.md)

### Set D - Battle HUD

- Main baseline: `Refined Battle HUD - Tactical Command` (`03af04196bfa4615b06d4284c66cf1f8`) -> `set-d-battle-hud-baseline.{html,png}`
- Warning state: `Battle HUD - Critical Core Warning State` (`84f0abf5723c4ad6960a7d849de527da`) -> `set-d-low-core-warning.{html,png}`
- Supporting popup variant: Stitch title `Refined Battle HUD - Tactical Command` (`e41a9d7e3b2946e681f68cac1b4f4edf`) -> local role/export `set-d-unit-stats-popup.{html,png}`
- Non-baseline project screen kept in Stitch only: `Battle HUD - Tactical View` (`bf3d08890f2d4a4e98f81c25e14d6073`) is a pre-refinement HUD candidate and is not part of the accepted local working set or Unity handoff route
- Handoff: [set-d-battle-hud.md](./set-d-battle-hud.md)

### Set E - Battle Results

- Victory result: `Mission Victory Summary` (`895e6c337c2d47da92a8e28d01ea2376`) -> `set-e-mission-victory-overlay.{html,png}`
- Defeat result: `Mission Defeat Summary` (`83c5d82066184ef4acb7676e3e823db8`) -> `set-e-mission-defeat-overlay.{html,png}`
- Handoff: [set-e-results.md](./set-e-results.md)

## Implementation Rules

- `.stitch/DESIGN.md` remains the visual concept SSOT for this Stitch pass.
- `CodexLobbyScene.unity` remains the runtime layout SSOT for Lobby and Garage.
- `GameScene.unity` remains the runtime layout SSOT for Battle HUD and result overlays.
- Each set handoff should make baseline, supporting state, CTA hierarchy, and Unity translation targets visible without opening the png/html first.
- Each set handoff should be read in this order: `Accepted Screens -> Intent -> Reading Order or equivalent hierarchy -> Screen Block Map -> CTA Priority Matrix -> Unity Translation Targets`.
- Preserve the accepted reading order and CTA hierarchy first.
- Rebuild geometry in scene/prefab structure that fits Unity layout groups and serialized contract roots.
- Do not add code-driven layout repair to imitate Stitch composition.

## Validation Reminder

- Lobby / Garage: `contract -> page-switch smoke -> feature smoke`
- Battle / Result: `contract -> summon/wave smoke -> outcome overlay smoke`
- Use `390x844` as the visual sanity frame whenever screenshots are captured.
