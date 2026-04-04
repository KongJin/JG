# Unity MCP Bridge

Unity 에디터 안에서 로컬 HTTP 서버를 띄워 외부 도구(Claude Code 등)가 에디터를 원격 제어할 수 있게 한다.

## 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트 확인: `ProjectSettings/UnityMcpPort.txt` (없으면 기본 51234, 충돌 시 52000대 fallback)
- 호출 방법: `powershell -Command "Invoke-RestMethod -Uri 'http://127.0.0.1:{port}/...' -Method ..."`

## 작업 순서 주의

- Unity가 플레이 중일 때 C# 스크립트나 브리지 코드(`Assets/Editor/UnityMcp/**`)를 수정해도 새 코드가 즉시 컴파일/반영되지 않을 수 있다.
- 새 엔드포인트 추가, 브리지 수정, 일반 스크립트 수정이 필요하면 반드시 `Play Stop -> 파일 수정 -> 컴파일 완료 확인 -> 브리지 상태 확인 -> 다시 Play` 순서로 진행한다.

## 엔드포인트

### 조회

| Method | Path | 설명 |
|---|---|---|
| GET | `/health` | 서버 상태 (포트, isCompiling, isPlaying 등) |
| GET | `/scene/current` | 현재 씬 정보 |
| GET | `/scene/hierarchy` | 씬 하이어라키 전체 조회 |
| GET | `/console/errors` | 콘솔 에러/경고 로그 |

### 플레이 제어

| Method | Path | 설명 |
|---|---|---|
| POST | `/scene/open` | 씬 열기 (`{"scenePath":"Assets/Scenes/..."}`) |
| POST | `/play/start` | 플레이 모드 시작 |
| POST | `/play/stop` | 플레이 모드 정지 |
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
  "path": "/Canvas/LobbyPanel/CreateRoomButton"
}
```

- `path`: 씬 하이어라키 기준 GameObject 경로. 대상 오브젝트에 `UnityEngine.UI.Button` 컴포넌트가 있어야 한다
- 이 엔드포인트는 Game View 좌표 클릭을 우회하고 `Button.onClick.Invoke()`를 직접 호출한다
- 플레이 모드에서만 호출 가능하다. 플레이 중이 아니면 `409 Button invoke unavailable`을 반환한다
- 좌표 클릭/submit이 반응하지 않는 UI 흐름 검증용으로 사용한다

### 게임오브젝트

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/gameobject/find` | `{"name":"..."}` 또는 `{"path":"..."}` | 오브젝트 조회 (컴포넌트/속성 포함) |
| POST | `/gameobject/create` | `{"name":"...", "parent":"...", "components":["..."]}` | 빈 오브젝트 생성 |
| POST | `/gameobject/create-primitive` | `{"name":"...", "primitiveType":"Sphere", "components":["..."]}` | 프리미티브 생성 |
| POST | `/gameobject/destroy` | `{"path":"/..."}` | 오브젝트 삭제 |
| POST | `/gameobject/set-active` | `{"path":"/...", "active":true}` | 활성/비활성 |

### 컴포넌트

| Method | Path | Body | 설명 |
|---|---|---|---|
| POST | `/component/add` | `{"gameObjectPath":"/...", "componentType":"Namespace.Class"}` | 컴포넌트 추가 |
| POST | `/component/get` | `{"gameObjectPath":"/...", "componentType":"Class"}` | 컴포넌트 속성 조회 |
| POST | `/component/set` | 아래 참조 | 컴포넌트 속성 수정 |

#### `/component/set` Body

```json
{
  "gameObjectPath": "/GameSceneBootstrap",
  "componentType": "GameSceneBootstrap",
  "propertyName": "_someField",
  "value": "값"
}
```

**value 형식:**
- 문자열: `"hello"`
- 숫자: `"3.5"`
- bool: `"true"` / `"false"`
- 오브젝트 참조: `"/경로::컴포넌트타입"` (예: `"/GameSceneBootstrap::StatusSetup"`)
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
| POST | `/prefab/get` | `{"assetPath":"Assets/...", "childPath":"..."}` | 프리팹 컴포넌트/속성 조회 |
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

- 컴파일 중에는 서버가 내려간다 — `/asset/refresh` 후 `/health`로 `isCompiling: false` 확인 필요
- 씬 엔드포인트(`/gameobject/*`, `/component/*`)는 `GameObject.Find` 기반 — **씬 오브젝트만** 대상
- 프리팹 엔드포인트(`/prefab/get`, `/prefab/set`, `/prefab/add-component`)는 `AssetDatabase.LoadAssetAtPath` 기반 — **프리팹 에셋 직접 수정 가능**
- 스크린샷 엔드포인트는 플레이 모드가 켜져 있어야 한다. 기본 출력 경로는 `Temp/UnityMcp/Screenshots`
- 입력 엔드포인트도 플레이 모드가 켜져 있어야 한다. 좌표는 Game View 기준이다
- `/ui/button/invoke`도 플레이 모드가 켜져 있어야 하며, 씬 오브젝트 경로를 정확히 넘겨야 한다
- 입력 엔드포인트는 Game View 이벤트 기반이라, 운영체제 레벨 마우스/키보드를 직접 움직이는 방식은 아니다
- 스크롤/조합키 해석은 게임 코드와 포커스 상태에 따라 반응이 다를 수 있다
- POST 엔드포인트에 GET으로 호출하면 404 반환
