# Unity MCP Bridge

Unity 에디터 안에서 로컬 HTTP 서버를 띄워 외부 도구(Claude Code 등)가 에디터를 원격 제어할 수 있게 한다.

## 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트 확인: `ProjectSettings/UnityMcpPort.txt` (없으면 기본 51234, 충돌 시 52000대 fallback)
- 호출 방법: `powershell -Command "Invoke-RestMethod -Uri 'http://127.0.0.1:{port}/...' -Method ..."`
- `tools/unity-mcp/server.js`(MCP stdio 서버)는 Unity로 나가는 HTTP에 keep-alive를 쓰고, 기본 타임아웃은 `10000ms`다. 환경 변수 `UNITY_MCP_HTTP_TIMEOUT_MS`로 덮어쓸 수 있다.

## 작업 순서 주의

- Unity가 플레이 중일 때 C# 스크립트나 브리지 코드(`Assets/Editor/UnityMcp/**`)를 수정해도 새 코드가 즉시 컴파일/반영되지 않을 수 있다.
- 새 엔드포인트 추가, 브리지 수정, 일반 스크립트 수정이 필요하면 반드시 `Play Stop -> 파일 수정 -> 컴파일 완료 확인 -> 브리지 상태 확인 -> 다시 Play` 순서로 진행한다.

## 테스트 / 로그 확인 SOP

- Unity MCP로 씬 열기, 플레이 시작/정지, 버튼 invoke, 입력 전달, 스크린샷 캡처 같은 테스트를 수행할 때는 현재 콘솔 상태를 함께 확인한다.
- 확인 범위는 에러만이 아니다. 가능하면 액션 전후로 에러/경고/일반 로그 흐름까지 본다.
- 기본 점검 순서:
  1. `/health`로 브리지/플레이/컴파일 상태 확인
  2. 주요 액션 직후 `GET /console/logs`로 브리지가 버퍼에 쌓아 둔 최근 콘솔 전체(Log/Warning/Error 등) 확인 (`limit`으로 개수 조절)
  3. 에러만 빠르게 볼 때는 `GET /console/errors` (Error/Exception/Assert만, 최근부터 최대 `limit`개)
  4. 버퍼 이전 로그·에디터 전체 로그는 `Editor.log` 최근 구간까지 읽어 보완
- 테스트 결과를 공유할 때는 가능하면 화면 상태 + 콘솔/로그 상태를 함께 적는다.
- 통합 스모크: `tools/mcp-test-lobby-scene.ps1` — 로비 플로우 + 스크린샷·콘솔. 게임 씬 시작 스킬 선택까지 포함하려면 `-CompleteGameSceneStartSkills`를 사용하고, 관련 흐름은 `Assets/Scripts/Features/Skill/SkillSetup.cs`와 `Assets/Scripts/Features/Skill/Presentation/StartSkillSelectionView.cs`를 기준으로 본다.
- UI 버튼 경로와 공통 폴링/콘솔 헬퍼는 `tools/mcp-test-common.ps1`를 기준으로 맞춘다. 로비·게임 UI 자동화 스크립트는 이 파일을 dot-source 하므로, 경로가 바뀌면 여기부터 갱신한다.
- `GET /scene/hierarchy`가 특정 조건에서 기대와 다르게 보이면 브리지를 바로 고치지 말고 `tools/mcp-diagnose-scene-hierarchy.ps1`로 `depth`/`includeComponents`/`path` 조합을 먼저 기록한다.
- JSON 결과(스모크 판정용): 아래 스크립트는 종료 시 `schemaVersion`, `ok`, `steps[]`, `finalHealth`, `screenshots[]`, (실패 시) `failure` 를 UTF-8 JSON 파일로 쓴다. 실패 시 exit code 1.
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
| GET | `/console/logs` | 브리지가 수집한 최근 콘솔 로그 전체 (`Log`, `Warning`, `Error`, `Exception`, `Assert`). 쿼리 `limit` 기본 100, 최대 200. 에디터/도메인 리로드 이후부터 쌓인 버퍼만 해당 (그 이전은 `Editor.log`) |
| GET | `/console/errors` | 위 버퍼 중 Error / Exception / Assert 만 최근부터 최대 `limit`개 (기본 20, 최대 100) |

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

- `requestFirst`: `true`이면 먼저 `/compile/request`와 동일하게 재컴파일을 요청한 뒤 대기한다. `false`이면 이미 진행 중인 컴파일이 끝날 때만 기다린다.
- `cleanBuildCache`: `requestFirst`일 때만 의미 있음 — `RequestScriptCompilationOptions.CleanBuildCache` (전체 다시 컴파일에 가깝게)
- `timeoutMs`: 대기 상한 (밀리초). 기본 300000(5분), 허용 범위 1000~600000
- `pollIntervalMs`: 메인 스레드에서 `isCompiling`을 확인하는 간격. 기본 100, 20~2000
- 응답: `ok`, `timedOut` (`true`면 타임아웃 시점에 아직 `isCompiling`), `waitedMs`, `isCompiling`(최종), `requestedCompilation`

수동 폴링: `POST /compile/request` 후 `GET /compile/status` 또는 `GET /health`의 `isCompiling`을 반복 호출해도 된다.

HTTP 클라이언트 타임아웃: `/compile/wait`는 응답이 수 분 걸릴 수 있다. PowerShell은 예를 들어 `-TimeoutSec 400`을 주고, `tools/unity-mcp/server.js`의 `unity_compile_wait`는 본문 `timeoutMs`보다 여유 있게 HTTP 타임아웃을 잡는다.

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
