# Account Feature

## Ownership

- Scene wiring owner: `LobbySetup`
- Feature composition root: `AccountSetup`
- Presentation owner: `AccountSettingsView`

## Runtime Contract

- Anonymous sign-in is the default entry path for Lobby.
- Account profile is loaded after sign-in and rendered into the Garage `AccountCard`.
- Delete flow stays ordered as `Firestore documents -> Firebase Auth account -> local sign-out -> scene reload`.
- Logout and delete both end by reloading the current Lobby scene so a fresh sign-in path can restart cleanly.

## Scene Contract

- `LobbySetup` inspector wiring is the source of truth for `_accountSetup`, `_loginLoadingView`, and `_accountSettingsView`.
- `AccountSettingsView` text/button references are scene-owned and inspector-wired. Do not add runtime child lookups or fallback `GetComponentInChildren<T>()` wiring.
- `CodexLobbyScene` right-rail `AccountCard` is the current host for account UI. If hierarchy changes, update the scene/builder wiring instead of patching runtime layout code.

## WebGL Smoke Contract

- Development builds expose `AccountSettingsView.WebglSmokeDeleteAccount*` entry points for browser smoke automation.
- Browser smoke should target the `AccountCard` GameObject through `unityInstance.SendMessage(...)`.
- Use `tools/webgl-smoke/account-delete-smoke.cjs` for account deletion smoke instead of coordinate-based clicking.
