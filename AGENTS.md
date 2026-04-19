# AGENTS.md

이 레포의 엔트리포인트입니다. 상위 범위별로 안내합니다.

---

## 먼저 볼 곳

| 찾고 싶은 것 | 위치 |
|---|---|
| 문서 전체 지도 | [`/docs/index.md`](docs/index.md) |
| 코딩 규칙, 아키텍처 | 하위 스킬 참조 (아키텍처, 패턴, 운영, Unity, 검증) |
| 게임 컨셉, 디자인 | [`/docs/design/game_design.md`](docs/design/game_design.md) |
| 진행 상황 | [`/docs/plans/progress.md`](docs/plans/progress.md) |
| Unity MCP 워크플로우 | [`/tools/unity-mcp/README.md`](tools/unity-mcp/README.md) |

---

## 현재 기준 메모

- `CodexLobbyScene.unity`는 Lobby/Garage UI의 runtime SSOT다.
- Lobby/Garage 검증 기본 레이어는 `contract -> EditMode/unit tests -> 얇은 smoke` 순서로 본다.
- Unity MCP 기본 진입점은 `workflow gate -> page-switch smoke -> feature smoke`다.

---
