# /agent/unity.md

Unity Editor 전용 작업 규칙. Unity 고유 직렬화, 에디터 조작, 에셋 관리에 따른 실수 방지 목적.

---

## 1. Unity 스크립트 리네임 시 meta GUID 보존

**규칙:** `.cs` 파일 이름을 변경할 때 동일한 `.cs.meta` 파일의 `guid` 값을 절대 변경하지 않는다.

**이유:** Unity는 `.meta`의 GUID로 스크립트 컴포넌트를 식별한다. GUID가 바뀌면 모든 씬(`.unity`), 프리팹(`.prefab`), Inspector 참조가 끊어진다. (실제 사례: `*Bootstrap.cs` → `*Setup.cs` 리네임 후 씬 연결 전체 손실)

**작업 순서 (Unity Editor 사용 시 — 권장):**
- Project 뷰에서 파일 선택 → F2 (Unity가 meta GUID 자동 유지)

**작업 순서 (Unity Editor 없을 때):**
1. `OldName.cs` → `NewName.cs` 파일명 변경
2. `OldName.cs.meta` → `NewName.cs.meta` 파일명 변경 (**내용 수정 금지**)
3. `.meta` 내부 `guid` 값이 원본과 완전히 동일함을 검증
4. 관련 `.unity` 씬 파일에서 해당 `guid` 참조가 유지되었음을 확인

**검증:** 리네임 후 `.meta` 파일의 `guid: xxx`가 변경 전과 동일한지 diff로 확인. 다르면 즉시 복구.

---

## 2. 씬 직렬화 계약

**규칙:** Inspector에서 `[Required, SerializeField]`로 연결된 참조만 신뢰한다.

- ✅ `[Required, SerializeField]` — Editor가 씬/프리팹 저장 시 자동 검증
- ❌ `GetComponent` / `FindObjectOfType` / `FindObjectsByType` — 런타임 탐색 금지
- ❌ 누락된 참조를 런타임에 폴백으로 복구 — 직접 씬/프리팹을 수정
- ❌ 런타임 UI 생성으로 누락된 씬 요소 대체 — scene/prefab 소유 원칙

**YAML 직렬화 읽기:**
- `.unity` / `.prefab` 파일은 YAML 형식
- `m_Script: {fileID: xxx, guid: yyy, type: z}` — 이 guid가 meta의 guid와 매핑
- `Missing (MonoScript)` = guid가 가리키는 .cs.meta가 존재하지 않음

### 2.1 열려 있는 scene/prefab 외부 수정 금지

**규칙:** Unity Editor에서 현재 열려 있는 `.unity` / `.prefab` 에셋은 에디터 외부에서 직접 수정하지 않는다.

- ✅ scene/prefab wiring 변경은 Unity MCP 또는 에디터 내부 작업을 우선 사용
- ✅ 외부 YAML 편집이 필요하면 먼저 사용자에게 대상 scene/prefab을 닫거나 다른 scene으로 전환하도록 요청
- ✅ 대상 자산이 계속 열려 있으면 직접 수정하지 않음
- ✅ 예외가 필요하면 reload 영향과 미저장 변경 손실 가능성을 설명하고 사용자 확인 후 진행
- ❌ Unity가 열어 둔 scene/prefab을 디스크에서 직접 패치
- ❌ reload popup을 무시한 채 직렬화 자산 수정 지속

**이유:** Unity가 메모리에 들고 있는 scene/prefab과 디스크 파일이 어긋나면 reload popup이 뜨고, 사용자의 미저장 변경이 손실될 수 있다.

**기본 판단 순서:**
1. Unity MCP/에디터 내부에서 처리 가능한지 먼저 확인
2. 불가능하면 사용자에게 대상 scene/prefab을 닫거나 다른 scene으로 전환하도록 요청
3. 대상 자산이 계속 열려 있으면 직접 수정하지 않음
4. 예외가 필요하면 사용자 확인 후 진행

**검증:** 직접 YAML 편집 후에는 `git diff --check`로 직렬화 공백/형식 문제를 확인하고, Unity에서 reload/reimport 후 참조가 유지되는지 검증한다.

**MCP 확인 작업:** scene/prefab/UI를 MCP로 확인한 경우 가능하면 종료 직전 `/screenshot/capture`로 화면을 남기고 실제 결과를 확인한다.

---

## 3. 프리팹 연결 규칙

**규칙:** 프리팹 루트에 Setup/Bootstrap만 연결, 자식 컴포넌트는 자체 해결.

