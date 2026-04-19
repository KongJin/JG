# Set B Handoff - Garage

> Accepted set date: 2026-04-19

## Accepted Screen

- Main baseline: `Tactical Unit Assembly Workspace` (`d440ad9223a24c0d8e746c7236f7ef27`)

## Intent

Garage should feel like one continuous mobile assembly workspace.
The player should first lock onto roster slots, then edit the selected part, then evaluate the resulting build, while the save dock stays obviously persistent.

## Reading Order

1. `Current Slot Summary + Slot Selector`
2. `Part Focus Bar`
3. `Focused Editor`
4. `Preview + Summary`
5. `Save Dock`

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
- Mobile workspace root: `/Canvas/GaragePageRoot/GarageMobileStackRoot`
- Focus bar anchor: `/Canvas/GaragePageRoot/GarageMobileStackRoot/GarageMobileTabBar`
- Scroll body host: `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost`
- Scroll body content: `/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent`
- Slot grid root: `/Canvas/GaragePageRoot/GarageContentRow/RosterListPane/MobileSlotGrid`
- Save dock root: `/Canvas/GaragePageRoot/MobileSaveDock`
- Save button: `/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveButton`
- Settings overlay root: `/Canvas/GaragePageRoot/GarageSettingsOverlay`
- Settings close action: `/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard/SettingsCloseButton`

## Translation Rules

- Preserve the existing `slot first -> single scroll body -> fixed save dock` contract.
- Use the Stitch workspace as a density and hierarchy target, not as a request to add a separate right rail or desktop split.
- The focused editor should dominate vertical space after slot selection.
- Preview and summary should feel evaluative and complete even when no flashy model content is available.
- Auxiliary account and settings surfaces stay visually quieter than the main assembly workspace.

## Validation Focus

- Slot state is readable at first screen
- Focused editor feels like the main work zone
- Save dock remains visible or obviously persistent in mobile flow
- Saving returns the user to a slot-confirmation context, not to a deep buried editor state
- Settings overlay still behaves as an auxiliary panel, not as a competing workspace

## Assumptions

- This handoff stays within the current mobile-only Garage policy.
- Weapon-pick overlay, comparison view, and cost breakdown remain follow-up variants, not baseline requirements for this pass.
