# Garage UI/UX Recovery Plan

> 마지막 업데이트: 2026-04-25
> 상태: draft
> doc_id: plans.garage-ui-ux-improvement
> role: plan
> owner_scope: Set B Garage Stitch-to-Unity recovery의 현재 범위, 실행 순서, acceptance closeout 기준
> upstream: plans.progress, design.ui-foundations, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `artifacts/stitch/11729197788183873077/d440ad9223a24c0d8e746c7236f7ef27/screen.html`, `artifacts/stitch/11729197788183873077/d440ad9223a24c0d8e746c7236f7ef27/screen.png`, `.stitch/contracts/intakes/set-b-garage-main-workspace.intake.json`, `in-memory://compiled/garage-main-workspace/screen-manifest`, `in-memory://compiled/garage-main-workspace/unity-map`, `in-memory://compiled/garage-main-workspace/presentation-contract`, `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab`, `artifacts/unity/set-b-garage-main-workspace-preflight-result.json`, `artifacts/unity/set-b-garage-main-workspace-translation-result.json`, `artifacts/unity/set-b-garage-main-workspace-pipeline-result.json`, `artifacts/unity/set-b-garage-main-workspace-scene-capture.png`
>
> 생성일: 2026-04-13
> 근거: 2026-04-25 Set B recovery audit

레이아웃/토큰/컴포넌트 SSOT는 [`ui_foundations.md`](../design/ui_foundations.md)를 우선한다.
운영 규칙 본문은 [`ops.stitch-data-workflow`](../ops/stitch_data_workflow.md), [`ops.unity-ui-authoring-workflow`](../ops/unity_ui_authoring_workflow.md), [`ops.acceptance-reporting-guardrails`](../ops/acceptance_reporting_guardrails.md)가 소유한다.
이 문서는 `Set B Garage` recovery를 닫기 위한 실행 순서와 현재 상태만 유지한다.

## Acceptance Lock

- 대상: `Set B Garage main workspace` Stitch-to-prefab recovery
- 맞아야 하는 것: accepted source freeze의 주요 구조, 블록 의미, CTA 위계, prefab target truth, fresh evidence가 한 줄로 닫혀야 한다
- 틀리면 실패인 것: missing prefab target, stale artifact 의존, compiled contract의 의미 축소, summary-card 누락, review/fidelity proof 부재, placeholder-like output
- 무엇과 비교할지: accepted source freeze `artifacts/stitch/.../screen.html/png` + source-derived execution contracts + committed prefab target + fresh translation/review evidence

## 현재 문제 요약

현재 병목은 polish 부족이 아니라 `source freeze -> execution contracts -> committed prefab target -> fresh evidence` 루프가 서로 다른 truth를 가리키는 점이다.

핵심 문제:

1. target prefab 부재 문제는 2026-04-25 translation 재실행으로 일단 복구됐다. 이제는 generated baseline이 accepted source 의미를 충분히 보존하는지가 남은 쟁점이다.
2. execution contract는 screen별 stored file이 아니라 compiled in-memory route가 authority다. active 판단에서 historical file trace를 다시 execution owner처럼 읽지 않게 유지해야 한다.
3. `summary-card` semantic block은 2026-04-25에 compiled contract와 translation path로 복구했다. 남은 일은 이 summary가 실제 fidelity 기대를 충분히 만족하는지 검토하는 것이다.
4. old `stitch-garage-*` artifact는 still historical로 남아 있어도 되지만, active 판단에는 더 이상 끼어들지 않게 유지해야 한다.
5. Set B review capture route와 `set-b-garage-main-workspace-scene-capture.png`는 생겼지만, 이 proof로 visual fidelity final judgment까지 바로 닫을 수 있는지는 아직 검토가 필요하다.
6. source provenance 일부가 compiled contract에서 약해지고, 일부 값은 heuristic/default에 기대고 있다.
7. runtime/wiring correctness는 이 plan의 직접 acceptance가 아니라 shared `Account/Garage` validation lane으로 이관돼야 한다.

## 범위

이 plan은 아래만 다룬다.

- `set-b-garage-main-workspace` source freeze
- source-derived execution contract parity
- `GaragePageRoot.prefab` target truth
- fresh `preflight / translation / pipeline` evidence
- Set B용 fidelity review proof
- visual fidelity final judgment에 필요한 comparison closeout

이 plan이 직접 소유하지 않는 것:

- Set A/C/D/E 확장
- Garage 장기 polish backlog 전반
- account/delete-confirm overlay lane
- battle HUD나 GameScene 작업
- Garage save/load WebGL validation
- Garage settings interaction validation
- shared Account/Garage runtime correctness closeout

## Recovery Principles

