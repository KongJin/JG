# Game Flow MCP Capture Report

- Generated: 2026-04-26 22:35 KST
- MCP root: `http://127.0.0.1:52675`
- Start scene: `LobbyScene`
- Final verified scene: `BattleScene`
- Console errors: `0`

## Flow Result

| Step | Result | Evidence |
|---|---|---|
| Login/session bootstrap to Lobby | PASS | `02-lobby.png` |
| Lobby tab and create-room form | PASS | `02-lobby.png`, `06-create-room-filled.png` |
| Garage tab | PASS | `03-garage.png` |
| Garage settings overlay open | PASS | `04-garage-settings.png` |
| Return to Lobby | PASS | `05-lobby-return.png` |
| Create room | PASS | `07-room-detail-or-create-result.png` |
| Ready toggle | PASS | `08-room-ready.png` |
| Start game to BattleScene | PASS | `09-battle-entry.png`, `ready-start-events.json` |

## Visual Findings

- Room detail screen works functionally, but layout is visibly broken: the player/display text wraps vertically and several placeholder/source strings are visible.
- BattleScene loads and initializes successfully, but HUD/text scale appears very small in the captured mobile frame.
- Garage settings overlay opens and closes through the Unity button event path; the overlay is simple but operational.

## Runtime Notes

- Photon lobby connection reached `Joined lobby. Ready for matchmaking.` before room creation.
- Ready and Start buttons were invoked via MCP `/ui/invoke`; `BattleScene` became the active scene.
- No Unity console errors were reported by `/console/errors` after the run.
- Warnings observed: `SoundPlayer` `DontDestroyOnLoad` placement warning, missing Firestore settings/stats docs, and Photon dev-region warning.

## Captures

- `artifacts/unity/game-flow/02-lobby.png`
- `artifacts/unity/game-flow/03-garage.png`
- `artifacts/unity/game-flow/04-garage-settings.png`
- `artifacts/unity/game-flow/05-lobby-return.png`
- `artifacts/unity/game-flow/06-create-room-filled.png`
- `artifacts/unity/game-flow/07-room-detail-or-create-result.png`
- `artifacts/unity/game-flow/08-room-ready.png`
- `artifacts/unity/game-flow/09-battle-entry.png`
- `artifacts/unity/game-flow/ready-start-events.json`
