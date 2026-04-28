# UITK Bottom Flow Fix Report

Date: 2026-04-28

## Scope

- Fixed LobbyScene embedded page bottom UI overlap with the shared navigation bar.
- Kept Battle HUD bottom dock out of scope because it belongs to BattleScene HUD policy.

## Changes

- `LobbyShell` now overrides the shared navigation template inside the shell so the nav participates in normal flex layout instead of floating over the workspace.
- Garage `SaveDock` moved inside `WorkspaceScroll` and now renders as the last scroll content block.
- Records `ReturnDock` moved inside `MemoryScroll` and now renders as the last scroll content block.
- Account and Connection were audited: Account has only decorative absolute bolts; Connection `ManualActionDock` is already in scroll flow.

## Evidence

- Garage: `artifacts/unity/uitk-bottom-flow-final-garage.png`
- Records: `artifacts/unity/uitk-bottom-flow-final-records.png`
- Lobby: `artifacts/unity/uitk-bottom-flow-final-lobby.png`
- Account: `artifacts/unity/uitk-bottom-flow-final-account.png`
- Connection: `artifacts/unity/uitk-bottom-flow-final-connection.png`

## Result

- Garage no longer shows `저장 및 배치` as a fixed dock above the shared navigation bar.
- Records no longer shows return/open-garage actions as a fixed dock above the shared navigation bar.
- The shared navigation bar remains the only fixed bottom chrome in LobbyScene.
- Compile check passed with 0 errors and 0 warnings.
