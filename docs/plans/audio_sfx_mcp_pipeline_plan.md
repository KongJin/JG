# Audio SFX MCP Pipeline Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.audio-sfx-mcp-pipeline
> role: plan
> owner_scope: sandraschi/suno-mcp 기반 SFX 생성, Unity audio asset import, SoundCatalog 등록, manual audition/replacement decision 실행 순서
> upstream: docs.index, ops.codex-coding-guardrails, ops.unity-ui-authoring-workflow, ops.acceptance-reporting-guardrails
> artifacts: `artifacts/audio/sfx/`

이 문서는 JG UITK/Unity 효과음 제작 파이프라인의 active owner다.
오디오 product smoke acceptance는 [`webgl_audio_closeout_plan.md`](./webgl_audio_closeout_plan.md)가 계속 소유하고, 이 문서는 효과음 생성/import/catalog-sync와 UITK event wiring 전 manual audition decision만 소유한다.

## Current Judgment

- 기본 MCP surface는 lean하게 유지하되, 오디오 생성 세션에서는 `sandraschi/suno-mcp` 계열 MCP를 직접 등록한다.
- 현재 로컬 설치는 원본 `sandraschi/suno-mcp` clone/raw endpoint가 404를 반환해서, 공개 fork인 `MeroZemory/suno-multi-mcp`를 `.codex-local/` 아래에 둔다.
- v1 기본 provider는 direct Suno MCP다. `tools/audio-mcp`는 Suno 생성 wrapper가 아니라 JG manifest, downloaded asset import, Unity `SoundCatalog` sync helper로만 둔다.
- Unity `SoundCatalog` 등록은 다운로드된 audio asset의 Unity `.meta` GUID가 생긴 뒤에 수행한다.
- Suno credential, browser session, cookies는 repo에 남기지 않는다.
- 12개 UITK SFX batch keys는 direct Suno Sounds UI/CDP route로 생성, 다운로드, trim, Unity import, `SoundCatalog` sync까지 기계 검증을 통과했다.
- 남은 active gate는 Unity manual audition으로 실제 음질/길이/볼륨을 확인하고, replacement 또는 volume tweak 여부를 결정하는 것이다.

## Acceptance

| Item | Required evidence | Closeout |
|---|---|---|
| Suno MCP route | direct Suno MCP 등록 절차와 session/credential 비커밋 규칙 문서화 | success / blocked / mismatch |
| SFX batch manifest | 12개 UITK soundKey batch와 prompt list를 dry-run으로 생성 가능 | success - manifest created at `artifacts/audio/sfx/sfx-batch-manifest.json` |
| Downloaded asset import | `artifacts/audio/inbox`의 soundKey 파일을 Unity audio root로 복사 가능 | success - all 12 batch keys imported to `Assets/Audio/UI` |
| Unity catalog sync | `.meta` GUID가 있는 다운로드 파일을 `SoundCatalog.asset`에 중복 key 없이 등록/갱신 가능 | success - all 12 batch keys synced; duplicate keys none |
| Manual audition decision | Unity에서 12개 SFX를 재생 확인하고 reject/replacement/volume tweak 결정을 기록 | success / blocked / mismatch |

## Execution Rule

- `.mcp.json` 기본값에는 Suno MCP나 audio helper MCP를 상시 등록하지 않는다.
- 생성 작업 때는 direct Suno MCP를 등록하고, Unity import/sync가 필요할 때만 `tools/audio-mcp` helper를 optional 등록한다.
- 실제 Suno 웹 자동화 smoke는 user Suno session이 필요하므로, session이 없으면 generation acceptance를 `blocked`로 남긴다. 현재는 일반 Chrome 로그인 profile을 CDP로 재사용해 `ui_click` generation smoke를 통과했다.
- 다운로드 asset은 `Assets/Audio/UI` 또는 `Assets/Audio/SFX` 아래로만 허용한다.

## Evidence

- `ui_click` prompt: `short futuristic game UI button click, 0.2 seconds, dry tactile digital tick, clean sci-fi interface, no melody, no vocals, isolated one-shot`
- `ui_click` Suno generated candidates:
  - `https://suno.com/song/537d9d97-b242-4e05-83e2-0031df0631f8`
  - `https://suno.com/song/9cf2ae94-c12d-4f1f-8c94-9709a3b1db89`
- Imported Unity asset: `Assets/Audio/UI/ui_click.mp3`
- Catalog entry: `ui_click` -> GUID `cffa487e334c3f94e9ff2708c9327552`, volume `0.75`, spatialBlend `0`, cooldown `0.05`
- `garage_save` Suno generated candidates:
  - `https://suno.com/song/1a879b42-cef1-4a84-9dd1-a89b367ab89c`
  - `https://suno.com/song/9a788b3f-debc-4e05-8800-817f65b9eef5`
- `lobby_ready` Suno generated candidates:
  - `https://suno.com/song/7c5bfbd5-2eca-4e17-92fb-1a2644f8a83e`
  - `https://suno.com/song/ff48faac-9335-47e3-9c89-6ebc3cb0d2d2`
- Imported Unity assets:
  - `Assets/Audio/UI/garage_save.mp3` trimmed to about `0.648s`
  - `Assets/Audio/UI/lobby_ready.mp3` trimmed to about `0.504s`
- Catalog entries:
  - `garage_save` -> GUID `de69fc5d1e23da946a976bf666df2a82`, volume `0.85`, spatialBlend `0`, cooldown `0.05`
  - `lobby_ready` -> GUID `2e505f403b272c44baac097bb8954ffe`, volume `0.82`, spatialBlend `0`, cooldown `0.05`
- Remaining batch generation evidence:
  - `artifacts/audio/sfx/remaining_sound_generate_result.json`
  - `artifacts/audio/sfx/remaining_download_result.json`
- Remaining imported keys: `ui_select`, `ui_confirm`, `ui_back`, `ui_error`, `ui_retry`, `garage_slot_select`, `garage_part_select`, `battle_slot_select`, `skill_select`
- Current manifest status: all 12 items are `catalog-synced` with source URL and trimmed duration.
- Mechanical validation: `audio:mcp:test`, `rules:lint`, Unity asset refresh, and `dotnet build JG.slnx -v:minimal` passed with 0 warnings and 0 errors.

## Residual

- UITK event wiring은 이 plan의 v1 scope 밖이다.
- WebGL 브라우저 오디오 재생/설정 저장 acceptance는 WebGL audio owner에서 닫는다.
- 12개 SFX manual audition decision은 이 plan의 남은 active gate다.
- Manual audition 후 reject된 clips는 `_alt` 후보 또는 재생성 후보로 교체한다.

owner impact:

- primary: `plans.audio-sfx-mcp-pipeline`
- secondary: `ops.unity-ui-authoring-workflow`, `plans.webgl-audio-closeout`
- out-of-scope: UITK click event wiring, WebGL autoplay/product smoke, Suno credential storage, broad audio UX redesign

doc lifecycle checked:

- active 유지. SFX generation/download/catalog sync는 mechanical success로 닫혔고, manual audition decision이 success/blocked/mismatch로 닫히면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - SFX generation/download/catalog sync와 manual audition decision scope checked
