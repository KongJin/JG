# Document Management Recurrence Prevention Plan

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.document-management-recurrence-prevention
> role: plan
> owner_scope: 문서관리 점검 후 재발하기 쉬운 index 비대화, plan lifecycle drift, reference 현재형 wording을 줄이는 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `docs/index.md`, `docs/plans/progress.md`, `docs/plans/*.md`
>
> 생성일: 2026-04-26
> 근거: 2026-04-26 문서관리 상태 점검에서 문서 체계는 lint-clean이지만 완료 기록 노출, active/draft 경계, reference 현재형 wording이 재발 후보로 확인됨

이 문서는 새 문서관리 규칙을 만들지 않는다.
문서 역할, 상태 전이, 새 문서 생성 기준, closeout 기준은 `ops.document-management-workflow`와 `ops.plan-authoring-review-workflow`를 따른다.

## Goal

- `docs.index`에서 완료된 문서관리 기록이 현재 실행 계획처럼 보이는 문제를 줄인다.
- active plan이 오래 남을 때 reference 전환 조건이 보이게 한다.
- draft plan이 부분 구현 기록과 초안 상태를 섞지 않게 재분류한다.
- reference plan이 현재 기준처럼 읽히는 표현을 owner 위임형으로 낮춘다.
- `plans.progress`가 현재 포커스와 다음 작업 중심으로 유지되게 한다.

## Scope

- primary owner: `plans.document-management-recurrence-prevention`
- secondary owners: `docs.index`, `plans.progress`, active/draft/reference plan 문서
- policy owners: `ops.document-management-workflow`, `ops.plan-authoring-review-workflow`

포함:

- `docs.index`의 `plans/` registry 노출 방식 보정
- active plan의 exit/reference 전환 조건 확인
- draft plan의 상태 재판정
- reference plan의 현재형 wording 점검
- `plans.progress` TODO와 다음 작업 압축 후보 정리

제외:

- 새 status/role 체계 추가
- 새 lint hard-fail 추가
- 새 closeout artifact 추가
- Unity scene, prefab, asset, Stitch 산출물 수정
- 제품 우선순위나 gameplay/design 판단 변경
- 완료된 reference plan 전체를 대규모로 재작성

## Current Findings

- `rules:lint`는 통과했지만, `docs.index`의 `plans/` 목록에서 완료된 문서관리/규칙 정리 기록이 현재 작업과 같은 무게로 보인다.
- active plan 일부는 남은 일이 직접 실행인지, residual 추적인지, reference 전환 대기인지 한눈에 보이지 않는다.
- draft plan 중 일부는 `first pass implemented` 같은 실행 기록을 이미 담고 있어 draft와 active/reference의 경계가 흐리다.
- reference plan 중 일부는 당시 기록이어야 하지만 `현재 상태`, `active route`, `현재 기준` 같은 표현이 많아 owner 문서처럼 읽힐 수 있다.
- `plans.progress`의 미완료 TODO가 세부 항목으로 길어지면 현재 포커스가 오래된 TODO 목록에 묻힌다.

## Target Shape

- `docs.index`는 registry를 유지하되 완료된 문서관리 기록을 묶어서 노출한다.
- active plan마다 `exit condition` 또는 reference 전환 후보 판단이 보인다.
- draft plan은 `draft 유지`, `active 전환`, `reference 전환` 중 하나로 재판정된다.
- reference plan은 현재 기준을 새로 소유하지 않고, 필요한 경우 active owner로 한 줄 위임한다.
- `plans.progress`는 lane별 현재 포커스와 다음 작업만 남기고 긴 evidence는 changelog나 owner plan으로 넘긴다.

## Phases

### Phase 1 - Index Exposure Diet

상태: completed

작업:

1. `docs.index`의 `plans/` 목록에서 문서관리 완료 기록을 한 묶음으로 낮출 수 있는지 확인한다.
2. 완료된 문서관리 reference plan은 개별 노출이 꼭 필요한 항목과 묶음으로 충분한 항목을 분리한다.
3. entry 문서가 규칙 본문이나 완료 로그를 다시 설명하지 않게 한다.

Acceptance:

- 현재 실행 plan과 완료 기록의 시각적 무게가 구분된다.
- `docs.index`에서 새 active plan과 주요 owner plan을 찾을 수 있다.
- 완료 reference plan 링크가 필요한 경우에는 여전히 도달 가능하다.

결과:

