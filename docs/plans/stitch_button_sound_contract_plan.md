# Stitch Button Sound Contract Plan

> 마지막 업데이트: 2026-04-26
> 상태: draft
> doc_id: plans.stitch-button-sound-contract
> role: plan
> owner_scope: Stitch-to-Unity prefab 생성 시 버튼 사운드 affordance를 contract와 translator 경로에 포함시키는 실행 계획
> upstream: ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.stitch-to-unity-translation-guide, ops.unity-ui-authoring-workflow
> artifacts: `tools/stitch-unity/`, `.stitch/contracts/schema/*.json`, `Assets/Scripts/Shared/Ui/ButtonSoundEmitter.cs`, `artifacts/unity/*-pipeline-result.json`

이 문서는 Stitch source에서 Unity prefab을 만들 때 interactive button이 사운드 시스템을 모르는 상태로 생성되는 문제를 줄이기 위한 계획이다.
규칙 본문은 `ops.stitch-data-workflow`, `ops.stitch-structured-handoff-contract`, `ops.unity-ui-authoring-workflow`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

## Draft Triage

- 판정: draft 유지.
- 이유: contract vocabulary, translator assurance, evidence summary first pass는 있으나 runtime initialization과 pilot surface acceptance가 남아 있다.
- active 전환 조건: Phase 3 또는 Phase 5를 실제 구현/검증할 때 active로 올린다.
- reference 전환 조건: button sound intent 기준이 owner 문서나 translator contract로 흡수되고 이 문서가 당시 실행 기록만 남게 되면 reference로 내린다.

## Problem

현재 사운드 런타임은 `SoundRequestEvent -> SoundPlayer -> SoundCatalog` 흐름으로 연결돼 있고, 공통 UI 컴포넌트로 `ButtonSoundEmitter`도 존재한다.

하지만 Stitch-to-Unity prefab 생성 경로는 버튼을 만들 때 아래 사실을 contract나 translator 입력으로 받지 않는다.

- 어떤 노드가 interactive button인지
- 그 버튼이 기본 click인지, select인지, confirm인지
- `ButtonSoundEmitter`를 붙여야 하는지
- scene/root setup에서 emitter를 초기화할 경로가 있는지

그 결과 새 Stitch 기반 prefab은 시각적으로 버튼처럼 보이고 `Button.onClick` wiring은 생겨도, 사운드 affordance는 누락되기 쉽다.

## Scope

Primary owner:

- `tools/stitch-unity` contract validation and translation route

Secondary owners:

- `.stitch/contracts/schema/*.json`
- `Assets/Scripts/Shared/Ui/ButtonSoundEmitter.cs`
- scene/root setup that initializes shared UI runtime services

This plan covers:

- source facts에서 button 후보와 CTA priority를 사운드 의도로 연결하는 기준
- draft/compiled contract가 button sound intent를 표현하는 방법
- translator가 Unity `Button` 생성/패치 시 `ButtonSoundEmitter`를 함께 보장하는 경로
- prefab/scene verification에서 sound emitter 누락을 잡는 최소 evidence

Out of scope:

- `SoundPlayer` 런타임 재설계
- 새 오디오 clip 제작
- SoundCatalog key 체계 전체 개편
- visual fidelity acceptance
- individual screen polish
- Unity UI workflow policy 완화

## Current Findings

- `ButtonSoundEmitter`는 `Shared.Ui`에 있으며 `soundKey` 기본값은 `ui_click`이다.
- 현재 repo 검색 기준, Stitch/Unity prefab 생성 계약과 translator에는 `ButtonSoundEmitter` 연결 기준이 드러나 있지 않다.
- Lobby의 일부 View는 `PublishSound("ui_select")`, `PublishSound("ui_confirm")`를 직접 호출한다.
- Garage/Account/Skill/WaveEnd 계열 버튼은 `onClick.AddListener(...)`만 있고, 새 버튼 사운드는 prefab/component authoring에 의존한다.
- `SoundCatalog.asset`에는 `ui_click`, `ui_select`, `ui_confirm`, `garage_select` 키가 이미 있다.

## Target Shape

새 Stitch surface에서 버튼은 아래 순서로 처리된다.

```text
source facts button candidates
-> contract draft interaction intent
-> validator checks soundKey/component consistency
-> translator ensures Button + ButtonSoundEmitter
-> scene/root initializes emitters through explicit wiring
-> pipeline result reports button sound coverage
```

한 줄 기준:

`Stitch는 버튼의 의미와 CTA 등급을 주고, Unity prefab은 ButtonSoundEmitter와 초기화 계약을 가진다.`

## Sound Intent Mapping

초기 mapping은 기존 catalog key만 사용한다.

| intent | soundKey | 적용 예 |
|---|---|---|
| default-click | `ui_click` | 닫기, 설정 열기, 보조 토글 |
| select | `ui_select` | 탭, 슬롯, 리스트 항목 선택 |
| confirm | `ui_confirm` | 저장, 시작, 생성, 삭제 확인 |
| garage-select | `garage_select` | Garage part cycle / unit module 선택 |

이 표는 실행 계획의 시작점이다.
최종 규칙 본문으로 승격하려면 `ops.stitch-structured-handoff-contract` 또는 shared component catalog owner에서 별도 검토한다.

## Phases

### Phase 1 - Contract Vocabulary

상태: first pass implemented

