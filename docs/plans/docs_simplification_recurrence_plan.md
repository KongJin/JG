# Docs Simplification Recurrence Plan

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.docs-simplification-recurrence
> role: plan
> owner_scope: 전체 문서 간소화와 문서 비대화 재발 방지 실행 순서
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `docs/index.md`, `docs/**`

## 목적

JG 문서 체계에서 문서 수 자체보다 **현재 기준처럼 읽히는 문서 수**를 줄인다.
작업자가 `progress.md -> docs/index.md -> 필요한 owner 문서`만 따라가도 현재 기준과 배경 기록을 구분할 수 있게 만든다.

이 문서는 문서 운영 규칙 본문을 새로 소유하지 않는다.
역할, 상태 전이, 참조, 삭제 기준은 `ops.document-management-workflow`를 따른다.

## 현재 문제

- `active` 문서가 많으면 실제 현재 기준이 흐려진다.
- 완료된 plan이 기록형으로 내려가지 않으면 계속 실행 중인 계획처럼 보인다.
- reference 문서가 “현재 기준”, “반드시”, “앞으로는” 같은 문구를 유지하면 owner 문서와 이중 기준이 된다.
- `docs/index.md`가 registry를 넘어 규칙 본문을 설명하기 시작하면 entry 역할이 무거워진다.
- 삭제 후보를 바로 지우면 맥락을 잃고, 삭제하지 않으면 탐색 비용이 남는다.

## 범위

포함:

- `docs/index.md` 기준 전체 문서 인벤토리
- `active / draft / reference / historical / delete candidate` 재분류
- 완료된 plan의 기록형 wording 정리
- reference 문서의 현재형 규칙 문구 제거
- 중복 SSOT, closeout, 검증 설명의 owner 위임
- 삭제 후보의 참조 확인과 단계적 제거
- 재발 방지를 위한 closeout 확인 항목 제안

제외:

- 제품 방향 재정의
- Unity scene, prefab, asset, Stitch artifact 수정
- 새 lint hard-fail 추가
- owner 문서의 실제 규칙 의미 변경
- reference/historical 기록의 무리한 삭제

## Owner Impact

- primary owner: `docs.index`, `docs/plans/*`
- secondary owner: `ops.document-management-workflow`, `ops.plan-authoring-review-workflow`
- out-of-scope: gameplay/design 판단 변경, Unity/Stitch 실행 산출물 변경, 새 hard-fail lint

## 실행 단계

### Phase 0: 인벤토리 작성

상태: 완료

작업:

1. `docs/**`와 `AGENTS.md`의 metadata를 수집한다.
2. 각 문서를 `keep active`, `draft`, `reference`, `historical`, `delete candidate`로 분류한다.
3. 분류 이유를 한 줄로 남긴다.

Acceptance:

- 모든 managed document가 하나의 후보 상태를 가진다.
- `docs/index.md` 상태와 파일 header 상태가 불일치하는 후보가 보인다.
- delete candidate는 참조 확인 전에는 삭제하지 않는다.

결과:

- `docs/**/*.md`와 `AGENTS.md`를 기준으로 42개 human-facing 문서를 확인했다.
- `*.md.meta`는 Unity metadata라 managed human document 인벤토리에서 제외한다.
- `docs/index.md` registry 누락 후보는 발견하지 않았다.
- Phase 0 기준 즉시 삭제 후보는 없다. 삭제 후보는 Phase 4에서 stale reference를 다시 확인한 뒤 판정한다.

