# Stitch Data Workflow

> 마지막 업데이트: 2026-04-24
> 상태: active
> doc_id: ops.stitch-data-workflow
> role: ssot
> owner_scope: Stitch working data ownership, source freeze, Unity handoff 운영
> upstream: design.ui-reference-workflow, plans.stitch-ui-ux-overhaul, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/screens/`, `.stitch/contracts/mappings/`, `.stitch/contracts/presentations/`, `artifacts/stitch/`

이 문서는 JG에서 `Stitch` 산출물을 어떻게 저장하고 Unity handoff로 넘길지 정하는 단일 운영 기준이다.
현재 활성 흐름은 `Stitch source freeze -> execution contracts -> Unity translation`이다.

현재 기본 실행은 source를 다시 읽어 필요한 execution contract를 준비한 뒤 translation까지 이어지는 흐름이다.
지원되는 화면 구조에서는 저장된 manifest/map를 실행 주인으로 보지 않고 parity/debug reference로만 남길 수 있다.
새 surface onboarding은 예전 저장 파일을 늘리는 방식이 아니라 source에서 바로 execution contract를 준비하는 쪽으로 닫는다.
조용한 우회 실행은 허용하지 않는다.

## 목적

- `Stitch prompt -> accepted screen -> Unity prefab 생성` 루프의 파일 경계를 고정한다.
- 같은 역할의 문서와 JSON이 여러 경로에서 중복되지 않게 한다.
- source artifact와 실행 contract를 분리한다.

## 역할 구분

### Stitch가 맡는 것

- visual language 탐색
- accepted screen 결정
- semantic block, CTA, state, validation 의도 정리

### Unity가 맡는 것

- runtime hierarchy
- serialized reference
- 실제 prefab/scene 저장
- preflight / translation / pipeline evidence

한 줄 기준:

`Stitch는 의미 계약을 주고, Unity는 실행 결과를 만든다.`

## 활성 파일 소유권

- `artifacts/stitch/<project>/<screen>/screen.html`
- `artifacts/stitch/<project>/<screen>/screen.png`
  - source freeze artifact
- `.stitch/contracts/screens/*.json`
  - semantic block 계약
- `.stitch/contracts/mappings/*.json`
  - Unity binding
- `.stitch/contracts/presentations/*.json`
  - source-derived presentation 계약
- `.stitch/contracts/schema/*.json`
  - 활성 schema
## 비활성 파일 소유권

아래는 활성 handoff 입력이 아니다.

- legacy contract files under `.stitch/contracts/`
- historical design exports under `.stitch/designs/`
- historical handoff notes under `.stitch/handoff/`
- prompt materials under `.stitch/prompt-briefs/`

남아 있는 파일은 historical reference로만 취급한다.

## 기본 작업 루프

### 1. Source Freeze

accepted Stitch screen 하나를 고정한다.
필수 기준:

- `projectId`
- `screenId`
- `url`
- 대응 `screen.html`
- 대응 `screen.png`

여러 후보안을 섞지 않는다.

### 2. Screen Manifest 작성

`.stitch/contracts/screens/<surface>.screen.json`에 아래를 적는다.

- 식별 정보
- source 정보
- ctaPriority
- states
- `blocks[]`
- validation

핵심은 `blocks[]`다.
여기에는 semantic block, CTA 의미, component composition, validation id만 적고, old override 문법은 쓰지 않는다.
경로, layout 숫자, label literal은 manifest에 적지 않는다.

지원되는 화면 구조에서는 manifest file이 기본 실행 입력이 아니다.
이 경우 file artifact는 parity/debug reference로만 남고, 실제 실행은 source에서 다시 준비된 contract가 소유한다.

### 3. Unity Map 작성

`.stitch/contracts/mappings/<surface>.unity-map.json`에 아래를 적는다.

- target
- contractRefs.manifestPath
- translationStrategy
- strategyMode
- block별 `hostPath`
- 필요 시 `aliases`

map은 경로 binding만 가진다.
시각 수치와 스타일 값은 map에 적지 않는다.

지원되는 화면 구조에서는 unity-map file도 기본 실행 입력이 아니다.
이 경우 file artifact는 parity/debug reference로만 남고, 실제 실행은 source에서 다시 준비된 contract가 소유한다.

### 4. Presentation Contract 생성

`.stitch/contracts/presentations/<surface>.presentation.json`에 아래를 적는다.

- `surfaceId`
- `surfaceRole`
- `extractionStatus`
- `sourceRefs`
- `derivedFrom`
- `unresolvedDerivedFields`
- `elements[]`

핵심은 `source-derived`다.
presentation contract는 source freeze에서 실제로 확인한 값만 적는다.
손으로 만든 literal, script fallback, "일단 맞아 보이는" 보정값을 owner처럼 올리면 안 된다.

`extractionStatus` 규칙:

- `pending-source-derivation`
  - source freeze는 있지만 필요한 presentation 값 추출이 아직 덜 끝난 상태
- `resolved`
  - 현재 translation에 필요한 값이 sourceRefs 기준으로 닫힌 상태
- `historical`
  - 더 이상 active generator 입력이 아닌 상태

translation은 `extractionStatus = resolved`일 때만 실행 가능하다.
resolved가 아니면 skeleton/debug baseline까지만 허용하고, active translation success로 취급하지 않는다.

### 5. Unity Translation 실행

실행 입력은 최종적으로 아래 세 개뿐이다.

- `screen manifest`
- `unity-map`
- `source-derived presentation contract`

지원되는 화면 구조에서는 이 manifest/map와 presentation contract가 실행 직전에 source에서 메모리로 다시 준비된다.

translator는:

1. manifest semantic block 순서를 읽고
2. map binding과 target을 검증하고
3. presentation contract가 `resolved`인지 확인하고
4. contract에 적힌 값만 적용해 prefab을 생성/갱신하고
5. controller wiring을 연결하고
6. prefab을 저장한다

추가 규칙:

- Stitch-driven script는 layout/style/text 상수나 fallback을 소유하지 않는다.
- 계약에 필요한 값이 없으면 script가 기본값으로 메우지 않고 즉시 실패한다.
- `source-derived`는 contract를 채우는 extraction 단계에만 허용되고, translator가 시각 결정을 새로 만드는 경로는 활성 기준이 아니다.

### 6. Translation Evidence 남기기

translation 뒤에는 아래 artifact를 남긴다.

- preflight
- translation
- `TempScene + SceneView capture` when review route exists
- pipeline

`pipeline`은 stage status, 입력 경로, artifact path만 남기는 얇은 요약이다.
세부 내용은 `preflight`와 `translation` artifact가 각각 소유한다.

## Reset / Reimport 기준

target prefab이 없어도 정상 케이스다.
이 경우에도 `patch-only`가 아니라 `generate` 또는 `generate-or-patch`로 surface를 실행한다.

권장 순서:

1. source freeze 확인
2. manifest 확인
3. unity-map 확인
4. presentation contract 확인
5. `extractionStatus = resolved` 확인
6. translator 실행
7. review route가 있으면 `TempScene + SceneView capture` 확인
8. preflight / translation / pipeline 확인

한 줄 기준:

`기존 prefab 복구`가 아니라 `contract에서 새 prefab 생성`이 기본값이다.

## 금지사항

- handoff md를 실행 계약처럼 쓰는 것
- html/png를 직접 구현 입력처럼 쓰는 것
- script-side constants나 fallback으로 contract 누락을 보정하는 것
- hand-authored literal을 source-derived presentation contract처럼 위장하는 것
- hand-authored profile 수정을 활성 execution contract처럼 취급하는 것
- `pending-source-derivation` 상태를 active translation-ready contract처럼 취급하는 것
- source 기반 준비가 실패했는데 script가 조용히 다른 경로로 내려가게 두는 것

## 읽기 순서

1. `docs/index.md`
2. `docs/ops/stitch-structured-handoff-contract.md`
3. 관련 `screen manifest`
4. 관련 `unity-map`
5. 관련 `presentation contract`
6. `docs/ops/stitch-to-unity-translation-guide.md`
