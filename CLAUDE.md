# CLAUDE.md

이 문서는 이 레포의 엔트리포인트다.
규칙을 다시 길게 설명하지 않고, 어떤 작업이면 어디로 가야 하는지만 안내한다.

## 먼저 읽기

1. 전역 구조, 레이어, 포트, 씬 계약 체크리스트는 `/agent/architecture.md`에서 시작한다.
2. `/Assets/Scripts/Features/<Name>/` 또는 `/Assets/Scripts/Shared/`를 수정하면, 먼저 `/agent/architecture.md`, `/agent/anti_patterns.md`, `/agent/event_rules.md`, `/agent/validation_gates.md`를 확인하고 그다음 해당 feature 또는 Shared의 `README.md`, `*Setup.cs` / `*Bootstrap.cs`, 실제 코드 경로를 읽는다.
3. 구현 중 금지 사항, Bootstrap 예외, runtime lookup 예외 판단은 `/agent/anti_patterns.md`를 따른다.
4. 이벤트 체인 설계, 이벤트 vs 직접 호출 판단은 `/agent/event_rules.md`를 따른다.
5. `clean`의 정의와 검증 게이트는 `/agent/validation_gates.md`를 따른다.
6. Unity 스크립트 리네임, meta GUID, 씬 직렬화, 프리팹 계약은 `/agent/unity.md`를 따른다.
7. 전역 초기화 순서와 late-join 전제는 각 feature의 `README.md`, `*Setup.cs` / `*Bootstrap.cs`, 씬/프리팹 직렬화 계약, 관련 전역 규칙 문서를 함께 읽고 판단한다.
8. feature README는 로컬 계약만 다루고, 전역 구조 규칙은 `/agent/*.md`에서만 정의한다.
9. 네트워크 상태 키와 동기화 채널은 **해당 키를 실제로 쓰는 코드**를 기준으로 본다. cross-feature로 읽을 때도 쓰기 소유 피처의 Application/Infrastructure 경로를 먼저 본다.
10. Unity MCP나 에디터 자동화는 `/agent/unity.md`를 먼저 읽는다.
11. 대규모 기계적 치환 전에는 대상 패턴, 제외 대상, 검증 방법을 먼저 정하고 문서 의미가 바뀌면 규칙 문서도 같이 갱신한다.
12. Codex가 repo-tracked 파일을 수정할 때는 `/agent/codex_patching.md`를 먼저 읽고, 큰 패치는 분할 적용과 패치 직후 검증을 기본값으로 둔다.
13. UseCase 실패, 에러 코드, 예외 전파는 `/agent/error_handling.md`를 따른다.
14. 로깅 전략, `Debug.Log` 대체, 릴리즈 빌드 로그 제어는 `/agent/logging_rules.md`를 따른다.

## 작업별 진입 경로

| 작업 | 먼저 볼 문서 |
|---|---|
| 피처 경계, 폴더, 레이어, 포트, 네이밍 | `/agent/architecture.md` |
| Bootstrap 책임, EventHandler 위치, runtime lookup 예외 | `/agent/anti_patterns.md` |
| 이벤트 체인 설계, 이벤트 vs 직접 호출 판단 | `/agent/event_rules.md` |
| clean 기준, compile/runtime 검증 게이트 | `/agent/validation_gates.md` |
| 전역 초기화 순서, late-join, scene 간 조립 순서 | `/Assets/Scripts/Features/<Name>/<Name>Setup.cs`, `/Assets/Scripts/Features/<Name>/<Name>Bootstrap.cs` 및 관련 씬 루트 |
| CustomProperties 키, 쓰기 소유권, 동기화 채널 선택 | 해당 키를 실제로 쓰는 `Application/Infrastructure` 코드 |
| 로컬 씬 wiring, lookup 예외, 프리팹/씬 계약 | 해당 feature의 `README.md`, `*Setup.cs` / `*Bootstrap.cs`, 씬/프리팹 직렬화 참조, `/agent/anti_patterns.md` |
| Shared에 둘 수 있는 것과 없는 것 | `/agent/architecture.md`, `/agent/anti_patterns.md`, `/Assets/Scripts/Shared/README.md`, `Assets/Scripts/Shared/**` |
| Unity 스크립트 리네임, meta GUID, 씬 직렬화, 프리팹 연결 | `/agent/unity.md` |
| Codex의 repo-tracked 파일 수정, `apply_patch` 분할/검증 운영 | `/agent/codex_patching.md` |
| Unity MCP 엔드포인트와 테스트 SOP | `/agent/unity.md` |
| UseCase 실패, 에러 코드, 예외 전파 패턴 | `/agent/error_handling.md` |
| 로깅 전략, Debug.Log 대체, 릴리즈 빌드 로그 제어 | `/agent/logging_rules.md` |
| 수치, MVP 기준, 디자인 문장 | `/docs/design/game_design.md` |
| 유닛/모듈 설계 SSOT | `/docs/design/unit_module_design.md` |
| Unit Feature 분리 설계 | `/docs/design/unit_feature_separation.md` |
| 문서 소유권과 SSOT 운영 원칙 | `/agent/work_principles.md` |

---

## 문서 폴더 구조

| 폴더 | 목적 |
|---|---|
| `/agent/` | AI 에이전트 규칙/컨텍스트 (불변, 자주 참조) |
| `/docs/design/` | 설계 SSOT (시스템의 "무엇, 왜" — 장기 보존) |
| `/docs/plans/` | 구현 계획 ("어떻게, 언제" — 완료 후 아카이브 가능) |
| `/docs/ops/` | 배포·빌드·운영 (Firebase, CI/CD) |
| `/docs/discussions/` | 토론/의사결정 기록 (배경, 대안, 결정 근거) |
| `/docs/playtest/` | 플레이테스트 템플릿/기록

---

## 충돌 시 우선순위

1. `/agent/architecture.md` (folders, features, dependencies, layers, naming, ports)
2. `/agent/anti_patterns.md` (금지 패턴, 예외 판단, 리팩터링 교훈)
3. `/agent/event_rules.md` (이벤트 체인 방향, 깊이 제한, 이벤트 vs 직접 호출 판단)
4. `/agent/validation_gates.md` (`clean`의 의미, compile/runtime 검증 게이트)
5. `/agent/error_handling.md` (ErrorCode, ErrorDetail, GameException, UseCase 예외 catch, Presentation 분기, 로깅 연계)
6. `/agent/logging_rules.md` (ILogger 기반 로깅, 릴리즈 빌드 로그 제어, 성능 규칙, 예외 처리 연계)
7. `/agent/unity.md` (Unity meta GUID, 씬 직렬화, 프리팹 계약, Editor 작업 규칙)
8. `/docs/design/game_design.md` (게임 컨셉 SSOT — 코드 책임과 기획 방향이 충돌하면 이 문서가 우선)
9. 해당 feature의 `README.md`, `*Setup.cs` / `*Bootstrap.cs`와 실제 씬/프리팹 계약 (로컬 wiring, lookup 예외, 네트워크 키 실제 사용 경로)
10. `/Assets/Scripts/Shared/README.md` (Shared 로컬 계약)
11. `/agent/work_principles.md` (문서 운영, SSOT 소유권, 응집도 원칙)
12. `/agent/codex_patching.md` (Codex 전용 패치 분할/적용/검증 운영)

---

## 진행 상황

진행 상황은 [`docs/plans/progress.md`](docs/plans/progress.md)에서 관리한다.
