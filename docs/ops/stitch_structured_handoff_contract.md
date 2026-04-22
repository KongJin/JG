# Stitch Structured Handoff Contract

> 마지막 업데이트: 2026-04-22
> 상태: active
> doc_id: ops.stitch-structured-handoff-contract
> role: ssot
> owner_scope: Stitch 산출물을 Unity 생성 계약 JSON으로 고정하는 구조, 필수 필드, 금지 입력
> upstream: docs.index, ops.stitch-data-workflow, design.ui-foundations, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/screens/*.json`, `.stitch/contracts/mappings/*.json`, `.stitch/contracts/schema/*.json`

이 문서는 JG의 활성 `Stitch -> Unity` handoff 형식을 고정한다.
현재 활성 경로는 `screen manifest + unity-map`이고, generator는 `manifest.blocks[] -> 공통 block builder -> controller wiring` 흐름으로만 동작한다.

## 목적

- accepted Stitch screen을 사람이 다시 해석하지 않아도 되는 실행 계약으로 고정한다.
- `화면 의미`와 `Unity 경로 연결`을 분리한다.
- surface별 전용 handoff 문법 대신 모든 세트가 같은 JSON 구조를 쓰게 만든다.

## 활성 산출물

- `.stitch/contracts/screens/*.json`
  - Stitch source에서 확정된 semantic block 계약
- `.stitch/contracts/mappings/*.json`
  - semantic block을 Unity target/path에 연결하는 binding
- `.stitch/contracts/schema/stitch-screen-manifest.schema.json`
- `.stitch/contracts/schema/stitch-unity-map.schema.json`

## 비활성 산출물

아래 경로는 더 이상 활성 handoff 입력으로 쓰지 않는다.

- legacy contract paths under `.stitch/contracts/`
- historical design exports under `.stitch/designs/`
- historical handoff notes under `.stitch/handoff/`

남아 있는 파일은 historical reference로만 본다.
새 작성, 갱신, 실행 의존 추가를 금지한다.

## 활성 계약 구조

### 1. Screen Manifest

`screen manifest`는 Stitch source가 확정한 의미 계약이다.
필수 top-level 필드는 아래만 가진다.

- `schemaVersion`
- `contractKind = "screen-manifest"`
- `setId`
- `surfaceId`
- `surfaceRole`
- `status`
- `source`
- `targets`
- `ctaPriority`
- `states`
- `blocks`
- `validation`
- optional `notes`

`blocks[]`가 핵심이다.
manifest는 semantic block을 순서대로 선언해야 하고, generator는 이 배열을 기준으로 block 조립 순서를 정한다.

각 block은 최소 아래 필드를 가진다.

- `blockId`
- `role`
- `sourceName`
- `unityTargetPath`
- `layout`
- `children`
- optional `notes`

`blockId`는 `slot-selector`, `focus-bar`, `primary-cta` 같은 의미 이름이다.
여기에는 Unity child path를 다시 나누어 적지 않는다.

### 2. Unity Map

`unity-map`은 semantic block을 Unity에 연결하는 binding 레이어다.
필수 top-level 필드는 아래만 가진다.

- `schemaVersion`
- `contractKind = "unity-surface-map"`
- `surfaceId`
- `target`
- `contractRefs.manifestPath`
- `translationStrategy`
- `blocks`

각 map block은 최소 아래를 가진다.

- `hostPath`
- optional `aliases`
- optional `requiredComponents`
- optional `verificationTags`
- optional `notes`

한 줄 기준:

`manifest는 무엇을 만들지`, `unity-map은 어디에 붙일지`를 가진다.

## 금지 입력

아래는 활성 handoff에 다시 넣지 않는다.

- legacy override 문법
- legacy contract ref path
- set별 handoff md 의존
- `png/html export를 직접 handoff 입력으로 삼는 규칙`

Stitch 원본 html/png는 source artifact일 뿐이고, generator 직접 입력은 아니다.

## 작성 규칙

### Manifest

- `blocks[]`는 semantic block 순서를 그대로 가진다.
- 같은 surface에서 generator가 반드시 조립해야 하는 block만 남긴다.
- CTA는 `ctaPriority[]`와 대응되게 쓴다.
- `validation.firstReadOrder`는 가능한 한 `blocks[].blockId`와 같은 어휘를 쓴다.
- `notes`는 source fidelity 경고나 재해석 금지사항만 적는다.

### Unity Map

- `blocks` key는 manifest의 `blockId`와 같아야 한다.
- `hostPath`는 현재 Unity hierarchy 기준의 연결점만 적는다.
- layout 숫자, 시각 토큰, typography 값을 다시 적지 않는다.
- rename 과도기에는 `aliases`로만 흡수하고, caller migration 후 제거한다.

## generator 규칙

generator는 아래만 수행한다.

1. manifest를 읽고 semantic block 순서를 확정한다.
2. block id별 공통 builder를 호출한다.
3. unity-map을 읽고 verification/wiring 경로를 확인한다.
4. controller/view serialized reference를 연결한다.
5. inspection / verification artifact를 남긴다.

generator가 legacy skeleton이나 override 문법을 다시 해석하는 경로는 활성 기준이 아니다.

## handoff 완료 기준

handoff는 아래가 모두 참일 때 complete다.

- baseline Stitch source가 `source.projectId/screenId/url`로 고정돼 있다.
- semantic block 순서가 `blocks[]`로 바로 읽힌다.
- CTA hierarchy가 `ctaPriority[]`로 바로 읽힌다.
- Unity target과 serialized owner가 `targets`에 명시돼 있다.
- 각 block이 Unity에서 어느 host에 붙는지 `unity-map`으로 바로 따라갈 수 있다.
- validation focus가 `requiredChecks`와 `firstReadOrder`에 적혀 있다.

위 항목 중 하나라도 빠지면 draft다.

## 읽기 순서

1. `screen manifest`
2. `unity-map`
3. 관련 presentation contract
4. `ops.stitch-to-unity-translation-guide`
5. `ops.unity-ui-authoring-workflow`
