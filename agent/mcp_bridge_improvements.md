# Unity MCP 브리지 개선 계획

> 생성일: 2026-04-12
> 마지막 갱신: 2026-04-12

이 문서는 Unity MCP 브리지(`UnityMcpBridge.cs`)의 기능 부족으로 인한 작업 불편함과 그 해결 방안을 기록한다.

---

## 진행 상황

| 항목 | 상태 | 완료일 |
|---|---|---|
| Inspector 필드 자동 연결 (`/component/set-serialized-field`) | ✅ 완료 | 2026-04-12 |
| 필드 자동 탐색 연결 (`/component/auto-connect-fields`) | ✅ 완료 | 2026-04-12 |
| UI 레이아웃 프리셋 (생성 시 anchor/pivot 자동 설정) | ✅ 완료 | 2026-04-12 |
| Play Mode 중 변경 큐잉 | 🟨 보류 | - |
| MCP 응답 디버깅 개선 | 🟨 보류 | - |
| 자동 씬 저장 옵션 | 🟨 보류 | - |
| 동적 엔드포인트 등록 시스템 | 🟨 보류 | - |

---

## 완료된 기능

### 1. Inspector 필드 자동 연결

**엔드포인트:** `POST /component/set-serialized-field`

**요청 형식:**
```json
{
  "componentPath": "/Canvas/GaragePageRoot",
  "componentTypeName": "GaragePageController",
  "fieldName": "_unitPreviewView",
  "targetPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport"
}
```

**응답 형식:**
```json
{
  "success": true,
  "message": "Set _unitPreviewView on GaragePageController",
  "componentPath": "/Canvas/GaragePageRoot",
  "fieldName": "_unitPreviewView",
  "targetPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport"
}
```

**사용 예시:**
```bash
# GaragePageController._unitPreviewView 연결
curl -X POST http://127.0.0.1:52676/component/set-serialized-field \
  -H "Content-Type: application/json" \
  -d '{
    "componentPath": "/Canvas/GaragePageRoot",
    "componentTypeName": "GaragePageController",
    "fieldName": "_unitPreviewView",
    "targetPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport"
  }'

# GarageResultPanelView._saveButton 연결
curl -X POST http://127.0.0.1:52676/component/set-serialized-field \
  -H "Content-Type: application/json" \
  -d '{
    "componentPath": "/Canvas/GaragePageRoot/ResultPane",
    "componentTypeName": "GarageResultPanelView",
    "fieldName": "_saveButton",
    "targetPath": "/Canvas/GaragePageRoot/ResultPane/SaveButton"
  }'
```

**구현 세부사항:**
- `SerializedObject` API를 사용하여 필드 값 설정
- 컴포넌트 타입 이름으로 부분 매칭 지원 (예: `GaragePageController` 또는 `Features.Garage.Presentation.GaragePageController`)
- `Undo.RecordObject`로 되돌리기 지원
- `EditorSceneManager.MarkSceneDirty`로 씬 변경 표시

---

### 2. 필드 자동 탐색 연결

**엔드포인트:** `POST /component/auto-connect-fields`

**요청 형식:**
```json
{
  "componentPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport",
  "componentTypeName": "GarageUnitPreviewView",
  "searchScope": "children"
}
```

**searchScope 옵션:**
| 값 | 설명 |
|---|---|
| `children` | 현재 GameObject의 자식에서 탐색 (기본값) |
| `scene` | 현재 씬의 모든 GameObject에서 탐색 |
| `path:/경로` | 특정 경로에서 탐색 |

**응답 형식:**
```json
{
  "success": true,
  "message": "Auto-connected 2 fields on GarageUnitPreviewView",
  "componentPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport",
  "connectedCount": 2,
  "connectedFields": ["_previewCamera", "_rawImage"]
}
```

