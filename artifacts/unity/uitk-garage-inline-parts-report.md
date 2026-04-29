# Garage Inline Part List UITK Runtime Report

- Stitch source: `Garage / Unit Assembly (Inline List)`, project `11729197788183873077`, screen `afcfd3b9bd84446b9cbd4657879c25bd`
- Route: active LobbyScene Garage UITK runtime replacement
- Unity surface: `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml`
- Runtime binding: `GaragePanelCatalog` -> inline `PartListCard` -> draft selection -> existing editor/preview/save flow

## Captures

- Frame list: `artifacts/unity/uitk-garage-inline-parts-frame.png`
- Firepower list: `artifacts/unity/uitk-garage-inline-parts-firepower.png`
- Firepower row selected: `artifacts/unity/uitk-garage-inline-parts-firepower-selected.png`
- Search filter: `artifacts/unity/uitk-garage-inline-parts-search-rail.png`
- Mobility list: `artifacts/unity/uitk-garage-inline-parts-mobility.png`

## Runtime Checks

- `[프레임] [무장] [기동]` focus changes rebuild the inline list from `GaragePanelCatalog`.
- Row selection updates draft part state and immediately rerenders the selected row, detail card, and preview selection state.
- Search text filters the visible list and updates the count text.
- `PartListCard`, editor, preview, save dock, and shared nav remain in the same scroll/shell composition without a separate popup or overlay.

## Validation

- Compile check: `tools/check-compile-errors.ps1` passed with `ERRORS: 0`, `WARNINGS: 0`.
- Direct coverage: `GarageSetBUitkSurfaceDirectTests` updated for required query names and factory search filtering.
- Console note: Play Mode produced Photon timeout errors unrelated to the Garage UITK binding while offline.