- `docs.index`의 `plans/` 목록에서 문서관리/규칙 정리 완료 기록을 별도 묶음으로 낮췄다.
- 각 reference plan은 lint registry 요구 때문에 status label entry를 유지한다.
- 이 plan은 실행 후 reference로 내려 현재 실행 계획처럼 보이지 않게 했다.

### Phase 2 - Active Plan Exit Criteria

상태: completed

작업:

1. 현재 active plan 목록을 `plans.progress`와 대조한다.
2. 각 active plan에 남은 직접 실행 조건과 reference 전환 조건이 있는지 확인한다.
3. residual이 이미 `plans.progress`, 다른 active plan, 또는 owner 문서로 이관됐으면 reference 전환 후보로 둔다.

Acceptance:

- active plan마다 왜 active인지 설명할 수 있다.
- 완료됐거나 residual만 이관된 plan은 active로 방치하지 않는다.
- 상태 변경이 있으면 파일 header와 `docs.index`가 함께 맞는다.

결과:

- `game_scene_agent_a_runtime_core_plan.md`, `game_scene_agent_b_hud_input_validation_plan.md`, `garage_ui_ux_improvement_plan.md`, `lobby_scene_ui_prefab_management_plan.md`, `lobby_scene_nova1492_model_application_plan.md`에 Lifecycle 섹션을 추가했다.
- 제품 우선순위 판단이 필요한 plan은 상태를 임의로 내리지 않고 reference 전환 조건만 명시했다.

### Phase 3 - Draft Plan Triage

상태: completed

작업:

1. draft plan 4개를 `draft 유지`, `active 전환`, `reference 전환` 후보로 나눈다.
2. 이미 구현된 first pass가 많은 draft는 남은 결정이 초안인지 실행 잔여인지 분리한다.
3. draft에 closeout 문구를 완료처럼 남기지 않는다.

Acceptance:

- draft plan이 초안, 실행 대기, 완료 기록 중 무엇인지 보인다.
- 부분 구현 기록이 많아도 현재 기준 owner처럼 읽히지 않는다.
- 상태 변경이 필요하면 `docs.index`와 같이 갱신한다.

결과:

- `stitch_llm_contract_pipeline_plan.md`, `stitch_screen_onboarding_simplification_plan.md`, `stitch_button_sound_contract_plan.md`, `game_scene_ui_ux_improvement_plan.md`에 Draft Triage 섹션을 추가했다.
- 네 문서 모두 현재는 draft 유지로 판단했다. active/reference 전환 조건은 각 문서에 남겼다.

### Phase 4 - Reference Wording Pass

상태: completed

작업:

1. reference plan에서 `현재 상태`, `현재 기준`, `active route`, `반드시`, `앞으로는` 표현을 점검한다.
2. 현재 기준을 말해야 하는 문장은 active owner 문서나 `plans.progress`로 위임한다.
3. 당시 판단 기록은 `작성 당시`, `reference로만 읽는다`, `현재 우선순위는 ...를 우선한다`처럼 낮춘다.

Acceptance:

- reference plan이 현재 규칙 본문처럼 읽히지 않는다.
- 현재 기준 owner가 필요한 경우 한 줄 위임이 보인다.
- historical/reference 가치가 있는 기록은 무리하게 삭제하지 않는다.

결과:

- `stitch_ui_ux_overhaul_plan.md`의 대표적인 `현재 상태` 표현을 `작성 당시 상태`로 낮췄다.
- 전체 reference plan 대규모 재작성은 하지 않았다. 남는 wording은 다음 문서관리 pass에서 owner가 불명확할 때만 다룬다.

### Phase 5 - Progress Compression

상태: completed

작업:

1. `plans.progress`의 미완료 TODO를 lane별로 압축할 수 있는지 검토한다.
2. dated evidence와 상세 완료 기록은 `progress_changelog.md` 또는 해당 owner plan/reference로 넘긴다.
3. 제품 우선순위 판단이 필요한 항목은 문서관리 작업에서 임의로 삭제하지 않는다.

Acceptance:

- `plans.progress`의 현재 포커스가 TODO 목록에 묻히지 않는다.
- 삭제가 아니라 이관이 필요한 항목은 owner를 분명히 한다.
- 제품/Unity/Stitch 우선순위 변경은 별도 owner 판단으로 남긴다.

결과:

