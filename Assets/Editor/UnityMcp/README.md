# Unity MCP Bridge

Unity 에디터 안에서 로컬 HTTP 서버를 띄워 외부 도구(Claude Code 등)가 에디터를 원격 제어할 수 있게 한다.

## 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트 확인: `ProjectSettings/UnityMcpPort.txt` (없으면 기본 51234, 충돌 시 52000대 fallback)
- 호출 방법: `powershell -Command "Invoke-RestMethod -Uri 'http://127.0.0.1:{port}/...' -Method ..."`
- `tools/unity-mcp/server.js`(MCP stdio 서버)는 Unity로 나가는 HTTP에 **keep-alive**를 쓰고, 기본 타임아웃은 **10000ms**다. 환경 변수 `UNITY_MCP_HTTP_TIMEOUT_MS`로 덮어쓸 수 있다.

## 작업 순서 주의

- Unity가 플레이 중일 때 C# 스크립트나 브리지 코드(`Assets/Editor/UnityMcp/**`)를 수정해도 새 코드가 즉시 컴파일/반영되지 않을 수 있다.
- 새 엔드포인트 추가, 브리지 수정, 일반 스크립트 수정이 필요하면 반드시 `Play Stop -> 파일 수정 -> 컴파일 완료 확인 -> 브리지 상태 확인 -> 다시 Play` 순서로 진행한다.

## 테스트 / 로그 확인 SOP

- Unity MCP로 씬 열기, 플레이 시작/정지, 버튼 invoke, 입력 전달, 스크린샷 캡처 같은 테스트를 수행할 때는 **현재 콘솔 상태를 함께 확인**한다.
- 확인 범위는 에러만이 아니다. 가능하면 액션 전후로 **에러/경고/일반 로그 흐름**까지 본다.
- 기본 점검 순서:
  1. `/health`로 브리지/플레이/컴파일 상태 확인
  2. 주요 액션 직후 **`GET /console/logs`**로 브리지가 버퍼에 쌓아 둔 최근 콘솔 전체(Log/Warning/Error 등) 확인 (`limit`으로 개수 조절)
  3. 에러만 빠르게 볼 때는 **`GET /console/errors`** (Error/Exception/Assert만, 최근부터 최대 `limit`개)
  4. 버퍼 이전 로그·에디터 전체 로그는 `Editor.log` 최근 구간까지 읽어 보완
- 테스트 결과를 공유할 때는 가능하면 **화면 상태 + 콘솔/로그 상태**를 함께 적는다.
- 통합 스모크: `tools/mcp-test-lobby-scene.ps1` — 로비 플로우 + 스크린샷·콘솔. **게임 씬 시작 스킬 선택까지** 포함하려면 `-CompleteGameSceneStartSkills` (씬 경로는 `../../Scripts/Features/Skill/README.md` MCP 절 참고).
- UI 버튼 경로와 공통 폴링/콘솔 헬퍼는 `tools/mcp-test-common.ps1`를 기준으로 맞춘다. 로비·게임 UI 자동화 스크립트는 이 파일을 dot-source 하므로, 경로가 바뀌면 여기부터 갱신한다.
- `GET /scene/hierarchy`가 특정 조건에서 기대와 다르게 보이면 브리지를 바로 고치지 말고 `tools/mcp-diagnose-scene-hierarchy.ps1`로 `depth`/`includeComponents`/`path` 조합을 먼저 기록한다.
- **JSON 결과 (스모크 판정용)**: 아래 스크립트는 종료 시 `schemaVersion`, `ok`, `steps[]`, `finalHealth`, `screenshots[]`, (실패 시) `failure` 를 UTF-8 JSON 파일로 쓴다. 실패 시 **exit code 1**.
  - `tools/mcp-test-lobby-scene.ps1` — 기본 `Temp/UnityMcp/last-lobby-scene-test.json`. `-ResultJsonPath`로 경로 지정, `-WriteJsonToStdout`면 파일 저장 후 동일 JSON을 stdout에도 출력.
  - `tools/mcp-lobby-to-game.ps1` — 기본 `Temp/UnityMcp/last-lobby-to-game.json`. 동일 파라미터.
  - `tools/mcp-diagnose-scene-hierarchy.ps1` — 기본 `Temp/UnityMcp/scene-hierarchy-diagnose.json`. 루트/서브트리 질의를 함께 저장해 `scene/hierarchy` 누락 재현 자료로 쓴다.
  - CI 예: `powershell -File .\tools\mcp-test-lobby-scene.ps1; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }` 또는 (jq 설치 시) `... | Out-Null; jq -e .ok Temp/UnityMcp/last-lobby-scene-test.json`

