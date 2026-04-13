# Unity MCP 개선 계획

> 생성일: 2026-04-13
> 상태: 초안

이 문서는 현재 Unity MCP 브리지의 기능을 **Playwright 스타일**의 범용 UI 탐색/제어 도구로 발전시키기 위한 개선 계획이다.

Playwright의 핵심 설계 원칙:
1. **Locator** — 요소 선택이 안정적이고 유연해야 한다
2. **Auto-wait** — 상태 변화를 기다리는 것이 기본 동작이어야 한다
3. **Snapshot** — 경량화된 상태 조회가 가능해야 한다
4. **Evaluate** — 런타임에서 객체 상태를 조회할 수 있어야 한다

이 원칙들을 Unity Editor 컨텍스트에 맞게 재해석한다.

---

## 현재 MCP 기능 요약

### 기존 엔드포인트 (완료)

| 그룹 | 엔드포인트 | 설명 |
|---|---|---|
| **Health** | `GET /health` | 브리지 상태, play, compile, active scene |
| | `GET /scene/current` | 현재 씬 정보 |
| **Scene** | `GET /scene/hierarchy` | 전체 씬 계층 구조 (JSON) |
| | `POST /scene/open` | 씬 열기 |
| | `POST /scene/save` | 씬 저장 |
| **GameObject** | `POST /gameobject/find` | path/name으로 찾기 |
| | `POST /gameobject/create` | GameObject 생성 (UI 프리셋 포함) |
| | `POST /gameobject/create-primitive` | 프리미티브 생성 |
| | `POST /gameobject/destroy` | GameObject 삭제 |
| | `POST /gameobject/set-active` | 활성/비활성 토글 |
| | `POST /gameobject/set-sibling` | 형제 순서 변경 |
| **Component** | `POST /component/add` | 컴포넌트 추가 |
| | `POST /component/set` | 직렬화 속성 값 설정 |
| | `POST /component/get` | 직렬화 속성 조회 |
| | `POST /component/set-serialized-field` | SerializedField 직접 연결 |
| | `POST /component/auto-connect-fields` | 필드명 기반 자동 연결 |
| **UI** | `POST /ui/button/invoke` | Button.onClick 실행 (play mode 전용) |
| | `POST /ui/create-button` | UI 버튼 생성 |
| | `POST /ui/create-panel` | UI 패널 생성 |
| | `POST /ui/create-raw-image` | UI RawImage 생성 |
| | `POST /ui/set-rect` | RectTransform 수정 |
| **Input** | `POST /input/click` | 마우스 클릭 |
| | `POST /input/move` | 마우스 이동 |
| | `POST /input/drag` | 마우스 드래그 |
| | `POST /input/key` | 키보드 키 입력 |
| | `POST /input/text` | 텍스트 입력 |
| | `POST /input/scroll` | 마우스 스크롤 |
| | `POST /input/key-combo` | 단축키 프리셋 (copy/paste 등) |
| **Prefab** | `POST /prefab/save` | GameObject를 프리팹으로 저장 |
| | `POST /prefab/get` | 프리팹 자산 조회 |
| | `POST /prefab/set` | 프리팹 컴포넌트 속성 설정 |
| | `POST /prefab/add-component` | 프리팹에 컴포넌트 추가 |
| **Play** | `POST /play/start` | Play mode 진입 |
| | `POST /play/stop` | Play mode 종료 |
| | `POST /screenshot/capture` | 스크린샷 캡처 |
| **Console** | `GET /console/errors` | 최근 에러 로그 |
| | `GET /console/logs` | 최근 전체 로그 |
| **Build** | `POST /asset/refresh` | AssetDatabase 새로고침 |
| | `GET /compile/status` | 컴파일 상태 확인 |
| | `POST /compile/request` | 스크립트 리로드 요청 |
| | `POST /compile/wait` | 컴파일 완료 대기 |
| | `POST /build/webgl` | WebGL 빌드 |
| | `POST /menu/execute` | Editor 메뉴 항목 실행 |
| | `GET /config/get` | MCP 설정 조회 |
| | `POST /config/set` | MCP 설정 변경 |

---

## Playwright vs Unity MCP 격차 분석