- ✅ 루트 프리팹: `[SerializeField] private XxxSetup` — Composition Root 역할
- ✅ 자식 프리팹: 내부에서 `[Required, SerializeField]`로 자기 참조 해결
- ❌ 루트에서 자식의 private 필드를 외부에서 연결 — 결합도 증가
- ❌ 프리팹 간 상호 참조 — scene wiring에서 조립

---

## 4. AssetDatabase / Editor 스크립트

**규칙:** `Assets/Editor/` 브리지 코드는 최소한으로 유지.

- Editor 전용 코드는 `#if UNITY_EDITOR` 가드로 감싼다
- AssetDatabase 조작 후 `AssetDatabase.Refresh()` 호출
- Editor 스크립트 변경은 런타임 빌드에 영향 없음을 확인

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

**용도:** Unity Editor 안의 로컬 HTTP 브리지를 통해 scene, prefab, play mode, 로그, 스크린샷을 원격 제어/확인한다.

**접속:**
- 주소: `http://127.0.0.1:{port}/`
- 포트: `ProjectSettings/UnityMcpPort.txt`
- stdio 서버: `tools/unity-mcp/server.js`
- 기본 HTTP 타임아웃: `10000ms` (`UNITY_MCP_HTTP_TIMEOUT_MS`로 덮어쓰기 가능)

**기본 순서:**
1. `GET /health`로 브리지, scene, `isCompiling`, `isPlaying` 확인
2. 작업 전 `Play Stop -> 파일 수정 -> 컴파일 완료 확인 -> /health 확인`
3. 주요 액션 뒤 `GET /console/logs` 또는 `GET /console/errors` 확인
4. hierarchy/scene 문제는 `GET /scene/hierarchy` 또는 `tools/mcp-diagnose-scene-hierarchy.ps1` 사용
5. 종료 직전 가능하면 `/screenshot/capture`로 화면 저장 후 실제 결과 확인

**운영 규칙:**
- MCP 테스트는 화면만 보지 말고 로그까지 같이 확인
- runtime UI flow 회귀를 hardcoded 자동 스크립트로 굳히지 않음
- 공식 기록은 `docs/playtest/runtime_validation_checklist.md`에 남기고, MCP 입력 라우트는 일회성 수동 진단에 사용

**주요 엔드포인트:**
- 조회 `GET`: `/health`, `/scene/current`, `/console/errors`, `/console/logs`, `/scene/hierarchy`, `/compile/status`
- 플레이/컴파일/빌드 `POST`: `/scene/open`, `/scene/save`, `/play/start`, `/play/stop`, `/screenshot/capture`, `/compile/request`, `/compile/wait`, `/build/webgl`
- 입력/UI `POST`: `/input/click`, `/input/move`, `/input/drag`, `/input/key`, `/input/text`, `/input/scroll`, `/input/key-combo`, `/ui/button/invoke`
- 씬/오브젝트/프리팹 편집 `POST`: `/gameobject/find`, `/gameobject/create`, `/gameobject/create-primitive`, `/gameobject/destroy`, `/gameobject/set-active`, `/component/add`, `/component/set`, `/component/get`, `/prefab/save`, `/prefab/get`, `/prefab/set`, `/prefab/add-component`, `/asset/refresh`
- 에디터 유틸리티 `POST`: `/menu/execute`

**자주 쓰는 요청 형식:**
```json
{ "scenePath": "Assets/Scenes/ExampleScene.unity", "saveCurrentSceneIfDirty": true }
```
`POST /scene/open`

```json
{ "outputPath": "Temp/UnityMcp/Screenshots/runtime-check.png", "superSize": 1, "overwrite": true }
```
`POST /screenshot/capture`

```json
{ "requestFirst": true, "cleanBuildCache": false, "timeoutMs": 300000, "pollIntervalMs": 100 }
```
`POST /compile/wait`

```json
{ "path": "/Canvas/TopTabs/GarageTabButton" }
```
`POST /ui/button/invoke`

---

## 7. 프리팹 인스턴스화 후 초기화

**규칙:** `PhotonNetwork.Instantiate` 또는 `Instantiate`로 생성된 객체는 명시적인 `Initialize()` 호출로 의존성을 주입한다.

- ✅ `setup.Initialize(eventBus, ...)` — 명시적 주입
- ❌ `FindObjectOfType`로 의존성 탐색 — 런타임 탐색 금지
- ❌ 정적 이벤트로 암묵적 초기화 — 우발적 타이밍 의존