## 엔드포인트

### 조회

| Method | Path | 설명 |
|---|---|---|
| GET | `/health` | 서버 상태 (포트, `isCompiling`, isPlaying, isPlayModeChanging 등) |
| GET | `/compile/status` | `isCompiling` 만 빠르게 조회 (`/health`와 동일 플래그) |
| GET | `/scene/current` | 현재 씬 정보 |
| GET | `/scene/hierarchy` | 씬 하이어라키 전체 조회 (쿼리: `depth`, `path`, `includeComponents` — 아래 참조) |
| GET | `/console/logs` | 브리지가 수집한 최근 콘솔 로그 전체 (`Log`, `Warning`, `Error`, `Exception`, `Assert`). 쿼리 `limit` 기본 100, 최대 200. **에디터/도메인 리로드 이후**부터 쌓인 버퍼만 해당 (그 이전은 `Editor.log`) |
| GET | `/console/errors` | 위 버퍼 중 **Error / Exception / Assert** 만 최근부터 최대 `limit`개 (기본 20, 최대 100) |

#### `GET /scene/hierarchy` 쿼리

- `depth`: 최대 깊이, 1–50, 기본 10
- `path` (선택): `GameObject.Find` 경로. 지정 시 해당 오브젝트를 루트로 한 서브트리만 반환
- `includeComponents` (선택): `false` 또는 `0`이면 각 노드의 `components` 배열을 채우지 않음(빈 배열). 하이어라키 구조만 필요할 때 부하 감소. 생략 시 기본은 목록 포함

### 플레이 제어

| Method | Path | 설명 |
|---|---|---|
| POST | `/scene/open` | 씬 열기 (`{"scenePath":"Assets/Scenes/..."}`) |
| POST | `/play/start` | 플레이 모드 시작 |
| POST | `/play/stop` | 플레이 모드 정지 |
| POST | `/compile/request` | 스크립트 재컴파일 요청 (`CompilationPipeline.RequestScriptCompilation`). Body 선택: `{"cleanBuildCache":true}` |
| POST | `/compile/wait` | `EditorApplication.isCompiling` 이 `false`가 될 때까지 대기(HTTP 장기 응답). Body 선택: 아래 참조 |
| POST | `/screenshot/capture` | 플레이 중 Game View 스크린샷을 프로젝트 내부 PNG로 저장하고 경로 반환 |

#### `/scene/open` Body

```json
{
  "scenePath": "Assets/Scenes/JG_LobbyScene.unity",
  "saveCurrentSceneIfDirty": true
}
```

- `scenePath`: 열 씬 경로
- `saveCurrentSceneIfDirty`: `true`면 현재 활성 씬이 dirty 상태일 때 먼저 자동 저장한 뒤 새 씬을 연다. Unity 저장 확인 모달을 피하고 싶을 때 사용한다.
- `saveCurrentSceneIfDirty`를 주지 않거나 `false`면 기존처럼 Unity의 저장 확인창을 따른다.

#### `/screenshot/capture` Body

```json
{
  "outputPath": "Temp/UnityMcp/Screenshots/lobby-check.png",
  "superSize": 1,
  "overwrite": true
}
```

