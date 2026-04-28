# UITK Shell Safe Area V2 Report

Date: 2026-04-28

## Scope

- Follow-up fix for LobbyScene shared shell chrome consistency.
- Applies to Lobby, Garage, Records, Account, and Connection pages inside `LobbyShell`.
- BattleScene HUD remains out of scope.

## Changes

- Shared top shell is fixed to 58px with min/max height and no vertical padding drift.
- Shared bottom navigation is fixed to 62px with min/max height and normal shell flow.
- Shared workspace now reserves a 12px gap above navigation.
- Embedded page scroll views now receive 112px bottom safe padding in shell context.
- Runtime fallback shell styling in `LobbyView` uses the same 58px top / 62px nav values.

## Evidence

- Lobby: `artifacts/unity/uitk-shell-safe-area-v2-lobby.png`
- Garage: `artifacts/unity/uitk-shell-safe-area-v2-garage.png`
- Records: `artifacts/unity/uitk-shell-safe-area-v2-records.png`
- Account: `artifacts/unity/uitk-shell-safe-area-v2-account.png`
- Connection: `artifacts/unity/uitk-shell-safe-area-v2-connection.png`

## Result

- Top shared UI height is visually consistent across all captured pages.
- Page content no longer sits directly under or partly inside the bottom navigation zone.
- Compile check passed with 0 errors and 0 warnings.