- `plans.progress`의 미완료 TODO를 lane별 표로 압축했다.
- TODO 의미는 삭제하지 않고 `GameScene runtime`, `Account/Garage WebGL`, `Audio WebGL`, `Set B Garage`, `Google Login` lane으로 묶었다.

### Phase 6 - Verification And Lifecycle

상태: completed

작업:

1. 변경 후 `npm run --silent rules:lint`를 실행한다.
2. 이 plan이 더 이상 직접 실행 기준이 아니면 reference로 내리고 `docs.index`를 맞춘다.
3. 남는 항목은 `plans.progress`, 다른 active plan, 또는 owner 문서로 이관한다.

Acceptance:

- lint가 통과하거나 실패 원인이 이번 변경과 분리되어 있다.
- 이 plan의 residual owner가 보인다.
- 완료 후 active plan으로 방치하지 않는다.

결과:

- 이 문서는 실행 결과 기록으로 전환했으므로 header와 `docs.index`를 `reference`로 맞췄다.
- `npm run --silent rules:lint`가 통과했다.

## Blocked / Residual Handling

- active plan reference 전환이 제품 우선순위 판단을 요구하면 해당 항목은 `blocked`로 남기고 문서관리 patch에서 임의 변경하지 않는다.
- reference wording을 낮추다가 현재 기준 owner가 불명확하면 삭제하지 말고 `residual`로 남긴다.
- `progress.md` 압축 중 상태 의미가 불명확한 TODO는 제거하지 않고 owner 확인 후보로 둔다.
- `docs.index` 묶음 정리가 링크 발견성을 떨어뜨리면 묶음 방식은 보류하고 설명만 줄인다.

## Verification

기본 검증:

```powershell
npm run --silent rules:lint
```

필요할 때만:

```powershell
npm run --silent rules:sync-closeout
```

`rules:sync-closeout`는 운영 규칙, policy, lint, repo-local skill, rule-harness 자체를 바꾸는 경우에만 같은 변경에서 검토한다.
단순 plan 작성과 registry 갱신은 `rules:lint`와 plan authoring review로 충분하다.

## Closeout Criteria

- `docs.index`의 완료 기록 노출이 현재 실행 계획과 구분된다.
- active plan은 유지 이유 또는 reference 전환 조건이 보인다.
- draft plan은 상태가 재판정되어 있다.
- reference plan의 현재형 owner wording이 낮아져 있다.
- `plans.progress`는 현재 포커스 중심으로 압축되어 있다.
- 남은 residual이 owner와 함께 기록되어 있다.
- `npm run --silent rules:lint`가 통과하거나 blocked 이유가 분리되어 있다.

## 문서 재리뷰

- 새 문서 생성 판단: 기존 `docs_simplification_recurrence_plan.md`와 `document_management_process_tuning_plan.md`는 완료 reference다. 이번 요청은 2026-04-26 점검에서 나온 새 재발 후보를 multi-session으로 추적해야 하므로 별도 active plan으로 둔다.
- 과한점 리뷰: 새 규칙, 새 status, 새 lint, 새 artifact를 만들지 않고 index 노출, lifecycle, wording, progress 압축만 다룬다.
- 부족한점 리뷰: owner/scope, 제외 범위, phases, acceptance, residual handling, verification, closeout criteria를 포함했다.
- 수정 후 재리뷰: `progress.md` 압축과 active plan 전환이 제품 우선순위 판단을 요구할 수 있어 blocked/residual 처리에 분리했다.
- owner impact: primary `plans.document-management-recurrence-prevention`; secondary `docs.index`, `plans.progress`, affected plan docs; out-of-scope product/Unity/Stitch runtime changes, new lint, new artifact, owner policy changes.
- doc lifecycle checked: 새 active plan으로 등록한다. 기존 문서관리 reference plans는 대체하지 않고, 이 plan closeout 뒤 reference 전환 후보로 본다.
- skill trigger checked: not changed.
- plan rereview: clean
- 2026-04-26 implementation pass: Phase 1~5를 실행했다. index 노출은 완료 기록 묶음으로 낮췄고, active plan lifecycle, draft triage, reference wording, progress TODO 압축을 반영했다.
- 2026-04-26 implementation rereview: 과한점 없음. 새 규칙, 새 lint, 새 artifact를 만들지 않았다. 부족한점으로 남았던 verification은 `rules:lint` 통과로 해소됐다.
- doc lifecycle checked: implementation pass 후 이 plan은 reference로 내리고 `docs.index` 상태 라벨을 맞췄다.
- plan rereview: clean
