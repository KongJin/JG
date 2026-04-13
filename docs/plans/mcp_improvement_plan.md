# MCP Bridge 개선 계획 (v2)

> 생성일: 2026-04-14
> 상태: 계획 작성 완료 (실행 대기)

이 문서는 Unity MCP 브리지의 4가지 개선 사항을 기록한다.

---

## 진행 상황

| 항목 | 상태 | 완료일 |
|---|---|---|
| Play Mode 중 변경 큐잉 | 🟨 계획 완료 | - |
| MCP 응답 디버깅 개선 | 🟨 계획 완료 | - |
| 자동 씬 저장 옵션 | 🟨 계획 완료 | - |
| 동적 엔드포인트 등록 시스템 | ✅ 이미 충분 | - |

---

## 1. Play Mode 중 변경 큐잉 (`SceneChangeQueue.cs` 신규)

### 문제
Play Mode 중에는 Unity가 씬 수정을 허용하지 않아 MCP 요청이 실패함
(`"This cannot be used during play mode."`)

### 해결 방안
`SceneChangeQueue.cs` 신규 생성 — Play Mode 중 변경 요청을 큐에 저장했다가 Play Mode 종료 시 자동 적용

### 구현 상세

**파일:** `Assets/Editor/UnityMcp/SceneChangeQueue.cs` (신규)

```csharp
// 핵심 API
public static async Task<bool> EnqueueOrExecuteAsync(Func<bool> execute, string description)
```

- Play Mode이 아니면 → 즉시 실행, `true` 반환
- Play Mode 중이면 → 큐에 저장, `false` 반환, 디버그 로그 출력

**큐 처리 트리거:**
```csharp
EditorApplication.playModeStateChanged
  → PlayModeStateChange.EnteredEditMode 감지 시 ProcessQueueAsync() 호출
```

**큐 처리 흐름:**
1. 대기 중인 변경사항을 복사 후 큐 클리어
2. 각 변경사항 순차 실행
3. 성공 시 `EditorSceneManager.MarkSceneDirty(activeScene)`
4. 결과 로깅 (성공/실패 카운트)

### 응답 모델 확장

**기존 응답에 `queued` 필드 추가:**
```csharp
[Serializable]
internal sealed class QueuedResponse
{
    public bool success;
    public string message;
    public bool queued;          // 큐에 저장됨 (Play Mode 중)
    public int pendingCount;     // 현재 큐에 쌓인 변경 요청 수
}
```

### 적용 대상 핸들러 (씬 수정)

| 핸들러 파일 | 대상 엔드포인트 |
|---|---|
| `GameObjectHandlers.cs` | `/gameobject/create`, `/gameobject/destroy`, `/gameobject/set-active`, `/gameobject/set-sibling` |
| `UiHandlers.cs` | `/ui/set-rect`, `/ui/create-button`, `/ui/create-panel`, `/ui/create-raw-image` |
| `ComponentHandlers.cs` | `/component/add`, `/component/set`, `/component/set-serialized-field`, `/component/auto-connect-fields` |

### 적용 대상 아님 (읽기 전용)
- `/gameobject/find`
- `/component/get`
- `/scene/hierarchy`
- 기타 GET 요청

---

## 2. MCP 응답 디버깅 개선

### 문제
에러 시 `"Bridge failure"`만 반환, 실제 원인/스택 트레이스 확인 불가

### 해결 방안
`ErrorResponse` 모델에 `stackTrace`, `hint` 필드 추가

### 구현 상세

**`Models.cs` 수정:**
```csharp
[Serializable]
internal sealed class ErrorResponse
{
    public string error;         // 기존: 에러 타입 문자열
    public string detail;        // 기존: 상세 메시지
    public string stackTrace;    // 신규: 스택 트레이스 (Editor에서만)
    public string hint;          // 신규: 해결 힌트
}
```

**`UnityMcpBridge.cs` 수정:**
- `HandleRequestAsync()` catch 블록 개선:
  ```csharp
  catch (Exception ex)
  {
      Debug.LogError("[Unity MCP] Unhandled exception: " + ex);
      await WriteJsonAsync(response, 500, new ErrorResponse
      {
          error = "Bridge failure",
          detail = ex.Message,
          stackTrace = ex.ToString(),  // 전체 스택 트레이스
          hint = GetErrorHint(ex)      // 예외 타입별 힌트
      });
      return 500;
  }
  ```

**`GetErrorHint()` 헬퍼:**
| 예외 타입 | hint |
|---|---|
| `Exception` (message에 "not found" 포함) | "Check if the path/name is correct. Use GET /scene/hierarchy to list objects." |
| `Exception` (message에 "parent" 포함) | "Check if the parent path exists." |
| `NullReferenceException` | "A required reference is missing. Check the component or GameObject." |
| `TimeoutException` | "The operation took too long. Unity may be compiling or in play mode." |
| 기본 | "See stackTrace for details." |

**404 응답 개선:**
```csharp
// 기존
new ErrorResponse { error = "Not found", detail = method + " " + path }

// 개선 후
new ErrorResponse {
    error = "Not found",
    detail = method + " " + path,
    hint = "Use GET /debug/endpoints to list available endpoints."
}
```

