# Unity CLI / Batchmode 워크플로우

Unity CLI (batchmode) 사용법, 빌드, 컴파일 에러 체크, 배치 작업.

---

## 1. CLI 기본 사용법

### Unity Editor 경로 (Windows)
```
Unity Hub: C:\Program Files\Unity Hub\Unity Hub.exe
Editor:    C:\Program Files\Unity\Hub\Editor\{버전}\Editor\Unity.exe
```

### 기본 CLI 패턴
```powershell
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$projectPath = "C:\Users\SOL\Documents\JG"

& $unityPath `
  -projectPath $projectPath `
  -batchmode `
  -nographics `
  -logFile - `
  -executeMethod YourNamespace.YourClass.YourMethod `
  -quit
```

### 주요 플래그
| 플래그 | 설명 |
|--------|------|
| `-batchmode` | 그래픽 없이 실행 (CI/CD용) |
| `-nographics` | 그래픽 서버 비활성화 |
| `-logFile -` | 로그를 stdout으로 출력 |
| `-executeMethod` | 실행할 정적 메서드 지정 |
| `-quit` | 작업 후 자동 종료 |

---

## 2. CLI가 유리한 작업

| 작업 | CLI 방법 | MCP 대비 이유 |
|------|----------|--------------|
| **컴파일 에러 체크** | `check-compile-errors.ps1` | 에디터 없이도 확인 가능 |
| **빌드** (WebGL, Android 등) | `-buildTarget` | batchmode가 목적에 맞음 |
| **여러 씬 순회 처리** | 루프로 씬 열고 처리 | 상태 없는 배치 작업 |
| **에셋 베이크/리프레시** | `AssetDatabase.Refresh()` | 무인 작업에 좋음 |
| **CI/CD 파이프라인** | `-batchmode` | 서버 환경에서 필수 |

---

## 3. 컴파일 에러 체크 (CLI)

### PowerShell 스크립트
```powershell
.\tools\check-compile-errors.ps1
```

- Editor.log를 파싱하여 에러/경고 표시
- 에디터 실행 여부와 무관
- 종료 코드 = 에러 개수

### Unity CLI로 직접 체크
```powershell
& $unityPath -projectPath $projectPath -batchmode -quit `
  -executeMethod UnityEditor.Compilation.CompilationPipeline
```

---

## 4. 빌드 (CLI)

### WebGL 빌드
```powershell
& $unityPath -projectPath $projectPath -batchmode `
  -buildTarget WebGL `
  -executeMethod UnityEditor.BuildPipeline.BuildPlayer `
  -quit
```

### 커스텀 빌드 메서드
`Assets/Editor/`에 빌드 스크립트 필요:
```csharp
#if UNITY_EDITOR
public static class BuildCommands
{
    public static void BuildWebGL()
    {
        var buildPath = "Build/WebGL";
        var buildOptions = BuildOptions.None;
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.WebGL, buildOptions);
    }
}
#endif
```

---

## 5. -executeMethod 패턴

### 기본 구조
```csharp
#if UNITY_EDITOR
using UnityEditor;

public static class CliCommands
{
    public static void YourCommand()
    {
        // Editor 전용 로직
        Debug.Log("[CLI] Running YourCommand...");

        // 컴파일 대기
        while (EditorApplication.isCompiling)
            System.Threading.Thread.Sleep(100);

        // 작업 수행
        // ...

        Debug.Log("[CLI] YourCommand completed.");
    }
}
#endif
```

### 메서드 위치
- 반드시 `#if UNITY_EDITOR`로 감싸기
- `Assets/Editor/` 폴더 내
- public static void, 파라미터 없음

---

## 6. CLI vs MCP 결정 트리

```
작업 요청
    │
    ├─> 에디터가 꺼져 있거나 CI/CD?
    │       └─> CLI (batchmode)
    │
    ├─> 스크린샷/실제 화면 필요?
    │       └─> MCP (GameView 필요)
    │
    ├─> Play Mode 실행/제어?
    │       └─> MCP (안정적)
    │
    ├─> 실시간 상태 모니터링?
    │       └─> MCP (/health, /console/stream)
    │
    ├─> 컴파일 에러 체크?
    │       └─> CLI (check-compile-errors.ps1) 또는 MCP 둘 다 가능
    │
    ├─> 빌드?
    │       └─> CLI (batchmode)
    │
    ├─> 여러 씬/에셋 일괄 처리?
    │       └─> CLI (배치 작업)
    │
    └─> UI 상태 확인/스크린샷?
            └─> MCP
```

---

## 7. 일반적인 사용 시나리오

### 시나리오 1: 컴파일 에러 체크
```powershell
# CLI (에디터 꺼짐/상관없음)
.\tools\check-compile-errors.ps1

# MCP (에디터 켜짐)
GET /unity/compile-errors
```

### 시나리오 2: 스크린샷 캡처
```powershell
# CLI (어려움 - GameView 없음)
# -executeMethod로 가능하지만 복잡함

# MCP (간단)
POST /screenshot/capture
```

### 시나리오 3: CI/CD 빌드
```powershell
# CLI (유일한 방법)
& $unityPath -batchmode -buildTarget WebGL -quit
```

### 시나리오 4: Play Mode 테스트
```powershell
# CLI (불안정)
# -batchmode에서 Play Mode는 제한적

# MCP (안정적)
POST /play/start
POST /play/stop
```

---

## 8. 주의사항

- **batchmode는 그래픽 렌더링을 하지 않습니다** → 스크린샷 불가
- **에디터 API는 사용 가능** 하지만 런타임 렌더링은 안 됨
- **컴파일 에러가 있으면 `-executeMethod`가 호출되지 않을 수 있음**
- **로그는 stdout으로 출력하거나 파일로 저장** (`-logFile`)

---

## 관련 문서

- [editor-workflow.md](editor-workflow.md) - MCP 관련
- [serialization.md](serialization.md) - 직렬화 규칙
- [coding-rules.md](coding-rules.md) - 코딩 규칙
