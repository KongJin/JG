# Work Principles

> 마지막 검토: 2026-04-21

## 목적

JG repo에서는 `AGENTS.md`와 `docs/index.md`로 current owner를 먼저 확인한다.
이 문서는 generic 운영 fallback/reference이며, repo owner docs가 우선한다.

이 문서는 온보딩 문서가 아니다.
문서 소유권, SSOT 운영, 응집도 기준만 다루며, 구현 규칙 자체는 각 주제의 SSOT에서 본다.

코드뿐 아니라 **문서·씬·프리팹·자동화·분석**까지, **응집도**와 **단방향 참조(DAG)**를 유지하는 기준을 한곳에 모은다.  
레이어·의존·폴더·네이밍 등 코드 규칙은 repo owner docs를 우선하고, fallback으로 [`rule-architecture`](../rule-architecture/SKILL.md)를 따른다. 이 파일은 **작업 단위·정보 출처**에 초점을 둔다.

---

## 핵심 개념

- **응집도**: 한 덩어리(피처, 문서, 스크립트, 씬에 묶인 오브젝트)가 **같은 이유로** 바뀌는가. 서로 다른 변경 이유를 한 파일·한 폴더에 섞지 않는다.
- **단방향 참조**: A→B이면 B가 A를 **되돌려 인용하지 않는다**. 문서·도구 간 **순환 링크**와 **이중 전체 서술**을 피한다. “진실(SSOT)”은 주제마다 한 곳에만 둔다.

---

## 주제별 SSOT (예시)

| 주제 | 단일 근거 |
|------|-----------|
| 아키텍처·폴더·레이어·의존·네이밍·피처 경계 | JG repo: `docs/index.md`에서 current owner 확인; fallback: [`rule-architecture`](../rule-architecture/SKILL.md) |
| 게임 컨셉 | JG repo: `docs/index.md`에서 current design owner 확인 |
| 재미·리스크 워크숍 | JG repo: current discussion/design owner 확인. 수치·MVP 정의는 design owner만 SSOT |
| MVP 재미 검증 실행 분해 | JG repo: current plan/design owner 확인. 수치·통과 기준·MVP 문장은 design owner만 SSOT |
| MVP 플레이테스트 세션 노트 | JG repo: current playtest/design owner 확인. 측정 정의·통과 기준은 design owner만 SSOT |
| 네트워크 키·소유권 | 키를 실제로 쓰는 feature code + current architecture owner |
| 피처별 씬·초기화 | 해당 feature의 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 계약 |
| 에디터 MCP 등 자동화 계약 | JG repo owner docs 우선; fallback: [`rule-unity`](../rule-unity/SKILL.md) |
| Codex의 repo-tracked 파일 수정 운영 | [codex_patching.md](codex_patching.md) |
| 스킬 분류 태그와 현재 코드 매핑 | 실제 feature code + current design owner. 수치·MVP 정의는 design owner만 SSOT |
| 제품·배포·기밀 맥락 | JG repo owner docs 우선; fallback: [`rule-context`](../rule-context/SKILL.md) |

같은 사실(예: CustomProperty 의미)을 **두 문서에 풀어서 정의하지 않는다**. 보조 문서에는 한 줄 위임만 두고, **역참조로 순환이 생기지 않게** 한다.
엔트리포인트 문서(`AGENTS.md` 등)는 **찾아가는 길과 작업 순서만 요약**하고, 규칙 본문은 해당 SSOT에만 둔다.

---

## 작업 레벨별 응집도

- **씬/프리팹**: 요구 오브젝트·참조·순서는 **해당 피처의 Setup/Bootstrap와 실제 serialized contract**에만 남긴다. 같은 내용을 별도 README로 중복 서술하지 않는다.
- **자동화 스크립트 / MCP**: 입력(경로, 대상)을 명시하고, **비즈니스 규칙을 스크립트 안에서 새로 정의하지 않는다**. 규칙은 도메인·Application 또는 전역 규칙 문서에 두고, 도구는 적용·검증만 담당한다.
- **분석(Firebase 등)**: 이벤트 이름·의미는 **코드 상수 한 곳 또는 문서 한 곳**에만 두고 대시보드와 이중 정의하지 않는다.
- **중간 계약 레이어**: 임시 contract, 추출 전 draft, staging artifact는 실행 계약과 같은 owner로 승격하지 않는다. `pending-source-derivation`처럼 아직 닫히지 않은 상태는 명시적으로 기록하고, translation-ready truth와 분리한다.

