# AGENTS.md

> 마지막 업데이트: 2026-04-24
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
| 코딩 규칙, 아키텍처 | 하위 스킬 참조 (아키텍처, 패턴, 운영, Unity, 검증) |
| 게임 컨셉, 디자인 | [`/docs/design/game_design.md`](docs/design/game_design.md) |
| 진행 상황 | [`/docs/plans/progress.md`](docs/plans/progress.md) |
| Unity UI/UX authoring workflow SSOT | [`/docs/ops/unity_ui_authoring_workflow.md`](docs/ops/unity_ui_authoring_workflow.md) |
| Unity MCP 실행 reference | [`/tools/unity-mcp/README.md`](tools/unity-mcp/README.md) |
| Stitch surface 실행 reference | [`/tools/stitch-unity/README.md`](tools/stitch-unity/README.md) |

---

## 현재 기준 메모

- Lobby/Garage는 기존 scene-owned SSOT를 폐기하고 `prefab-first reset` 기준으로 다시 가져오는 중이다.
- 규칙/운영/Codex 절차 작업이거나 현재 collaboration mode가 `Plan Mode`면 먼저 [`docs/index.md`](docs/index.md)에서 current owner path를 확인하고 `rule-operations` owner 문서를 읽는다. 이 lane에서는 mutation을 금지한다.
- Unity UI/UX 작업은 먼저 [`docs/index.md`](docs/index.md)에서 current owner path를 확인한 뒤 `ops.unity-ui-authoring-workflow` owner 문서를 읽고 시작한다.
- Stitch/Unity 현재 활성 레이어는 `source freeze -> execution contracts -> translation output`이다.
- Lobby/Garage 검증 기본 레이어는 `contract -> EditMode/unit tests -> 얇은 smoke` 순서로 본다.
- reset 동안 Stitch/Unity 기본 진입점은 `source freeze 확인 -> execution contract 확인 -> baseline prefab wiring -> fresh translation/review capture`다.

---
