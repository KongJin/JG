# Audio SFX MCP Pipeline Plan

> 마지막 업데이트: 2026-05-05
> 상태: active
> age: 1 day
> doc_id: plans.audio-sfx-mcp-pipeline
> role: plan
> owner_scope: 12개 UITK SFX manual audition verdict와 replacement/volume tweak decision
> upstream: docs.index, ops.codex-coding-guardrails, ops.acceptance-reporting-guardrails
> artifacts: none
> blocked owner: Unity manual audition verdict owner
> next decision: 12개 SFX를 Unity에서 재생하고 keep/replace/volume-tweak 중 하나를 기록

이 문서는 JG UITK/Unity 효과음 제작 파이프라인의 남은 active HITL gate만 소유한다.
Suno/CDP generation, download, trim, Unity import, `SoundCatalog` sync의 mechanical closeout evidence는 [`audio_sfx_mcp_pipeline_mechanical_closeout.md`](../reference/audio_sfx_mcp_pipeline_mechanical_closeout.md)로 압축 보존한다.
WebGL 브라우저 오디오 재생/설정 저장 acceptance는 [`webgl-audio-closeout.md`](./webgl-audio-closeout.md)가 계속 소유한다.

## Current Judgment

- 12개 UITK SFX batch keys는 direct Suno Sounds UI/CDP route로 생성, 다운로드, trim, Unity import, `SoundCatalog` sync까지 기계 검증을 통과했다.
- 남은 active gate는 Unity manual audition으로 실제 음질/길이/볼륨을 확인하고, replacement 또는 volume tweak 여부를 결정하는 것이다.
- manual audition을 이번 audio pass에서 진행하지 않기로 결정하면 이 active plan은 닫고, audition residual은 `plans.progress` 또는 새 audio/product owner에 한 줄로 이관한다.

## HITL Gate

- Decision owner: Unity에서 SFX를 실제로 들어볼 수 있는 사람의 manual audition verdict.
- Next action: 12개 soundKey를 Unity에서 재생하고 `keep`, `replace`, `volume-tweak`, `blocked` 중 하나를 기록한다.
- Closeout: 12개 verdict가 모두 기록되고, reject된 clips의 replacement 또는 volume tweak follow-up owner가 정해지면 `success`, `blocked`, `mismatch` 중 하나로 닫는다.

## Manual Audition Verdict

| soundKey | Verdict | Notes |
|---|---|---|
| `ui_click` | blocked | audition not recorded |
| `ui_select` | blocked | audition not recorded |
| `ui_confirm` | blocked | audition not recorded |
| `ui_back` | blocked | audition not recorded |
| `ui_error` | blocked | audition not recorded |
| `ui_retry` | blocked | audition not recorded |
| `garage_save` | blocked | audition not recorded |
| `garage_slot_select` | blocked | audition not recorded |
| `garage_part_select` | blocked | audition not recorded |
| `lobby_ready` | blocked | audition not recorded |
| `battle_slot_select` | blocked | audition not recorded |
| `skill_select` | blocked | audition not recorded |

## Residual

- UITK event wiring은 이 plan의 v1 scope 밖이다.
- WebGL 브라우저 오디오 재생/설정 저장 acceptance는 WebGL audio owner에서 닫는다.
- Manual audition 후 reject된 clips는 `_alt` 후보 또는 재생성 후보로 교체한다.

owner impact:

- primary: `plans.audio-sfx-mcp-pipeline`
- secondary: `plans.audio-sfx-mcp-pipeline-mechanical-closeout`, `plans.webgl-audio-closeout`
- out-of-scope: UITK click event wiring, WebGL autoplay/product smoke, Suno credential storage, broad audio UX redesign

doc lifecycle checked:

- active 유지. Mechanical evidence는 reference로 압축했고, active는 12개 SFX audition verdict만 소유한다.
- plan rereview: clean - active HITL gate와 reference mechanical closeout split checked