- `outputPath`: 프로젝트 루트 기준 상대 경로. 비우면 `Temp/UnityMcp/Screenshots/shot-yyyymmdd-hhmmss-fff.png`
- `superSize`: Unity `ScreenCapture.CaptureScreenshot` 배율. 기본 `1`, 권장 `1`
- `overwrite`: 같은 파일이 있으면 덮어쓸지 여부. 기본 `false`
- 응답은 이미지 바이트를 직접 반환하지 않고 저장된 파일 경로만 돌려준다. 그래서 캡처 자체는 토큰을 거의 쓰지 않고, 정말 필요할 때만 이미지를 열어 보면 된다.

#### `/compile/wait` Body

```json
{
  "requestFirst": true,
  "cleanBuildCache": false,
  "timeoutMs": 300000,
  "pollIntervalMs": 100
}
```

- `requestFirst`: `true`이면 먼저 `/compile/request`와 동일하게 재컴파일을 요청한 뒤 대기한다. `false`이면 **이미 진행 중인 컴파일**이 끝날 때만 기다린다.
- `cleanBuildCache`: `requestFirst`일 때만 의미 있음 — `RequestScriptCompilationOptions.CleanBuildCache` (전체 다시 컴파일에 가깝게)
- `timeoutMs`: 대기 상한 (밀리초). 기본 300000(5분), 허용 범위 1000~600000
- `pollIntervalMs`: 메인 스레드에서 `isCompiling`을 확인하는 간격. 기본 100, 20~2000
- 응답: `ok`, `timedOut` (`true`면 타임아웃 시점에 아직 `isCompiling`), `waitedMs`, `isCompiling`(최종), `requestedCompilation`

**수동 폴링:** `POST /compile/request` 후 `GET /compile/status` 또는 `GET /health`의 `isCompiling`을 반복 호출해도 된다.

**HTTP 클라이언트 타임아웃:** `/compile/wait`는 응답이 수 분 걸릴 수 있다. PowerShell은 예를 들어 `-TimeoutSec 400`을 주고, `tools/unity-mcp/server.js`의 `unity_compile_wait`는 본문 `timeoutMs`보다 여유 있게 HTTP 타임아웃을 잡는다.

### 입력

| Method | Path | 설명 |
|---|---|---|
| POST | `/input/click` | 플레이 중 Game View에 마우스 클릭 이벤트 전달 |
| POST | `/input/move` | 플레이 중 Game View에 마우스 이동 이벤트만 전달 |
| POST | `/input/drag` | 플레이 중 Game View에 드래그 이벤트 전달 |
| POST | `/input/key` | 플레이 중 Game View에 키 입력 이벤트 전달 |
| POST | `/input/text` | 플레이 중 Game View에 문자열 입력 |
| POST | `/input/scroll` | 플레이 중 Game View에 마우스 휠 이벤트 전달 |
| POST | `/input/key-combo` | 자주 쓰는 키 조합 프리셋 실행 |
| POST | `/ui/button/invoke` | 플레이 중 씬 경로로 `Button.onClick` 직접 invoke |

#### `/input/click` Body

```json
{
  "x": 0.5,
  "y": 0.5,
  "normalized": true,
  "button": 0,
  "clickCount": 1
}
```

- `normalized: true`면 `x`, `y`는 `0~1` 비율 좌표다. 해상도와 무관하게 같은 위치를 누를 때 권장
- `normalized: false`면 `x`, `y`는 Game View 내부 픽셀 좌표다
- `button`: `0` 왼쪽, `1` 오른쪽, `2` 가운데
- `clickCount`: 기본 `1`
- 호출 시 Game View를 열고 포커스한 뒤 `MouseMove -> MouseDown -> MouseUp` 순서로 이벤트를 보낸다

#### `/input/move` Body

```json
{
  "x": 0.5,
  "y": 0.5,
  "normalized": true
}
```

- 클릭 없이 포인터 위치만 업데이트한다
- 좌표 규칙은 `/input/click` 과 동일하다