---

## 3. 자동 씬 저장 옵션

### 문제
MCP로 GameObject 생성/수정 후 씬 저장 안 하면 변경사항 손실

### 해결 방안
요청 모델에 `autoSave` 필드 추가, 공통 헬퍼로 중복 제거

### 구현 상세

**`Models.cs` 수정 — 관련 요청 모델에 `autoSave` 필드 추가:**
```csharp
[Serializable] internal sealed class CreateRequest { ... public bool autoSave; }
[Serializable] internal sealed class UiSetRectRequest { ... public bool autoSave; }
[Serializable] internal sealed class UiCreateButtonRequest { ... public bool autoSave; }
[Serializable] internal sealed class UiCreatePanelRequest { ... public bool autoSave; }
[Serializable] internal sealed class UiCreateRawImageRequest { ... public bool autoSave; }
[Serializable] internal sealed class ComponentAddRequest { ... public bool autoSave; }
[Serializable] internal sealed class ComponentSetRequest { ... public bool autoSave; }
[Serializable] internal sealed class ComponentSetSerializedFieldRequest { ... public bool autoSave; }
[Serializable] internal sealed class GameObjectSetActiveRequest { ... public bool autoSave; }
[Serializable] internal sealed class GameObjectSetSiblingRequest { ... public bool autoSave; }
```

**`McpSharedHelpers.cs` — 공통 헬퍼 추가:**
```csharp
public static bool TryAutoSave()
{
    try
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path)) return false;
        return EditorSceneManager.SaveScene(scene);
    }
    catch
    {
        return false;
    }
}
```

**핸들러 적용 예시:**
```csharp
// 기존
EditorSceneManager.MarkSceneDirty(go.scene);
return new CreateResponse { ... };

// 개선 후
EditorSceneManager.MarkSceneDirty(go.scene);
var autoSaved = req.autoSave && McpSharedHelpers.TryAutoSave();
return new CreateResponse { ..., autoSaved = autoSaved };
```

**응답 모델에 `autoSaved` 필드 추가:**
- `CreateResponse`, `GenericResponse`, `UiCreateButtonResponse` 등 관련 응답에
  ```csharp
  public bool autoSaved;  // autoSave 요청으로 인해 자동 저장됨
  ```

### 사용 예시
```bash
# autoSave: true — 생성 후 자동 저장
curl -X POST http://127.0.0.1:52676/gameobject/create \
  -H "Content-Type: application/json" \
  -d '{"name": "SaveButton", "parent": "/Canvas/Panel", "autoSave": true}'

# 응답: {"success": true, "autoSaved": true, "path": "/Canvas/Panel/SaveButton", ...}
```

---

## 4. 동적 엔드포인트 등록 시스템

### 현재 상태: ✅ 이미 충분

`EndpointRegistry.cs`가 이미 잘 구현되어 있음:
- `IMcpEndpoint` 인터페이스
- `FuncEndpoint` 클래스
- `"POST".Register(path, description, handler)` 확장 메서드
- static constructor에서 자동 등록
- `/debug/endpoints`로 전체 엔드포인트 조회 가능

### 추가 작업: 문서화만

**`mcp_bridge_improvements.md`에 사용법 기록:**
```csharp
// 커스텀 엔드포인트 추가 예시 (임의의 .cs 파일에서)
internal static class MyCustomHandlers
{
    static MyCustomHandlers()
    {
        "POST".Register("/custom/my-action", "My custom action", async (req, res) =>
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(req);
            // ... 처리 로직
            await UnityMcpBridge.WriteJsonAsync(res, 200, new { success = true });
        });
    }
}
```

---

## 구현 파일 목록

| 파일 | 작업 |
|---|---|
| `Assets/Editor/UnityMcp/SceneChangeQueue.cs` | **신규 생성** |
| `Assets/Editor/UnityMcp/Models.cs` | ErrorResponse 필드 추가, autoSave 필드 추가, queued/autoSaved 응답 필드 추가 |
| `Assets/Editor/UnityMcp/UnityMcpBridge.cs` | HandleRequestAsync 예외 처리 개선, GetErrorHint 헬퍼 추가 |
| `Assets/Editor/UnityMcp/McpSharedHelpers.cs` | TryAutoSave 헬퍼 추가 |
| `Assets/Editor/UnityMcp/Handlers/GameObjectHandlers.cs` | SceneChangeQueue, autoSave 적용 |
| `Assets/Editor/UnityMcp/Handlers/UiHandlers.cs` | SceneChangeQueue, autoSave 적용 |
| `Assets/Editor/UnityMcp/Handlers/ComponentHandlers.cs` | SceneChangeQueue, autoSave 적용 |
| `agent/mcp_bridge_improvements.md` | 완료 상태로 업데이트 |

---

## Assumptions

- 기존 엔드포인트의 기본 동작은 변경하지 않음 (하위 호환성 유지)
- `autoSave` 기본값은 `false` (기존 요청과 호환)
- Play Mode 큐잉은 에디터 전용 기능 (런타임 빌드 영향 없음)
- 스택 트레이스는 Editor 빌드에서만 포함 (릴리즈 빌드 제어는 추후 검토)
