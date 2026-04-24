# Garage UI/UX Recovery Plan

> 마지막 업데이트: 2026-04-24
> 상태: draft
> doc_id: plans.garage-ui-ux-improvement
> role: plan
> owner_scope: Garage Stitch-to-Unity recovery 우선순위와 레이어 감량 기준
> upstream: plans.progress, design.ui-foundations, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/screens/set-b-garage-main-workspace.screen.json`, `.stitch/contracts/mappings/garage-main-workspace.unity-map.json`, `Assets/Scenes/TempScene.unity`, `artifacts/unity/stitch-garage-preflight-result.json`, `artifacts/unity/stitch-garage-translation-result.json`, `artifacts/unity/stitch-garage-pipeline-result.json`
>
> 생성일: 2026-04-13
> 근거: 2026-04-24 repo audit

레이아웃/토큰/컴포넌트 SSOT는 [`ui_foundations.md`](../design/ui_foundations.md)를 우선한다.
이 문서는 Garage UI polish backlog가 아니라, 현재 `Set B Garage` recovery를 닫기 위한 실행 순서만 유지한다.

## 현재 상태

### Repo truth snapshot

- 현재 active surface는 `Set B Garage main workspace` 하나다.
- active handoff 입력은 아래 둘뿐이다.
  - `.stitch/contracts/screens/set-b-garage-main-workspace.screen.json`
  - `.stitch/contracts/mappings/garage-main-workspace.unity-map.json`
- 현재 committed scene asset은 `Assets/Scenes/TempScene.unity`뿐이다.
- `TempScene`은 staging surface일 뿐 committed runtime SSOT가 아니다.
- pass 캡처와 historical scene artifact가 많이 남아 있지만, current recovery input으로는 쓰지 않는다.
- 병목은 visual polish 부족이 아니라 `contract -> prefab target -> fresh evidence` 루프가 current repo truth와 어긋난 상태다.

## Recovery Principles

1. 새 레이어를 추가하지 않는다.
2. active input은 `manifest + unity-map + existing SSOT`만 본다.
3. `TempScene`과 pass 캡처는 staging/reference로만 본다.
4. acceptance는 screenshot 인상평보다 `preflight / translation / pipeline` freshness를 먼저 본다.
5. `Set B`가 닫히기 전에는 다른 세트로 확장하지 않는다.

## Current Work Sequence

### 1. Surface freeze

- [ ] active source freeze를 `set-b-garage-main-workspace` 하나로 고정한다.
- [ ] pass 캡처, handoff md, old prefab pack summary를 active 입력처럼 읽지 않는다.

### 2. Prefab target truth

- [ ] `unity-map.target.assetPath` 기준의 실제 committed target을 확인한다.
- [ ] target이 없으면 scene staging 전에 prefab baseline부터 다시 세운다.
- [ ] target path, hierarchy root, hostPath를 문서와 artifact에서 같은 이름으로 맞춘다.

### 3. Evidence truth

- [ ] `preflight / translation / pipeline` artifact만 active evidence로 본다.
- [ ] current repo truth와 충돌하는 older success artifact는 historical reference로 낮춘다.
- [ ] screenshot/pass 산출물은 evidence 보조 자료로만 유지한다.

### 4. Scene assembly last

- [ ] `TempScene`은 staging surface로만 사용한다.
- [ ] concrete runtime scene은 prefab target과 fresh evidence가 닫힌 뒤 마지막에만 다시 조립한다.

## 진행 상황

| Phase | 상태 | 시작일 | 완료일 | 비고 |
|---|---|---|---|---|
| Phase 1: layer freeze | ✅ 완료 | 2026-04-24 | 2026-04-24 | active layer와 reference layer를 다시 구분 |
| Phase 2: target truth restore | 🟨 진행 중 | 2026-04-24 | - | prefab target / artifact truth를 current repo와 맞추는 중 |
| Phase 3: fresh evidence loop | ⬜ 대기 | - | - | translator path와 current artifact를 다시 닫아야 함 |
| Phase 4: scene assembly resume | ⬜ 대기 | - | - | prefab-first reset이 닫힌 뒤 재개 |

## Acceptance Checks

- [ ] active input이 `manifest + unity-map` 하나로 고정돼 있다.
- [ ] prefab target truth가 current repo와 일치한다.
- [ ] `stitch-garage-preflight-result.json`이 current route 기준으로 fresh하다.
- [ ] `stitch-garage-translation-result.json`이 current route 기준으로 fresh하다.
- [ ] `stitch-garage-pipeline-result.json`이 current route 기준으로 fresh하다.
- [ ] 그 다음에만 screenshot이나 density 비교를 보조 evidence로 읽는다.

## 가정 및 제약

1. 현재 병목은 polish 부족보다 layer duplication과 stale evidence다.
2. `Set B`는 목적이 아니라 recovery용 active surface다.
3. `Set B`가 닫히기 전에는 다른 세트 확장을 진행하지 않는다.
4. presentation code fallback으로 scene/prefab truth를 메우지 않는다.

## 관련 문서

- UI foundations SSOT: `/docs/design/ui_foundations.md`
- Stitch overhaul plan: `/docs/plans/stitch_ui_ux_overhaul_plan.md`
- MCP 개선 계획: `/docs/plans/mcp_improvement_plan.md`
- 진행 상황 SSOT: `/docs/plans/progress.md`