1. active 입력은 항상 `source freeze -> execution contracts` 순서로 본다.
2. source-derived compiled contract와 stored contract가 다르면 둘을 함께 성공으로 취급하지 않는다.
3. 실제 committed prefab target이 닫히기 전에는 translation success artifact를 acceptance proof로 올리지 않는다.
4. screenshot 인상평보다 contract parity, prefab truth, fresh artifact, review proof를 먼저 본다.
5. Set B가 닫히기 전에는 다른 세트 확장을 진행하지 않는다.
6. `blocked`와 `mismatch`를 success처럼 포장하지 않는다.
7. existing TempScene review route는 visual fidelity proof 전용으로만 쓴다.
8. Garage 전용 runtime host, Garage 전용 smoke entry, Garage 전용 result artifact는 새로 만들지 않는다.

## Workstreams

### 1. Source Freeze Lock

목표:

- active source freeze를 `artifacts/stitch/11729197788183873077/d440ad9223a24c0d8e746c7236f7ef27/screen.html/png` 하나로 고정한다.
- handoff md, pass capture, old summary artifact를 active 입력처럼 쓰지 않게 정리한다.

작업:

- accepted source freeze 경로와 기준 화면을 다시 명시한다.
- `.stitch/handoff/set-b-garage.md`와 pass capture 묶음은 reference lane으로만 읽는다.
- current execution이 어떤 source file을 읽는지 artifact와 문서에서 같은 경로로 맞춘다.

### 2. Contract Authority And Parity

목표:

- `intake -> compiled in-memory manifest/map/presentation` 사이의 의미 차이를 제거한다.

작업:

- `summary-card`, `preview`, `save-dock`, `settings quiet action` 같은 핵심 meaning block이 intake와 compiled contract에서 모두 유지되는지 비교한다.
- compiled contract가 현재 active execution authority라는 점을 문서와 artifact에서 같은 말로 유지한다.
- compiled generator가 source metadata를 축소하는 부분은 provenance를 잃지 않게 보강한다.
- heuristic/default로 채운 값이 source-derived truth처럼 보이는 지점을 찾아 source 재해석 또는 explicit unresolved 처리로 바꾼다.

decision gate:

- compiled-family path가 핵심 meaning block과 provenance를 보존하면 이 경로를 active execution authority로 유지한다.
- 이 조건을 만족하지 못하면 `blocked`로 두고, Set B에 한해 explicit stored execution contract path를 다시 활성 기준으로 올릴지 결정한다.

### 3. Prefab Target Truth Restore

목표:

- `Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab`가 실제 committed target으로 닫히게 만든다.

작업:

- repo 기준 실제 prefab asset 존재 여부를 먼저 확인한다.
- target이 비어 있거나 누락되면 scene staging 전에 prefab baseline부터 다시 세운다.
- generator hostPath와 실제 prefab hierarchy root/path가 같은 이름을 쓰게 맞춘다.
- stale alias, old path, summary-only helper path를 active truth에서 제거한다.

### 4. Evidence Reset

목표:

- Set B의 active evidence를 current route 기준으로 다시 생성한다.

작업:

- old `stitch-garage-*` artifact는 historical/reference로 낮춘다.
- current active evidence는 `set-b-garage-main-workspace-preflight/translation/pipeline`만 본다.
- blocked로 멈추면 `blockedReason`이 artifact에 남는지 확인한다.
- artifact timestamp와 current source/prefab truth를 다시 맞춘다.

### 5. Fidelity Review Proof

목표:

- Set B에도 translation pass 외에 fidelity를 다시 볼 수 있는 proof를 만든다.

작업:

- Set B용 review capture route를 추가하거나, 그에 준하는 explicit staging proof를 정한다.
- current route는 `artifacts/unity/set-b-garage-main-workspace-scene-capture.png`를 표준 review proof 이름으로 사용한다.
- 선택한 review proof artifact path를 문서와 실행 결과에서 같은 이름으로 고정한다.
- review proof는 `390x844` 기준으로 남긴다.
- slot strip, focus bar, editor dominance, preview completion, summary completion, persistent save dock, quiet settings를 한 번에 볼 수 있어야 한다.

### 6. Shared Validation Handoff

목표:

- Set B prefab lane이 직접 소유하지 않는 validation을 shared `Account/Garage` lane으로 명확히 넘긴다.

작업:

- Garage save action이 실제 사용자 흐름에서 도달 가능한지, Garage settings overlay open -> close가 유지되는지, Garage save/load WebGL validation이 Set B UI 기준으로 계속 유효한지를 shared lane backlog로 유지한다.
- 이 validation들은 `Garage-only smoke`나 전용 result artifact로 새로 만들지 않고, existing `Account/Garage` validation 문맥에서 계속 추적한다.
- Set B closeout에서는 shared lane 미완료를 숨기지 않되, 이 lane의 직접 acceptance와 섞어서 다시 blocked로 돌리지 않는다.

