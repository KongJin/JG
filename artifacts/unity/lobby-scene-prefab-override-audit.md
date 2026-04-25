# LobbyScene Prefab Override Audit

> generatedAt: 2026-04-25
> source: `Assets/Scenes/LobbyScene.unity`
> purpose: optional closeout evidence for `plans.lobby-scene-ui-prefab-management`

This is a read-only YAML audit of prefab instance overrides currently present in `LobbyScene`.
It is not a new workflow gate.

## Summary

| Surface | Prefab asset | Override count | Active overrides | Classification | Note |
|---|---|---:|---:|---|---|
| `SetCRoomDetailPanelRoot` | `Assets/Prefabs/Features/Lobby/Independent/SetCRoomDetailPanelRoot.prefab` | 22 | 1 | allowed candidate | Root placement/default active override only. |
| `GaragePageRoot` | `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab` | 25 | 4 | review candidate | Root placement plus multiple active overrides; confirm these are intentional scene-state hides. |
| `SetACreateRoomModalRoot` | `Assets/Prefabs/Features/Lobby/Root/SetACreateRoomModalRoot.prefab` | 22 | 1 | allowed candidate | Root placement/default active override only. |
| `LobbyPageRoot` | `Assets/Prefabs/Features/Lobby/Root/LobbyPageRoot.prefab` | 24 | 3 | review candidate | Root placement plus multiple active overrides; confirm these are intentional scene-state hides. |
| `SetCCommonErrorDialogRoot` | `Assets/Prefabs/Features/Common/Independent/SetCCommonErrorDialogRoot.prefab` | 22 | 1 | allowed candidate | Root placement/default active override only. |
| `SetCLoginLoadingOverlayRoot` | `Assets/Prefabs/Features/Common/Independent/SetCLoginLoadingOverlayRoot.prefab` | 22 | 1 | allowed candidate | Root placement/default active override only. |

## Property Families

All captured overrides currently fall into root placement/default-state families:

- `m_Name`
- `m_IsActive`
- `m_AnchorMin.*`
- `m_AnchorMax.*`
- `m_AnchoredPosition.*`
- `m_SizeDelta.*`
- `m_Pivot.*`
- `m_LocalPosition.*`
- `m_LocalRotation.*`
- `m_LocalEulerAnglesHint.*`

No `m_text`, `m_Color`, sprite, material, or component reference override appeared in this pass.

## Follow-Up

- Confirm the extra `m_IsActive` overrides on `LobbyPageRoot` and `GaragePageRoot` are the intended scene-level hides for runtime assembly.
- If future audits show text/color/child layout overrides inside prefab surfaces, move those changes back to the prefab or Stitch translation lane unless there is a scene-contract reason to keep them.