#### `/input/drag` Body

```json
{
  "startX": 0.2,
  "startY": 0.5,
  "endX": 0.8,
  "endY": 0.5,
  "normalized": true,
  "button": 0,
  "steps": 16
}
```

- `normalized` 규칙은 클릭과 동일하다
- `steps`는 드래그 중간 지점 개수다. 기본 `12`
- 호출 시 `MouseMove -> MouseDown -> MouseDrag... -> MouseUp` 순서로 이벤트를 보낸다

#### `/input/key` Body

```json
{
  "keyCode": "Space",
  "phase": "press",
  "shift": false,
  "control": false,
  "alt": false,
  "command": false
}
```

- `phase`: `press`, `down`, `up` 중 하나. 기본 `press`
- `keyCode`: Unity `KeyCode` 이름
- `character`: 필요하면 함께 보낼 문자. 예: `"a"`, `"\\n"`
- 수정키는 `shift`, `control`, `alt`, `command`로 지정한다

#### `/input/text` Body

```json
{
  "text": "hello world",
  "appendReturn": true
}
```

- `text`의 각 문자를 순서대로 Game View에 전달한다
- `appendReturn: true`면 마지막에 Enter를 한 번 더 보낸다
- 텍스트 입력은 UI 입력 필드나 문자 기반 처리에 적합하고, 게임플레이 키 바인딩 테스트는 `/input/key`가 더 적합하다

#### `/input/scroll` Body

```json
{
  "x": 0.5,
  "y": 0.5,
  "normalized": true,
  "deltaY": 120
}
```

- 먼저 해당 좌표로 `MouseMove`를 보낸 뒤 `ScrollWheel` 이벤트를 보낸다
- `deltaY`가 주 입력값이고, `delta`는 하위 호환 단축값이다
- 가로 스크롤이 필요하면 `deltaX`도 함께 줄 수 있다

#### `/input/key-combo` Body

```json
{
  "preset": "copy",
  "repeat": 1
}
```

- 지원 프리셋: `copy`, `paste`, `cut`, `selectAll`, `undo`, `redo`, `submit`, `cancel`, `tabForward`, `tabBackward`, `delete`
- 현재 프리셋은 Windows 기준 `Control` 조합을 사용한다
- `repeat`로 같은 조합을 여러 번 연속 전송할 수 있다

#### `/ui/button/invoke` Body

```json
{
  "path": "/UIRoot/Canvas/lobby/RoomListView/Header/CreateRoomButton"
}
```

- `path`: 씬 하이어라키 기준 GameObject 경로. 대상 오브젝트에 `UnityEngine.UI.Button` 컴포넌트가 있어야 한다
- 이 엔드포인트는 Game View 좌표 클릭을 우회하고 `Button.onClick.Invoke()`를 직접 호출한다
- 플레이 모드에서만 호출 가능하다. 플레이 중이 아니면 `409 Button invoke unavailable`을 반환한다
- 좌표 클릭/submit이 반응하지 않는 UI 흐름 검증용으로 사용한다

#### 로비 → 게임 씬 (버튼 invoke 자동화)

`RoomListView`의 방 생성은 `LobbyRule` 검증을 거친다. **방 이름 입력이 비어 있거나 2글자 미만이면 Create Room이 실패**하므로, MCP로 버튼만 누르기 전에 `JG_LobbyScene`의 `TMP_InputField` 기본 텍스트(방 이름·표시명·정원)가 채워져 있어야 한다.

권장 순서:

