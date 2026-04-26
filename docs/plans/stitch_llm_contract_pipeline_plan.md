# Stitch LLM Contract Pipeline Plan

> 마지막 업데이트: 2026-04-26
> 상태: draft
> doc_id: plans.stitch-llm-contract-pipeline
> role: plan
> owner_scope: Stitch screen을 Unity prefab으로 옮길 때 parser 중심 구조를 source facts + LLM contract draft + script validation 구조로 줄이는 실행 계획
> upstream: ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.stitch-to-unity-translation-guide, tools.stitch-unity-readme
> artifacts: `in-memory://collector/*`, `in-memory://draft/*`, `in-memory://compiled/*`, `artifacts/unity/*-pipeline-result.json`, `artifacts/unity/*-scene-capture.png`

이 문서는 새 screen이 추가될 때마다 PowerShell parser가 화면 문법을 계속 배워야 하는 문제를 줄이기 위한 전환 계획이다.
규칙 본문은 `ops.stitch-data-workflow`와 `ops.stitch-structured-handoff-contract`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

## Draft Triage

- 판정: draft 유지.
- 이유: collector/validator first pass는 있으나 LLM draft 생성 자동화와 supported screen acceptance가 아직 닫히지 않았다.
- active 전환 조건: 다음 Stitch surface 작업에서 이 route를 직접 실행 기준으로 삼고, provider/prompt 범위를 제외한 acceptance를 닫아야 할 때만 active로 올린다.
- reference 전환 조건: 현재 generic draft/validate route가 충분하다고 판단되어 별도 LLM draft plan이 실행 기준이 아니게 되면 reference로 내린다.

## Scope

- primary owner: `tools/stitch-unity` execution pipeline
- secondary owner: `.stitch/contracts/schema/*.json`
- source owner: accepted Stitch `html/png`
- runtime owner: Unity prefab target

이 plan이 직접 다루는 것:

- source facts collector
- LLM contract draft handoff
- draft schema validation
- current translator와 SceneView capture 연결

범위 밖:

- OpenAI/LLM provider 선택
- prompt 품질 실험 전체
- runtime wiring correctness closeout
- visual polish backlog
- 새 target capability 설계

## Current

현재 구조:

```text
source freeze
-> thick PowerShell parser
-> in-memory compiled contract
-> preflight
-> translation
-> SceneView capture
-> visual judgment
```

현재 병목:

- parser가 화면 구조를 직접 판단한다.
- 지원되지 않는 screen은 parser 보강 없이는 `unsupported`가 된다.
- Set C dialog는 잘 맞지만, Set A lobby/form, loading/status overlay, detail panel은 추가 문법이 필요하다.

## Target

목표 구조:

```text
source freeze
-> Collect(source facts)
-> Draft(contract)
-> Validate(contract)
-> Translate(prefab)
-> Capture(evidence)
-> Judge(fidelity)
```

책임 분리:

| Step | Owner | Output |
|---|---|---|
| Collect | script | source facts |
| Draft | LLM | contract draft JSON |
| Validate | script | validated contract or blocked reason |
| Translate | script | prefab update |
| Capture | script | pipeline result, SceneView capture |
| Judge | human or LLM | pass/mismatch note |

한 줄 기준:

`script는 반복 수집/검증/실행을 맡고, LLM은 screen마다 달라지는 의미 판단만 맡는다.`

## Source Facts

collector가 뽑는 반복 정보:

- `surfaceId`
- `htmlPath`
- `imagePath`
- `target.assetPath`
- visible text list
- button text list
- icon list
- input/select 후보
- repeated card/list 후보
- color token 후보
- spacing/size class 후보
- fixed/sticky/scroll/layout hint
- viewport hint

collector output은 기본적으로 in-memory 값이다.
디버그가 필요할 때만 별도 artifact로 남긴다.

## Contract Draft

LLM draft가 판단하는 화면별 정보:

- screen purpose
- semantic blocks
- first-read order
- CTA priority
- list/card/form/status 의미
- selected/empty/error/loading state 의미
- Unity hierarchy naming proposal
- visual emphasis intent

