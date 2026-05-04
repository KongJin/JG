# Claude Code Skills

이 폴더는 Codex Skill Routing Registry를 참조합니다.

## Codex Skills 매핑

| Claude Code 사용 | Codex Skill | Codex 문서 경로 |
|---|---|---|
| 코딩 규칙 | `jg-coding-guardrails` | `../../docs/ops/codex_coding_guardrails.md` |
| Coupling/Cohesion | `jg-coupling-review` | `../../docs/ops/cohesion_coupling_policy.md` |
| 문서 운영 | `jg-doc-lifecycle` | `../../docs/ops/document_management_workflow.md` |
| 규칙 캡처 | `jg-forward-rule-capture` | `../../docs/ops/codex_coding_guardrails.md` |
| 게임 디자인 | `jg-game-design` | `../../docs/design/game_design.md` |
| Unity 워크플로우 | `jg-unity-workflow` | `../../docs/ops/unity_ui_authoring_workflow.md` |

## Skill Routing Trigger

사용자가 다음 표면을 언급하면 해당 skill/route를 따릅니다:

| 트리거 표면 | Skill |
|---|---|
| `UXML`, `USS`, `UI Toolkit`, `VisualElement`, `ScrollView` | `unity-uitoolkit` + `jg-unity-workflow` |
| `codex`, `guardrails`, `coding rule` | `jg-coding-guardrails` |
| `coupling`, `cohesion`, `layer` | `jg-coupling-review` |
| `design`, `game concept` | `jg-game-design` |

---

**Note**: 실제 Codex skill 본문은 `.codex/skills/jg-*/SKILL.md`에 있으나 원본을 건드리지 않고 참조만 합니다.