1. `POST /scene/open` — `Assets/Scenes/JG_LobbyScene.unity`
2. `POST /play/start`
3. `/health`로 플레이 모드 전환·첫 프레임까지 대기: `isPlaying == true`, `isPlayModeChanging == false`, `isCompiling == false`를 확인한다. (`tools/mcp-test-common.ps1`의 `Wait-McpPlayModeReady`, 스크립트 기본 타임아웃 `120s`)
4. `/health`로 활성 씬이 `JG_LobbyScene`인지 확인한다. (`Wait-McpSceneActive`)
5. 고정 대기 후 `POST /ui/button/invoke` — `path`: `/UIRoot/Canvas/lobby/RoomListView/Header/CreateRoomButton`
6. `GET /console/logs`, `GET /console/errors`로 Create Room 직후 상태를 본다. 필요하면 `POST /screenshot/capture`
7. `POST /ui/button/invoke` — `path`: `/UIRoot/Canvas/lobby/RoomDetailPanel/ReadyButton`
8. 다시 `GET /console/logs`, `GET /console/errors`
9. `POST /ui/button/invoke` — `path`: `/UIRoot/Canvas/lobby/RoomDetailPanel/StartGameButton` (마스터만 유효)
10. `PhotonNetwork.LoadLevel` 후 `/health`의 `activeScene`이 `JG_GameScene`으로 바뀌는지 확인하고, 필요하면 스크린샷을 남긴다

자주 쓰는 UI 경로는 `tools/mcp-test-common.ps1`의 `Get-McpUiPathSpec`이 SSOT다. 예를 들면:

- 로비 Create Room: `/UIRoot/Canvas/lobby/RoomListView/Header/CreateRoomButton`
- 로비 Ready: `/UIRoot/Canvas/lobby/RoomDetailPanel/ReadyButton`
- 로비 Start Game: `/UIRoot/Canvas/lobby/RoomDetailPanel/StartGameButton`
- 공용 에러 배너 루트: `/UIRoot/Canvas/ErrorBannerRoot`
- 공용 에러 모달 닫기: `/UIRoot/Canvas/ErrorModalRoot/Panel/DismissButton`
- 시작 스킬 선택 패널: `/UIRoot/StartSkillSelectionCanvas/Panel`

프로젝트 루트에서 한 번에 돌리려면 `tools/mcp-lobby-to-game.ps1` 또는 `tools/mcp-test-lobby-scene.ps1`을 사용한다. 두 스크립트 모두 `tools/mcp-test-common.ps1`을 통해 경로/폴링/콘솔 수집을 공유한다. 포트는 `ProjectSettings/UnityMcpPort.txt` 또는 `-BaseUrl`로 지정한다. 이미 플레이 중이고 활성 씬이 `JG_LobbyScene`이면 `scene/open`·`play/start`는 건너뛴다(에디터는 플레이 모드에서 씬 열기를 허용하지 않음). 다른 씬에서 플레이 중이면 먼저 `play/stop` 후 로비를 연다. 로비 씬 이름이 `/health`에 늦게 잡히면 `-LobbySceneActiveTimeoutSec`으로 대기 상한을 늘린다.

#### 테스트 성공 기준

스크립트 기반 MCP 테스트가 "성공"으로 간주되려면 다음 기준을 모두 충족해야 한다.

| 기준 | 수치 | 비고 |
|---|---|---|
| 전체 소요 시간 | **120초 이내** | 로비 진입 → 게임 씬 진입 기준 |
| 콘솔 에러 | **0개** | `GET /console/errors` 기준. Photon dev region 경고는 제외 |
| 스크린 | **최소 3장** | 로비, 스킬선택, 게임 HUD (선택적 플래그) |
| JSON 결과 | **매 실행 시 생성** | `Temp/UnityMcp/last-*.json`, `ok: true` |
| 계층 재현 진단 | **별도 스크립트** | `tools/mcp-hierarchy-diag.ps1`로 `/scene/hierarchy` 응답 안정성 검증 |

실패 시 스크립트는 `exit 1`을 반환하며, JSON 결과 파일의 `failure` 필드에 실패 단계와 메시지가 기록된다.

