# Unity 에디터 작업 흐름

MCP, 컴파일 에러 확인, 플레이 모드, 메인 스레드 제약에 대한 규칙.

---

## Unity Editor 작업 절차

glm이 Unity editor 작업을 할 때 다음 절차를 따르세요:

### 1. 작업 전 상태 확인
1. Unity Editor 실행 중인지 확인
2. 현재 열려 있는 scene 확인
3. Play Mode 상태 확인

### 2. MCP 연결 확인
1. `GET /health`로 bridge 상태 확인
2. 포트 번호 확인: `ProjectSettings/UnityMcpPort.txt` 또는 응답의 `port` 필드
3. 프로젝트 키 확인: health 응답의 `projectKey` 확인 (다른 프로젝트 bridge와 혼동 방지)

### 3. 스크립트/코드 수정
1. `#if UNITY_EDITOR` 가드 사용 (Editor 전용 코드)
2. Editor API는 메인 스레드에서만 호출: `UnityMcpBridge.RunOnMainThreadAsync` 사용
3. deprecated API 확인: `EditorSceneManager` → `SceneManagement.SceneManager`

### 4. 작업 후 검증
1. 컴파일 완료 확인: `GET /console/errors` 또는 `GET /console/logs`
2. 로그 확인: `GET /console/stream`으로 실시간 스트리밍
3. 스크린샷: 작업 종료 직전 `POST /screenshot/capture`로 화면 저장

---

## 5. 플레이 모드 중 스크립트 수정 금지

**규칙:** Unity 플레이 중에 C# 스크립트나 `Assets/Editor/**` 코드를 수정하고 즉시 적용될 것이라 가정하지 않는다.

**올바른 순서:**
1. Play Stop
2. 스크립트 수정
3. 컴파일 완료 확인
4. 테스트 재개

---

## 6. Unity MCP 운영

**코드 위치:** `Assets/Editor/UnityMcp/`

**용도:** Unity Editor 내장 HTTP 서버(`HttpListener`)를 통해 scene, prefab, play mode, 입력, 로그, 스크린샷, 빌드를 원격 제어/확인한다.

### 구조

```
Assets/Editor/UnityMcp/
  UnityMcpBridge.cs       — HttpListener 서버, [InitializeOnLoad] 자동 기동
  EndpointRegistry.cs     — 동적 엔드포인트 등록/디스패치
  Models.cs               — 요청/응답 DTO 전부
  McpConfig.cs            — 메모리 설정 (AutoSaveSceneOnPlayStop/OnBuild)
  McpSharedHelpers.cs     — 공통 유틸 (GameObject 탐색, 컴포넌트 조작 등)
  PlayModeChangeQueue.cs  — Play mode 변경 직렬화
  McpRequestLogger.cs     — 요청 로깅
  Handlers/
    BuildHandlers.cs      — /compile/*, /build/webgl, /asset/refresh, /menu/execute, /config/*
    ComponentHandlers.cs  — /component/*
    ConsoleHandlers.cs    — /console/errors, /console/logs (legacy)
    GameObjectHandlers.cs — /gameobject/* (legacy)
    InputHandlers.cs      — /input/*
    PlayHandlers.cs       — /health, /scene/current, /play/*, /screenshot/*
    PrefabHandlers.cs     — /prefab/*
    SceneHandlers.cs      — /scene/open, /scene/save, /scene/hierarchy
    UiHandlers.cs         — /ui/* (legacy)
    # 개선된 핸들러 (2026-04-15 개편)
    ImprovedConsoleHandlers.cs  — /console/stream, /console/logs/filter, /console/stats
    ImprovedUiHandlers.cs       — /ui/invoke, /ui/get-state, /ui/set-value, /ui/list-handlers
    ImprovedGameObjectHandlers.cs — /gameobject/find-with-fields
    LocatorHandlers.cs     — /locator/*
    WaitHandlers.cs       — /wait/*
    EvalHandlers.cs       — /eval/*
    SnapshotHandlers.cs    — /snapshot/*
    ExploreHandlers.cs    — /explore/*
```

**엔드포인트 SSOT:** 문서에 엔드포인트 목록을 적어두지 않는다. 대신 아래를 따른다.
- **런타임 조회:** `GET /debug/endpoints` — 등록된 전체 엔드포인트 + 설명 반환
- **코드 기준:** `Handlers/*.cs` 각 파일의 static constructor에서 `Register()` 호출
- **DTO 기준:** `Models.cs` — 모든 요청/응답 스키마

