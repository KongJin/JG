# Stitch Unity Surface Generator

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: tools.stitch-unity-readme
> role: reference
> owner_scope: Stitch Unity surface generator 실행 reference, command guide, active route summary
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, ops.stitch-to-unity-translation-guide
> artifacts: `tools/stitch-unity/`, `.stitch/contracts/screens/`, `.stitch/contracts/mappings/`, `artifacts/unity/`

이 문서는 `tools/stitch-unity` 실행 reference다.
데이터 ownership은 [`docs/ops/stitch_data_workflow.md`](../../docs/ops/stitch_data_workflow.md), fidelity 판단은 [`docs/ops/stitch_to_unity_translation_guide.md`](../../docs/ops/stitch_to_unity_translation_guide.md)가 소유한다.
여기서는 `manifest.blocks[] + unity-map`을 어떤 명령으로 생성/실행하는가만 다룬다.

## 목적

- `screen manifest + unity-map` 기준으로 surface를 실행한다.
- target prefab이 없어도 preflight와 generator strategy로 surface를 다시 만든다.
- translation 뒤에 preflight / translation / pipeline artifact를 남긴다.
- generator는 계약을 읽고 적용만 한다.
- Stitch-driven script는 UI 상수나 fallback 값을 소유하지 않는다.

## 핵심 파일

- `engine/Test-StitchSurfaceGenerationPreflight.ps1`
- `engine/Resolve-UnitySurfaceMap.ps1`
- `surfaces/Invoke-StitchSurfaceTranslation.ps1`

## Map 규칙

`*.unity-map.json`은 아래를 최소로 가진다.

- `translationStrategy`
- `strategyMode`
- `target`
- `contractRefs`
- `blocks`

`unity-map`은 경로 매핑 레이어다.
여기서는 `block -> hostPath`, alias, required component, strategy 같은 연결 정보만 소유한다.
레이아웃 숫자나 시각 수치는 script가 판단하지 않는다.
필요한 값은 contract에 명시돼 있어야 하고, 누락 시 translator는 생성 대신 실패해야 한다.
기본 실행 단위는 `screen manifest + unity-map`이다.

`screen manifest`는 semantic block 계약 레이어다.
여기서는 `blocks[]`, `ctaPriority`, `states`, `validation`만 보고 surface 의미를 읽어야 한다.
manifest는 path/layout/label literal을 소유하지 않는다.

`strategyMode` 의미:

- `patch`: target asset이 있어야만 실행 가능
- `generate`: target asset 유무와 무관하게 새 asset 생성이 기본
- `generate-or-patch`: target이 있으면 재사용할 수 있지만, 없어도 generator path가 있어야 함

## 실행 순서

1. preflight
2. dependency execution
3. translation
4. pipeline result write

## 명령 예시

Set B Garage preflight:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\engine\Test-StitchSurfaceGenerationPreflight.ps1 `
  -SurfaceId garage-main-workspace `
  -AsJson
```

Set B Garage translation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId garage-main-workspace
```

Stitch-driven policy lint:

```powershell
npm run --silent stitch:policy:lint
```

## 준비되지 않은 플로우의 신호

- target prefab이 없는데 strategy가 `patch`뿐이다
- dependency 준비가 runtime 실패로만 드러난다
- translator가 contract 대신 script-side constants나 fallback에 의존한다
- pipeline artifact가 번역 실패 원인을 설명하지 못하는데도 성공처럼 보인다

## Policy Guardrail

이 lane에서 prior generator가 상수와 fallback을 소유하게 된 원인은 `source-derived generator implementation`이 script 내부 판단을 허용했기 때문이다.
현재는 이 경로를 금지한다.

- Stitch-driven script는 색상, 크기, padding, fontSize, text, RectTransform 값을 하드코딩하지 않는다.
- `Get-OptionalProperty ... -Default ...` 같은 script-side fallback을 두지 않는다.
- 계약이 불완전하면 translator는 보정하지 않고 실패해야 한다.
- `npm run --silent stitch:policy:lint`가 위 패턴을 repo-local guardrail로 검사한다.

한 줄 기준:

`surface generator`는 계약만 읽고 실행해야 하며, script가 UI 결정을 대신하면 실패로 본다.
