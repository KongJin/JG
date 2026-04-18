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
- `InitializeGarageUseCase` must also sync the restored committed roster into Photon player custom properties so room join and GameScene entry can reuse it without another manual save.
- `GarageNetworkAdapter` treats Photon player custom properties as the source of truth for in-room roster handoff.
- New scene instances must be able to hydrate roster/ready cache from existing Photon player properties without waiting for a fresh `OnPlayerPropertiesUpdate`.

## View Responsibilities

- `GarageRosterListView`: slot card list with saved/unsaved state.
- `GarageUnitEditorView`: selected slot drafting controls.
- `GarageResultPanelView`: ready status, validation copy, stats, save CTA.
- `GarageUnitPreviewView`: lightweight preview support for the selected draft slot.

## Scene Contract

- `CodexLobbyScene` and the editor builder/augmenter own Garage layout geometry.
- `GaragePageController` is not a layout author. Do not reintroduce runtime `RectTransform` anchor/offset/size rewrites for `RosterListPane`, `UnitEditorPane`, `ResultPane`, `PreviewRawImage`, `AccountCard`, or their parent containers.
- `GarageUnitEditorView`, `GarageResultPanelView`, `GaragePartSelectorView` button/text refs are inspector-wired. Do not restore runtime `transform.Find` or `GetComponentInChildren<TMP_Text>()` fallbacks for scene-owned UI.
- If desktop/mobile structure changes, update the scene/builder contract and serialized hierarchy instead of adding `NormalizeLayout()`-style runtime fixes in presentation code.
- Development builds expose `GaragePageController.WebglSmoke*` entry points for browser smoke automation. Use them from `tools/webgl-smoke/garage-save-load-smoke.cjs` instead of coordinate-based clicking.

## Event Contract

- `GarageInitializedEvent`: committed roster loaded from storage.
- `RosterSavedEvent`: committed roster saved and synced.
- `GarageDraftStateChangedEvent`: current unsaved state and Ready eligibility for lobby UI.
