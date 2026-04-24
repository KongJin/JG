# Stitch Unity Surface Generator

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: tools.stitch-unity-readme
> role: reference
> owner_scope: Stitch Unity surface generator 실행 reference, command guide, active route summary
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.stitch-to-unity-translation-guide
> artifacts: `tools/stitch-unity/`, `artifacts/stitch/`, `artifacts/unity/`, `in-memory://compiled/*`

이 문서는 `tools/stitch-unity` 실행 reference다.
데이터 ownership은 [`docs/ops/stitch_data_workflow.md`](../../docs/ops/stitch_data_workflow.md), fidelity 판단은 [`docs/ops/stitch_to_unity_translation_guide.md`](../../docs/ops/stitch_to_unity_translation_guide.md)가 소유한다.
여기서는 `source freeze -> execution contracts -> prefab/output`를 어떤 명령으로 생성/실행하는가만 다룬다.

## 목적

- accepted source freeze에서 execution contracts를 다시 준비한 뒤 surface를 실행한다.
- target prefab이 없어도 preflight와 generator strategy로 surface를 다시 만든다.
- translation 뒤에 preflight / translation / pipeline artifact를 남긴다.
- blocked로 멈춘 경우 preflight 또는 pipeline artifact에 `blockedReason`을 남긴다.
- generator는 계약을 읽고 적용만 한다.
- Stitch-driven script는 UI 상수나 fallback 값을 소유하지 않는다.
- pipeline artifact는 raw inspection dump가 아니라 stage status와 artifact path를 묶은 얇은 요약으로 유지한다.

## 핵심 파일

- `engine/Test-StitchSurfaceGenerationPreflight.ps1`
- `engine/Resolve-UnitySurfaceMap.ps1`
- `presentations/Generate-StitchPresentationProfile.ps1`
- `presentations/Resolve-StitchPresentationContract.ps1`
- `surfaces/Invoke-StitchSurfaceTranslation.ps1`

## Map 규칙

`unity-map` contract shape는 아래를 최소로 가진다.

- `translationStrategy`
- `strategyMode`
- `target`
- `contractRefs`
- `blocks`

`unity-map`은 경로 매핑 레이어다.
여기서는 `block -> hostPath`, alias, required component, strategy 같은 연결 정보만 소유한다.
레이아웃 숫자나 시각 수치는 script가 판단하지 않는다.
필요한 값은 contract에 명시돼 있어야 하고, 누락 시 translator는 생성 대신 실패해야 한다.
기본 실행 단위는 source freeze에서 다시 준비된 execution contracts다.

`screen manifest`는 semantic block 계약 레이어다.
여기서는 `blocks[]`, `ctaPriority`, `states`, `validation`만 보고 surface 의미를 읽어야 한다.
manifest는 path/layout/label literal을 소유하지 않는다.

`presentation-contract`는 source-derived presentation 레이어다.
여기서는 `sourceRefs`, `derivedFrom`, `unresolvedDerivedFields`, `elements[]`, `extractionStatus`만 보고 translation-ready presentation 값을 읽어야 한다.
hand-authored literal이나 translator fallback을 owner처럼 올리지 않는다.

source를 읽는 내부 생성 단계는 extractor와 compiled contract 준비를 위한 실행 구현이다.
기본 경로는 screen별 profile/presentation file을 남기지 않는다.

`strategyMode` 의미:

- `patch`: target asset이 있어야만 실행 가능
- `generate`: target asset 유무와 무관하게 새 asset 생성이 기본
- `generate-or-patch`: target이 있으면 재사용할 수 있지만, 없어도 generator path가 있어야 함

## 실행 순서

1. source freeze 확인
2. execution contracts 준비
3. preflight
4. dependency execution
5. translation
6. `TempScene + SceneView capture` when the surface has a configured review route
7. pipeline result write

지원되는 화면 구조에서는:

- `Resolve-StitchPresentationContract.ps1`와 `Invoke-StitchSurfaceTranslation.ps1`가 실행 전에 source html/png에서 필요한 내부 생성 단계를 다시 수행한다.
- 즉 `source -> in-memory presentation-contract + manifest/map compile -> translation`이 한 루프로 닫힌다.

