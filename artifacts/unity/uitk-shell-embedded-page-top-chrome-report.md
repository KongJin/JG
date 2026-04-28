# UITK Shell Embedded Page Top Chrome Report

Date: 2026-04-28

## Scope

- Removed overlapping page-owned top chrome when Garage, Records, Account, and Connection are embedded in the Lobby shared shell.
- Kept authored page UXML structure intact for standalone preview/debug use.
- Preserved the single production shell top bar and shared navigation bar inside `LobbyShell`.

## Changed Behavior

- `GarageUitkHost` now carries `garage-shell-host` so the embedded Garage workspace can hide `TopAppBar` only in the shared-shell route.
- `RecordsUitkHost`, `AccountUitkHost`, and `ConnectionUitkHost` hide their own top/title strips through shell-scoped USS selectors.
- `LobbyView` updates `ShellStateLabel` per route so the shared top shell still communicates each page state after embedded headers are hidden.

## Play Mode Captures

- Lobby: `artifacts/unity/uitk-shell-clean-v2-lobby.png`
- Garage: `artifacts/unity/uitk-shell-clean-v2-garage.png`
- Records: `artifacts/unity/uitk-shell-clean-v2-records.png`
- Account: `artifacts/unity/uitk-shell-clean-v2-account.png`
- Connection: `artifacts/unity/uitk-shell-clean-v2-connection.png`

## Result

- Garage no longer shows the page-local `격납고 관리` top bar under the shared `차고` shell.
- Records no longer shows `MemoryTopShell` under the shared `기록` shell.
- Account no longer shows `AccountTopShell` or `AccountTitleStrip` under the shared `계정` shell.
- Connection no longer shows `ConnectionTopShell` under the shared `연결` shell.
- Shared navigation remains visible on all LobbyScene pages.
