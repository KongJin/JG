# Skill Trigger Matrix

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: ops.skill-trigger-matrix
> role: reference
> owner_scope: prompt-signal fixtures for repo-local and repo-facing external skill route coverage
> upstream: docs.index, ops.skill-routing-registry, ops.document-management-workflow
> artifacts: none

이 문서는 repo-local skill과 repo-facing external/global skill route의 trigger 기대값을 fixture처럼 관리한다.
실제 LLM skill selection을 실행 검증하는 문서가 아니라, skill description을 바꿀 때 과소/과잉 trigger를 사람이 재리뷰할 최소 prompt-signal set이다.
skill 이름과 route 등록은 `ops.skill-routing-registry`가 소유한다.

## Trigger Fixtures

| ID | User prompt signal | Expected skill routes | Companion / handoff | Boundary |
|---|---|---|---|---|
| T01 | "구현해줘", "버그 고쳐줘", "리팩터 시작", "빼자/제외/비활성/삭제/되돌려", "제거한 흔적도 없게", "TDD/테스트 우선으로" | `jg-coding-guardrails` | `jg-issue-investigation` when cause is uncertain; lane owner docs after guardrails | Do not skip repo evidence, validation criteria, scope clarification for product/data/pipeline ambiguity, or absence semantics for removals. |
| T02 | "문서/skills 관리방법 기술부채", "stale owner 정리", "docs/plans 정리" | `jg-doc-lifecycle`, `rule-operations` | `jg-coupling-review` when owner boundary judgment is needed | Do not treat skill-entry text as policy body. |
| T03 | "응집도/결합도 봐줘", "interface design", "mocking", "deep module", "owner boundary" | `jg-coupling-review` | `jg-doc-lifecycle` if the output becomes document lifecycle execution | Do not create a generalized score or hard-fail from meaning judgment. |
| T04 | "왜 실패했어", "원인 파악", "가설 검증", "예전 캡쳐를 보고 판단", "visual mismatch", "아마/추정/가능성" | `jg-issue-investigation` | `jg-coding-guardrails` when a verified fix starts; `jg-unity-workflow` when Unity capture evidence is involved | Do not report unverified hypotheses or stale visual evidence as current root cause. |
| T05 | "게임 방향", "MVP 범위", "핵심 재미", "Nova1492 세계관", "권리/이름", "밸런스", "Garage/Battle UX" | `jg-game-design` | implementation route only after design owner decision | Do not put product/design truth in a standalone glossary by default. |
| T06 | ".stitch", "prompt brief", "source freeze", "contract/map/presentation JSON" | `jg-stitch-workflow` | `jg-stitch-unity-import` for Unity candidate import | Do not make Stitch router own Unity implementation closeout. |
| T07 | "Stitch 화면 Unity로 가져와", "reimport", "UI Toolkit candidate", "visual fidelity" | `jg-stitch-unity-import`, `jg-unity-workflow` | `jg-coupling-review` if screen/runtime owner boundary is unclear | Do not call pilot render accepted runtime replacement. |
| T08 | "JG scene/prefab authoring", "UITK/UI policy", "MCP evidence/runtime smoke", "새 캡쳐 기준", "fresh evidence pending", "Stitch handoff Unity owner" | `jg-unity-workflow` | `rule-unity` only for generic Unity mechanics; `jg-coding-guardrails` for script edits; `rule-validation` when evidence gates decide closeout | Do not mutate Unity surfaces in Plan Mode or judge visual state before fresh capture evidence exists. |
| T09 | "Plan Mode", "SSOT", "closeout", "규칙 충돌", "old trace/stale rule" | `rule-operations`, `jg-doc-lifecycle` | `rule-plan-authoring` when persistent plan docs change | Do not mutate in Plan Mode. |
| T10 | "계획 문서 작성", "docs/plans", "Acceptance/TODO", "과한점/부족한점 재리뷰" | `rule-plan-authoring` | `jg-doc-lifecycle` when status/lifecycle, owner movement, deletion, compression, or stale traces are part of the plan change; `jg-coding-guardrails` only when implementation starts | Do not create a plan doc when a session checklist is enough. |
| T11 | "레이어", "의존 방향", "포트", "Bootstrap", "폴더 구조", "아키텍처 위반" | `rule-architecture`, `jg-coupling-review` | `jg-coding-guardrails` when implementation starts | Do not let global architecture notes override repo owner boundaries. |
| T12 | "금지 패턴", "이벤트 순환", "예외 처리", "ErrorCode", "로그 정책" | `rule-patterns` | `jg-coding-guardrails` for code changes; `jg-coupling-review` for boundary judgment | Do not turn pattern advice into a new repo SSOT. |
| T13 | "검증 통과", "Clean Levels", "static-clean", "compile-clean", "runtime-smoke-clean", "fresh evidence pending", "current artifact" | `rule-validation` | `jg-unity-workflow` for Unity evidence; `jg-coding-guardrails` for test fixes; `jg-issue-investigation` for mismatch/root-cause follow-up | Do not merge mechanical pass or current artifact creation with product acceptance. |
| T14 | "배포 전략", "Firebase", "WebGL", "팀 규모", "출시/수익화", "자동화 우선순위", "제품 전략 배경" | `rule-context` | `plans.progress`, `ops.firebase-hosting`; route product/UX judgment to `jg-game-design` | Do not let background context override current progress or design owner docs. |
| T15 | "meta GUID", "scene serialization", "Unity API", "batchmode build", "compile diagnostics fallback" | `rule-unity` | `jg-unity-workflow` only when JG owner workflow, scene/prefab authoring, or UI policy is in scope | Do not let generic Unity mechanics own JG acceptance. |
| T16 | "fallback으로 덮지 마", "silent fail", "runtime repair", "hidden lookup", "contract 누락", "pending/review인데 성공 처리", "조용히 정상처럼 보임" | `jg-no-silent-fallback` | `jg-coding-guardrails` for implementation; `jg-coupling-review` for owner boundary; `jg-issue-investigation` for verified root cause | Do not let missing contracts become normal preview, gameplay, data promotion, or acceptance success. |

## Maintenance Rule

- Every repo-local `jg-*` skill should appear in at least one fixture row.
- Every registered external/global `rule-*` skill from `ops.skill-routing-registry` should appear in at least one fixture row.
- If a skill description changes, compare it against this matrix and leave `skill trigger checked` when the trigger surface changed.
- Keep skill descriptions as concise trigger indexes, not policy bodies. Target roughly 25 words or less unless a longer description is explicitly justified.
- When shortening a description, preserve at least one recognizable prompt signal for each expected route row that depends on that skill.
- If a fixture starts implying a different owner body, update the owner document or split the route instead of stretching the skill description.