| 문서 | 후보 | 이유 |
|---|---|---|
| `AGENTS.md` | keep active | 레포 최상위 entry다. |
| `docs/index.md` | keep active | docs registry와 상태 라벨 owner다. |
| `docs/design/game_design.md` | keep active | 현재 게임 방향 SSOT다. |
| `docs/design/ui_foundations.md` | keep active | Lobby/Garage UI 레이아웃과 토큰 SSOT다. |
| `docs/design/ui_reference_workflow.md` | keep active | Stitch 시안 활용 원칙 SSOT다. |
| `docs/design/unit_module_design.md` | keep active | 유닛/모듈 설계 기준 SSOT다. |
| `docs/design/module_data_structure.md` | keep active | 실제 유닛/모듈 데이터 구조 SSOT다. |
| `docs/design/architecture-diagram.md` | reference | 포트와 레이어 구조 시각 요약이다. |
| `docs/design/stitch_test_brief.md` | reference | Stitch 테스트 프롬프트 템플릿이다. |
| `docs/design/unit_feature_separation.md` | reference | Unit feature 분리 배경 기록이다. |
| `docs/discussions/discussion_removed_features_rationale.md` | reference | 제외 기능 근거 메모다. |
| `docs/discussions/discussion_game_fun_personas.md` | reference | 재미 검토 질문 프레임이다. |
| `docs/discussions/discussion_unity.md` | historical | 과거 Unity 규칙과 도구 한계 기록이다. |
| `docs/ops/document_management_workflow.md` | keep active | 문서 운영 상위 원칙 SSOT다. |
| `docs/ops/cohesion_coupling_policy.md` | keep active | 응집도/결합도 정의 SSOT다. |
| `docs/ops/acceptance_reporting_guardrails.md` | keep active | acceptance/reporting 판정 SSOT다. |
| `docs/ops/plan_authoring_review_workflow.md` | keep active | plan 재리뷰 절차 SSOT다. |
| `docs/ops/unity_ui_authoring_workflow.md` | keep active | Unity UI/UX authoring route SSOT다. |
| `docs/ops/stitch_data_workflow.md` | keep active | Stitch working data 운영 SSOT다. |
| `docs/ops/stitch_structured_handoff_contract.md` | keep active | Stitch structured handoff contract SSOT다. |
| `docs/ops/stitch_to_unity_translation_guide.md` | reference | Stitch screen -> Unity translation 실무 가이드다. |
| `docs/ops/stitch_handoff_completeness_checklist.md` | reference | Stitch handoff 빠른 점검표다. |
| `docs/ops/firebase_hosting.md` | reference | Firebase Hosting 운영 절차다. |
| `docs/plans/progress.md` | keep active | 레포 전체 현재 상태와 포커스 SSOT다. |
| `docs/plans/garage_ui_ux_improvement_plan.md` | keep active | 현재 Set B Garage recovery plan이다. |
| `docs/plans/docs_simplification_recurrence_plan.md` | reference | 문서 간소화 실행 결과와 재발방지 기록이다. |
| `docs/plans/stitch_llm_contract_pipeline_plan.md` | draft | Stitch LLM contract pipeline 전환 계획이다. |
| `docs/plans/game_scene_ui_ux_improvement_plan.md` | draft | GameScene HUD/소환 UX 재설계 초안이다. |
| `docs/plans/progress_changelog.md` | reference | progress에서 분리한 dated log다. |
| `docs/plans/account_system_plan.md` | reference | 계정/차고 복구 기준 reference다. |
| `docs/plans/game_scene_entry_plan.md` | reference | GameScene 진입 상위 흐름 reference다. |
| `docs/plans/stitch_ui_ux_overhaul_plan.md` | reference | Stitch inventory와 reset 기준 reference다. |
| `docs/plans/nova1492_resource_integration_plan.md` | reference | Nova1492 staging 결과와 후속 기준이다. |
| `docs/plans/rule_trigger_skill_extraction_plan.md` | reference | skill trigger 분리 결과와 후속 자동화 검토 기록이다. |
| `docs/plans/rule_revision_trace_cleanup_plan.md` | reference | old trace 정리 결과와 후속 reference다. |
| `docs/plans/tech_debt_reduction_plan.md` | reference | 기술부채 우선순위 reference다. |
| `docs/plans/ops_rules_simplification_plan.md` | reference | 운영 규칙 축약 결과 기록이다. |
| `docs/plans/mcp_improvement_plan.md` | reference | Unity MCP 역할 정리 reference다. |
| `docs/playtest/runtime_validation_checklist.md` | keep active | 런타임 수동 검증 SSOT다. |
| `docs/playtest/webgl_smoke_checklist.md` | reference | WebGL 수동 smoke 절차다. |
| `docs/playtest/mvp_fun_checklist.md` | reference | MVP 재미 검증 실행 체크리스트다. |
| `docs/playtest/playtest_mvp_template.md` | reference | 플레이테스트 세션 템플릿이다. |

### Phase 1: Active 다이어트

상태: 완료

작업:

1. `active`를 현재 작업자가 반드시 따라야 하는 기준으로만 제한한다.
2. 완료된 plan, 보류된 plan, 배경 plan은 `reference` 또는 `historical`로 내린다.
3. 현재 진행 상태는 `progress.md`가 우선한다는 전제를 유지한다.

Acceptance:

- `active` 목록이 현재 SSOT, 현재 lane SSOT, 현재 실행 중 plan 중심으로 줄어든다.
- 완료된 plan이 `active` 또는 `draft`로 남지 않는다.
- 상태 변경은 `docs/index.md`와 파일 header가 함께 맞는다.

