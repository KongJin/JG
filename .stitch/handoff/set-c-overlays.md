# Set C Handoff - Lobby / Account Overlays

> Accepted set date: 2026-04-19

## Accepted Screens

- In-room detail: `Room Detail Panel - Selected State` (`e785bb1479da48de9037dbad91e16ddf`)
- Loading: `Login / Connection Loading Overlay` (`056724f23ac54729903db6fdecd1eab1`)
- Destructive confirm: `Account Deletion Confirmation Overlay` (`b39c877f686d4ea19a2e0ed93e604fcc`)
- Error dialog: `System Connection Error Overlay` (`09d03272c8aa4e90978945b00763ba69`)

## Intent

This set defines the overlay language for room-entry, connection wait, destructive confirmation, and recoverable error states.
These surfaces should interrupt clearly while still feeling like part of the same tactical product.

## Overlay Rules

- Overlays must inherit the same dark tactical hangar language as Lobby and Garage.
- Loading must feel intentional and system-like, not like a generic spinner on black.
- Error and destructive dialogs must isolate the decision clearly without overpowering the entire app shell.
- In-room detail is not a generic modal. It is a selected-room state layered on top of the Lobby flow.

## CTA Priority

### Room Detail

- Primary: room join / ready progression
- Secondary: close or back
- Keep action wording short and operational

### Loading

- No conflicting CTA while connection is in progress
- Optional cancel should remain clearly secondary if exposed

### Account Delete Confirm

- Primary warning action: destructive confirm
- Secondary safe action: cancel
- Visual separation between the two is mandatory

### Error Dialog

- Primary: dismiss / acknowledge / retry depending on runtime error type
- Never surface multiple equal-priority recovery buttons without a concrete reason

## Unity Translation Targets

### Room Detail

- Root: `/Canvas/LobbyPageRoot/RoomDetailPanel`
- Primary room action: `/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/ReadyButton`
- Host start action: `/Canvas/LobbyPageRoot/RoomDetailPanel/ActionButtons/StartGameButton`

### Loading

- Overlay root: `/Canvas/LoginLoadingOverlay`
- Loading panel: `/Canvas/LoginLoadingOverlay/LoadingPanel`
- Error fallback panel: `/Canvas/LoginLoadingOverlay/ErrorPanel`

### Settings / Account Context

- Settings overlay root: `/Canvas/GaragePageRoot/GarageSettingsOverlay`
- Account card root: `/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard`

### Shared Error Presentation

- Lobby-side banner presenter: `/Canvas/SceneErrorPresenter/Banner/BannerMessage`
- Shared modal root in battle/runtime contexts: `/HudCanvas/SceneErrorPresenter/Modal`
- Modal card: `/HudCanvas/SceneErrorPresenter/Modal/ModalCard`
- Modal message: `/HudCanvas/SceneErrorPresenter/Modal/ModalCard/ModalMessage`
- Dismiss button: `/HudCanvas/SceneErrorPresenter/Modal/ModalCard/DismissButton`

## Translation Rules

- Keep room detail as a bottom-sheet-like selected-state panel, not a full page replacement.
- The loading overlay should use short staged status lines and clear progress framing.
- Account deletion must keep destructive emphasis restrained but unmistakable.
- Shared errors should map into the existing presenter pattern instead of introducing ad hoc popup widgets.

## Validation Focus

- `RoomDetailPanel` reads as an in-room state layered over Lobby, not as a detached screen
- `ReadyButton` and `StartGameButton` remain clearly distinguishable by role
- Login loading transition stays readable during waits and error fallback
- Delete confirm cannot be confused with a safe dialog
- Error presenter copy remains short and dismiss flow is obvious

## Assumptions

- The same visual overlay grammar can be reused across Lobby, Garage settings, and shared scene errors as long as CTA roles remain explicit.
