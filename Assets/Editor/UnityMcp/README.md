# Unity MCP Bridge

Unity 에디터 안에서 로컬 HTTP 서버를 띄워 외부 도구(Claude Code 등)가 에디터를 원격 제어할 수 있게 한다.

## 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트 확인: `ProjectSettings/UnityMcpPort.txt` (없으면 기본 51234, 충돌 시 52000대 fallback)
- 호출 방법: `powershell -Command "Invoke-RestMethod -Uri 'http://127.0.0.1:{port}/...' -Method ..."`

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
| POST | `/play/start` | 플레이 모드 시작 |
| POST | `/play/stop` | 플레이 모드 정지 |

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

## 주의사항

- 컴파일 중에는 서버가 내려간다 — `/asset/refresh` 후 `/health`로 `isCompiling: false` 확인 필요
- `GameObject.Find` 기반이므로 **씬 오브젝트만** 대상 — 프리팹 직접 수정 불가
- 프리팹 수정이 필요하면 에디터 스크립트(`[InitializeOnLoad]` 또는 `[MenuItem]`)를 작성
- POST 엔드포인트에 GET으로 호출하면 404 반환