결과:

- `docs/index.md`의 active 목록을 `progress.md` 현재 포커스와 대조했다.
- 완료 plan이나 배경 plan이 active로 남은 후보는 발견하지 않았다.
- 현재 active 문서는 entry, 제품/설계 SSOT, 운영 SSOT, 현재 Stitch/Garage lane SSOT, runtime validation SSOT로 분류된다.
- 이번 phase에서 추가 상태 변경은 하지 않았다.

### Phase 2: Reference 기록화

상태: 완료

작업:

1. reference 문서에서 현재 owner처럼 읽히는 표현을 찾는다.
2. “현재 기준”, “반드시”, “앞으로는”, “Closeout 기준”, “핵심 원칙”, “실행 계획이다” 같은 문구를 기록형으로 낮춘다.
3. 현재 기준이 필요하면 해당 owner 문서로 한 줄 위임한다.

Acceptance:

- reference 문서가 현재 규칙 본문을 새로 소유하지 않는다.
- 완료된 plan은 “당시 목표”, “작성 당시”, “Closeout 기록”, “reference로만 읽는다”처럼 읽힌다.
- 현재 기준을 말하는 문장은 owner 문서로 위임되어 있다.

결과:

- reference/historical 문서에서 현재 기준처럼 읽히는 문구를 점검했다.
- `discussion_removed_features_rationale`, `discussion_unity`, `account_system_plan`, `game_scene_entry_plan`, `mcp_improvement_plan`, `stitch_ui_ux_overhaul_plan`, `tech_debt_reduction_plan`의 현재형 wording을 기록형 또는 owner 위임형으로 낮췄다.
- 남은 `현재 기준`, `반드시`, `항상` 검색 결과는 active owner 문서, draft plan, 질문 예시, 또는 이 plan의 점검 대상 문구다.

### Phase 3: 중복 설명 제거

상태: 완료

작업:

1. 반복 설명의 owner를 확인한다.
2. 다른 문서에는 장문 재서술 대신 짧은 위임만 남긴다.
3. `docs/index.md`는 registry와 길 안내만 유지한다.

Owner 기본값:

- SSOT, Role, Cohesion, Closeout: `ops.document-management-workflow`
- 응집도/결합도 정의: `ops.cohesion-coupling-policy`
- blocked/mismatch/success 의미: `ops.acceptance-reporting-guardrails`
- plan 재리뷰: `ops.plan-authoring-review-workflow`
- 현재 상태: `plans.progress`

Acceptance:

- 같은 결정이 여러 문서에 장문으로 반복되지 않는다.
- entry 문서가 owner 본문을 재서술하지 않는다.
- reference 문서의 장문 규칙 설명이 줄어든다.

결과:

- `docs/index.md`와 `AGENTS.md`는 registry와 entry 역할만 유지하고 있어 추가 축약하지 않았다.
- `SSOT / Role / Cohesion / Closeout`, 응집도/결합도, acceptance 판정, plan 재리뷰 의미는 각 active owner 문서가 소유한다.
- reference plan에 남은 반복 설명은 완료된 정리 기록으로 유지하되, Phase 2에서 현재형 owner wording을 낮췄다.
- 이번 phase에서 별도 삭제나 owner 의미 변경은 하지 않았다.

### Phase 4: 삭제 후보 처리

상태: 완료

작업:

1. delete candidate마다 `rg`로 참조를 확인한다.
2. `docs/index.md`, active skill, tool README 참조를 먼저 제거한다.
3. 필요하면 owner 문서나 `progress.md`에 owner 이동을 한 줄로 남긴다.
4. 삭제 후 `rules:lint`로 잔존 링크를 확인한다.

Acceptance:

- 삭제 후보는 참조 확인 없이 삭제되지 않는다.
- 삭제된 문서의 잔존 링크가 없다.
- historical 가치가 있는 문서는 삭제 대신 historical/reference로 격리된다.

결과:

- Phase 0~3 기준 즉시 삭제 후보는 없다.
- `docs/index.md` registry에 모든 human-facing `.md` 문서가 포함되어 있다.
- 기존 삭제/이동 후보 이름(`codex_lobby_garage_panel_plan`, `implementation_plan_mvp_fun`, `docs/plans/webgl_smoke_checklist`, `tech_debt_review`, `HISTORICAL_LOBBY_SCENE_ROUTE`, `Invoke-CodexLobbyUiWorkflowGate`)의 잔존 참조는 발견하지 않았다.
- 삭제 대신 reference/historical로 격리된 문서를 유지한다.

