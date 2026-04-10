# /agent/work_principles.md

## 목적

이 문서는 온보딩 문서가 아니다.
문서 소유권, SSOT 운영, 응집도 기준만 다루며, 구현 규칙 자체는 각 주제의 SSOT에서 본다.

코드뿐 아니라 **문서·씬·프리팹·자동화·분석**까지, **응집도**와 **단방향 참조(DAG)**를 유지하는 기준을 한곳에 모은다.  
레이어·의존·폴더·네이밍 등 코드 규칙의 단일 기준은 [architecture.md](architecture.md)이며, 이 파일은 **작업 단위·정보 출처**에 초점을 둔다.

---

## 핵심 개념

- **응집도**: 한 덩어리(피처, 문서, 스크립트, 씬에 묶인 오브젝트)가 **같은 이유로** 바뀌는가. 서로 다른 변경 이유를 한 파일·한 폴더에 섞지 않는다.
- **단방향 참조**: A→B이면 B가 A를 **되돌려 인용하지 않는다**. 문서·도구 간 **순환 링크**와 **이중 전체 서술**을 피한다. “진실(SSOT)”은 주제마다 한 곳에만 둔다.

---

## 주제별 SSOT (예시)

| 주제 | 단일 근거 |
|------|-----------|
| 아키텍처·폴더·레이어·의존·네이밍·피처 경계 | [architecture.md](architecture.md) |
| 게임 컨셉 | [game_design.md](../docs/design/game_design.md) |
| 재미·리스크 워크숍(4인 페르소나 토의·질문 풀) | [discussion_game_fun_personas.md](../docs/discussions/discussion_game_fun_personas.md) — 수치·MVP 정의는 `../docs/design/game_design.md`만 SSOT |
| MVP 재미 검증 실행 분해(체크리스트·파일 맵) | [implementation_plan_mvp_fun.md](../docs/plans/implementation_plan_mvp_fun.md) — 수치·통과 기준·MVP 문장은 `../docs/design/game_design.md`만 SSOT |
| MVP 플레이테스트 세션 노트 (양식) | [playtest_mvp_template.md](../docs/playtest/playtest_mvp_template.md) — 측정 정의·통과 기준은 `../docs/design/game_design.md`만 SSOT |
| 네트워크 키·소유권 | 키를 실제로 쓰는 feature code (`../Assets/Scripts/Features/<Name>/Application/**`, `../Assets/Scripts/Features/<Name>/Infrastructure/**`) + [architecture.md](architecture.md) |
| 피처별 씬·초기화 | 해당 feature의 `Setup`/`Bootstrap`, 씬/프리팹 직렬화 계약 |
| 에디터 MCP 등 자동화 계약 | [unity_mcp.md](../docs/ops/unity_mcp.md) |
| 스킬 분류 태그와 현재 코드 매핑 | [SkillGameplayTags.cs](../Assets/Scripts/Features/Skill/Domain/SkillGameplayTags.cs), [SkillGameplayTagResolver.cs](../Assets/Scripts/Features/Skill/Domain/SkillGameplayTagResolver.cs), [SkillData.cs](../Assets/Scripts/Features/Skill/Infrastructure/SkillData.cs) — 수치·MVP 정의는 `../docs/design/game_design.md`만 SSOT |
| 제품·배포·기밀 맥락 | [developer_context.md](developer_context.md) |

같은 사실(예: CustomProperty 의미)을 **두 문서에 풀어서 정의하지 않는다**. 보조 문서에는 한 줄 위임만 두고, **역참조로 순환이 생기지 않게** 한다.
엔트리포인트 문서(`../CLAUDE.md` 등)는 **찾아가는 길과 작업 순서만 요약**하고, 규칙 본문은 해당 SSOT에만 둔다.

---

## 작업 레벨별 응집도

- **씬/프리팹**: 요구 오브젝트·참조·순서는 **해당 피처의 Setup/Bootstrap와 실제 serialized contract**에만 남긴다. 같은 내용을 별도 README로 중복 서술하지 않는다.
- **자동화 스크립트 / MCP**: 입력(경로, 대상)을 명시하고, **비즈니스 규칙을 스크립트 안에서 새로 정의하지 않는다**. 규칙은 도메인·Application 또는 전역 규칙 문서에 두고, 도구는 적용·검증만 담당한다.
- **분석(Firebase 등)**: 이벤트 이름·의미는 **코드 상수 한 곳 또는 문서 한 곳**에만 두고 대시보드와 이중 정의하지 않는다.

---

## 피할 패턴

- 문서 A↔B에 **동일 결정을 각각 풀어 쓴다** → 한쪽만 고쳐져 이중 진실이 된다.
- 런타임/스크립트로 씬을 **조용히 고쳐** README 씬 계약과 어긋난다 → [anti_patterns.md](anti_patterns.md)의 배선 규칙과 충돌한다.
- 에이전트 채팅에만 규칙을 두고 레포를 갱신하지 않는다 → **결정은 SSOT에 반영**한다.

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
| `docs/ops/` | 운영/배포/도구 설정 변경 시 | 코드 작성자 (AI 포함) |
| `docs/discussions/` | 토론/의사결정 기록 시 | 코드 작성자 (AI 포함) |
| `docs/playtest/` | 플레이테스트 템플릿/결과 기록 시 | 코드 작성자 (AI 포함) |
| `agent/` | 규칙 자체 변경, 새 금지 패턴 발견, SSOT 소유권 변경 시 | 코드 작성자 (AI 포함) |
| `QWEN.md` | 세션 종료 시 작업 요약, 다음 세션 시작점 갱신 | AI 에이전트 (세션 종료 직전) |
| `CLAUDE.md` | 엔트리포인트 경로/참조 구조 변경 시 | 코드 작성자 (AI 포함) |
| `**/README.md` | 피처 로컬 wiring 변경, 직렬화 참조 변경, 포트 추가/삭제 시 | 코드 작성자 (AI 포함) |

### 업데이트 항목

코드写完 후 다음을 점검하고 문서에 반영한다:

1. **진행 상태** — Phase 완료/진행률, `progress.md` 테이블
2. **TODO 리스트** — 완료된 항목 제거, 신규 TODO 추가, 우선순위 변경
3. **최근 변경 커밋** — `progress.md` 커밋 테이블에 추가
4. **설계 변경 근거** — 기존 설계와 달라졌으면 `docs/design/` 또는 `docs/discussions/`에 기록
5. **규칙 문서 갱신** — 새 패턴 발견 시 `agent/` 문서 반영

### 확인 방법

- `git status`로 변경된 코드 파악
- `git diff HEAD`로 실제 변경분 검토
- `progress.md`와 대조: "이 코드가 어느 Phase에 해당하는가?"
- 문서가 틀렸으면 즉시 수정, 애매하면 TODO에 기록

### 면책

실험 코드, WIP 커밋, 임시 브랜치 작업은 문서 업데이트를 **선택**으로 둔다.
단, `main` 또는 안정 브랜치에 merge될 때는 **반드시** 문서를 동기화한다.