**구현 세부사항:**
- `SerializedProperty.GetIterator()`로 모든 직렬화 필드 순회
- 필드명과 GameObject 이름이 일치하면 자동 연결
- `m_` 접두어 필드(Unity 내부 필드)는 제외
- `ObjectReference` 타입이고 값이 null인 필드만 연결

---

### 3. UI 레이아웃 프리셋

**기능:** `POST /gameobject/create` 시 `uiPreset` 파라미터로 레이아웃 자동 설정

**요청 형식:**
```json
{
  "name": "SaveButton",
  "parent": "/Canvas/ResultPane",
  "components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"],
  "uiPreset": "button-bottom-center",
  "width": 160,
  "height": 30
}
```

**사용 가능한 프리셋:**

| 프리셋명 | Anchor Min/Max | Pivot | 용도 |
|---|---|---|---|
| `button-bottom-center` | (0.5, 0) | (0.5, 0) | 하단 중앙 버튼 |
| `button-top-center` | (0.5, 1) | (0.5, 1) | 상단 중앙 버튼 |
| `panel-top-center` | (0.5, 1) | (0.5, 1) | 상단 중앙 패널 |
| `panel-bottom-center` | (0.5, 0) | (0.5, 0) | 하단 중앙 패널 |
| `stretch-parent` | (0, 0)-(1, 1) | (0.5, 0.5) | 부모 전체 채우기 |
| `center-fixed` | (0.5, 0.5) | (0.5, 0.5) | 중앙 고정 크기 |
| `raw-image-top` | (0.5, 1) | (0.5, 1) | 상단 RawImage (3D 뷰포트용) |

**사용 예시:**
```bash
# 하단 중앙 버튼 생성
curl -X POST http://127.0.0.1:52676/gameobject/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "SaveButton",
    "parent": "/Canvas/GaragePageRoot/ResultPane",
    "components": ["UnityEngine.UI.Button", "UnityEngine.UI.Image"],
    "uiPreset": "button-bottom-center",
    "width": 160,
    "height": 30
  }'

# 상단 RawImage 생성 (3D 뷰포트용)
curl -X POST http://127.0.0.1:52676/gameobject/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "PreviewRawImage",
    "parent": "/Canvas/GaragePageRoot/ResultPane",
    "components": ["UnityEngine.UI.RawImage"],
    "uiPreset": "raw-image-top",
    "width": 256,
    "height": 256
  }'
```

**구현 세부사항:**
- `CreateRequest`에 `uiPreset`, `width`, `height` 필드 추가
- `ApplyUiPreset()` 메서드에서 프리셋별 RectTransform 설정
- 기본값: width=160, height=30 (raw-image-top은 256x256)
- 지정되지 않은 프리셋은 `center-fixed`로 폴백

---

## 문제점 및 해결 방안

Garage UI/UX 개선 작업 중 MCP를 통해 Unity Editor에 GameObject를 생성하고 레이아웃을 조정하는 과정에서 여러 불편함이 발견되었다. 현재 MCP 브리지는 기본적인 GameObject 생성/조회 기능만 제공하며, UI 작업에 필요한 고급 기능이 부족하다.

---

## 문제점 및 해결 방안

### 문제 1: UI GameObject 생성 시 RectTransform이 기본값

**현상:**
- `POST /gameobject/create`로 UI 요소를 만들면 `RectTransform`이 (0,0) 중앙에 고정
- Anchor, Pivot, Size가 모두 기본값 → 요소들이 겹침
- 생성 후 별도로 `RectTransform`을 조정해야 함

**해결 방안:**
MCP에 **UI 생성 시 레이아웃 프리셋** 파라미터 추가:
```json
{
  "name": "SaveButton",
  "parent": "/Canvas/ResultPane",
  "uiPreset": "button-bottom-center",
  "width": 160,
  "height": 30
}
```

