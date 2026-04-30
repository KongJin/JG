# 진행 상황

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: none

`plans.progress`는 현재 verdict와 다음 blocker만 소유한다. 세부 evidence, dated smoke log, closeout 판단과 artifact 디렉터리 소유는 각 active owner plan 또는 artifact를 우선한다.

## 현재 포커스

| Lane | Active owner | 현재 verdict | 다음 blocker |
|---|---|---|---|
| `GameScene / Actual Flow` | [`game_scene_flow_validation_closeout_plan.md`](./game_scene_flow_validation_closeout_plan.md) | single-client baseline과 targeted direct EditMode tests는 통과 | result HUD actual player-flow checklist |
| `GameScene / Multiplayer Sync` | [`game_scene_multiplayer_sync_closeout_plan.md`](./game_scene_multiplayer_sync_closeout_plan.md) | Phase 5/9 code path는 있으나 2-client acceptance는 `blocked: two-client runner unavailable` | 수동 2-client session 또는 runner 구현으로 late-join, BattleEntity, Energy, Wave sync 확인 |
| `WebGL Account/Garage / Product Smoke` | [`account_garage_webgl_closeout_plan.md`](./account_garage_webgl_closeout_plan.md) | Firestore/Garage 핵심 경로와 Google linking code path는 있으나 WebGL product smoke 전 | Garage save/load, account delete, Google linking, settings/accessibility WebGL smoke |
| `WebGL Audio / Product Smoke` | [`webgl_audio_closeout_plan.md`](./webgl_audio_closeout_plan.md) | WebGL audio product smoke 전 | 사운드 설정 UI 저장 확장, WebGL 오디오 로드/재생 smoke |
| `UI / Source Candidate Handoff` | [`non_stitch_ui_stitch_reimport_plan.md`](./non_stitch_ui_stitch_reimport_plan.md) | native/mixed 후보는 source freeze -> UI Toolkit candidate -> owner handoff route로 처리 | Battle HUD/skill-selection source freeze, candidate handoff, runtime/product owner 분리 |
| `Nova1492 Content / Release Gate` | [`nova1492_content_residual_plan.md`](./nova1492_content_residual_plan.md) | UnitParts playable 승격은 닫힘, rights/naming gate와 owner handoff가 남음 | 권리/이름 release gate, 밸런스/UI/model 후보 owner handoff |

## 완료 baseline

- Phase 0-4, 6, 8의 code path baseline은 완료 상태로 본다.
- Phase 5/9의 multiplayer acceptance, Phase 10/11의 WebGL product acceptance는 위 active owner plan에서만 추적한다. Phase 7 direct drag/drop execution은 actual-flow owner에서 닫혔다.
- Setup/Root drift, runtime lookup, dynamic repair 재발 방지는 reference gate로 유지하고, 새 runtime repair가 실제 작업으로 열릴 때만 feature/runtime owner로 분리한다.

## 다음 작업

- GameScene actual-flow/multiplayer와 WebGL account/audio acceptance를 각각의 active owner에서 success/blocked/mismatch로 분리해 닫는다.
- UI 변경은 Unity UI authoring workflow와 Stitch owner route를 먼저 확인하고, product acceptance와 candidate evidence를 분리한다.
- `UI / Source Candidate Handoff`와 `Nova1492 Content / Release Gate`는 실행 success owner가 아니라 routing/gate owner로 유지하고, source freeze 또는 rights/naming gate가 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- 새 문서 추가보다 active owner 압축과 residual 이관을 우선한다.
