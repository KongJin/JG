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

자세한 MCP 호출·로그 확인 SOP는 `/docs/ops/unity_mcp.md`를 따른다.

---

## 6. 프리팹 인스턴스화 후 초기화

**규칙:** `PhotonNetwork.Instantiate` 또는 `Instantiate`로 생성된 객체는 명시적인 `Initialize()` 호출로 의존성을 주입한다.

- ✅ `setup.Initialize(eventBus, ...)` — 명시적 주입
- ❌ `FindObjectOfType`로 의존성 탐색 — 런타임 탐색 금지
- ❌ 정적 이벤트로 암묵적 초기화 — 우발적 타이밍 의존

---

## 7. 런타임 탐색 정책

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

## 8. Scene 계약

**규칙:** scene 소유 feature는 `Setup`/`Bootstrap`, 직렬화 scene/prefab 참조, 관련 코드 경로에서 scene 계약을 명시적으로 유지해야 한다.

필요한 scene 계약 체크리스트는 `architecture.md`가 소유한다. 로컬 문서에서 이 체크리스트를 재정의하지 않는다. 실제 코드/scene 연결을 최신 상태로 유지하고 필요시 architecture 규칙을 참조.

---

확실하지 않을 때:

Shared보다 현재 feature 안에 코드를 유지하는 것을 선호한다.