### 접속

- 주소: `http://127.0.0.1:{port}/`
- 포트: `ProjectSettings/UnityMcpPort.txt` (기본 `51234`)
- 프로젝트 키: 경로 해시로 생성, health에서 반환 — 다른 프로젝트 bridge와 혼동 방지
- health probe로 동일 프로젝트 bridge 인지 확인 후 재사용

> **참고:** 포트 번호가 불확실할 경우 `ProjectSettings/UnityMcpPort.txt` 파일을 직접 확인하거나 `GET /health` 응답에서 `port` 필드를 확인

**기본 순서:**
1. `GET /health`로 bridge, scene, `isCompiling`, `isPlaying`, `isPlayModeChanging` 확인
2. 작업 전 `Play Stop → 파일 수정 → 컴파일 완료 확인 → /health 확인`
3. 주요 액션 뒤 `GET /console/logs` 또는 `GET /console/errors` 확인 (또는 `GET /console/stream`으로 실시간 스트리밍)
4. hierarchy/scene 문제는 `GET /scene/hierarchy` 또는 `tools/mcp-diagnose-scene-hierarchy.ps1` 사용
5. 종료 직전 가능하면 `POST /screenshot/capture`로 화면 저장 후 실제 결과 확인

### 운영 규칙

- MCP 테스트는 화면만 보지 말고 로그까지 같이 확인
- runtime UI flow 회귀를 hardcoded 자동 스크립트로 굳히지 않음
- 공식 기록은 `docs/playtest/runtime_validation_checklist.md`에 남기고, MCP 입력 라우트는 일회성 수동 진단에 사용
- `/ui/invoke`와 `/input/*`은 **play mode 전용** — Game view 렌더링 필요
- `PlayModeChangeQueue`를 통해 play mode 변경을 직렬화 — 동시 진입 방지
- 프로젝트 키 기반으로 다른 프로젝트 bridge와 혼동하지 않음 — health probe로 확인
- scene/prefab wiring 변경은 가능하면 MCP/에디터 내부에서 처리 (serialization.md#2.1 참조)

---

## 14. Unity MCP 핸들러의 메인 스레드 제약

**규칙:** MCP 핸들러는 `HttpListener` thread pool에서 실행된다. Unity API(`GameObject.name`, `transform`, `GetComponent`, `SceneManager` 등)는 **반드시 `UnityMcpBridge.RunOnMainThreadAsync` 안에서만 호출**한다.

```csharp
// ❌ 잘못됨 — await 이후 Unity API 직행 호출
var go = FindSomething();
var path = McpSharedHelpers.GetTransformPath(go.transform); // UnityException: main thread only
var name = go.name;                                          // 동일 에러

// ✅ 올바름 — Unity API 전체를 RunOnMainThreadAsync 안에서
var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
{
    var go = FindSomething();
    return new { path = McpSharedHelpers.GetTransformPath(go.transform), name = go.name };
});
```

**polling 기반 핸들러 패턴 (Phase 2 Auto-wait):**

```csharp
// 1. condition: 호출하는 쪽에서 이미 main thread 결과를 반환
var result = await WaitForConditionAsync(
    () => UnityMcpBridge.RunOnMainThreadAsync(FindTarget),  // condition 자체가 main thread 통과
    timeoutMs, pollIntervalMs, "target-found");

// 2. Task.Delay 이후 — 절대 Unity API 호출 금지
await Task.Delay(pollIntervalMs);  // 여기서는 순수 C#만

// 3. 결과 추출 — 다시 RunOnMainThreadAsync로 감싸기
var item = await UnityMcpBridge.RunOnMainThreadAsync(() =>
    BuildLocatorItem(foundGo));
```

**금지:**
- `Task.Delay` 이후 `go.name`, `go.transform`, `GetComponent` 등 Unity API 호출
- condition 함수 내부에서 Unity API를 호출하되 `RunOnMainThreadAsync`로 감싸지 않는 경우
- 핸들러 응답 생성 시 Unity 객체를 직접 참조 (에러 시 `ObjectDisposedException` 유발)

**실제 사례:** Phase 2 `WaitForConditionAsync`에서 `Task.Delay` 루프 이후 `found.name`을 호출하여 `UnityException: GetName can only be called from the main thread` 발생. condition 실행과 결과 추출을 각각 `RunOnMainThreadAsync`로 완전히 분리하여 해결.

---

## 15. inactive GameObject와 Coroutine

**규칙:** Unity는 inactive GameObject에서 `StartCoroutine`을 호출하면 에러를 내고 코루틴을 시작하지 않는다.

```
Coroutine couldn't be started because the game object 'Xxx' is inactive!
```

View의 `Render()` 메서드가 코루틴을 사용할 때, 호출 시점에 GameObject가 inactive일 수 있다면 반드시 방어한다.

```csharp
// ❌ 잘못됨 — inactive 체크 없이 StartCoroutine 직행
public void Render(ViewModel vm)
{
    StartCoroutine(FadeColor(targetColor, 0.15f));
}

// ✅ 올바름 — inactive 체크 후 fallback
public void Render(ViewModel vm)
{
    if (!gameObject.activeInHierarchy)
    {
        // 애니메이션 건너뛰고 최종 상태 즉시 적용
        _background.color = targetColor;
        return;
    }
    StartCoroutine(FadeColor(targetColor, 0.15f));
}
```

**판단 기준:**
| 상황 | 접근 |
|---|---|
| View가 언제 Render될지 모름 (다양한 호출 경로) | View 스스로 `activeInHierarchy` 방어 |
| 호출부가 초기화 순서를 제어함 | 호출부에서 active 보장 + View도 방어 (이중 안전) |
| 코루틴이 실행 중 inactive로 전환 | Unity가 코루틴 자동 중단 — 별도 처리 불필요 |

**실제 사례:** `GarageSlotItemView.Render()`에서 `FadeBackgroundColor` 코루틴 호출 시 `GaragePageRoot`가 inactive 상태여서 `Coroutine couldn't be started` 에러 발생. `!gameObject.activeInHierarchy` 체크 후 색상 즉시 적용 fallback 추가.

---

## 16. 컴파일 에러 확인

**규칙:** Unity Editor가 실행 중일 때도 컴파일 에러/경고를 빠르게 확인할 수 있어야 한다.

### 방법

**1. PowerShell 스크립트 (에디터 상관없음)**
```powershell
tools/check-compile-errors.ps1
```
- Editor.log를 파싱하여 에러/경고를 그룹화해 표시
- `error CSxxxx` 패턴으로 추출, 중복 제거
- 종료 코드 = 에러 개수

**2. MCP 엔드포인트 (브릿지 실행 중)**
```
GET /unity/compile-errors
```
- `logPath`, `errorCount`, `warningCount`, 최근 에러/경고 배열 반환
- `GET /unity/log-path`로 로그 파일 경로 확인

### 로그 경로

| 플랫폼 | 경로 |
|-------|------|
| Windows | `%LOCALAPPDATA%\Unity\Editor\Editor.log` |
| macOS | `~/Library/Logs/Unity/Editor.log` |
| Linux | `~/.config/unity3d/Editor.log` |

### 개선 기능

Play Mode 중 변경 요청 처리, 응답 디버깅 개선, 자동 씬 저장 등 개선이 적용되었습니다.

| 개선 항목 | 상태 |
|-----------|------|
| Play Mode 중 변경 큐잉 | ✅ 완료 - `SceneChangeQueue.cs` |
| 응답 스택트레이스 | ✅ 완료 - `ErrorResponse.stackTrace`, `hint` |
| 자동 씬 저장 | ✅ 완료 - `autoSave` 필드 |

> **상세 계획**: [`/docs/plans/mcp_improvement_plan.md`](../../../docs/plans/mcp_improvement_plan.md)

### 기본 절차

스크립트 수정 후: `수정 → 저장 → 컴파일 대기 → 에러 확인`

---

## 17. 표준 MCP 핸들러 템플릿

**규칙:** 모든 MCP 핸들러는 일관된 구조와 에러 처리를 따라야 한다. 120회 자동화 버그 헌팅에서 발견된 3가지 주요 패턴을 방지하기 위한 표준 템플릿.

### 3가지 주요 버그 패턴 방지

**패턴 1: 메인 스레드 위반** ⚠️
- 증상: `get_isPlaying can only be called from the main thread`
- 원인: `EditorApplication` API를 async 핸들러에서 직접 호출
- 해결: `RunOnMainThreadAsync` 래핑

**패턴 2: JSON 파싱 예외 미처리** 💥
- 증상: HTTP 500 Internal Server Error
- 원인: `JsonUtility.FromJson`이 null/invalid JSON에서 예외 발생, catch 없음
- 해결: null 체크 + try-catch

**패턴 3: 요청 본문 체크 불일치** 🔀
- 증상: 같은 유형 엔드포인트도 일부 작동, 일부 500
- 원인: null 체크 방식 불일치
- 해결: 일관된 `string.IsNullOrWhiteSpace` 체크

### 표준 템플릿

```csharp
public static async Task HandleXxxAsync(
    HttpListenerRequest request,
    HttpListenerResponse response)
{
    try {
        // 1️⃣ 요청 본문 읽기 + 파싱 (null 체크 포함)
        var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
        var req = string.IsNullOrWhiteSpace(body) ? null : 
                  JsonUtility.FromJson<RequestType>(body);
        
        // 2️⃣ 필수값 검증
        if (req == null) {
            await UnityMcpBridge.WriteJsonAsync(response, 400,
                new ErrorResponse { error = "Request body required" });
            return;
        }
        
        // 3️⃣ 메인 스레드에서 작업 실행
        var result = await UnityMcpBridge.RunOnMainThreadAsync(() => {
            // ✅ EditorApplication API 호출 안전
            // ✅ 씬 변경, GameObject 수정 등 작업 수행
            return new ResponseType { /* ... */ };
        });
        
        // 4️⃣ 응답 전송
        await UnityMcpBridge.WriteJsonAsync(response, 200, result);
    } 
    catch (Exception ex) {
        // 5️⃣ 예외 처리
        await UnityMcpBridge.WriteJsonAsync(response, 500,
            new ErrorResponse { 
                error = "Processing failed", 
                detail = ex.Message 
            });
    }
}
```

### 템플릿 적용 예시

**✅ 올바른 핸들러 (ComponentHandlers.cs 기반):**
```csharp
public static async Task HandleComponentGetAsync(
    HttpListenerRequest request,
    HttpListenerResponse response)
{
    try {
        var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
        var req = string.IsNullOrWhiteSpace(body) ? null : 
                  JsonUtility.FromJson<ComponentGetRequest>(body);
        
        if (req == null) {
            await UnityMcpBridge.WriteJsonAsync(response, 400,
                new ErrorResponse { error = "Request body required" });
            return;
        }
        
        var result = await UnityMcpBridge.RunOnMainThreadAsync(() => {
            var go = McpSharedHelpers.FindGameObjectByPath(req.gameObjectPath);
            if (go == null)
                throw new Exception("GameObject not found: " + req.gameObjectPath);
            
            // Unity API 호출 안전
            var comp = go.GetComponent(req.componentType);
            if (comp == null)
                throw new Exception("Component not found: " + req.componentType);
                
            return new ComponentGetResponse { /* ... */ };
        });
        
        await UnityMcpBridge.WriteJsonAsync(response, 200, result);
    } 
    catch (Exception ex) {
        await UnityMcpBridge.WriteJsonAsync(response, 500,
            new ErrorResponse { 
                error = "Processing failed", 
                detail = ex.Message 
            });
    }
}
```

**❌ 잘못된 핸들러 (버그 패턴 포함):**
```csharp
public static async Task HandleComponentGetAsync(
    HttpListenerRequest request,
    HttpListenerResponse response)
{
    // ❌ 패턴 2: null 체크 없음
    var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
    var req = JsonUtility.FromJson<ComponentGetRequest>(body); // crash 가능
    
    // ❌ 패턴 1: 메인 스레드 위반
    var go = McpSharedHelpers.FindGameObjectByPath(req.gameObjectPath); // UnityException
    var comp = go.GetComponent(req.componentType); // UnityException
    
    // ❌ 패턴 2: try-catch 없음
    await UnityMcpBridge.WriteJsonAsync(response, 200, result); // 예외 시 500
}
```

### 검사 체크리스트

모든 MCP 핸들러 작성/수정 시 확인:
- [ ] `EditorApplication` 호출이 `RunOnMainThreadAsync`로 래핑됨
- [ ] `JsonUtility.FromJson` 전에 `string.IsNullOrWhiteSpace(body)` 체크
- [ ] 요청 본문 필수인지 검증 (`req == null` 체크)
- [ ] 전체 핸들러가 `try-catch`로 감싸짐
- [ ] 에러 응답이 400/500으로 적절히 구분됨 (400: 클라이언트 잘못, 500: 서버 에러)

### 관련 규칙

- **메인 스레드 제약:** [섹션 14](#14-unity-mcp-핸들러의-메인-스레드-제약) 참조
- **컴파일 에러 확인:** [섹션 16](#16-컴파일-에러-확인) 참조
- **씬 직렬화:** `serialization.md` 참조
- **코딩 규칙:** `coding-rules.md` 참조