---

## 8. 런타임 탐색 정책

**규칙:** 런타임 탐색은 scene/prefab 계약을 대체할 수 없다.

**기본 금지:**
- 의존성 획득을 위한 `GetComponent`
- 누락된 의존성 복구를 위한 `AddComponent`
- `FindObjectOfType` / `FindObjectsByType`
- `Resources.FindObjectsOfTypeAll`
- 런타임 객체 발견을 위한 scene scan

**허용 예외 (모든 조건을 만족해야 함):**
- 동일 GameObject 내부 helper 탐색만
- 일회성 획득만
- Unity/Photon이 해당 사례를 inspector에서 연결할 수 없어서 필요한 경우
- 사용 사이트에 짧은 주석으로 정당화 또는 예외가 지속되면 전역 규칙 문서에 기록
- 누락된 scene 참조의 폴백으로 사용 금지

**DDOL singleton (Shared 인프라 전용):**
- 기본: feature 코드에 새로운 정적 `Instance` + `DontDestroyOnLoad` 패턴을 도입하지 않는다.
- 허용: 프로세스 전체 서비스가 필요한 `Shared/**` 인프라에만. 예: `SoundPlayer.Instance` — 오디오 루트 하나, lobby scene에서 생성, `Initialize`가 씬별 `EventBus`를 재바인딩. 첫 scene이 `[Required, SerializeField]`로 연결; 이후 scene은 `Instance`를 재바인딩에만 사용하고, scene `Find*`에는 사용 금지.
- 금지: feature Presentation/Infrastructure에 임의의 DDOL singleton.

---

## 9. Scene 계약

**규칙:** scene 소유 feature는 `Setup`/`Bootstrap`, 직렬화 scene/prefab 참조, 관련 코드 경로에서 scene 계약을 명시적으로 유지해야 한다.

필요한 scene 계약 체크리스트는 `architecture.md`가 소유한다. 로컬 문서에서 이 체크리스트를 재정의하지 않는다. 실제 코드/scene 연결을 최신 상태로 유지하고 필요시 architecture 규칙을 참조.

---

확실하지 않을 때:

Shared보다 현재 feature 안에 코드를 유지하는 것을 선호한다.

---

## 10. 의존성 필드 규칙

**의존성 필드 멤버 3가지 규칙** (`[SerializeField]`, 주입된 참조):

1. **한 번에 수신**: constructor 또는 `Initialize()`에서만. 별도 setter 메서드 금지 (예: `SetXxx()`).
2. **올바른 레이어에서 검증**: `[Required, SerializeField]` 필드는 Editor가 씬/프리팹 저장 시 검증하므로 런타임 null-check 불필요. `Initialize()` 파라미터 등 Inspector에서 검증할 수 없는 값만 런타임에 `Debug.LogError`로 검증한다.
3. **초기화 후 신뢰**: 이벤트 핸들러, Update, 기타 런타임 메서드에서 재검증 금지. 런타임에 null이면 초기화 버그 — NullReferenceException이 표면화되도록 둔다.

**`[Required]` 범위 규칙:**
`[Required]`는 Inspector에서 연결되는 **참조 의존성**에만 사용한다. `bool`, `int`, `float`, `enum`, `Color`, `Vector*`, `string` 같은 스칼라/config 값에는 붙이지 않는다. 스칼라 값 검증은 `Range`, `Min`, 도메인 검증, 전용 validator로 처리한다.

---

## 11. 타입 enum 기반 조건 분기

enum 기반 switch로 타입별 행동을 분기하지 않는다. Factory + Strategy 패턴을 사용한다.

**허용:** 명령 디스패치와 단순 값 매핑은 switch 허용.

**Strategy 패턴 파일 구조:**
- enum, interface, factory는 한 파일에.
- 각 구현체(Strategy 클래스)는 개별 파일.

---

## 12. 정적 이벤트 규칙

정적 이벤트는 엔진/네트워크 콜백을 Application 또는 Bootstrap으로 bridge할 때만 예외적으로 허용한다. gameplay event bus 대체제로 사용하지 않는다. 사용 시 `OnDestroy` 해제와 README 명시는 필수다.

---

## 13. 운영 환경 런타임 UI 생성

운영용 UI는 scene 소유 또는 prefab 소유여야 한다. 런타임 UI 생성은 디버깅 도구 또는 일시적 마이그레이션에서만 허용한다.