draft는 schema validation을 통과해야 translation 입력이 된다.
통과하지 못하면 translation을 시작하지 않고 pipeline에 blocked reason을 남긴다.

## Validation

script validation이 확인하는 것:

- JSON parse 가능 여부
- schema version과 contract kind
- `surfaceId`, source refs, target path
- block id 중복
- host path 중복
- required block 누락
- required CTA 누락
- presentation element 필수값
- target capability와 block role 매칭
- empty text/color/layout 값

validation은 디자인 품질을 판단하지 않는다.
품질 판단은 capture 이후 `Judge`에서 한다.

## Migration Order

1. `Collect-StitchSourceFacts` entry를 만든다.
2. Set B와 Set C supported screens에서 source facts를 생성해 현재 compiled contract와 비교한다.
3. LLM draft output schema를 현재 `manifest/map/presentation`에 맞게 고정한다.
4. draft schema validation을 translation 전에 끼운다.
5. `Generate-StitchPresentationProfile.ps1`의 thick parser 경로를 fallback이 아니라 legacy path로 격리한다.
6. Set C dialog 1개를 LLM draft route로 통과시킨다.
7. Set A create-room modal을 parser 보강 없이 LLM draft route로 통과시킨다.
8. Set A lobby/detail 계열 중 1개를 같은 route로 통과시킨다.
9. pipeline result와 capture만 기본 evidence로 남긴다.

## Current Implementation

- `tools/stitch-unity/collectors/Collect-StitchSourceFacts.ps1` entry를 추가했다.
- collector는 `html/png + surfaceId + target prefab path`를 받아 stdout JSON으로 source facts를 반환한다.
- 기본 artifact는 만들지 않고, 필요할 때만 `-OutputPath`로 쓴다.
- `.stitch/contracts/schema/stitch-contract-draft.schema.json`을 추가했다.
- `tools/stitch-unity/validators/Test-StitchContractDraft.ps1` entry를 추가했다.
- validator는 LLM draft의 manifest/map/presentation 묶음, block alignment, target path, resolved presentation을 검사한다.
- `Invoke-StitchSurfaceTranslation.ps1 -DraftPath`는 validator를 먼저 실행하고, 통과한 draft를 in-memory manifest/map/presentation context로 넘긴다.
- draft map에 pipeline result path가 없으면 `artifacts/unity/<surface-id>-pipeline-result.json`을 기본값으로 채운다.
- validation 실패는 Unity bridge 호출 전에 pipeline에 `blockedReason`으로 남긴다.
- 아직 LLM draft 생성 자체는 자동화하지 않았다.

## Acceptance

- 새 screen 추가 시 필요한 입력이 `html/png + surfaceId + target prefab path`로 닫힌다.
- Set C dialog 1개가 LLM draft route로 translation까지 통과한다.
- Set A form/modal 1개가 PowerShell parser 문법 추가 없이 translation까지 통과한다.
- Set A lobby/detail 계열 1개가 같은 route로 blocked 또는 passed를 명확히 남긴다.
- validation 실패는 `blockedReason`으로 남고 prefab translation을 시작하지 않는다.
- 기본 저장 evidence는 `pipeline-result.json`과 `scene-capture.png`만 유지한다.
- source PNG와 SceneView capture를 비교할 수 있는 상태가 된다.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -SurfaceId <surface-id> `
  -HtmlPath <source.html> `
  -ImagePath <source.png> `
  -TargetAssetPath <target.prefab> `
  -WriteJsonArtifacts
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\surfaces\Invoke-StitchSurfaceTranslation.ps1 `
  -DraftPath <draft.json> `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab> `
  -WriteJsonArtifacts
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\stitch-unity\validators\Test-StitchContractDraft.ps1 `
  -DraftPath <draft.json> `
  -SurfaceId <surface-id> `
  -TargetAssetPath <target.prefab>
```

```powershell
npm run --silent rules:lint
```

## Residual Risks

- LLM draft 품질은 schema validation만으로는 보장되지 않는다.
- visual fidelity judgment는 capture 비교 단계가 계속 필요하다.
- target capability가 없는 새 UI는 별도 capability 작업으로 분리해야 한다.
- source facts가 너무 빈약하면 LLM draft가 흔들릴 수 있다.
