# /agent/logging_rules.md

> 최초 작성: 2026-04-12

이 문서는 프로젝트 로깅 전략의 SSOT다. Unity 공식 `ILogger`/`ILogHandler` 구조를 따른다.

---

## Unity 로깅 인프라 개요

Unity는 자체 로깅 시스템을 제공한다:

| 타입 | 역할 |
|---|---|
| `Debug.Log/LogWarning/LogError` | 정적 편의 메서드 (내부적으로 `Debug.unityLogger` 호출) |
| `ILogger` | 로거 인터페이스 (`logEnabled`, `filterLogType`, `logHandler`) |
| `ILogHandler` | 로그 출력 대상 제어 (콘솔, 파일, 원격) |
| `Logger` | `ILogger` + `ILogHandler` 결합 구현체 |
| `Debug.unityLogger` | 전역 Unity 기본 로거 인스턴스 |

**공식 문서 참조:**
- `ILogger`: https://docs.unity3d.com/ScriptReference/ILogger.html
- `Logger`: https://docs.unity3d.com/ScriptReference/Logger.html
- `ILogHandler`: https://docs.unity3d.com/ScriptReference/ILogHandler.html

---

## 프로젝트 로깅 아키텍처

### 레이어별 허용 규칙

| 레이어 | 허용 | 금지 |
|---|---|---|
| **Domain** | 로깅 금지 (순수 비즈니스 로직) | `Debug.*`, `ILogger` |
| **Application** | 결과 반환 + **예외 catch 시** 로깅 허용 | 일반 로직 중 `Debug.*`, `ILogger` 금지 |
| **Presentation** | `Log.Info/Warn/Error` | `Debug.Log/LogWarning/LogError` 직행 |
| **Infrastructure** | `Log.Warn/Error` | `Debug.Log` 직행 |
| **Bootstrap** | `Log.Info/Warn/Error` | `Debug.Log` 직행 |
| **Shared/Logging/** | `ILogHandler` 구현, `Log` 정적 진입점 | — |

**참고:** Application 레이어의 UseCase는 예외 catch 시 로깅을 담당합니다. [`error_handling.md`](error_handling.md#예외-로깅-연계)를 참조하세요.

**원칙:** 로깅은 **외부 시스템과 직접 통신하는 레이어**에서만 한다. Domain/Application은 결과(반환값, 이벤트)로 상태 전파.

### 로그 레벨 정의

| 레벨 | 용도 | 릴리즈 빌드 |
|---|---|---|
| `Info` | 개발 디버깅 (초화 순서, 상태 전환, 선택 결과) | **제거됨** |
| `Warn` | 복구 가능 문제 (누락된 참조 폴백, 네트워크 지연) | **유지** |
| `Error` | 복구 불가능 문제 (컴포넌트 누락, 치명적 실패) | **유지** |

**규칙:**
* `Info`는 개발 중에만 의미. 릴리즈에서는 `IsLogTypeAllowed` 체크로 문자열 생성 비용 제거.
* `Warn`은 사용자에게 배너로 표시해도 되는 수준.
* `Error`는 사용자에게 모달로 표시 + Firebase Analytics 전송 대상.

---

## Log 정적 진입점

`Shared/Logging/Log.cs` — 프로젝트 전역 로깅 진입점.

```csharp
using UnityEngine;

namespace Shared.Logging
{
    public static class Log
    {
        // 개발 빌드에서만 Info 로그 활성화
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string tag, object message, Object context = null)
        {
            if (Debug.unityLogger.IsLogTypeAllowed(LogType.Log))
                Debug.unityLogger.Log(tag, message, context);
        }

        // 항상 출력 — Warn 이상
        public static void Warn(string tag, object message, Object context = null)
        {
            Debug.unityLogger.LogWarning(tag, message, context);
        }

        // 항상 출력 — Error
        public static void Error(string tag, object message, Object context = null)
        {
            Debug.unityLogger.LogError(tag, message, context);
        }

        // 예외 — 스택 트레이스 포함
        public static void Exception(System.Exception ex, Object context = null)
        {
            Debug.unityLogger.LogException(ex, context);
        }
    }
}
```

**`Conditional` 속성:**
* `UNITY_EDITOR`: 에디터에서 Info 로그 활성화
* `DEVELOPMENT_BUILD`: Development Build 체크박스 켠 플레이어 빌드에서만 활성화
* 릴리즈 빌드: `Info()` 호출 자체가 **컴파일 시 제거** (문자열 연결 비용 0)

---

## 커스텀 ILogHandler (선택 확장)

릴리즈 빌드에서 Warn/Error를 Firebase Analytics로 전송하려면 `ILogHandler`를 구현한다.

```csharp
// Shared/Logging/AnalyticsLogHandler.cs
using System;
using UnityEngine;

namespace Shared.Logging
{
    public class AnalyticsLogHandler : ILogHandler
    {
        private readonly ILogHandler _inner;
        private readonly IAnalyticsPort _analytics;

        public AnalyticsLogHandler(ILogHandler inner, IAnalyticsPort analytics)
        {
            _inner = inner;
            _analytics = analytics;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            // 기존 콘솔 출력 유지
            _inner.LogFormat(logType, context, format, args);

            // Warn/Error를 Analytics로 전송
            if (logType >= LogType.Warning)
            {
                _analytics.LogEvent("unity_log", new AnalyticsParams
                {
                    { "level", logType.ToString() },
                    { "message", string.Format(format, args) }
                });
            }
        }

        public void LogException(Exception exception, Object context)
        {
            _inner.LogException(exception, context);
            _analytics.LogEvent("unity_exception", new AnalyticsParams
            {
                { "message", exception.Message },
                { "stack", exception.StackTrace }
            });
        }
    }
}
```

Bootstrap에서 설치:

```csharp
Debug.unityLogger.logHandler = new AnalyticsLogHandler(
    Debug.unityLogger.logHandler,  // 기존 콘솔 핸들러 유지
    analyticsPort
);
```

---

## 성능 규칙

### 1. 성능 크리티컬 컨텍스트에서 Log 사용 금지

`Update()`, `FixedUpdate()`, 네트워크 RPC 콜백, 물리 프레임에서 `Log.Info` 호출 금지.

**이유:** `Conditional`이 있어도 `IsLogTypeAllowed` 체크 전까지 문자열 포맷이 실행될 수 있음.

```csharp
// ❌ Update에서 매 프레임 로그
void Update()
{
    Log.Info("Player", $"HP: {player.CurrentHp}");
}

// ✅ 상태 변경 시에만
void OnDamaged(int damage)
{
    Log.Warn("Player", $"Took {damage} damage. HP: {player.CurrentHp}");
}
```

### 2. 포맷 문자열 vs 문자열 연결

```csharp
// ✅ LogFormat 사용 (지연 평가)
Debug.unityLogger.LogFormat(LogType.Log, null, "Frame: {0}, FPS: {1}", frame, fps);

// ❌ 문자열 연결 — 로그 꺼져도 실행됨
Debug.Log("Frame: " + frame + ", FPS: " + fps);
```

### 3. 민감 정보 로그 금지

```csharp
// ❌ API 키, 토큰, UID 직접 로그
Log.Info("Auth", $"Token: {token.IdToken}");

// ✅ 마스킹 또는 생략
Log.Info("Auth", $"Token: {token.IdToken.Substring(0, 8)}...");
Log.Info("Auth", "Sign-in successful");
```

---

## 마이그레이션 가이드

기존 `Debug.Log/LogWarning/LogError`를 `Log.Info/Warn/Error`로 점진적 전환:

| 기존 | 새 |
|---|---|
| `Debug.Log("msg")` | `Log.Info("Tag", "msg")` |
| `Debug.LogWarning("msg")` | `Log.Warn("Tag", "msg")` |
| `Debug.LogError("msg")` | `Log.Error("Tag", "msg")` |
| `Debug.LogException(ex)` | `Log.Exception(ex)` |
| `Debug.Log($"HP: {hp}")` | `Log.Info("Tag", $"HP: {hp}")` (개발 빌드에서만) |

**우선순위:**
1. `Info` 레벨 로그부터 전환 (릴리즈에서 자동 제거)
2. `Warn`/`Error`는 기존 `Debug.*` 유지해도 동작상 문제없음 (점진적 전환)
3. 성능 크리티컬 경로(`Update`, RPC 콜백)는 즉시 제거 또는 `Warn`으로 변경

---

## 예외 처리 연계

UseCase에서 Infrastructure 예외를 잡을 때 로깅 책임이 명확하다.
세부 규칙은 `/agent/error_handling.md`를 따른다.

**GameException<TCode> catch:**
```csharp
catch (GameException<AccountError> ex)
{
    Log.Error("Auth", $"{ex.Code}: {ex.Message}");
    return Result.Failure(new ErrorDetail<AccountError>(ex.Code, ex.Message, ex));
}
```
- `Log.Error` 사용 — 콘솔 출력 + 릴리즈 유지
- `Inner`에 원본 예외 포함 (Analytics 추적용)

**기타 Exception catch:**
```csharp
catch (Exception ex)
{
    Log.Exception(ex);  // 스택 트레이스 포함
    return Result.Failure(new ErrorDetail<AccountError>(
        AccountError.Unknown, "Unexpected error occurred.", ex));
}
```
- `Log.Exception` 사용 — 스택 트레이스 전체 기록

**Presentation은 로깅 금지:**
```csharp
// ❌ 잘못됨 — Presentation에서 Log 호출
if (result.IsFailure)
{
    Log.Error("Auth", result.Error);  // 금지
    _errorPresenter.Show(result.Error);
}

// ✅ 올바름 — UI 표시만
if (result.IsFailure)
{
    _errorPresenter.Show(result.ErrorDetail.Message);
}
```

**정리:**
| 레이어 | 역할 |
|---|---|
| Infrastructure | `GameException<TCode>` throw |
| UseCase | `Log.Error/Exception` + `Result.Failure` 반환 |
| Presentation | `ErrorCode`로 분기 → UI 표시 (로깅 안 함) |

---

## Bootstrap 초기화 시 로깅

Scene Bootstrap은 시작 시 `Info` 로그로 초기화 순서를 기록:

```csharp
// LobbySetup.cs
void Start()
{
    Log.Info("Lobby", "=== LobbySetup Start ===");
    Log.Info("Lobby", "Initializing anonymous sign-in...");
    // ...
    Log.Info("Lobby", "Lobby initialized. Waiting for user input.");
}
```

이 로그는 릴리즈 빌드에서 **전체 제거**되므로 성능 영향 없음.

---

## 요약 — 규칙 체크리스트

| 규칙 | 준수 여부 확인 |
|---|---|
| Domain/Application에서 로깅 금지 | ✅ |
| Presentation/Infrastructure/Bootstrap만 `Log.*` 사용 | ✅ |
| `Info`는 `Conditional`로 릴리즈 제거 | ✅ |
| 성능 크리티컬 컨텍스트에서 `Log` 금지 | ✅ |
| 민감 정보(토큰, API 키, UID) 로그 마스킹 | ✅ |
| 기존 `Debug.*` 직행 금지 (점진적 마이그레이션) | ✅ |
