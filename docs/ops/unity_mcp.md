# Unity MCP Bridge

Unity 에디터 안에서 로컬 HTTP 서버를 띄워 외부 도구(Codex, Claude Code 등)가 에디터를 원격 제어할 수 있게 한다. 현재 브리지 라우트는 `Assets/Editor/UnityMcp/UnityMcpBridge.cs` 기준 **36개**다.

## 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트 확인: `ProjectSettings/UnityMcpPort.txt` (없으면 기본 `51234`, 충돌 시 `52000`대 fallback)
- 호출 예: `powershell -Command "Invoke-RestMethod -Uri 'http://127.0.0.1:{port}/health'"`
- MCP stdio 서버: `tools/unity-mcp/server.js`
- 기본 HTTP 타임아웃: `10000ms` (`UNITY_MCP_HTTP_TIMEOUT_MS`로 덮어쓰기 가능)

## 작업 순서 주의

- 브리지나 일반 C# 스크립트를 수정할 때는 `Play Stop -> 파일 수정 -> 컴파일 완료 확인 -> /health 확인 -> 다시 Play` 순서를 지킨다.
- Unity MCP 테스트는 화면만 보지 말고 `/console/logs` 또는 `/console/errors`를 함께 확인한다.
- 씬/하이어라키 문제를 의심할 때는 브리지 수정 전에 `tools/mcp-diagnose-scene-hierarchy.ps1`로 재현 정보를 남긴다.
- MCP 작업 마무리 단계에서는 **최종 작업 위치/화면 상태를 유지한 채** `/screenshot/capture`로 캡처를 남기고, 캡처 이미지를 직접 확인한 뒤 종료한다.
- runtime UI flow 회귀는 고정 스크립트로 운영하지 않는다. 공식 기록은 `docs/playtest/runtime_validation_checklist.md`에 남기고, MCP 입력 라우트는 일회성 수동 진단에만 쓴다.

## 테스트 / 로그 SOP

1. `GET /health`로 브리지, 플레이, 컴파일 상태를 확인한다.
2. 주요 액션 뒤 `GET /console/logs?limit=...`로 로그 흐름을 확인한다.
3. 에러만 빠르게 볼 때는 `GET /console/errors?limit=...`를 쓴다.
4. compile/status 확인만 필요하면 `tools/mcp-test-compile.ps1`를 쓴다.
5. hierarchy 재현이 필요하면 `tools/mcp-diagnose-scene-hierarchy.ps1`, `tools/mcp-hierarchy-diag.ps1` 같은 진단 스크립트를 일회성으로 실행한다.
6. runtime flow 회귀 확인은 `docs/playtest/runtime_validation_checklist.md`에 수동으로 기록한다.
7. 작업 종료 직전에는 현재 작업 위치에서 `/screenshot/capture`를 호출하고, 저장된 이미지를 열어 실제 결과가 의도와 맞는지 확인한다.
8. 필요하면 `Editor.log` 최근 구간과 함께 본다.

## 엔드포인트

### 조회 `GET` (6)

| Path | 설명 |
|---|---|
| `/health` | 포트, 프로젝트 키, `isCompiling`, `isPlaying`, `isPlayModeChanging`, active scene 등 브리지 상태 |
| `/scene/current` | 현재 활성 씬 정보 |
| `/console/errors` | 최근 Error / Exception / Assert 로그 |
| `/console/logs` | 최근 Log / Warning / Error / Exception / Assert 로그 |
| `/scene/hierarchy` | 하이어라키 또는 특정 서브트리 조회 |
| `/compile/status` | `isCompiling` 빠른 조회 |

`/scene/hierarchy` 쿼리:

- `depth`: 1~50, 기본 `10`
- `path`: `GameObject.Find` 경로. 지정 시 해당 루트만 조회
- `includeComponents`: `false` 또는 `0`이면 컴포넌트 목록 생략

### 플레이 / 컴파일 / 빌드 `POST` (8)

| Path | 설명 |
|---|---|
| `/scene/open` | 씬 열기 |
| `/scene/save` | 현재 활성 씬 저장 |
| `/play/start` | 플레이 모드 시작 |
| `/play/stop` | 플레이 모드 정지 |
| `/screenshot/capture` | Game View 스크린샷 저장 |
| `/compile/request` | 스크립트 재컴파일 요청 |
| `/compile/wait` | 컴파일 완료까지 장기 대기 |
| `/build/webgl` | WebGL 빌드 실행 |