### 게임오브젝트

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/gameobject/find` | `name` / `path` + 선택 `lightweight`, `componentFilter` | 오브젝트 조회 |
| POST | `/gameobject/create` | `{"name":"...", "parent":"...", "components":["..."]}` | 빈 오브젝트 생성 |
| POST | `/gameobject/create-primitive` | `{"name":"...", "primitiveType":"Sphere", "components":["..."]}` | 프리미티브 생성 |
| POST | `/gameobject/destroy` | `{"path":"/..."}` | 오브젝트 삭제 |
| POST | `/gameobject/set-active` | `{"path":"/...", "active":true}` | 활성/비활성 |

#### `/gameobject/find` Body

- `path` 또는 `name` 중 하나: `path`는 `GameObject.Find` 전체 경로, `name`은 **활성 씬**에서만 BFS로 첫 일치(비영속 씬 오브젝트)
- `lightweight` (선택): `true`이면 `SerializedObject`를 쓰지 않고 컴포넌트 타입명만 반환(`properties`는 빈 배열). 생략 시 기본은 기존과 같이 **전체 속성 포함**(Unity `JsonUtility`는 bool 생략 시 `false`이므로 opt-in 플래그로 둠)
- `componentFilter` (선택): 문자열 배열. 지정 시 **이 타입들(짧은 이름 또는 전체 이름, 대소문자 무시)만** 직렬화 속성을 채우고, 나머지 컴포넌트는 타입명만(`properties` 빈 배열). `lightweight: true`이면 `componentFilter`는 무시됨

```json
{
  "path": "/Canvas/Panel",
  "lightweight": false,
  "componentFilter": ["RectTransform", "UnityEngine.UI.Image"]
}
```

### 컴포넌트

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/component/add` | `{"gameObjectPath":"/...", "componentType":"Namespace.Class"}` | 컴포넌트 추가 |
| POST | `/component/get` | `gameObjectPath`, `componentType` + 선택 `propertyNames[]` | 컴포넌트 속성 조회 |
| POST | `/component/set` | 아래 참조 | 컴포넌트 속성 수정 |

#### `/component/get` Body

- `propertyNames` (선택): 문자열 배열. 지정 시 해당 `SerializedProperty` 이름만 반환(대소문자 구분, `m_Script`는 항상 제외). 생략 시 기존과 같이 보이는 필드 전체

#### `/component/set` Body

```json
{
  "gameObjectPath": "/GameSceneRoot",
  "componentType": "GameSceneRoot",
  "propertyName": "_someField",
  "value": "값"
}
```

**value 형식:**
- 문자열: `"hello"`
- 숫자: `"3.5"`
- bool: `"true"` / `"false"`
- 오브젝트 참조: `"/경로::컴포넌트타입"` (예: `"/GameSceneRoot::StatusSetup"`)
- 에셋 참조: `assetPath` 필드 사용 (예: `"assetPath": "Assets/Resources/MyAsset.asset"`)

### 메뉴

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/menu/execute` | `{"menuPath":"Tools/Setup Friendly Fire UI"}` | 에디터 메뉴 아이템 실행 |

### 씬/에셋

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/scene/save` | (없음) | 현재 씬 저장 |
| POST | `/asset/refresh` | (없음) | AssetDatabase 리프레시 (코드 변경 후 컴파일 트리거) |
| POST | `/prefab/save` | `{"gameObjectPath":"/...", "savePath":"Assets/...", "destroySceneObject":true}` | 씬 오브젝트를 프리팹으로 저장 |
| POST | `/prefab/get` | `assetPath`, `childPath` + 선택 `lightweight`, `componentFilter` (`/gameobject/find`와 동일 의미) | 프리팹 컴포넌트/속성 조회 |
| POST | `/prefab/set` | 아래 참조 | 프리팹 속성 수정 |
| POST | `/prefab/add-component` | `{"assetPath":"Assets/...", "childPath":"...", "componentType":"..."}` | 프리팹에 컴포넌트 추가 |

#### `/prefab/set` Body