---

## Plan 모드 운영 규칙

### Plan Mode precedence

- system/developer가 지정한 collaboration mode는 자율 실행 기본값보다 항상 우선한다.
- 사용자 요청이 실행형이어도 현재 모드가 `Plan Mode`면 그 요청은 **실행 계획 요청**으로 재해석한다.
- Plan Mode에서는 규칙 문서, 기존 코드, 로컬 환경으로 답을 찾는 탐색을 먼저 하고, 그래도 답이 없으면 질문한다.

### 허용/금지 경계

Plan Mode에서는 **non-mutating 탐색만 허용**한다.

허용:

- 읽기, 검색, 정적 점검
- build/test/check처럼 repo-tracked 파일을 바꾸지 않는 검증
- 현재 구조와 owner 문서를 확인하기 위한 로컬 명령

금지:

- `apply_patch`
- 파일 생성, 삭제, 이동, rename
- formatter rewrite, codegen rewrite, migration apply
- scene/prefab 수정
- generated `.csproj` 직접 수정
- 결과 artifact를 새로 써서 성공 근거처럼 남기는 행위
- 커밋, 브랜치 정리, repo-tracked state mutation 전반

**AI가 스스로 판단해 진행하는 경우:**
- 규칙 문서(`agent/*.md`)에 명시된 패턴을 따르는 경우
- 기존 코드 컨벤션, 네이밍, 폴더 구조를 그대로 따르는 경우
- 사소한 변수명, 타입 선택, 리팩터링으로 제품 방향에 영향이 없는 경우

**반드시 질문하는 경우:**
- 아키텍처 변경 (새 레이어 추가, 계층 위반, 패턴 변경)
- 되돌리기 어려운 결정 (DB 스키마, API 계약, 씬 구조 대수정)
- 여러 유효한 대안이 있고 제품 방향·UX에 영향
- 규칙 문서에 명시가 없고 기존 코드에도 선례가 없는 경우

**예시:**
- ✅ AI 자율: "기존 Feature가 `Domain/ErrorCodes.cs` 패턴을 따르므로 동일 위치에 추가"
- ✅ AI 자율: "`Debug.Log` → `Log.Info` 치환 — `logging_rules.md`에 명시된 패턴"
- ❌ 질문 필요: "에러 코드를 enum에서 string으로 바꿀까요, 제네릭 유지할까요?" (아키텍처 판단)
- ❌ 질문 필요: "Google 로그인 실패 시 재시도 UI를 모달로 할까요, 인라인으로 할까요?" (UX 판단)

### 결과 보고 언어 규칙

- mutation이 없던 턴은 `검토했다`, `탐색했다`, `계획을 정리했다`처럼 탐색형 표현만 쓴다.
- `고쳤다`, `반영했다`, `통과했다`는 실제 수정 또는 실행 증거가 있을 때만 쓴다.
- dirty worktree가 이미 있으면 이번 턴 변경과 기존 변경을 분리해서 설명한다.
- Plan Mode closeout은 구현 완료 보고가 아니라, 현재 상태와 다음 mutation-ready 결정을 명확히 남기는 데 집중한다.

---

## 피할 패턴

- 문서 A↔B에 **동일 결정을 각각 풀어 쓴다** → 한쪽만 고쳐져 이중 진실이 된다.
- 런타임/스크립트로 씬을 **조용히 고쳐** README 씬 계약과 어긋난다 → [`rule-patterns/anti_patterns.md`](../rule-patterns/anti_patterns.md)의 배선 규칙과 충돌한다.
- 에이전트 채팅에만 규칙을 두고 레포를 갱신하지 않는다 → **결정은 SSOT에 반영**한다.
- 임시 추출값이나 손보정 literal을 "이제 실행된다"는 이유만으로 execution contract에 올린다 → 다음 패스부터 source provenance와 runtime truth가 함께 무너진다.