프리셋 옵션:
| 프리셋명 | Anchor Min | Anchor Max | Pivot | 용도 |
|---|---|---|---|---|
| `button-bottom-center` | (0.5, 0) | (0.5, 0) | (0.5, 0) | 하단 중앙 버튼 |
| `panel-top-center` | (0.5, 1) | (0.5, 1) | (0.5, 1) | 상단 중앙 패널 |
| `stretch-parent` | (0, 0) | (1, 1) | (0.5, 0.5) | 부모 전체 채우기 |
| `center-fixed` | (0.5, 0.5) | (0.5, 0.5) | (0.5, 0.5) | 중앙 고정 크기 |

**구현 위치:**
- `UnityMcpBridge.cs` → `HandleGameObjectCreateAsync` 수정
- `CreateRequest`에 `uiPreset`, `width`, `height` 필드 추가

---

### 문제 2: Play Mode 중에는 씬 수정 불가

**현상:**
- `POST /ui/set-rect` 호출 시 `"This cannot be used during play mode."` 에러
- Play Mode에서 레이아웃 조정 시도 → 실패
- 매번 Play Mode를 중지하고 다시 시작해야 함

**해결 방안:**
두 가지 옵션:

**옵션 A: Play Mode 감지 시 에디터 모드 전환 후 실행**
- MCP가 Play Mode 감지 시 자동 Stop → 수정 → 재시작
- 단점: 상태 초기화 발생

**옵션 B: 변경 사항 큐잉**
- Play Mode 중 변경 요청을 큐에 저장
- Play Mode 종료 시 자동 적용
- 응답: `{"queued": true, "message": "Applied after play mode stops"}`

**권장:** 옵션 B (큐잉 시스템)

**구현 위치:**
- `UnityMcpBridge.cs`에 정적 큐 `ConcurrentQueue<PendingSceneChange>` 추가
- `EditorApplication.playModeStateChanged`에서 큐 처리

---

### 문제 3: Inspector 필드 자동 연결 불가 (가장 중요)

**현상:**
- 새 컴포넌트 추가 후 Inspector의 `[SerializeField]` 필드를 연결하려면 수동으로 드래그해야 함
- MCP로 **직렬화 필드 연결** 기능 없음
- Garage UI 개선에서 `GaragePageController`, `GarageUnitPreviewView` 등의 필드 연결이 불가능

**해결 방안:**
MCP에 **SerializeField 자동 연결** 엔드포인트 추가:

```
POST /component/set-serialized-field
{
  "componentPath": "/Canvas/GaragePageRoot/GarageSetup/GaragePageController",
  "fieldName": "_unitPreviewView",
  "targetPath": "/Canvas/GaragePageRoot/ResultPane/UnitPreviewViewport"
}
```

**고급 기능: 자동 탐색 연결**
```
POST /component/auto-connect-fields
{
  "componentPath": "/Canvas/GaragePageRoot/GarageSetup/GaragePageController",
  "searchScope": "children" // "children" | "scene" | "path:..."
}
```
- 필드명과 GameObject 이름이 일치하면 자동 연결
- 타입이 호환되는지 검증

**구현 위치:**
- `UnityMcpBridge.cs` → `HandleComponentSetSerializedFieldAsync`
- `SerializedObject` API 사용하여 필드 값 설정

---

### 문제 4: GarageUnitPreviewView의 Prefab/카메라 연결 불가

**현상:**
- `GarageUnitPreviewView`에 `_previewCamera`, `_rawImage`, `_framePrefab` 등 연결 필요
- 수동 연결 필요

**해결 방안:**
**옵션 A: MCP 자동 연결** (문제 3 해결 시 동시 해결)

**옵션 B: 런타임 자동 탐색** (코드 수정)
```csharp
public void Initialize()
{
    // 하위 GameObject에서 자동 탐색
    if (_previewCamera == null)
        _previewCamera = GetComponentInChildren<Camera>();
    
    if (_rawImage == null)
        _rawImage = GetComponentInChildren<RawImage>();
    
    // 기본 프리팹 자동 생성
    if (_framePrefab == null)
        _framePrefab = CreateDefaultFramePrefab();
}
```