source 구조를 읽는 내부 준비 단계가 지원되면:

- `Invoke-StitchSurfaceTranslation.ps1`는 실행 전에 source에서 필요한 execution contract를 다시 준비한다.
- screen별 manifest/map/presentation file은 active route에 남기지 않는다.
- preflight / pipeline artifact에는 내부 provenance가 함께 남을 수 있지만, 이 필드는 디버그 해석용일 뿐 실행 규칙을 대신하지 않는다.
- source freeze 기본 lookup은 `.stitch/designs/*<surface>.html/png`와 `artifacts/stitch/**/meta.json`를 우선 읽는다.

기본 기준:

- 기본 실행 경로는 `source freeze -> execution contracts -> translation`이다.
- 새 surface onboarding은 저장 파일을 늘리는 것이 아니라 source에서 바로 execution contract를 준비하는 쪽으로 닫는다.
- script가 조용히 다른 경로로 내려가면 안 된다.

## 명령 예시

Stitch-driven policy lint:

```powershell
npm run --silent stitch:policy:lint
```

Example presentation extraction:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\presentations\Resolve-StitchPresentationContract.ps1 `
  -SurfaceId account-delete-confirm
```

Example translation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId account-delete-confirm
```

Compiled contract debug dump:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId account-delete-confirm `
  -CompiledContractDebugPath artifacts/unity/account-delete-confirm-compiled-contract.json
```

Example review capture only:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceReviewCapture.ps1 `
  -SurfaceId account-delete-confirm
```

표준 review loop:

- accepted overlay surface는 `translation -> TempScene review prep -> SceneView capture`를 한 루프로 본다.
- 현재 generic review route는 `workspace-screen-v1`, `overlay-dialog-v1` family를 읽는다.
- route 호출 전 `Temp/StitchRuntimeReview/request.json`에 target prefab request를 쓰고, Unity menu는 그 request를 읽어 TempScene review surface를 준비한다.
- `Invoke-StitchSurfaceTranslation.ps1`는 기본적으로 review route가 있는 surface에서 `scene-capture.png`를 함께 갱신한다.
- 필요하면 `-SkipReviewCapture`로 생략할 수 있다.
- `screenshot/capture`는 현재 Stitch prefab review 표준 경로가 아니다.

한 줄 기준:

`TempScene + SceneView capture`는 translation fidelity review용 staging proof다. runtime/mobile truth 자체는 아니다.

## Presentation Contract Gate

translation 전에는 아래를 먼저 만족해야 한다.

- compiled execution contract가 source-derived presentation contract를 준비했다.
- `extractionStatus = resolved`
- `unresolvedDerivedFields`는 translation blocker를 숨기지 않는다.

`pending-source-derivation` 상태는 skeleton/debug baseline까지만 허용한다.
active translation success로 보고 진행하면 안 된다.

## 준비되지 않은 플로우의 신호

- target prefab이 없는데 strategy가 `patch`뿐이다
- dependency 준비가 runtime 실패로만 드러난다
- presentation contract가 없거나 `pending-source-derivation`인데 translation을 강행한다
- translator가 contract 대신 script-side constants나 fallback에 의존한다
- pipeline artifact가 번역 실패 원인을 설명하지 못하는데도 성공처럼 보인다

## Policy Guardrail

이 lane은 script 내부 판단이 UI owner처럼 커지는 걸 금지한다.

- Stitch-driven script는 색상, 크기, padding, fontSize, text, RectTransform 값을 하드코딩하지 않는다.
- `Get-OptionalProperty ... -Default ...` 같은 script-side fallback을 두지 않는다.
- 계약이 불완전하면 translator는 보정하지 않고 실패해야 한다.
- presentation contract 값은 source-derived extraction으로만 채운다.
- 손으로 적은 literal을 source-derived truth처럼 취급하지 않는다.
- `npm run --silent stitch:policy:lint`가 위 패턴을 repo-local guardrail로 검사한다.

한 줄 기준:

`surface generator`는 계약만 읽고 실행해야 하며, script가 UI 결정을 대신하면 실패로 본다.