```json
{
  "assetPath": "Assets/Resources/ProjectilePhysicsAdapter.prefab",
  "childPath": "",
  "componentType": "ProjectileView",
  "propertyName": "_lifetimeRelease",
  "value": "",
  "assetReferencePath": "",
  "autoWireType": "LifetimeRelease"
}
```

**필드 설명:**
- `assetPath`: 프리팹 에셋 경로 (필수)
- `childPath`: 프리팹 내 자식 경로 (빈 문자열이면 루트). `Transform.Find` 형식 (예: `"Body/Head"`)
- `autoWireType`: 설정 시 프리팹 하이어라키에서 해당 타입 컴포넌트를 찾아 자동 연결. `value`/`assetReferencePath`보다 우선
- `assetReferencePath`: 외부 에셋 참조 (예: `"Assets/Materials/Red.mat"`)
- `value`: 일반 값 또는 씬 오브젝트 참조 (`"/경로::컴포넌트타입"`)

### 빌드

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/build/webgl` | `{"outputPath":"...", "fastBuild":true}` | WebGL 빌드 |

#### `/build/webgl` Body

```json
{
  "outputPath": "Build/WebGL",
  "fastBuild": true
}
```

- `outputPath`: 빌드 출력 경로 (기본값 `Build/WebGL`)
- `fastBuild`: `true`이면 QA용 빠른 빌드 — Development 모드 + Gzip 압축 (Brotli 대신). 빌드 후 압축 설정은 자동 복원

### 빌드 + 배포 통합 스크립트

`tools/build-and-deploy-webgl.ps1` — MCP 빌드 호출 후 Firebase Hosting 배포까지 한 번에 실행한다.

```powershell
# 빠른 빌드 + QA 프리뷰 채널 배포 (기본)
.\tools\build-and-deploy-webgl.ps1 -Fast

# 빠른 빌드 + 라이브 배포
.\tools\build-and-deploy-webgl.ps1 -Fast -Live

# 릴리즈 빌드 + 라이브 배포
.\tools\build-and-deploy-webgl.ps1 -Live

# 빌드 스킵, 배포만
.\tools\build-and-deploy-webgl.ps1 -SkipBuild
```

| 플래그 | 역할 |
|---|---|
| `-Fast` | `fastBuild:true` (Development + Gzip) |
| `-Live` | `firebase deploy --only hosting` (프로덕션) |
| (기본) | `firebase hosting:channel:deploy qa` (프리뷰 채널) |
| `-SkipBuild` | 빌드 생략, 배포만 실행 |
| `-Channel` | 프리뷰 채널 이름 (기본 `qa`) |

## 주의사항

- 컴파일·도메인 리로드 구간에는 HTTP 응답이 잠깐 불안정할 수 있다 — 스크립트 수정 후에는 `POST /compile/wait`(`requestFirst:true`) 또는 `/compile/request` + `/compile/status`·`/health` 폴링으로 `isCompiling: false` 확인
- 씬 엔드포인트(`/gameobject/*`, `/component/*`)는 `GameObject.Find` 기반 — **씬 오브젝트만** 대상
- 프리팹 엔드포인트(`/prefab/get`, `/prefab/set`, `/prefab/add-component`)는 `AssetDatabase.LoadAssetAtPath` 기반 — **프리팹 에셋 직접 수정 가능**
- 스크린샷 엔드포인트는 플레이 모드가 켜져 있어야 한다. 기본 출력 경로는 `Temp/UnityMcp/Screenshots`
- 입력 엔드포인트도 플레이 모드가 켜져 있어야 한다. 좌표는 Game View 기준이다
- `/ui/button/invoke`도 플레이 모드가 켜져 있어야 하며, 씬 오브젝트 경로를 정확히 넘겨야 한다
- 입력 엔드포인트는 Game View 이벤트 기반이라, 운영체제 레벨 마우스/키보드를 직접 움직이는 방식은 아니다
- 스크롤/조합키 해석은 게임 코드와 포커스 상태에 따라 반응이 다를 수 있다
- POST 엔드포인트에 GET으로 호출하면 404 반환
