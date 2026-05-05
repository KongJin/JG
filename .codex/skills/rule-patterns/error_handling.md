# Error Handling

> 최초 작성: 2026-04-12
> 마지막 수정: 2026-04-12 (예외-로깅 연계 규칙 추가)

이 문서는 UseCase 실패 시 Presentation으로 오류를 전파하는 패턴의 SSOT다.
로깅 연계 규칙은 [`logging_rules.md`](logging_rules.md)와 함께 읽는다.

---

## 개요

이 프로젝트는 **ErrorCode enum + ErrorDetail<TCode> + Result<T>** 패턴을 사용한다.

* Infrastructure는 예외를 `throw` 한다 (UnityWebRequest, Firebase SDK 등 외부 시스템 특성상 불가피).
* UseCase는 Infrastructure 예외를 `catch` → `Log.Warn/Error` 기록 → `Result.Failure(ErrorDetail)` 반환.
* Presentation은 `Result.IsFailure` 확인 후 `ErrorCode`로 분기. **string 메시지로 비교 금지**.

---

## GameException<TCode> — Shared 레이어

**규칙:** `GameException<TCode>`는 **Shared/ErrorHandling/** 에 둔다. 모든 Feature가 재사용.

```
Assets/Scripts/Shared/ErrorHandling/GameException.cs
```

```csharp
using System;

namespace Shared.ErrorHandling
{
    public class GameException<TCode> : Exception where TCode : Enum
    {
        public TCode Code { get; }

        public GameException(TCode code, string message, Exception inner = null)
            : base(message, inner)
        {
            Code = code;
        }
    }
}
```

**규칙:**
* `TCode`는 Feature별 ErrorCode enum을 사용 (`AccountError`, `UnitError` 등).
* Infrastructure에서 Infrastructure 예외 발생 시 이 타입으로 throw.
* 의미 없는 `new Exception("error")` 금지 — 반드시 `GameException<TCode>` 사용.

---

## ErrorCode enum — Domain 레이어

**규칙:** ErrorCode enum은 **Domain** 레이어에 둔다. 순수 C# enum이며 Unity/Photon 의존 금지.

```
Assets/Scripts/Features/<Name>/Domain/ErrorCodes.cs
```

**명명:** `<Feature>Error` 접미사 사용.

```csharp
// Features/Account/Domain/ErrorCodes.cs
namespace Features.Account.Domain
{
    public enum AccountError
    {
        None = 0,
        NetworkTimeout,
        InvalidToken,
        AccountAlreadyLinked,
        UserCancelled,
        Unknown,
    }
}
```

**필수 규칙:**
* `None = 0` — 기본값, 성공 상태
* `Unknown` — 매핑할 수 없는 예외 폴백
* 새 enum 값 추가 시 `switch`에서 `default`는 `throw new ArgumentOutOfRangeException()` (`anti_patterns.md` 완전 switch 규칙)

---

## ErrorDetail<TCode> — Application 레이어

**규칙:** ErrorDetail은 **Application** 레이어에 둔다.

```
Assets/Scripts/Features/<Name>/Application/ErrorDetail.cs
```

```csharp
using System;

namespace Features.Account.Application
{
    public readonly struct ErrorDetail<TCode> where TCode : Enum
    {
        public TCode Code { get; }
        public string Message { get; }       // 사용자에게 보여줄 메시지
        public Exception Inner { get; }      // 로깅/분석용 — Presentation 접근 금지

        public ErrorDetail(TCode code, string message, Exception inner = null)
        {
            Code = code;
            Message = message;
            Inner = inner;
        }
    }
}
```

**규칙:**
* Presentation은 `Code`로만 분기한다. `Message`는 표시 전용, `Inner`는 접근 금지.
* Bootstrap은 `Inner`를 `Log.Exception`으로 로깅 가능.
* `Inner`는 Firebase Analytics로 원본 예외 추적 시 사용.

---

## Result<T> 확장

`Shared/Kernel/Result.cs`에 `ErrorDetail` 오버로드 추가:

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }          // 기존 호환용 (Message)
    public ErrorDetail<TCode> ErrorDetail { get; }  // TCode는 UseCase 시그니처에서 유추

    public static Result<T> Success(T value) => new Result<T>(value, true, string.Empty, default);
    public static Result<T> Failure(string error) => ...; // 기존 유지 (하위 호환, Validation용)
    public static Result<T> Failure<TCode>(ErrorDetail<TCode> detail) where TCode : Enum
        => new Result<T>(default, false, detail.Message, detail);
}
```

**규칙:**
* 기존 `Failure(string)`은 단순 케이스용(Validation, 입력 검증)으로 유지.
* Infrastructure 예외를 통한 실패는 `Failure(ErrorDetail)` 사용.

---

## UseCase 에러 처리 규칙

**규칙:** UseCase는 Infrastructure 예외를 **반드시 잡아서** 로깅 + `Result.Failure`로 변환한다.

```csharp
public async Task<Result<AccountProfile>> Execute(string googleIdToken)
{
    if (string.IsNullOrWhiteSpace(googleIdToken))
        return Result<AccountProfile>.Failure("Google ID 토큰이 비어 있습니다."); // Validation

    try
    {
        var token = await _authPort.SignInWithGoogle(googleIdToken);
        var account = await _dataPort.LoadProfile(token.Uid, token.IdToken);
        return Result<AccountProfile>.Success(account);
    }
    catch (GameException<AccountError> ex)
    {
        // GameException은 이미 코드 정보를 가짐 — 로깅 후 반환
        Log.Error("Auth", $"{ex.Code}: {ex.Message}");
        return Result<AccountProfile>.Failure(new ErrorDetail<AccountError>(ex.Code, ex.Message, ex));
    }
    catch (Exception ex)
    {
        // 알 수 없는 예외 — 스택 트레이스 포함 로깅
        Log.Exception(ex);
        return Result<AccountProfile>.Failure(new ErrorDetail<AccountError>(
            AccountError.Unknown, "Unexpected error occurred.", ex));
    }
}
```

**규칙:**
* `try-catch` 범위는 Infrastructure 호출 지점만 감싼다. 도메인 로직은 `try` 외부에 둔다.
* 여러 Infrastructure 호출이 있으면 각각 독립적으로 `try-catch`.
* **로깅은 UseCase 책임** — Presentation은 로깅 안 함 (UI 표시만).

---

## Presentation 분기 규칙

**규칙:** Presentation은 **반드시 ErrorCode로 분기**한다. string 메시지 비교 금지.

```csharp
// ✅ 올바름
var result = await _useCase.Execute(token);
if (result.IsFailure)
{
    switch (result.ErrorDetail.Code)
    {
        case AccountError.NetworkTimeout:
            ShowRetryDialog();
            break;
        case AccountError.UserCancelled:
            // 사용자 취소 — 조용히 무시
            break;
        default:
            _errorPresenter.Show(result.ErrorDetail.Message);
            break;
    }
}

// ❌ 잘못됨 — string 비교
if (result.Error == "Network timeout") { ... }
```

**에러 표시 전략:**

| 시나리오 | UI 컴포넌트 | 비고 |
|---|---|---|
| 전역 에러 (네트워크, 서버) | `SceneErrorPresenter` (Banner/Modal) | `UiErrorRequestedEvent` 경유 |
| 로컬 에러 (입력 검증, 배치) | 전용 `ErrorView` | 2초 자동 숨김 |
| 토스트 (경량 피드백) | `GarageResultPanelView.ShowToast` | success/error 색상 구분 |
| 로그인 실패 | `LoginLoadingView.OnLoginFailed` | 재시도 카운트 포함 |

---

## Infrastructure 예외 규칙

**규칙:** Infrastructure는 **GameException<TCode>** 를 던진다.

```csharp
// ✅ 올바름
throw new GameException<AccountError>(
    AccountError.InvalidToken,
    "Invalid Google ID token.",
    originalException);

throw new ArgumentException("Google ID token is required.", nameof(googleIdToken));
    // 파라미터 검증은 ArgumentException 유지

// ❌ 잘못됨
throw new Exception("Error");  // 의미 없음, 코드 정보 없음
```

**타임아웃:** `UnityWebRequest.timeout` 기본값 15초. 타임아웃 시 `GameException<TCode>`로 래핑하여 throw.

---

## 예외-로깅 연계 규칙

로깅 세부 규칙은 [`logging_rules.md`](logging_rules.md)를 따른다. 여기서는 연계 패턴만 정의한다.

| 상황 | 로깅 | Result 반환 |
|---|---|---|
| `GameException<TCode>` catch | `Log.Error(tag, $"{ex.Code}: {ex.Message}")` | `Failure(ErrorDetail)` |
| 기타 `Exception` catch | `Log.Exception(ex)` (스택 트레이스) | `Failure(Unknown)` |
| Validation 실패 | 로깅 안 함 (정상 분기) | `Failure(string)` |
| EventBus 핸들러 예외 | `Log.Exception(ex)` | — (EventBus가 삼킴) |

**Presentation은 로깅하지 않는다.** UI 표시만 담당.

---

## EventBus 예외 보호

EventBus는 이미 핸들러 예외를 `try-catch`로 보호한다 (`EventBus.cs`):

```csharp
try
{
    ((Action<T>)snapshot[i])(e);
}
catch (Exception ex)
{
    Log.Exception(ex);
}
```

**규칙:** 핸들러가 던진 예외는 로깅만 하고 삼킨다. 다른 핸들러는 계속 실행.

---

## 정리 — 패턴 요약

```
Infrastructure:  throw GameException<TCode>(code, message, inner)
       ↓
UseCase:         catch → Log.Error/Exception → Result.Failure(ErrorDetail)
       ↓
Presentation:    Result.IsFailure → switch (ErrorDetail.Code) → UI 표시
```

**금지:**
* Presentation에서 `result.Error` string 비교
* Presentation에서 로깅 (`Log.*` 호출 금지 — UI 표시만)
* UseCase에서 Infrastructure 예외를 catch하지 않고 투과
* Domain에서 `ErrorDetail` 참조 (Domain은 ErrorCode enum만)
* Infrastructure에서 `GameException` 없이 `Exception` 직행 throw
