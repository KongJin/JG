# Unity UI Authoring Workflow

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: ops.unity-ui-authoring-workflow
> role: ssot
> owner_scope: Unity UI/UX authoring route, evidence gate, 금지 경로, 종료 proof
> upstream: plans.progress
> artifacts: `artifacts/unity/`, `tools/unity-mcp/*.ps1`

이 문서는 이 레포에서 Unity UI/UX 작업을 시작할 때 에이전트가 가장 먼저 따라야 하는 단일 기준이다.
정책 본문은 여기 한 곳에만 둔다.
`AGENTS.md`, `docs/index.md`, skill 문서, `tools/unity-mcp/README.md`는 이 문서를 가리키는 엔트리 또는 실행 reference로만 유지한다.
응집도와 결합도의 상위 정의, 예외, hard-fail/review 경계는 `ops.cohesion-coupling-policy`를 따른다.
이 문서는 Unity UI authoring lane의 파생 규칙과 evidence gate만 소유한다.

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
| `codex-lobby-ui` | legacy Lobby/Garage scene가 아직 실제 authoring 대상일 때만 | historical route. scene가 실제 존재할 때만 MCP repair 사용 | fresh workflow gate |
| `game-scene-ui` | legacy battle scene가 아직 실제 authoring 대상일 때만 | scene가 실제 존재할 때만 MCP repair로 직접 정리 | compile/reload + workflow policy check + route-specific verification |
| `prefab-first reset` | scene/prefab 결과물을 의도적으로 폐기하고 Stitch handoff에서 다시 가져올 때 | scene repair 대신 surface별 baseline prefab을 먼저 재구성하고, scene은 마지막에 새로 조립 | compile/reload + workflow policy check + prefab wiring review + scene 생성 후 fresh contract/translation pipeline |

## 금지 작업

- `Features.*.Presentation`에서 geometry, transform, material 같은 visual authoring 수행 금지
- `Features.*.Presentation/*PageController.cs`에서 smoke entrypoint, theme/style literal 처리, page-sized chrome orchestration 누적 금지
- 새 UI prefab 생성 기본 금지
- Unity가 열어 둔 scene의 디스크 파일 직접 overwrite 금지
- code-driven builder 또는 rebuild route를 UI authoring 기본 경로로 재도입 금지
- capture나 runtime proof를 layout authoring의 대체 수단으로 사용 금지
- scene registry를 `FindFirstObjectByType<*SceneRegistry>` 또는 `AddComponent<*SceneRegistry>` 같은 runtime repair 경로로 복구하는 것 금지

## 허용 작업

- scene/prefab authoring은 Unity MCP repair로 수행
- 코드는 상태 렌더, 이벤트, 데이터 연결, thin orchestration만 담당
- page chrome styling, smoke bridge, oversized save/input helper는 production `*PageController` 바깥의 dedicated collaborator로 분리한다
- scene contract owner는 serialized scene/prefab 상태로 유지
- visual handoff는 `Stitch`를 참고하되, runtime SSOT는 Unity scene/prefab으로 유지
- runtime scene이 이미 폐기된 경우에는 `prefab-first reset` route를 선택하고, surface baseline prefab을 다시 세운 뒤 scene contract를 재조립한다
- scene registry 같은 runtime wiring dependency는 scene/prefab contract로만 유지하고, 숨은 runtime lookup/add-component fallback은 재도입하지 않는다
- `presentation-code` route는 값 하드코딩이나 fallback으로 contract 빈칸을 메우지 않는다. 값 owner는 token, serialized contract, 또는 scene/prefab SSOT다

## Route별 필수 증거

### `CodexLobbyScene`

이 evidence는 legacy scene route가 실제로 존재할 때만 acceptance proof다.
reset 상태에서는 historical reference로만 본다.

- `artifacts/unity/codex-lobby-ui-workflow-result.json`
- artifact는 관련 source 변경보다 최신이어야 한다.

### `GameScene`

- compile/reload 안정화
- `verify-presentation-layout-ownership` 결과
- route-specific verification artifact

### 일반 Unity UI 변경

- compile/reload 안정화
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` 결과

### `prefab-first reset`

- compile/reload 안정화
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` 결과
- prefab 단위 required reference / hierarchy 점검
- 새 scene이 생긴 뒤 fresh contract, translation pipeline 재생성

추가 규칙:

- reset 중에는 helper script, prefab-pack summary artifact, 과거 prefab path 메모를 곧바로 신뢰하지 말고 실제 repo 파일 트리와 현재 prefab hierarchy를 먼저 대조한다.
- hierarchy 이름이 교체되는 과도기에는 translation helper와 verifier가 legacy path와 current path를 모두 처리하도록 유지하고, 모든 호출자가 옮겨간 뒤에만 구형 path를 제거한다.

## 작업 종료 순서

1. compile/reload를 안정화한다.
2. workflow policy check를 실행한다.
3. route-specific verification을 실행한다.
4. artifact freshness를 확인한다.
5. 관련 문서를 동기화한다.

`prefab-first reset` route는 예외적으로 아래 순서를 따른다.

1. compile/reload를 안정화한다.
2. workflow policy check를 실행한다.
3. prefab baseline hierarchy와 required reference를 먼저 점검한다.
4. 새 scene을 조립한 뒤 contract와 translation pipeline artifact를 fresh 상태로 다시 만든다.
5. reset 이전 artifact는 historical reference로만 남긴다.

## 강제 장치

- `PresentationLayoutOwnershipValidator`
  - `Features.*.Presentation`의 runtime geometry/visual authoring을 hard-stop으로 차단한다.
- `presentation responsibility lint`
  - `*PageController`의 line/method/serialized-field 규모와 smoke/style ownership을 검사한다.
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
  - 현재 변경 파일을 읽고 route를 판정한다.
  - 필요한 validator, presentation responsibility lint, artifact freshness, prefab 금지 규칙을 검사한다.
- `Invoke-CodexLobbyUiWorkflowGate.ps1`
  - `CodexLobbyScene` acceptance proof를 생성한다.

## 실행 Entry

- 엔트리: [`../../AGENTS.md`](../../AGENTS.md)
- 문서 인덱스: [`../index.md`](../index.md)
- MCP 실행 reference: [`../../tools/unity-mcp/README.md`](../../tools/unity-mcp/README.md)
- policy script: [`../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`](../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1)
