# LobbyScene Prefab Override Audit

> generatedAt: 2026-04-27 00:18:03+09:00
> source: `Assets/Scenes/LobbyScene.unity`
> purpose: warning evidence for `plans.prefab-management-gap-closeout`

This is a read-only YAML audit of prefab instance overrides currently present in `LobbyScene`.
It is workflow policy evidence, but starts as warning/review evidence rather than a hard gate.

## Summary

- surfaces: 6
- allowed candidates: 3
- review candidates: 2
- warnings: 1
- visual/text/color/asset-reference overrides: 1

| Surface | Prefab asset | Override count | Active overrides | Visual overrides | Classification | Note |
|---|---|---:|---:|---:|---|---|
| GaragePageRoot | Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab | 25 | 4 | 0 | review-candidate | Root placement/default active is allowed, but internal active/layout override candidates should be confirmed. internalActive=3 internalLayoutTargets=0 |
| LobbyPageRoot | Assets/Prefabs/Features/Lobby/Root/LobbyPageRoot.prefab | 24 | 3 | 0 | review-candidate | Root placement/default active is allowed, but internal active/layout override candidates should be confirmed. internalActive=2 internalLayoutTargets=0 |
| SetACreateRoomModalRoot | Assets/Prefabs/Features/Lobby/Root/SetACreateRoomModalRoot.prefab | 22 | 1 | 0 | allowed-candidate | Only root name, placement, and default active override families were found. |
| SetCCommonErrorDialogRoot | Assets/Prefabs/Features/Common/Independent/SetCCommonErrorDialogRoot.prefab | 22 | 1 | 0 | allowed-candidate | Only root name, placement, and default active override families were found. |
| SetCLoginLoadingOverlayRoot | Assets/Prefabs/Features/Common/Independent/SetCLoginLoadingOverlayRoot.prefab | 22 | 1 | 0 | allowed-candidate | Only root name, placement, and default active override families were found. |
| SetCRoomDetailPanelRoot | Assets/Prefabs/Features/Lobby/Independent/SetCRoomDetailPanelRoot.prefab | 24 | 2 | 1 | warning | Visual/text/color/asset-reference or unknown prefab overrides need review before acceptance. visual=1 unknown=0 |

## Property Families

Warnings are triggered by text/color/asset-reference/unknown property overrides. Review candidates are internal active/layout overrides that may still be intentional scene state.

## Follow-Up

- Review warning surfaces before acceptance: SetCRoomDetailPanelRoot.
- Confirm review-candidate active/layout overrides are intentional scene-level state: GaragePageRoot, LobbyPageRoot.