### 7. Document Sync

목표:

- 문서가 current route를 다시 잘 가리키게 만든다.

작업:

- `garage_ui_ux_improvement_plan`의 artifact 이름과 acceptance를 current route 기준으로 유지한다.
- 필요하면 `stitch_ui_ux_overhaul_plan`, `progress.md`, 관련 SSOT의 wording을 source-derived compiled execution 기준으로 맞춘다.
- plan closeout 전 최신 재리뷰가 clean인지 확인한다.

## Implementation Order

1. source freeze와 active artifact scope를 다시 잠근다.
2. contract authority를 하나로 정하고 parity diff를 만든다.
3. missing meaning block과 source provenance 손실을 먼저 고친다.
4. committed prefab target을 실제 repo에 다시 닫는다.
5. current route 기준 fresh preflight / translation / pipeline artifact를 재생성한다.
6. Set B용 fidelity review proof를 추가한다.
7. shared `Account/Garage` validation handoff를 문서에 잠근다.
8. 관련 plan/SSOT wording을 current truth에 맞춰 동기화한다.

## Acceptance Checks

- [x] active source freeze가 `artifacts/stitch/11729197788183873077/d440ad9223a24c0d8e746c7236f7ef27/screen.html/png` 하나로 고정돼 있다.
- [x] compiled execution contract가 accepted source의 핵심 meaning block을 누락하지 않는다.
- [x] `summary-card`와 equivalent evaluative summary meaning이 execution path에서 유지된다.
- [x] current repo에 committed prefab target `GaragePageRoot.prefab`가 실제로 존재한다.
- [x] generator hostPath와 실제 prefab hierarchy가 current route 기준으로 맞는다.
- [x] `set-b-garage-main-workspace-preflight-result.json`이 fresh하다.
- [x] `set-b-garage-main-workspace-translation-result.json`이 fresh하다.
- [x] `set-b-garage-main-workspace-pipeline-result.json`이 fresh하다.
- [ ] blocked 상태면 artifact에 `blockedReason`이 남는다.
- [x] Set B용 fidelity review proof가 fresh하다.
- [x] Set B용 fidelity review proof artifact path가 문서와 실행 결과에서 같은 이름으로 닫혀 있다.
- [x] Garage runtime/wiring correctness는 shared `Account/Garage` validation lane 소유로 분리돼 있다.
- [x] existing TempScene review route가 visual fidelity proof 전용으로만 남아 있다.
- [ ] 그 다음에만 screenshot 인상평이나 density 비교를 보조 evidence로 읽는다.

## Residual Risk Rules

- compiled generator가 `summary-card` 같은 핵심 meaning을 계속 보존하지 못하면 `blocked`다.
- target prefab이 artifact상 존재해도 실제 repo file tree에 없으면 `mismatch`다.
- review proof 없이 translation pass만 있으면 `blocked`다.
- shared `Account/Garage` validation 미완료는 별도 open item이지만, Set B prefab lane 직접 acceptance를 자동으로 `blocked`로 되돌리지는 않는다.
- review capture는 visual fidelity proof일 뿐 runtime proof가 아니다.

## 진행 상황

| Phase | 상태 | 시작일 | 완료일 | 비고 |
|---|---|---|---|---|
| Phase 1: source and authority lock | ✅ 완료 | 2026-04-25 | 2026-04-25 | active source freeze 경로와 review proof 이름을 current route로 잠금 |
| Phase 2: contract parity repair | ✅ 완료 | 2026-04-25 | 2026-04-25 | compiled contract에 source provenance와 `summary-card` execution path를 다시 올림 |
| Phase 3: prefab target restore | ✅ 완료 | 2026-04-25 | 2026-04-25 | `GaragePageRoot.prefab` baseline을 current route로 다시 생성함 |
| Phase 4: fresh evidence loop | ✅ 완료 | 2026-04-25 | 2026-04-25 | current route 기준 preflight/translation/pipeline/scene capture를 다시 생성함 |
| Phase 5: fidelity final judgment | 🟨 진행 중 | 2026-04-25 | - | review proof는 생겼고, 이제 visual fidelity comparison closeout만 남음 |

## 관련 문서

- UI foundations SSOT: `/docs/design/ui_foundations.md`
- Stitch data workflow SSOT: `/docs/ops/stitch_data_workflow.md`
- Unity UI authoring SSOT: `/docs/ops/unity_ui_authoring_workflow.md`
- Acceptance/reporting guardrails: `/docs/ops/acceptance_reporting_guardrails.md`
- Stitch overhaul plan: `/docs/plans/stitch_ui_ux_overhaul_plan.md`
- 진행 상황 SSOT: `/docs/plans/progress.md`
