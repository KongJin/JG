# Records Bottom CTA Removal Report

- Scope: LobbyScene Records UITK page
- Change: removed the embedded `ReturnDock` with `RETURN TO LOBBY` and `OPEN GARAGE`
- Reason: those routes already exist in the shared `SharedNavigationBar`, so the page-level CTA duplicated navigation chrome
- Capture: `artifacts/unity/records-no-bottom-cta.png`

## Validation

- Static audit: `ReturnDock`, `ReturnToLobbyButton`, `OpenGarageButton`, `memory-return-dock`, `memory-primary-button`, and `memory-secondary-button` no longer exist in the Records UITK surface or Lobby callback wiring.
- Compile check: `tools/check-compile-errors.ps1` passed with `ERRORS: 0`, `WARNINGS: 0`.
- Play Mode capture: Records page now ends after the operation memory content, with only the shared bottom nav visible.
