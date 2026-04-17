# Garage Feature

## Ownership

- Scene root: `CodexLobbyScene/GaragePageRoot`
- Scene wiring owner: `GarageSetup`
- Presentation owner: `GaragePageController`

## Runtime Contract

- `GaragePageState` keeps both `CommittedRoster` and `DraftRoster`.
- Only `CommittedRoster` is saved to Firestore/local cache/Photon and can unlock Lobby Ready.
- `DraftRoster` is local UI state. Part cycling and clear actions mutate draft only.
- `SaveRosterUseCase` is called only from the explicit save action.

## View Responsibilities

- `GarageRosterListView`: slot card list with saved/unsaved state.
- `GarageUnitEditorView`: selected slot drafting controls.
- `GarageResultPanelView`: ready status, validation copy, stats, save CTA.
- `GarageUnitPreviewView`: lightweight preview support for the selected draft slot.

## Scene Contract

- `GaragePageController` layout targets (`RosterListPane`, `UnitEditorPane`, `ResultPane`, `PreviewRawImage`, `AccountCard`) are inspector-wired `RectTransform` refs.
- `GarageUnitEditorView`, `GarageResultPanelView`, `GaragePartSelectorView` button/text refs are inspector-wired. Do not restore runtime `transform.Find` or `GetComponentInChildren<TMP_Text>()` fallbacks for scene-owned UI.

## Event Contract

- `GarageInitializedEvent`: committed roster loaded from storage.
- `RosterSavedEvent`: committed roster saved and synced.
- `GarageDraftStateChangedEvent`: current unsaved state and Ready eligibility for lobby UI.