- contract draft 또는 compiled contract에서 interactive element에 `interaction.intent`와 `interaction.soundKey`를 표현할 위치를 정한다.
- active generator input을 늘리는 경우 `.stitch/contracts/schema/*.json`에 최소 필드만 추가한다.
- field가 과하면 `requiredComponents` 또는 element `properties[]` 안에서 표현 가능한지 먼저 검토한다.

First pass:

- 새 top-level contract field를 만들지 않고, 기존 presentation `components[]`와 `properties[]`에 `ButtonSoundEmitter.soundKey`를 표현한다.
- draft templates와 generic overlay draft helper의 primary CTA에 `ui_confirm` intent를 명시했다.
- validator는 알려진 sound key 외 값이 들어오면 blocked issue로 보고한다.

Acceptance:

- button sound intent가 source facts와 CTA priority에서 추적 가능하다.
- draft validator가 알 수 없는 `soundKey`를 blocked로 잡거나 warning으로 남긴다.
- 기존 Set A/B/C passed draft가 false positive 없이 유지된다.

### Phase 2 - Translator Component Assurance

상태: first pass implemented

- translator가 `Button` component를 가진 node에 `ButtonSoundEmitter`를 보장한다.
- `soundKey`가 없으면 `ui_click`을 기본값으로 쓰되, primary CTA는 contract intent가 없으면 validator warning을 남긴다.
- Button label child 생성 규칙과 충돌하지 않도록 component 추가만 담당한다.

First pass:

- `contract-complete-translator-v1`은 map `requiredComponents` 또는 presentation `components`에 `Button`이 있으면 `ButtonSoundEmitter`를 함께 보장한다.
- source-derived presentation resolver는 임의의 `soundKey` 추론을 하지 않는다.
- explicit soundKey가 없는 버튼은 translator 경로에서 `ButtonSoundEmitter` 기본값을 유지한다.

Acceptance:

- 새 prefab 생성 route에서 interactive button마다 `ButtonSoundEmitter`가 붙는다.
- soundKey override가 serialized field에 반영된다.
- visual hierarchy나 label text placement를 바꾸지 않는다.

### Phase 3 - Explicit Runtime Initialization

상태: planned

- scene/root setup이 prefab 내 `ButtonSoundEmitter`들을 명시적으로 초기화하는 얇은 collaborator를 둔다.
- runtime child traversal이 금지된 일반 경로와 충돌하지 않게, approved UI root 또는 scene-owned UI scope에서만 수행한다.
- Lobby owner id와 battle/player owner id를 구분한다.

Acceptance:

- Lobby/Garage prefab 버튼은 Lobby EventBus와 `SoundPlayer.LobbyOwnerId`로 초기화된다.
- GameScene UI 버튼은 local player id 또는 scene owner policy를 명확히 사용한다.
- hidden global lookup이나 scene registry repair를 도입하지 않는다.

### Phase 4 - Validation And Evidence

상태: first pass implemented

- `tools/stitch-unity` pipeline result에 button sound coverage summary를 남긴다.
- Unity UI authoring workflow policy 또는 별도 validator가 `Button` without `ButtonSoundEmitter`를 review issue로 보고할 수 있는지 검토한다.
- Play Mode smoke는 우선 필수가 아니라 후속 runtime acceptance로 분리한다.

First pass:

- top-level translation result에 `buttonSoundCoverage` summary를 추가했다.
- coverage는 `buttonCount`, `soundEmitterCount`, missing paths를 보고한다.
- runtime click 재생 여부는 아직 Phase 3/5 acceptance로 남긴다.

Acceptance:

- mechanical evidence가 `buttonCount`, `soundEmitterCount`, missing paths를 보여준다.
- missing emitter가 있으면 translation success와 visual acceptance를 분리해 `blocked` 또는 `mismatch`로 보고한다.
- workflow policy를 완화해서 성공 처리하지 않는다.

### Phase 5 - Pilot Surfaces

상태: planned

- `set-b-garage-main-workspace`를 pilot으로 삼아 select/confirm/garage-select mapping을 확인한다.
- `set-a-create-room-modal` 또는 `set-c-account-delete-confirm` 중 하나를 confirm/cancel overlay pilot으로 삼는다.
- pipeline passed와 runtime click sound 확인을 분리해서 기록한다.

Acceptance:

- Garage pilot에서 slot/tab/part/save 버튼 사운드 intent가 구분된다.
- Overlay pilot에서 confirm/cancel/close 버튼의 soundKey가 확인된다.
- 기존 직접 `PublishSound(...)` View 경로와 새 prefab emitter 경로가 중복 재생을 만들지 않는다.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\validators\Test-StitchContractDraft.ps1 `
  -DraftPath Temp\StitchDraftRoute\<surface-id>-draft.json `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab>
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -DraftPath Temp\StitchDraftRoute\<surface-id>-draft.json `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab> `
  -WriteJsonArtifacts
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1
```

```powershell
npm run --silent rules:lint
```

## Residual Risks

- `ButtonSoundEmitter`가 모든 버튼에 붙으면 View가 직접 `PublishSound(...)`를 호출하는 기존 경로와 중복될 수 있다.
- source text만으로 `ui_select`와 `ui_confirm`을 항상 정확히 구분하기 어렵다.
- runtime initialization scope를 잘못 잡으면 금지된 hidden lookup처럼 변질될 수 있다.
- visual/pipeline pass만으로 실제 WebGL 오디오 재생은 보장되지 않는다.
