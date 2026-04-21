# Historical LobbyScene Route

> 마지막 업데이트: 2026-04-21
> 상태: historical
> doc_id: tools.unity-mcp-historical-lobby-scene-route
> role: reference
> owner_scope: 폐기된 Lobby/Garage scene-first route에 대한 간단한 보존 메모
> upstream: tools.unity-mcp-readme, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/unity/`

이 문서는 과거 `LobbyScene.unity` scene-first authoring route를 짧게 보존하는 historical reference다.
현재 기본 경로는 `prefab-first reset`이며, 상세 정책 owner는 `ops.unity-ui-authoring-workflow`다.
구현을 다시 열어야 한다면 현재 경로는 `docs/index.md`에서 해석하고, 이 문서는 historical context로만 사용한다.

## 언제만 참고하는가

- concrete `Assets/Scenes/LobbyScene.unity` authoring scene를 다시 운영하기로 명시적으로 결정했을 때
- `Invoke-CodexLobbyUiWorkflowGate.ps1` 와 `Invoke-LobbyGaragePageSwitchSmoke.ps1` 를 legacy acceptance proof로 다시 활성화할 때

## 현재 기본값

- Lobby/Garage UI는 scene-first repair 대신 `accepted Stitch contract -> baseline prefab wiring -> new scene assembly -> fresh contract/smoke` 순서를 따른다.
- historical gate와 page-switch smoke는 active acceptance proof가 아니다.

## historical route가 다시 필요하면 확인할 것

- `tools/unity-mcp/README.md`
- `docs/ops/unity_ui_authoring_workflow.md`
- `tools/unity-mcp/Invoke-CodexLobbyUiWorkflowGate.ps1`
- `tools/unity-mcp/Invoke-LobbyGaragePageSwitchSmoke.ps1`

## note

- 예전 sentinel node와 serialized reference 상세 목록은 historical 구현 세부에 속하므로, route를 정말 부활시킬 때 스크립트와 당시 scene contract를 함께 재확인한다.
