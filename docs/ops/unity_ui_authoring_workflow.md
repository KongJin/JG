# Unity UI Authoring Workflow

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: ops.unity-ui-authoring-workflow
> role: ssot
> owner_scope: Unity UI/UX authoring route, evidence gate, 금지 경로, 종료 proof
> upstream: plans.progress
> artifacts: `artifacts/unity/`, `tools/unity-mcp/*.ps1`

이 문서는 이 레포에서 Unity UI/UX 작업을 시작할 때 에이전트가 가장 먼저 따라야 하는 단일 기준이다.
정책 본문은 여기 한 곳에만 둔다.
`AGENTS.md`, `docs/index.md`, skill 문서, `tools/unity-mcp/README.md`는 이 문서를 가리키는 엔트리 또는 실행 reference로만 유지한다.

## 목적

- Unity UI/UX 작업의 허용 route를 먼저 고정한다.
- 금지된 authoring 경로를 작업 시작 전에 차단한다.
- 작업 종료 시 무엇을 증명해야 하는지 artifact 기준으로 고정한다.

## 허용 Route

| route | 언제 쓰는가 | 기본 작업 방식 | 최소 증거 |
|---|---|---|---|
| `scene/prefab authoring` | `Assets/Scenes/*.unity`, `Assets/**/*.prefab` 중심 UI 작업 | MCP로 현재 hierarchy와 serialized ref를 읽고, scene/prefab을 직접 authoring | compile/reload + workflow policy check |
| `presentation-code` | `Assets/Scripts/Features/*/Presentation/*.cs` 변경이 필요한 상태 렌더/이벤트/UI 바인딩 작업 | 코드는 상태 렌더, 이벤트, 데이터 연결만 담당 | compile/reload + workflow policy check + presentation validator |
| `mixed` | scene/prefab과 presentation code를 함께 건드릴 때 | scene contract를 먼저 읽고, scene/prefab authoring을 주인으로 유지 | compile/reload + workflow policy check + 필요한 scene-specific evidence |
| `codex-lobby-ui` | `CodexLobbyScene` 또는 Lobby/Garage UI 작업 | `CodexLobbyScene.unity`를 runtime SSOT로 보고 MCP repair 우선 | fresh workflow gate + fresh canonical smoke |
| `game-scene-ui` | `GameScene` HUD/소환/결과 UI 작업 | `GameScene.unity`와 관련 prefab을 MCP repair로 직접 정리 | compile/reload + workflow policy check + relevant smoke freshness when present |

## 금지 작업

- `Features.*.Presentation`에서 geometry, transform, material 같은 visual authoring 수행 금지
- 새 UI prefab 생성 기본 금지
- Unity가 열어 둔 scene의 디스크 파일 직접 overwrite 금지
- code-driven builder 또는 rebuild route를 UI authoring 기본 경로로 재도입 금지
- smoke나 capture를 layout authoring의 대체 수단으로 사용 금지

## 허용 작업

- scene/prefab authoring은 Unity MCP repair로 수행
- 코드는 상태 렌더, 이벤트, 데이터 연결, thin orchestration만 담당
- scene contract owner는 serialized scene/prefab 상태로 유지
- visual handoff는 `Stitch`를 참고하되, runtime SSOT는 Unity scene/prefab으로 유지

## Route별 필수 증거

### `CodexLobbyScene`

- `artifacts/unity/codex-lobby-ui-workflow-result.json`
- `artifacts/unity/lobby-garage-page-switch-result.json`
- 두 artifact는 관련 source 변경보다 최신이어야 한다.

### `GameScene`

- compile/reload 안정화
- `verify-presentation-layout-ownership` 결과
- 관련 smoke artifact가 이미 존재하면 freshness도 확인
- 대표 artifact:
  - `artifacts/unity/game-scene-summon-smoke-result.json`
  - `artifacts/unity/game-scene-placement-wave-result.json`

### 일반 Unity UI 변경

- compile/reload 안정화
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` 결과

## 작업 종료 순서

1. compile/reload를 안정화한다.
2. workflow policy check를 실행한다.
3. scene-specific gate 또는 smoke를 실행한다.
4. artifact freshness를 확인한다.
5. 관련 문서를 동기화한다.

## 강제 장치

- `PresentationLayoutOwnershipValidator`
  - `Features.*.Presentation`의 runtime geometry/visual authoring을 hard-stop으로 차단한다.
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
  - 현재 변경 파일을 읽고 route를 판정한다.
  - 필요한 validator, artifact freshness, prefab 금지 규칙을 검사한다.
- `Invoke-CodexLobbyUiWorkflowGate.ps1`
  - `CodexLobbyScene` acceptance proof를 생성한다.
- `Invoke-LobbyGaragePageSwitchSmoke.ps1`
  - canonical runtime evidence를 생성한다.

## 실행 Entry

- 엔트리: [`../../AGENTS.md`](../../AGENTS.md)
- 문서 인덱스: [`../index.md`](../index.md)
- MCP 실행 reference: [`../../tools/unity-mcp/README.md`](../../tools/unity-mcp/README.md)
- policy script: [`../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`](../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1)