### 입력 / UI `POST` (8)

| Path | 설명 |
|---|---|
| `/input/click` | Game View 마우스 클릭 |
| `/input/move` | Game View 마우스 이동 |
| `/input/drag` | Game View 드래그 |
| `/input/key` | 키 입력 |
| `/input/text` | 텍스트 입력 |
| `/input/scroll` | 마우스 휠 입력 |
| `/input/key-combo` | 자주 쓰는 키 조합 프리셋 |
| `/ui/button/invoke` | 씬 경로 기준 `Button.onClick` 직접 invoke |

### 씬 / 오브젝트 / 프리팹 편집 `POST` (13)

| Path | 설명 |
|---|---|
| `/gameobject/find` | GameObject 찾기 |
| `/gameobject/create` | 빈 GameObject 생성 |
| `/gameobject/create-primitive` | primitive 생성 |
| `/gameobject/destroy` | GameObject 삭제 |
| `/gameobject/set-active` | active 상태 변경 |
| `/component/add` | 컴포넌트 추가 |
| `/component/set` | 컴포넌트 필드 설정 |
| `/component/get` | 컴포넌트 필드 조회 |
| `/prefab/save` | 씬 오브젝트를 프리팹으로 저장 |
| `/prefab/get` | 프리팹 정보 조회 |
| `/prefab/set` | 프리팹 필드 설정 |
| `/prefab/add-component` | 프리팹에 컴포넌트 추가 |
| `/asset/refresh` | `AssetDatabase.Refresh()` 실행 |

### 에디터 유틸리티 `POST` (1)

| Path | 설명 |
|---|---|
| `/menu/execute` | Unity 메뉴 명령 실행 |

## 주요 요청 Body

### `/scene/open`

```json
{
  "scenePath": "Assets/Scenes/ExampleScene.unity",
  "saveCurrentSceneIfDirty": true
}
```

- `scenePath`: 열 씬 경로
- `saveCurrentSceneIfDirty`: `true`면 dirty 씬을 먼저 저장

### `/screenshot/capture`

```json
{
  "outputPath": "Temp/UnityMcp/Screenshots/runtime-check.png",
  "superSize": 1,
  "overwrite": true
}
```

- `outputPath`: 프로젝트 루트 기준 상대 경로
- `superSize`: `ScreenCapture.CaptureScreenshot` 배율
- `overwrite`: 같은 파일 덮어쓰기 여부

### `/compile/wait`

```json
{
  "requestFirst": true,
  "cleanBuildCache": false,
  "timeoutMs": 300000,
  "pollIntervalMs": 100
}
```

- `requestFirst`: 먼저 재컴파일 요청 후 대기할지 여부
- `cleanBuildCache`: `requestFirst`일 때만 의미 있음
- `timeoutMs`: 1000~600000, 기본 `300000`
- `pollIntervalMs`: 20~2000, 기본 `100`

### `/input/click`

```json
{
  "x": 0.5,
  "y": 0.5,
  "normalized": true,
  "button": 0,
  "clickCount": 1
}
```

- `normalized: true`면 `x`, `y`는 `0~1` 비율 좌표
- `button`: `0` 왼쪽, `1` 오른쪽, `2` 가운데

### `/input/drag`

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

- `steps` 기본값은 `12`
- 호출 순서는 `MouseMove -> MouseDown -> MouseDrag... -> MouseUp`

### `/input/key`

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

- `phase`: `press`, `down`, `up`
- `character`를 함께 보내면 문자 입력도 전달할 수 있다

### `/input/text`

```json
{
  "text": "hello world",
  "appendReturn": true
}
```

- `appendReturn: true`면 마지막에 Enter를 한 번 더 보낸다

## 참고

- 입력/UI 라우트는 브리지 기능 목록으로만 문서화한다. 특정 씬 이름, hierarchy path, 버튼 path를 고정한 자동 smoke 운영은 현재 정책 범위 밖이다.
- `tools/unity-mcp/server.js`는 런타임에 `ProjectSettings/UnityMcpPort.txt`를 읽어 현재 포트를 따라간다.
