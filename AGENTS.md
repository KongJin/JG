# AGENTS.md

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: 레포 최상위 진입점, 상위 읽기 순서, 현재 owner 문서 시작 경로
> upstream: none
> artifacts: none

이 레포의 엔트리포인트입니다. 상위 범위별로 안내합니다.
현재 owner 문서 경로 해석은 [`/docs/index.md`](docs/index.md)를 기준으로 합니다.

---

## 먼저 볼 곳

| 찾고 싶은 것 | 위치 |
|---|---|
| 문서 전체 지도 / owner path registry | [`/docs/index.md`](docs/index.md) |
| Plan Mode / Codex 운영 규칙 | [`/docs/index.md`](docs/index.md)에서 current path 확인 후 `rule-operations` owner 문서 |
| Codex 코딩 가드레일 / LLM coding 오류 방지 | [`/docs/ops/codex_coding_guardrails.md`](docs/ops/codex_coding_guardrails.md) |
| 문서 운영 / closeout 상위 원칙 | [`/docs/ops/document_management_workflow.md`](docs/ops/document_management_workflow.md) |
| 코딩 규칙, 아키텍처 | 하위 스킬 참조 (아키텍처, 패턴, 운영, Unity, 검증) |
| 게임 컨셉, 디자인 | [`/docs/design/game_design.md`](docs/design/game_design.md) |
| 진행 상황 | [`/docs/plans/progress.md`](docs/plans/progress.md) |
| Unity UI/UX authoring workflow SSOT | [`/docs/ops/unity_ui_authoring_workflow.md`](docs/ops/unity_ui_authoring_workflow.md) |
| Unity MCP 실행 reference | [`/tools/unity-mcp/README.md`](tools/unity-mcp/README.md) |
| Stitch surface 실행 reference | [`/tools/stitch-unity/README.md`](tools/stitch-unity/README.md) |

---

## 현재 기준 메모

- Lobby/Garage의 새 UI 작업 route 본문은 [`/docs/ops/unity_ui_authoring_workflow.md`](docs/ops/unity_ui_authoring_workflow.md)와 Stitch owner 문서를 따른다.
- 현재 상태, 우선순위, 검증 기본선은 [`/docs/plans/progress.md`](docs/plans/progress.md)를 우선으로 본다.
- 구현, 버그 수정, 리팩터, 테스트/검증 작업은 [`/docs/ops/codex_coding_guardrails.md`](docs/ops/codex_coding_guardrails.md)를 먼저 확인한 뒤 관련 owner 문서를 따른다.
- Plan Mode 또는 규칙/운영/Codex 절차 작업은 [`/docs/index.md`](docs/index.md)에서 current owner path를 확인한 뒤 `rule-operations` owner 문서로 라우팅하고, 그 lane에서는 mutation을 금지한다.
- 운영 규칙 본문과 current route는 [`/docs/index.md`](docs/index.md)에서 owner 문서를 찾아 확인한다. `AGENTS.md`는 경로 안내와 현재 lane 메모만 유지한다.

---
