# Account / Connection UI Toolkit Candidate Report

- Status: candidate surface previewed, runtime integration not started
- Route: `uitk-candidate`
- Viewport basis: `390x844` UI Toolkit preview scenes
- Capture route: GameView screenshot from isolated preview scenes

## Account Sync

- Source freeze: `artifacts/stitch/11729197788183873077/7bc5b4ca92ca45559d4207a067057b57/`
- UXML: `Assets/UI/UIToolkit/AccountSync/AccountSyncConsole.uxml`
- USS: `Assets/UI/UIToolkit/AccountSync/AccountSyncConsole.uss`
- Preview scene: `Assets/Scenes/AccountSyncUitkPreview.unity`
- PanelSettings: `Assets/Settings/UI/AccountSyncPanelSettings.asset`
- Capture: `artifacts/unity/account-sync-uitk-preview-gameview.png`

## Connection Reconnect

- Source freeze: `artifacts/stitch/11729197788183873077/4e2da1df82fe4c619de57a4133a527dc/`
- UXML: `Assets/UI/UIToolkit/ConnectionReconnect/ConnectionReconnectControl.uxml`
- USS: `Assets/UI/UIToolkit/ConnectionReconnect/ConnectionReconnectControl.uss`
- Preview scene: `Assets/Scenes/ConnectionReconnectUitkPreview.unity`
- PanelSettings: `Assets/Settings/UI/ConnectionReconnectPanelSettings.asset`
- Capture: `artifacts/unity/connection-reconnect-uitk-preview-gameview.png`

## Scope

- This pass does not replace `LobbyScene` or any runtime uGUI surface.
- This pass does not wire account/cloud/reconnect behavior.
- The preview scenes exist only to review UI Toolkit candidate structure and first-read hierarchy.

## Review Notes

- Account candidate keeps identity, sync state, blocked reason, data state, and system parameter groups visible in a single mobile-first column.
- Connection candidate keeps dominant blocked state, pipeline rows, reason block, manual retry, lobby return, and diagnostics visible with no blank capture.
- Both captures are mechanical preview evidence, not product acceptance.
