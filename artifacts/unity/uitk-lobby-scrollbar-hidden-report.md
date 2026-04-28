# UITK Lobby Scrollbar Hidden Report

Generated: 2026-04-28 21:05 KST

## Scope

- Hide the visible right-side scrollbar on the Lobby page while preserving scroll behavior.

## Result

- Changed `LobbyUitkPage` in `Assets/UI/UIToolkit/Lobby/LobbyShell.uxml` from `vertical-scroller-visibility="Auto"` to `vertical-scroller-visibility="Hidden"`.
- Fresh Play Mode capture confirms the white right-side scrollbar is no longer visible:
  - `artifacts/unity/uitk-lobby-scrollbar-hidden.png`

## Validation

- `tools/check-compile-errors.ps1`: errors 0, warnings 0.
- Play Mode was stopped after capture.
