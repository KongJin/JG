# Stitch Handoff Index

> Last updated: 2026-04-19
> Master project: `JG UI Refresh - Lobby Garage GameScene`
> Project ID: `11729197788183873077`

This folder is the handoff layer between the Stitch concept pass and the Unity implementation pass.

Use these docs in this order:

1. Read this file to identify the accepted baseline per set.
2. Read the matching set handoff doc for reading order, CTA priority, and Unity translation roots.
3. Implement in Unity as scene-owned layout, not as a literal Stitch clone.

## Accepted Baselines

### Set A - Lobby

- Main baseline: `Tactical Hangar Lobby - Populated` (`be28b236edb64094878f680f2e4f5f42`)
- Supporting state: `Tactical Hangar Lobby` (`3b2f3bca917b42dea3fa7485f6e207ff`)
- Supporting overlay: `Create Operation Modal Overlay` (`07ca2d1148804194947b71557745d41b`)
- Handoff: [set-a-lobby.md](./set-a-lobby.md)

### Set B - Garage

- Main baseline: `Tactical Unit Assembly Workspace` (`d440ad9223a24c0d8e746c7236f7ef27`)
- Handoff: [set-b-garage.md](./set-b-garage.md)

### Set C - Lobby / Account Overlays

- In-room detail: `Room Detail Panel - Selected State` (`e785bb1479da48de9037dbad91e16ddf`)
- Loading: `Login / Connection Loading Overlay` (`056724f23ac54729903db6fdecd1eab1`)
- Destructive confirm: `Account Deletion Confirmation Overlay` (`b39c877f686d4ea19a2e0ed93e604fcc`)
- Error dialog: `System Connection Error Overlay` (`09d03272c8aa4e90978945b00763ba69`)
- Handoff: [set-c-overlays.md](./set-c-overlays.md)

### Set D - Battle HUD

- Main baseline: `Refined Battle HUD - Tactical Command` (`03af04196bfa4615b06d4284c66cf1f8`)
- Warning state: `Battle HUD - Critical Core Warning State` (`84f0abf5723c4ad6960a7d849de527da`)
- Supporting popup variant: unit stats popup variant on battle HUD (`e41a9d7e3b2946e681f68cac1b4f4edf`)
- Handoff: [set-d-battle-hud.md](./set-d-battle-hud.md)

### Set E - Battle Results

- Victory result: `Mission Victory Summary` (`895e6c337c2d47da92a8e28d01ea2376`)
- Defeat result: `Mission Defeat Summary` (`83c5d82066184ef4acb7676e3e823db8`)
- Handoff: [set-e-results.md](./set-e-results.md)

## Implementation Rules

- `.stitch/DESIGN.md` remains the visual concept SSOT for this Stitch pass.
- `CodexLobbyScene.unity` remains the runtime layout SSOT for Lobby and Garage.
- `GameScene.unity` remains the runtime layout SSOT for Battle HUD and result overlays.
- Preserve the accepted reading order and CTA hierarchy first.
- Rebuild geometry in scene/prefab structure that fits Unity layout groups and serialized contract roots.
- Do not add code-driven layout repair to imitate Stitch composition.

## Validation Reminder

- Lobby / Garage: `contract -> page-switch smoke -> feature smoke`
- Battle / Result: `contract -> summon/wave smoke -> outcome overlay smoke`
- Use `390x844` as the visual sanity frame whenever screenshots are captured.
