# /agent/work_principles.md

## 목적

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
| 게임 컨셉 | [game_design.md](game_design.md) |
| 재미·리스크 워크숍(4인 페르소나 토의·질문 풀) | [discussion_game_fun_personas.md](discussion_game_fun_personas.md) — 수치·MVP 정의는 `game_design.md`만 SSOT |
| MVP 재미 검증 실행 분해(체크리스트·파일 맵) | [implementation_plan_mvp_fun.md](implementation_plan_mvp_fun.md) — 수치·통과 기준·MVP 문장은 `game_design.md`만 SSOT |
| MVP 플레이테스트 세션 노트 (양식) | [playtest_mvp_template.md](playtest_mvp_template.md) — 측정 정의·통과 기준은 `game_design.md`만 SSOT |
| 네트워크 키·소유권 | [state_ownership.md](state_ownership.md) |
| 피처별 씬·초기화 | `Assets/Scripts/Features/<Name>/README.md` |
| 에디터 MCP 등 자동화 계약 | 해당 도구 README (예: `Assets/Editor/UnityMcp/README.md`) |
| 제품·배포·기밀 맥락 | [developer_context.md](developer_context.md) |

같은 사실(예: CustomProperty 의미)을 **두 문서에 풀어서 정의하지 않는다**. 보조 문서에는 한 줄 위임만 두고, **역참조로 순환이 생기지 않게** 한다.

---

## 작업 레벨별 응집도

- **씬/프리팹**: 요구 오브젝트·참조·순서는 **해당 피처 README의 씬 계약**에만 상세히 쓴다. 다른 README는 중복 나열 대신 위임한다.
- **자동화 스크립트 / MCP**: 입력(경로, 대상)을 명시하고, **비즈니스 규칙을 스크립트 안에서 새로 정의하지 않는다**. 규칙은 도메인·Application 또는 README에 두고, 도구는 적용·검증만 담당한다.
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
4. 피처 간에는 **포트·이벤트 이름**으로 경계를 두고, 타 피처 README에 장문으로 “상대 구현”을 적지 않는다.
