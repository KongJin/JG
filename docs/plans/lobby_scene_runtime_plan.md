# LobbyScene 런타임 조립 계획

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: plans.lobby-scene-runtime
> role: plan
> owner_scope: `Assets/Scenes/LobbyScene.unity` 생성, runtime wiring, acceptance route
> upstream: plans.progress, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Scenes/LobbyScene.unity`, `ProjectSettings/EditorBuildSettings.asset`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 `LobbyScene`을 visual-only 화면이 아니라 실제 런타임 진입 씬으로 조립하기 위한 실행 계획이다.
현재 상태와 우선순위는 `progress.md`를 우선하고, Unity UI authoring route와 evidence gate는 [`../ops/unity_ui_authoring_workflow.md`](../ops/unity_ui_authoring_workflow.md)를 따른다.

## 목표

- `Assets/Scenes/LobbyScene.unity`를 새로 만든다.
- `LobbyScene`을 Build Settings의 첫 번째 enabled scene으로 등록한다.
- 로그인, 로비, 개러지, 방 생성/상세 흐름이 Play Mode에서 required reference 누락 없이 진입되게 한다.
- `LobbyScene -> GameScene` 전환 이름은 `GameScene`으로 유지하되, `GameScene.unity` 생성은 이번 계획 범위에 넣지 않는다.

## 현재 기준

- `Assets/Scenes/LobbyScene.unity`는 생성됐고 Build Settings에 enabled scene으로 등록됐다.
- `Tools/Scene/Rebuild Lobby Scene Runtime (Destructive)` helper로 scene instance wiring을 한 차례 조립했다.
- 최신 smoke capture는 `artifacts/unity/lobby-scene-runtime-smoke.png`다.
- 기존 `LobbySceneFileTool`은 빈 씬 생성과 열기만 담당하며, scene contract owner가 아니다.
- `LobbyPageRoot`, `GaragePageRoot`, Set A/C overlay prefab들은 UI surface로 사용할 수 있지만, 현재 prefab 자체가 active runtime owner라고 보지 않는다.
- 런타임 SSOT는 Unity scene/prefab의 serialized wiring이다.
- 현재 runtime wiring smoke는 통과했지만, visual fidelity final pass는 아직 별도 판단이 필요하다.

## Authoring route

- 기본 route는 `scene/prefab authoring`이다.
- Unity MCP로 hierarchy, component, serialized reference를 직접 만들고 확인한다.
- editor helper는 MCP만으로 반복 reference binding이 안정적으로 닫히지 않을 때의 bounded fallback으로만 쓴다.
- helper를 쓰더라도 code-driven rebuild route나 hidden runtime lookup을 scene contract owner로 만들지 않는다.

## 조립 구조

씬 루트는 아래 구조를 기준으로 만든다.

| Root | 역할 |
|---|---|
| `/LobbyRuntime` | `LobbySetup`, adapter, setup, sound, error presenter 등 런타임 오브젝트 |
| `/LobbyCanvas` | Lobby/Garage page와 modal/overlay UI surface |
| `/EventSystem` | UGUI 입력 |

`/LobbyRuntime`에는 최소 아래 컴포넌트가 배치되고 serialized reference가 채워져야 한다.

- `LobbySetup`
- `LobbyPhotonAdapter`
- `AccountSetup`
- `UnitSetup`
- `GarageSetup`
- `SoundPlayer`
- `SceneErrorPresenter`

`/LobbyCanvas`에는 기존 surface prefab을 배치하되, scene instance 기준으로 필요한 view/presenter reference를 명시적으로 연결한다.

- `LobbyPageRoot`
- `GaragePageRoot`
- `SetACreateRoomModalRoot`
- `SetCRoomDetailPanelRoot`
- `SetCCommonErrorDialogRoot`
- `SetCLoginLoadingOverlayRoot`

## Wiring 기준

- `LobbySetup`은 `LobbyView`, `LobbyPhotonAdapter`, `SceneErrorPresenter`, `SoundPlayer`, `AccountSetup`, `LoginLoadingView`, `AccountSettingsView`, `UnitSetup`, `GarageSetup`을 참조한다.
- `AccountSetup`은 `AccountConfig.asset`을 참조한다.
- `UnitSetup`은 `ModuleCatalog.asset`을 참조한다.
- `SoundPlayer`는 `SoundCatalog.asset`을 참조한다.
- `GarageSetup`은 `GarageNetworkAdapter`, `GaragePageController`, module catalog 기반 흐름을 참조한다.
- `LobbyView`는 Lobby/Garage page roots, nav bar, room list/detail, canvas groups, garage summary view를 참조한다.
- `LoginLoadingView`, `AccountSettingsView`, `SceneErrorPresenter`, `RoomListView`, `RoomDetailView`, `GaragePageController`는 Play Mode에서 필수 serialized field가 비지 않아야 한다.

## 실행 순서

1. 현재 prefab hierarchy와 required reference를 다시 audit한다. 완료.
2. Unity MCP로 씬 생성과 메뉴 실행을 수행한다. 완료.
3. MCP에 prefab instantiate endpoint가 없어 bounded editor helper로 scene instance wiring을 조립한다. 완료.
4. `LobbyScene`을 열어 required reference와 hierarchy를 점검한다. 완료.
5. Play Mode에서 로그인 로딩과 Lobby/Garage tab 전환을 smoke 확인한다. 완료.
6. 시각 겹침과 layout density를 별도 visual fidelity pass로 정리한다.
7. `GameScene` 로드 시도는 다음 `GameScene` 생성 작업의 연결 후보로 남긴다.

## Acceptance

- `Assets/Scenes/LobbyScene.unity`가 존재한다.
- Build Settings에 `LobbyScene`이 enabled로 등록되어 있다.
- `LobbySetup`과 관련 view/presenter의 required serialized reference가 비어 있지 않다.
- Play Mode 진입 시 MissingReferenceException 또는 NullReferenceException 없이 Lobby 화면이 뜬다.
- Lobby/Garage tab 전환이 동작한다.
- 로그인 로딩/에러 UI가 깨지지 않는다.
- 방 생성/상세 패널 호출이 missing reference 없이 동작한다.
- Garage 화면이 열리고 기본 roster/editor/save UI가 missing reference 없이 반응한다.
- `GameScene.unity` 부재로 start-game 경로가 막히면, 이는 LobbyScene wiring 실패가 아니라 다음 GameScene 작업 blocker로 기록한다.

## 검증

- Unity compile/reload 안정화
- `npm run --silent rules:lint`
- `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- 씬 required-field audit
- Play Mode smoke

검증 결과가 막히면 `blocked`로 남기고 아래를 분리한다.

- LobbyScene wiring 자체 문제
- 기존 presentation responsibility lint residual
- `GameScene.unity` 부재로 인한 start-game end-to-end blocker

## 제외 범위

- `GameScene.unity` 생성
- 전투 HUD/결과 overlay runtime 연결
- 새로운 Stitch source 생성
- 기존 Stitch prefab 삭제 또는 이동
- code-driven builder 또는 rebuild route를 기본 authoring 경로로 재도입
- 정책을 우회하기 위한 runtime lookup/add-component fallback 도입

## 리스크

- 현재 visual prefab들이 runtime-ready owner가 아니므로 scene instance wiring 양이 많다.
- `GaragePageController` 관련 presentation responsibility lint residual이 남아 있으면, LobbyScene assembly 검증과 별도 blocker로 분리해야 한다.
- `GameScene` 부재 상태에서는 start-game end-to-end acceptance를 닫을 수 없다.
- 현재 smoke capture에서 Stitch visual surface와 runtime controls가 일부 겹치므로 visual fidelity pass가 필요하다.