### Phase 5: 재발 방지 반영

상태: 완료

작업:

1. 새 문서 작성 또는 큰 문서 변경 closeout에서 lifecycle 확인을 남길지 검토한다.
2. owner 문서에 반영할 경우 `ops.document-management-workflow`가 소유하게 한다.
3. 행동 트리거가 필요하면 `rule-operations` 또는 `rule-plan-authoring` skill description 보강 여부를 함께 검토한다.

후보 closeout 문구:

- `doc lifecycle checked: active/reference/historical/delete 후보 확인`

Acceptance:

- 새 행동 규칙을 owner 문서에 추가할지 여부가 결정되어 있다.
- owner 문서에 추가했다면 skill trigger 검토 결과가 남는다.
- 새 hard-fail lint 없이도 closeout에서 문서 lifecycle이 드러난다.

결과:

- `ops.document-management-workflow` closeout/빠른 체크에 큰 문서 작업의 `doc lifecycle checked` 확인을 추가했다.
- `ops.plan-authoring-review-workflow` 부족한점 체크에 큰 문서 작업의 lifecycle 후보 확인을 추가했다.
- `rule-operations`와 `rule-plan-authoring` skill description에 `doc lifecycle checked` trigger를 추가했다.
- 새 hard-fail lint는 추가하지 않았다.

## 검증

문서 변경 후:

```powershell
npm run --silent rules:sync-closeout
npm run --silent rules:lint
```

검증 실패 시:

- 이번 plan 변경 때문인지 기존 dirty worktree 때문인지 분리한다.
- broken link, status mismatch, owner reference mismatch를 우선 수정한다.
- 정책 판단이 필요한 항목은 `blocked` 또는 `residual`로 남긴다.

## 리스크와 처리

| 리스크 | 처리 |
|---|---|
| 너무 많이 삭제해 맥락을 잃음 | 삭제 전 reference/historical 가치 확인 |
| active를 과하게 줄여 작업자가 기준을 놓침 | `progress.md`와 현재 lane SSOT는 유지 |
| reference 문서가 다시 현재 기준처럼 읽힘 | 기록형 wording과 owner 위임으로 낮춤 |
| 새 재발방지 규칙이 또 문서 부담이 됨 | 먼저 closeout 문구 후보로 운용하고 hard-fail은 보류 |
| 문서 간소화가 제품/Unity 작업과 섞임 | owner impact와 out-of-scope를 closeout에 남김 |

## 2026-04-26 Follow-Up: Active Owner Split Guard

문제:

- 큰 active plan에서 Phase 전용 active plan을 분리한 뒤, 부모 plan도 계속 active로 남으면 같은 residual을 두 문서가 동시에 직접 소유한다.
- `progress.md`의 code path 완료와 smoke acceptance 미완료가 같은 `완료` 표현으로 묶이면 다음 작업자가 실제 blocker를 다시 해석해야 한다.

운영 계획:

1. active plan을 새로 만들거나 Phase 전용 plan을 추출하면, 같은 patch에서 기존 parent plan의 lifecycle을 재판정한다.
2. child plan이 직접 실행 owner이면 parent plan은 reference/handoff로 낮추거나, active 유지 이유를 child plan과 겹치지 않게 다시 쓴다.
3. `progress.md`는 code path 완료와 acceptance smoke 미완료를 한 상태로 뭉개지 않고 분리해 적는다.
4. 코드 간소화 후보는 바로 새 plan을 만들기보다 `progress.md`, 기존 tech-debt reference, 또는 해당 active owner plan의 residual로 먼저 등록한다.

Acceptance:

- 같은 이유로 바뀌는 active plan이 둘 이상 남지 않는다.
- parent/child plan 관계가 있으면 parent는 scope reference, child는 execution owner로 읽힌다.
- smoke 미완료가 `완료` phase 표기 뒤에 숨어 있지 않다.
- 새 규칙/skill trigger 추가 없이 `doc lifecycle checked`와 owner 재판정으로 처리된다.

첫 실행:

