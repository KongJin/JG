# Set B Handoff - Garage

> Accepted set date: 2026-04-19

## Accepted Screen

- Main baseline: `Tactical Unit Assembly Workspace` (`d440ad9223a24c0d8e746c7236f7ef27`) -> `set-b-garage-main-workspace.{html,png}`
- Non-baseline project screen: `Garage / Unit Editor` (`1fe9da270421469b8838f1450cbbfc57`) remains a project-side candidate only and should not be used as the current Garage baseline or Unity translation source

## Intent

Garage should feel like one continuous mobile assembly workspace.
The player should first lock onto roster slots, then edit the selected part, then evaluate the resulting build, while the save dock stays obviously persistent.

## Reading Order

1. `Current Slot Summary + Slot Selector`
2. `Part Focus Bar`
3. `Focused Editor`
4. `Preview + Summary`
5. `Save Dock`

## Screen Block Map

- `Current Slot Summary + Slot Selector`
  - Purpose: tell the player which roster slot is active and where to switch next
  - Must survive in Unity as the first-screen anchor and roster context reset point
- `Part Focus Bar`
  - Purpose: switch editing mode between `Frame`, `Weapon`, and `Mobility`
  - Must survive in Unity as a fast mode selector, not as a tab shell for multiple pages
- `Focused Editor`
  - Purpose: main work area for choosing and understanding the active part options
  - Must survive in Unity as the largest vertical block in the scroll body
- `Preview + Summary`
  - Purpose: confirm what the build currently becomes and whether the slot feels battle-ready
  - Must survive in Unity as an evaluative block, even when preview content is sparse
- `Save Dock`
  - Purpose: keep commit state and save action permanently obvious
  - Must survive in Unity as the clearest persistent action surface

## CTA Priority Matrix

- Primary persistent CTA: `Save`
- Secondary CTA: slot switch
- Secondary CTA: part focus change
- Tertiary CTA: part card selection inside the focused editor
- Tertiary CTA: settings / account access

Priority rules:

- The Garage screen should always answer "what slot am I editing?" before "which option should I tap?"
- `Save` must visually outrank any single part card or selector chip.
- Settings and account actions are auxiliary and must not compete with assembly flow.

## CTA Priority

- Primary persistent CTA: `Save` in the bottom save dock
- Secondary: slot selection
- Secondary: part focus changes (`Frame / Weapon / Mobility`)
- Tertiary: settings / account access

The save action must remain clearer than any single editor action.

## Covered States

- Selected slot editing state
- Filled / empty slot distinction
- Focused part editing
- Finished preview / summary presence

## Unity Translation Targets

- Garage root: `/Canvas/GaragePageRoot`
- Shared nav root: `/Canvas/LobbyGarageNavBar`
- Shared nav Lobby tab: `/Canvas/LobbyGarageNavBar/LobbyTabButton`
- Mobile workspace root: `/Canvas/GaragePageRoot/GarageMobileStackRoot`
- Focus bar anchor: `/Canvas/GaragePageRoot/GarageMobileStackRoot/GarageMobileTabBar`
- Scroll body host: `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost`
- Scroll body content: `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent`
- Slot strip root: `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/SlotStripRow`
- Save dock root: `/Canvas/GaragePageRoot/MobileSaveDock`
- Save button: `/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveButton`
- Settings overlay root: `/Canvas/GaragePageRoot/GarageSettingsOverlay`
- Settings close action: `/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard/SettingsCloseButton`

## Translation Rules

- Preserve the existing `slot first -> single scroll body -> fixed save dock` contract.
- Treat `/Canvas/LobbyGarageNavBar` as the page nav shell and `GarageMobileTabBar` as the part focus bar.
- Use the Stitch workspace as a density and hierarchy target, not as a request to add a separate right rail or desktop split.
- The focused editor should dominate vertical space after slot selection.
- Preview and summary should feel evaluative and complete even when no flashy model content is available.
- Auxiliary account and settings surfaces stay visually quieter than the main assembly workspace.
- If vertical space becomes contested, preserve `Save Dock` persistence and `Focused Editor` clarity before adding more preview ornament.

## Validation Focus

- Slot state is readable at first screen
- Focused editor feels like the main work zone
- Save dock remains visible or obviously persistent in mobile flow
- Saving returns the user to a slot-confirmation context, not to a deep buried editor state
- Settings overlay still behaves as an auxiliary panel, not as a competing workspace

## Assumptions

- This handoff stays within the current mobile-only Garage policy.
- Weapon-pick overlay, comparison view, and cost breakdown remain follow-up variants, not baseline requirements for this pass.
- The Garage succeeds when the player can read `active slot -> active part focus -> save readiness` without hunting through the scroll body.