---

## 체크리스트 (사람·AI 공통)

1. 이번 변경의 **이유가 하나**인가? (무관한 문서+코드+씬을 한 커밋에 섞지 않기.)
2. 이 정보의 **주인 문서**는 어디인가?
3. 새 링크가 **문서 순환**을 만드는가?
4. 피처 간에는 **포트·이벤트 이름**으로 경계를 두고, 타 피처 README에 장문으로 "상대 구현"을 적지 않는다.

---

## 코드·문서 동기화 규칙

### 원칙: 코드写完 → 문서도 같이 쓴다

코드 변경이 **기능·상태·진행률**에 영향을 줄 때, 관련된 문서도 **동일 커밋 또는 직후 커밋**에서 반드시 업데이트한다.
"코드는 작동하는데 문서가 틀린" 상태는 **정적 규칙 위반과 동급**으로 취급한다.

### 적용 대상 (폴더 패턴 기준)

문서 목록은 폴더 패턴으로 관리한다. 개별 파일명을 열거하지 않는다 — 새 파일이 생겨도 규칙이 자동으로 적용되어야 한다. (개방-폐쇄 원칙)

| 경로 패턴 | 언제 업데이트 | 누가 |
|---|---|---|
| `docs/plans/` | 작업 범위, Phase 상태, 진행률 변경 시 | 코드 작성자 (AI 포함) |
| `docs/design/` | 설계 결정, MVP 범위 변경, 폐기 기능 명시 시 | 코드 작성자 (AI 포함) |
| `docs/owners/operations/` | JG repo 운영/배포/도구 설정 변경 시 | 코드 작성자 (AI 포함) |
| `docs/discussions/` | 토론/의사결정 기록 시 | 코드 작성자 (AI 포함) |
| `docs/playtest/` | 플레이테스트 템플릿/결과 기록 시 | 코드 작성자 (AI 포함) |
| repo-local/imported skills | 규칙 자체 변경, 새 금지 패턴 발견, SSOT 소유권 변경 시 | 코드 작성자 (AI 포함) |
| session summary artifact | 별도 세션 메모가 존재하고 사용자가 요구할 때만 갱신. 공식 진행률 SSOT 아님 | AI 에이전트 |
| `AGENTS.md` | 엔트리포인트 경로/참조 구조 변경 시 | 코드 작성자 (AI 포함) |
| `**/README.md` | 피처 로컬 wiring 변경, 직렬화 참조 변경, 포트 추가/삭제 시 | 코드 작성자 (AI 포함) |

### 최소 업데이트 항목

코드/문서 변경 후 아래 세 가지를 먼저 점검한다:

1. **진행 상태** — Phase 완료/진행률, 남은 TODO가 바뀌면 `progress.md` 갱신
2. **설계/결정 근거** — 제품 방향, 범위, 폐기 기능 판단이 바뀌면 `docs/design/` 또는 `docs/discussions/` 갱신
3. **로컬 계약/규칙** — wiring, 직렬화 참조, 규칙 자체가 바뀌면 해당 README 또는 관련 skill/owner 문서 갱신

### 확인 방법

- `git status`로 변경된 코드 파악
- `git diff HEAD`로 실제 변경분 검토
- `progress.md`와 대조: "이 코드가 어느 Phase에 해당하는가?"
- 문서가 틀렸으면 즉시 수정하고, 아직 확정되지 않았으면 TODO 또는 discussion으로 남긴다

### 면책

실험 코드, WIP 커밋, 임시 브랜치 작업은 문서 업데이트를 **선택**으로 둔다.
단, `main` 또는 안정 브랜치에 merge될 때는 **반드시** 문서를 동기화한다.
