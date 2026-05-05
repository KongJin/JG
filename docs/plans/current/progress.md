# 진행 상황

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: none

`plans.progress`는 현재 verdict와 다음 blocker만 소유한다. 세부 evidence, dated smoke log, closeout 판단과 artifact 디렉터리 소유는 각 active owner plan, playtest checklist, 또는 artifact를 우선한다.

## 현재 포커스

| Lane | Current owner | 현재 verdict | 다음 blocker |
|---|---|---|---|
| `GameScene / Actual Flow` | [`game_scene_flow_validation_closeout_plan.md`](../active/game_scene_flow_validation_closeout_plan.md) | single-client baseline과 targeted direct EditMode tests는 통과 | result HUD actual player-flow checklist |
| `GameScene / Multiplayer Sync` | [`runtime_validation_checklist.md`](../../owners/validation/runtime_validation_checklist.md) | Phase 5/9 code path는 있으나 2-client acceptance는 `blocked: two-client runner unavailable` | 수동 2-client session 또는 runner 구현으로 late-join, BattleEntity, Energy, Wave sync 확인 |
| `WebGL Account/Garage / Product Smoke` | [`webgl_smoke_checklist.md`](../../owners/validation/webgl_smoke_checklist.md) | Firestore/Garage 핵심 경로와 Google linking code path는 있으나 WebGL product smoke 전 | Garage save/load, account delete, Google linking, settings/accessibility WebGL smoke |
| `WebGL Audio / Product Smoke` | [`webgl-audio-closeout.md`](../active/webgl-audio-closeout.md) | SoundPlayer runtime contract는 scene-owned AudioSource host로 전환, WebGL audio product smoke 전 | 사운드 설정 UI 저장 확장, WebGL 오디오 로드/재생 smoke |
| `Audio SFX / MCP Pipeline` | [`audio_sfx_mcp_pipeline_plan.md`](../active/audio_sfx_mcp_pipeline_plan.md) | direct Suno MCP/CDP route generated, trimmed, imported, and catalog-synced all 12 UITK SFX batch keys; manual audition decision remains active | manual audition for all 12 SFX, then decide replacements/volume tweaks before UITK event wiring |
| `UI / Source Candidate Handoff` | reference route: [`non_stitch_ui_stitch_reimport_plan.md`](../reference/non_stitch_ui_stitch_reimport_plan.md) | Account/Connection source/candidate handoff는 reference로 닫힘; 새 native/mixed UI는 upstream Stitch/Unity owner route로 다시 연다 | Battle HUD/skill-selection 작업이 열릴 때 source freeze, candidate handoff, runtime/product owner 분리 |
| `Nova1492 Content / Release Gate` | [`nova1492-content-residual-plan.md`](../active/nova1492-content-residual-plan.md) | UnitParts playable 승격은 닫힘, rights/naming gate와 owner handoff가 남음 | 권리/이름 release gate, 밸런스/UI/model 후보 owner handoff |
| `Nova1492 Assembly / Profile Recovery` | historical: [`nova1492_assembly_profile_recovery_plan.md`](../historical/nova1492_assembly_profile_recovery_plan.md) | generated playable catalog와 assembly profile은 114개 지원 행 기준으로 정리됨 | 새 조립 형태를 제품 범위로 열 때만 새 owner 생성 |

## 완료 baseline

- Docs owner tree/lifecycle migration closeout은 완료된 baseline으로 본다. 근거는 [`local-doc-tree-migration-20260505.json`](../../../artifacts/rules/issue-recurrence-closeout.d/local-doc-tree-migration-20260505.json)의 `rules:lint`, docs-lint unit test, stale-path search, Unity asset hygiene 기록이다.
- Phase 0-4, 6, 8의 code path baseline은 완료 상태로 본다.
- Phase 5/9의 multiplayer acceptance는 runtime validation checklist에서, Phase 10/11의 WebGL account/garage product acceptance는 WebGL smoke checklist에서 추적한다. Phase 7 direct drag/drop execution은 actual-flow owner에서 닫혔다.
- Setup/Root drift, runtime lookup, dynamic repair 재발 방지는 reference gate로 유지하고, 새 runtime repair가 실제 작업으로 열릴 때만 feature/runtime owner로 분리한다.

## 다음 작업

- Primary lane은 GameScene actual-flow와 WebGL audio acceptance다. 각각의 active owner에서 success/blocked/mismatch로 분리해 닫고, multiplayer/WebGL account residual은 관련 playtest checklist 기준으로 추적한다.
- UI 변경은 Unity UI authoring workflow와 Stitch owner route를 먼저 확인하고, product acceptance와 candidate evidence를 분리한다.
- Audio SFX pipeline은 12개 SFX의 generation/import/catalog-sync mechanical path와 Unity manual audition decision을 분리한다. WebGL/browser audio product acceptance는 WebGL audio owner에서만 닫는다.
- 새 active plan을 열기 전에는 Audio SFX manual audition, WebGL audio smoke, Nova1492 rights/naming처럼 HITL gate로 막힌 active plan을 먼저 `active 유지`, `blocked residual 이관`, 또는 `reference 압축` 후보로 판정한다.
- `UI / Source Candidate Handoff`는 reference 압축 보존으로 내렸고, 새 Battle HUD/skill-selection UI 작업이 열리면 upstream Stitch/Unity owner route에서 다시 판단한다. `Nova1492 Content / Release Gate`는 rights/naming gate가 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- Nova1492 조립 위치 복구 기록은 historical로 내렸고, 새 조립 형태를 제품 범위로 열 때만 새 owner에서 다시 판단한다.