**권장:** 옵션 A (MCP 개선) + 옵션 B (폴백)

---

### 문제 5: MCP 엔드포인트 추가 시 코드 수정 필요

**현상:**
- 새로운 기능(`ui/create-button`, `ui/set-rect`)을 추가할 때마다 `UnityMcpBridge.cs` 수정
- 컴파일 → Unity 재시작 필요

**해결 방안:**
**동적 엔드포인트 등록 시스템** 도입:
```csharp
// McpEndpointRegistry.cs
public static class McpEndpointRegistry
{
    private static readonly Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> Endpoints = new();
    
    public static void Register(string path, Func<...> handler) { ... }
    public static bool TryGetHandler(string path, out Func<...> handler) { ... }
}
```

또는 **JSON 기반 엔드포인트 정의**:
```json
{
  "endpoints": [
    {
      "path": "/ui/create-button",
      "handler": "McpUiHandlers.CreateButton",
      "method": "POST"
    }
  ]
}
```

**구현 위치:**
- 신규 파일: `McpEndpointRegistry.cs`
- `UnityMcpBridge.cs`의 라우팅 로직 수정

---

### 문제 6: 씬 저장/씬 전환 후 변경사항 유실

**현상:**
- MCP로 GameObject 생성/수정 후 씬 저장 안 하면 변경사항 손실
- Play Mode 시작 시 자동 저장되지 않음

**해결 방안:**
MCP 작업 후 **자동 씬 저장** 옵션 추가:
```json
{
  "name": "SaveButton",
  "parent": "...",
  "autoSave": true
}
```

또는 작업 완료 시 **사용자에게 저장 확인** 요청:
```json
{
  "success": true,
  "message": "Created SaveButton",
  "sceneDirty": true,
  "saveHint": "Call POST /scene/save to persist changes"
}
```

**구현 위치:**
- `UnityMcpBridge.cs`의 각 핸들러에 `EditorSceneManager.SaveScene()` 추가

---

### 문제 7: MCP 응답 디버깅 어려움

**현상:**
- MCP 호출 실패 시 `"Bridge failure"`만 반환
- 실제 에러 메시지, 스택 트레이스 확인 불가

**해결 방안:**
MCP 응답에 **상세 에러 정보** 추가:
```json
{
  "error": "Bridge failure",
  "detail": "Parent not found: /Canvas/ResultPane",
  "stackTrace": "at UnityMcpBridge.HandleGameObjectCreateAsync...\n...",
  "hint": "Check if the parent path is correct. Use GET /scene/hierarchy to list objects."
}
```

**구현 위치:**
- `UnityMcpBridge.cs`의 예외 처리 로직 개선
- 개발 모드에서 스택 트레이스 포함

---

## 구현 우선순위

| 순위 | 항목 | 예상 공수 | 의존성 |
|---|---|---|---|
| 1 | Inspector 필드 자동 연결 (`/component/set-serialized-field`) | 3-4시간 | 없음 |
| 2 | UI 레이아웃 프리셋 (생성 시 anchor/pivot 자동 설정) | 2-3시간 | 없음 |
| 3 | 런타임 자동 탐색 (GarageUnitPreviewView 등) | 1-2시간 | 없음 |
| 4 | Play Mode 중 변경 큐잉 | 2-3시간 | 없음 |
| 5 | MCP 응답 디버깅 개선 | 1시간 | 없음 |
| 6 | 자동 씬 저장 옵션 | 1시간 | 없음 |
| 7 | 동적 엔드포인트 등록 시스템 | 4-6시간 | 없음 |

---

## Assumptions

- MCP 브리지 개선은 현재 Garage UI/UX 개선 작업과 병행한다.
- 기존 엔드포인트의 동작은 변경하지 않는다 (하위 호환성 유지).
- 새 엔드포인트는 기존 라우팅 구조에 추가한다.
- Play Mode 중 씬 수정 제한은 Unity Editor의 기본 동작이므로, 큐잉 방식으로 우회한다.
