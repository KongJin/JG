# UITK Shared Navigation Template Report

Generated: 2026-04-28 21:00 KST

## Scope

- `LobbyShell.uxml` now uses the Stitch-derived shared navigation asset instead of duplicating the nav markup inline.
- Source asset: `Assets/UI/UIToolkit/Shared/SharedNavigationBar.uxml`
- Stitch source: `artifacts/stitch/11729197788183873077/7a083f26ec05412ca84188517d17d13f/screen.png`

## Result

- Replaced inline `SharedNavigationBar` markup in `Assets/UI/UIToolkit/Lobby/LobbyShell.uxml` with a `ui:Template` / `ui:Instance` of `SharedNavigationBar.uxml`.
- Existing runtime query names are preserved: `LobbyNavButton`, `GarageNavButton`, `RecordsNavButton`.
- Fresh Play Mode captures confirm nav renders on Lobby and Garage:
  - `artifacts/unity/uitk-nav-shared-template-fresh-lobby.png`
  - `artifacts/unity/uitk-nav-shared-template-fresh-garage.png`

## Validation

- `tools/check-compile-errors.ps1`: errors 0, warnings 0.
- Play Mode was stopped after capture.
