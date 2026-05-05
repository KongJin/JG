# Unity 직렬화 규칙

Unity 씬, 프리팹, 에셋의 직렬화와 meta 파일 관리 규칙.

> **참고:** Scene 파일(`.unity`)과 Prefab 파일(`.prefab`)은 YAML 형식이므로 C# 규칙과 별도로 검증 필요. 적용 영역은 [SKILL.md](SKILL.md#적용-영역) 참조.

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
   ```powershell
   # Unity MCP health 확인
   $response = Invoke-WebRequest "http://127.0.0.1:51234/health"
   $response | ConvertFrom-Json
   # 응답에 "bridge", "isPlaying", "isCompiling" 확인
   ```
2. 불가능하면 사용자에게 대상 scene/prefab을 닫거나 다른 scene으로 전환하도록 요청
3. 대상 자산이 계속 열려 있으면 직접 수정하지 않음
4. 예외가 필요하면 사용자 확인 후 진행

> **참고:** Unity MCP 사용법은 [editor-workflow.md](editor-workflow.md) "Unity MCP 운영" 섹션 참조

**검증:** 직접 YAML 편집 후에는 `git diff --check`로 직렬화 공백/형식 문제를 확인하고, Unity에서 reload/reimport 후 참조가 유지되는지 검증한다.

**MCP 확인 작업:** scene/prefab/UI를 MCP로 확인한 경우 가능하면 종료 직전 `/screenshot/capture`로 화면을 남기고 실제 결과를 확인한다.

---

## 3. 프리팹 연결 규칙

**규칙:** 프리팹 루트에 Setup/Bootstrap만 연결, 자식 컴포넌트는 자체 해결.

- ✅ 루트 프리팹: `[SerializeField] private XxxSetup` — Composition Root 역할
- ✅ 자식 프리팹: 내부에서 `[Required, SerializeField]`로 자기 참조 해결
- ❌ 루트에서 자식의 private 필드를 외부에서 연결 — 결합도 증가
- ❌ 프리팹 간 상호 참조 — scene wiring에서 조립