| Playwright 기능 | Unity MCP 현재 | 상태 |
|---|---|---|
| **Locator** (querySelector, getByRole, getByText) | path/name 기반 찾기만 가능 | ❌ 격차 큼 |
| **Auto-wait** (waitForSelector, waitForLoadState) | 대기 기능 전무 | ❌ 없음 |
| **Snapshot** (접근성 스냅샷, 경량 상태) | hierarchy 전체 JSON (무거움) | ⚠️ 부분 |
| **Evaluate** (런타임 JS 실행) | 런타임 메서드 호출 불가 | ❌ 없음 |
| **네트워크 인터셉트** | Unity 네트워크와 무관 | ✅ 해당 없음 |
| **프레임/iframe** | Prefab get/set으로 부분 커버 | ⚠️ 부분 |
| **스크린샷** | ✅ screenshot/capture | ✅ 동등 |
| **상호작용** (click, fill, type, key) | ✅ input/*, ui/button/invoke | ✅ 동등 |
| **페이지 탐색** (navigate, goBack) | ✅ scene/open, scene/current | ✅ 동등 |

---

## 개선 항목 (우선순위별)

### Phase 1: Locator 시스템 `[Editor-only]`

**문제:** 현재 `path` 탐색은 GameObject 이름이나 계층 경로에만 의존한다. UI 리팩토링 시 경로가 바뀌면 모든 호출이 깨진다.

**목표:** Playwright의 `locator()`처럼 **다양한 전략으로 요소를 선택**하고, **일치하는 요소를 필터링**하는 시스템을 도입한다.

> **1차 지원 selector:** `component:`, `name:`, `path:` 만 우선 구현.
> `text:`, `tag:`, `layer:`, `button` 단축 문법은 나중에 추가.

#### 새 엔드포인트

| 엔드포인트 | 태그 | 설명 |
|---|---|---|
| `POST /locator/find` | `[Editor-only]` | selector로 GameObject 찾기 |
| `POST /locator/find-all` | `[Editor-only]` | 일치하는 모든 GameObject 목록 |
| `GET /locator/count` | `[Editor-only]` | 일치하는 요소 수 |

#### Selector 문법 (Unity 전용)

```json
{
  "selector": "component:GarageSlotItemView",
  "scope": "/Canvas",
  "activeOnly": true
}
```

| Selector 패턴 | 의미 | 1차 지원 |
|---|---|---|
| `component:Button` | 컴포넌트를 가진 GameObject | ✅ |
| `name:SaveButton` | GameObject 이름이 정확히 일치 | ✅ |
| `path:/Canvas/SaveButton` | 기존 계층 경로 (하위 호환) | ✅ |
| `text:저장` | TMP_Text에 해당 텍스트 | ⏳ 나중에 |
| `tag:Player` | Unity 태그 일치 | ⏳ 나중에 |
| `layer:UI` | Unity 레이어 일치 | ⏳ 나중에 |

#### Scope 멀티씬 지원

| Scope 값 | 의미 |
|---|---|
| `"/Canvas"` | 지정 경로 하위만 검색 |
| `"scene:All"` | 로드된 모든 씬 (additive 포함) |
| `"scene:GameScene"` | 특정 씬 이름으로 제한 |
| 생략 | active scene 루트부터

#### 응답 형식

```json
{
  "found": true,
  "count": 1,
  "items": [
    {
      "path": "/Canvas/GaragePageRoot/ResultPane/SaveButton",
      "name": "SaveButton",
      "activeSelf": true,
      "activeInHierarchy": true,
      "components": ["RectTransform", "Image", "Button"]
    }
  ]
}
```

---

### Phase 2: 조건부 대기 (Auto-wait) `[Editor-only]`

**문제:** Play mode 진입 후 UI가 렌더링되기 전에 API를 호출하면 요소를 찾지 못한다. 현재는 호출자가 직접 poll해야 한다.

**목표:** 조건이 충족될 때까지 **자동으로 대기**하고, 타임아웃 시 명확한 에러를 반환한다.

#### 새 엔드포인트

| 엔드포인트 | 태그 | 설명 |
|---|---|---|
| `POST /wait/for-locator` | `[Editor-only]` | selector가 요소를 찾을 때까지 대기 |
| `POST /wait/for-active` | `[Editor-only]` | GameObject가 active해질 때까지 대기 |
| `POST /wait/for-component` | `[Editor-only]` | 특정 컴포넌트가 붙을 때까지 대기 |
| `POST /wait/for-scene` | `[Editor-only]` | 특정 씬이 로드될 때까지 대기 |
| `POST /wait/for-inactive` | `[Editor-only]` | GameObject가 inactive해질 때까지 대기 |

#### 요청 형식

```json
{
  "selector": "component:GarageResultPanelView",
  "timeoutMs": 10000,
  "pollIntervalMs": 100
}
```

#### 응답 형식

```json
{
  "success": true,
  "waitedMs": 1200,
  "condition": "locator-found",
  "result": {
    "path": "/Canvas/GaragePageRoot/ResultPane",
    "name": "ResultPane"
  }
}
```

타임아웃 시:
```json
{
  "success": false,
  "timedOut": true,
  "waitedMs": 10000,
  "condition": "locator-found",
  "hint": "Check if the UI has been loaded. Use GET /scene/hierarchy to inspect."
}
```

---

### Phase 3: 런타임 상태 조회 (Evaluate) `[Editor-only]` `[Play-mode-only]`

**문제:** UseCase 실행 결과, Domain 상태, Bootstrap의 내부 상태를 직접 조회할 방법이 없다.

**목표:** Playwright의 `page.evaluate()`처럼 **런타임에서 객체 상태를 조회**할 수 있게 한다.

> **제한:** public 필드 조회만 지원. 메서드 호출은 부작용/직렬화 문제로 인해 1차에서 제외.

#### 새 엔드포인트

| 엔드포인트 | 태그 | 설명 |
|---|---|---|
| `POST /eval/find-component` | `[Editor-only]` `[Play-mode-only]` | GameObject의 컴포넌트 찾기 + public 속성 조회 |
| `POST /eval/get-public-state` | `[Editor-only]` `[Play-mode-only]` | MonoBehaviour의 public 필드 값을 JSON으로 반환 |

#### 요청 형식 — public state 조회

```json
{
  "path": "/GarageSetup",
  "componentType": "GarageSetup",
  "fields": ["_eventBus", "_rosterListView", "_unitEditorView"]
}
```

#### 응답 형식

```json
{
  "success": true,
  "componentPath": "/GarageSetup",
  "componentType": "Features.Garage.Presentation.GarageSetup",
  "fields": {
    "_eventBus": "EventBus (refs: 5)",
    "_rosterListView": "GarageRosterListView (active: true)"
  }
}
```

**안전 규칙:**
- public 멤버만 조회 가능 (private/protected 리플렉션 금지)
- 반환 값은 JSON 직렬화 가능한 타입으로 제한 (string, int, float, bool, enum, Unity Object 이름)
- 호출 결과는 로그로 기록 (디버깅 용도)
- play mode에서만 의미 있는 결과 반환

---

### Phase 4: 경량 스냅샷 `[Editor-only]`

**문제:** `GET /scene/hierarchy`는 전체 씬을 JSON으로 뱉는다. CodexLobbyScene 기준 **수십 KB**에 달해서 자주 호출하기 부담스럽다.

**목표:** Playwright의 접근성 스냅샷처럼 **필요한 정보만 추린 경량 뷰**를 제공한다.

#### 새 엔드포인트

| 엔드포인트 | 설명 |
|---|---|
| `GET /snapshot/ui` | Canvas UI만 — 이름, 컴포넌트 타입, active 상태 |
| `GET /snapshot/components` | 루트 레벨 컴포넌트 요약 (Setup, Adapter, Network) |
| `POST /snapshot/diff` | 두 snapshot 비교 — 추가/삭제/변경된 요소 |

#### 응답 형식 — UI 스냅샷

```json
{
  "scene": "CodexLobbyScene",
  "canvasPath": "/Canvas",
  "uiNodes": [
    {
      "path": "/Canvas/GaragePageRoot",
      "name": "GaragePageRoot",
      "activeSelf": false,
      "childCount": 6,
      "views": ["GaragePageController", "GarageRosterListView"]
    },
    {
      "path": "/Canvas/GaragePageRoot/RosterListPane/GarageSlot1",
      "name": "GarageSlot1",
      "activeSelf": true,
      "childCount": 3,
      "views": ["Button", "GarageSlotItemView"]
    }
  ],
  "totalUiNodes": 42,
  "interactiveElements": 15
}
```

**규칙:**
- 기본 depth = 8 (설정 가능)
- MonoBehaviour만 views에 포함 (Unity 내장 컴포넌트는 제외)
- interactiveElements: Button, TMP_InputField, Toggle 수

---

### Phase 5: 대화형 요소 목록 `[Editor-only]`

**문제:** 씬에 어떤 UI가 있고, 어떤 버튼이 있는지 미리 알 수 없다. hierarchy를 읽어야만 파악 가능하다.

**목표:** **클릭 가능한 요소를 나열**하여 빠른 파악을 가능하게 한다.

> **축소:** `GET /explore/interactive` 하나로 축소. `traverse`는 Unity 게임의 동적 UI 특성상 신뢰하기 어려워 제외.

#### 새 엔드포인트

| 엔드포인트 | 태그 | 설명 |
|---|---|---|
| `GET /explore/interactive` | `[Editor-only]` | 클릭/입력 가능한 모든 UI 요소 목록 |

#### 응답 형식 — 대화형 요소 목록

```json
{
  "scene": "CodexLobbyScene",
  "interactiveElements": [
    {
      "path": "/Canvas/GaragePageRoot/TopTabs/GarageTabButton",
      "name": "GarageTabButton",
      "type": "Button",
      "text": "Garage",
      "interactable": true,
      "activeInHierarchy": true
    },
    {
      "path": "/Canvas/GaragePageRoot/RosterListPane/GarageSlot1",
      "name": "GarageSlot1",
      "type": "Button",
      "text": null,
      "interactable": true,
      "activeInHierarchy": true
    }
  ],
  "totalInteractive": 15,
  "byType": {
    "Button": 12,
    "TMP_InputField": 3
  }
}
```

---

## 구현 순서

```
Phase 1 (Locator)
  → McpSharedHelpers.FindBySelector() 추가 (component/name/path)
  → LocatorHandlers.cs 신규 파일
  → 기존 /gameobject/find 하위 호환 유지
  → scope 파라미터: path, scene:All, scene:<Name>

Phase 2 (Auto-wait)
  → WaitHandlers.cs 신규 파일
  → Polling 루프 + 타임아웃 처리
  → PlayModeChangeQueue와 통합 고려
  → polling 간격 기본 100ms, main thread block 금지

Phase 3 (Evaluate — 조회만)
  → EvalHandlers.cs 신규 파일
  → 리플렉션 기반 public 필드 접근만
  → private/protected 리플렉션 금지
  → play mode에서만 의미 있는 결과

Phase 4 (Snapshot)
  → SnapshotHandlers.cs 신규 파일
  → hierarchy 응답의 경량 서브셋
  → /snapshot/diff는 단순 JSON diff

Phase 5 (Explore — 목록만)
  → ExploreHandlers.cs 신규 파일
  → interactive 요소 스캔 (Button, TMP_InputField, Toggle)
  → traverse 제외 (동적 UI 특성상 신뢰 어려움)
```

---

## 가정 및 제약

1. **Editor 전용** — 모든 새 엔드포인트는 `#if UNITY_EDITOR` 범위 내에서 동작
2. **태그 규칙**:
   - `[Editor-only]` — Editor에서만 의미 (대부분의 새 엔드포인트)
   - `[Play-mode-only]` — Play mode에서만 의미 있는 결과 (`/eval/*`)
   - 태그 없는 엔드포인트는 에디터/플레이 모드 모두에서 동작
3. **하위 호환** — 기존 엔드포인트는 수정하지 않고 추가만 한다
4. **멀티씬** — `scope` 파라미터로 additive 씬의 요소도 조회 가능
5. **성능** — polling 간격은 기본 100ms, hierarchy 스캔은 depth 제한 필수, main thread block 금지
6. **보안** — private 리플렉션 금지, public 필드 조회만 허용
7. **문서** — 새 엔드포인트는 CLAUDE.md의 작업별 진입 경로에 반영하지 않음 (런타임 조회용이므로)

### 에러 응답 형식 표준

모든 새 엔드포인트는 통일된 에러 형식을 따른다:

```json
{
  "error": {
    "code": "TIMEOUT",
    "message": "Locator not found within 10000ms",
    "detail": "selector=component:GarageResultPanelView, scope=/Canvas"
  }
}
```

| 에러 코드 | 의미 |
|---|---|
| `NOT_FOUND` | 대상 GameObject/컴포넌트가 없음 |
| `TIMEOUT` | 대기 시간 초과 |
| `INVALID_SELECTOR` | selector 문법 오류 |
| `PLAY_MODE_REQUIRED` | play mode 필수 엔드포인트 |
| `SERIALIZATION_ERROR` | JSON 직렬화 실패 |

---

## 파일 구조 변경 (예정)

```
Assets/Editor/UnityMcp/
  UnityMcpBridge.cs          — 변경 없음 (라우팅은 EndpointRegistry가 처리)
  EndpointRegistry.cs        — 변경 없음
  Models.cs                  — 새 Request/Response 타입 추가
  McpSharedHelpers.cs        — FindBySelector() 메서드 추가
  Handlers/
    LocatorHandlers.cs       — [신규] Phase 1
    WaitHandlers.cs          — [신규] Phase 2
    EvalHandlers.cs          — [신규] Phase 3 (조회만)
    SnapshotHandlers.cs      — [신규] Phase 4
    ExploreHandlers.cs       — [신규] Phase 5 (목록만)
    BuildHandlers.cs         — 변경 없음
    ComponentHandlers.cs     — 변경 없음
    ConsoleHandlers.cs       — 변경 없음
    GameObjectHandlers.cs    — 변경 없음
    InputHandlers.cs         — 변경 없음
    PlayHandlers.cs          — 변경 없음
    PrefabHandlers.cs        — 변경 없음
    SceneHandlers.cs         — 변경 없음
    UiHandlers.cs            — 변경 없음
```

---

## 검토 항목

- [x] Selector 문법 → 1차 `component:`, `name:`, `path:` 만 지원 (나머지는 나중에)
- [x] Auto-wait 타임아웃 기본값 (10초) + poll 간격 100ms
- [x] Eval → public 필드 조회만, 메서드 호출 제외
- [x] Snapshot depth 기본값 (8) — CodexLobbyScene 최대 depth 6이므로 충분
- [x] Explore → `GET /explore/interactive` 하나로 축소 (traverse 제외)
- [x] 에러 응답 형식 표준 (`code`, `message`, `detail`)
- [x] `[Editor-only]`, `[Play-mode-only]` 태그 규칙 명시
- [x] 멀티씬 scope 지원 (`scene:All`, `scene:<Name>`)

---

## 배운 교훈

### Unity 메인 스레드 제약 (Phase 2)

MCP 핸들러는 `HttpListener` thread pool에서 실행되므로, `async/await` 이후 Unity API를 직행 호출하면 `UnityException`이 발생한다.

```
UnityException: GetName can only be called from the main thread.
```

**해결:** `WaitForConditionAsync`에서 condition 실행과 결과 추출을 완전히 분리.
- condition은 호출하는 쪽에서 `RunOnMainThreadAsync`로 감싸서 반환
- `Task.Delay` 이후 절대 Unity API 호출 금지
- 결과 객체(`LocatorItem`) 생성도 별도 `RunOnMainThreadAsync`로 감싸기

자세한 패턴은 `/agent/unity.md` 섹션 14 참조.

### 사소한 컴파일 에러로 인한 지연

아래 사소한 에러들이 MCP 브리지 재시작 실패로 이어져 디버깅이 어려웠다:

| 에러 | 원인 |
|---|---|
| `CS1022: end-of-file expected` | `enum` 키워드를 타입처럼 사용 (`Enum`이 정확함) |
| `CS0103: EditorApplication does not exist` | `using UnityEditor;` 누락 |
| `CS1061: WaitRequest does not contain scope` | Models.cs에 필드 추가 누락 |
| `CS0234: System.Reflection.Instance does not exist` | `BindingFlags.Instance` → `BindingFlags.Public \| BindingFlags.Instance` |

**교훈:** 새 핸들러 파일 추가 시 컴파일 에러가 있으면 MCP 브리지 자동 재시작이 실패한다. 에러 로그를 먼저 확인하고, Unity `InitializeOnLoad`가 컴파일 완료 후 자동으로 브리지를 재시작하므로 파일 수정만 정확히 하면 된다.
