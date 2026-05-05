---
name: rule-unity
description: "Generic Unity reference. Triggers: meta GUID, scene serialization, Unity API, MCP/CLI mechanics, batchmode build, compile diagnostics fallback."
---

# Unity

Unity 전용 규칙: 직렬화, 코딩 규칙, 에디터 워크플로우 (MCP/CLI).

JG repo에서는 `AGENTS.md`, `docs/index.md`, `jg-unity-workflow`, Unity owner docs를 먼저 확인한다.
이 skill은 generic Unity serialization/MCP/CLI fallback/reference다.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| meta GUID · 씬 직렬화 | [serialization.md](serialization.md) - 스크립트 리네임 시 meta GUID 보존, 씬 직렬화 계약 |
| 런타임 탐색 · 의존성 필드 | [coding-rules.md](coding-rules.md) - 의존성 필드 규칙, 런타임 탐색 정책 |
| MCP · 에디터 워크플로우 | [editor-workflow.md](editor-workflow.md) - Unity MCP 운영, 컴파일 에러 확인, 플레이 모드 |
| CLI · Batchmode · 빌드 | [cli-workflow.md](cli-workflow.md) - Unity CLI 사용법, 빌드, 컴파일 에러 체크, 배치 작업 |

---

## 적용 영역

### 적용됨
- `Assets/Scripts/Features/**` — 게임 로직 코드
- `Assets/Scripts/Shared/Infrastructure/**` — 인프라 코드 (일부 규칙 제한적 적용)

### 제외됨
- `Assets/Editor/**` — Editor 전용 코드
- `Assets/FromStore/**` — 외부 라이브러리 (Photon, DOTween 등)
- `Assets/Editor/UnityMcp/**` — MCP 브리지

> **참고:** Scene 파일(`.unity`)과 Prefab 파일(`.prefab`)은 YAML 형식이므로 C# 규칙과 별도로 검증 필요 |

---

## 하위 문서 읽어오기

사용자가 다음 주제에 대해 질문하면 해당 하위 문서를 읽어와서 답변하세요:

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| meta GUID, 씬 직렬화, 스크립트 리네임, meta GUID 보존, 씬 직렬화 계약 | [serialization.md](serialization.md) |
| 의존성 필드, 런타임 탐색 정책, 의존성 필드 규칙 | [coding-rules.md](coding-rules.md) |
| Unity MCP, 컴파일 에러 확인, 플레이 모드, 에디터 워크플로우 | [editor-workflow.md](editor-workflow.md) |
| Unity CLI, batchmode, 빌드, 컴파일 에러 체크, executeMethod, CI/CD | [cli-workflow.md](cli-workflow.md) |

---

## CLI vs MCP 빠른 참조

| 작업 | 추천 | 이유 |
|------|------|------|
| 컴파일 에러 체크 | CLI | 에디터 없이도 확인 가능 (`check-compile-errors.ps1`) |
| 빌드 (WebGL, Android 등) | CLI | batchmode가 목적에 맞음 |
| 스크린샷 캡처 | MCP | GameView 렌더링 필요 |
| Play Mode 실행/제어 | MCP | 안정적, 실시간 상태 확인 |
| 실시간 로그 스트리밍 | MCP | `/console/stream` 사용 |
| 여러 씬/에셋 일괄 처리 | CLI | 배치 작업에 유리 |
| UI 상태 확인 | MCP | 실제 화면 필요 |
| CI/CD 파이프라인 | CLI | 그래픽 없는 환경 |