- 2026-04-26 Guard Run 1에서 `docs.index`의 active plan registry와 `plans.progress` 현재 포커스를 대조했다.
- GameScene 쪽은 parent Agent A runtime plan을 current registry에서 제거했고, execution owner는 `game_scene_phase5_multiplayer_sync_plan.md`와 `game_scene_agent_b_hud_input_validation_plan.md`로 분리되어 있다.
- Lobby/Garage 쪽 active plan은 둘 다 `LobbyScene`을 언급하지만 직접 residual이 다르다. `lobby_scene_ui_prefab_management_plan.md`는 prefab/placeholder/controller 책임 경계를 보고, `lobby_scene_nova1492_model_application_plan.md`는 Nova1492 Phase 4 로비 장식 후보 판단을 본다.
- `garage_ui_ux_improvement_plan.md`는 Set B visual fidelity verdict만 직접 residual로 보며, shared Account/Garage WebGL 검증과 섞지 않는다.
- 추가 parent/child active 중복은 발견하지 않았다. 다만 Lobby/Garage scene mutation은 여전히 같은 씬을 건드릴 수 있으므로 실제 구현 때 한 writer씩 순차 처리한다.

Guard Run 1 closeout:

- owner impact: primary `plans.docs-simplification-recurrence`; secondary `docs.index`, `plans.progress`, active plan registry
- doc lifecycle checked: active parent/child 중복 없음. GameScene parent는 reference, Phase 5/Agent B는 active execution owner로 유지한다.
- skill trigger checked: covered by `rule-operations` and `rule-plan-authoring`
- plan rereview: clean

## Closeout 조건

- 전체 문서 인벤토리와 상태 후보가 작성되어 있다.
- `active` 문서가 현재 기준 중심으로 줄어들었다.
- 완료된 plan은 `reference` 또는 `historical`로 내려갔다.
- reference 문서의 stale active wording이 정리됐다.
- 삭제 후보는 참조 확인 후 처리됐다.
- 재발방지 closeout 항목의 owner 반영 여부가 결정됐다.
- `rules:sync-closeout`와 `rules:lint`가 통과하거나, 실패 원인이 residual/blocked로 분리되어 있다.

## Closeout

- status: completed
- owner impact: primary=`plans.docs-simplification-recurrence`, secondary=`docs.index`, `ops.document-management-workflow`, `ops.plan-authoring-review-workflow`, `rule-operations`, `rule-plan-authoring`
- doc lifecycle checked: active/reference/historical/delete 후보 확인, 완료 plan reference 전환
- skill trigger checked: added to `rule-operations`, `rule-plan-authoring`
- residual: 새 hard-fail lint는 만들지 않았다. 문서 lifecycle 확인은 closeout 문구와 skill trigger로 먼저 운용한다.

## Plan Rereview

- 2026-04-25 초안 작성: 전체 문서 간소화와 재발 방지를 phase 단위로 정리했다.
- 2026-04-25 Phase 0 실행: 전체 human-facing 문서 인벤토리와 후보 상태를 기록했다. 즉시 삭제 후보는 없고, Phase 4에서 stale reference를 다시 확인한다.
- 2026-04-25 Phase 0 재리뷰: plan rereview: clean. 모든 human-facing `.md` 문서가 후보 상태를 가졌고, 별도 inventory artifact를 만들지 않아 문서 수 증가를 피했다.
- 2026-04-25 Phase 1 실행: active 목록을 현재 포커스와 대조했다. 완료 plan이나 배경 plan이 active로 남은 후보는 없어서 추가 상태 변경은 하지 않았다.
- 2026-04-25 Phase 1 재리뷰: plan rereview: clean. active index link target은 모두 존재하고, 남은 active 문서는 entry/SSOT/current plan/runtime validation 범위다.
- 2026-04-25 Phase 2 실행: reference/historical 문서의 stale active wording을 기록형 또는 owner 위임형으로 낮췄다.
- 2026-04-25 Phase 3 실행: 중복 설명 owner를 확인했다. entry와 registry는 본문 owner가 아니며, 반복 설명은 active owner 문서 또는 완료 기록으로 분리되어 있어 추가 축약하지 않았다.
- 2026-04-25 Phase 4 실행: 즉시 삭제 후보와 삭제된 옛 경로 잔존 참조를 확인했다. 모든 human-facing `.md` 문서는 index에 남아 있고, 추가 삭제는 하지 않았다.
- 2026-04-25 Phase 5 실행: `doc lifecycle checked`를 owner 문서와 skill trigger에 반영하고, 새 hard-fail lint는 추가하지 않았다.
- 2026-04-26 follow-up 실행: GameScene Agent A parent plan을 reference로 낮추고, Phase 5 multiplayer sync와 Agent B placement automation을 직접 실행 owner로 분리했다.
- 2026-04-26 follow-up 재리뷰: plan rereview: clean. 과한점은 새 status, 새 lint, 새 artifact를 만들지 않은 점에서 해소됐고, 부족한점은 parent/child active owner 중복과 progress의 code-path/smoke 표현을 함께 보정해 해소했다.
