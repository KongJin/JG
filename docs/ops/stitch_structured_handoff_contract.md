# Stitch Structured Handoff Contract

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: ops.stitch-structured-handoff-contract
> role: ssot
> owner_scope: Stitch 산출물을 Unity 생성 계약 JSON으로 고정하는 구조, 필수 필드, 금지 입력
> upstream: docs.index, ops.stitch-data-workflow, design.ui-foundations, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/screens/*.json`, `.stitch/contracts/mappings/*.json`, `.stitch/contracts/schema/*.json`, `.stitch/contracts/components/*.json`, `.stitch/contracts/components/schema/*.json`

이 문서는 JG의 활성 `Stitch -> Unity` handoff 형식을 고정한다.
현재 활성 경로는 `screen manifest + unity-map`이고, generator는 `manifest semantic order -> contract-complete translator -> controller wiring` 흐름만 허용한다.
`.stitch/contracts/components/*.json`은 shared UI vocabulary를 설명하는 companion contract지만, v1에서는 active generator input이 아니다.

## 목적

- accepted Stitch screen을 사람이 다시 해석하지 않아도 되는 실행 계약으로 고정한다.
- `화면 의미`와 `Unity 경로 연결`을 분리한다.
- 재사용 가능한 shared UI vocabulary를 실행 계약과 섞지 않고 companion lane으로 분리한다.
- surface별 전용 handoff 문법 대신 모든 세트가 같은 JSON 구조를 쓰게 만든다.

## 활성 산출물

- `.stitch/contracts/screens/*.json`
  - Stitch source에서 확정된 semantic block 계약
- `.stitch/contracts/mappings/*.json`
  - semantic block을 Unity target/path에 연결하는 binding
- `.stitch/contracts/schema/stitch-screen-manifest.schema.json`
- `.stitch/contracts/schema/stitch-unity-map.schema.json`

## companion 산출물

- `.stitch/contracts/components/*.json`
  - shared `atom / molecule` vocabulary를 고정하는 reference companion contract
- `.stitch/contracts/components/schema/*.json`
  - component catalog schema

이 companion lane은 사람이 block을 shared component로 해석할 때 쓰는 기준이다.
v1 generator는 이 파일을 직접 읽지 않는다.

## 비활성 산출물

아래 경로는 더 이상 활성 handoff 입력으로 쓰지 않는다.

- legacy contract paths under `.stitch/contracts/`
- historical design exports under `.stitch/designs/`
- historical handoff notes under `.stitch/handoff/`

남아 있는 파일은 historical reference로만 본다.
새 작성, 갱신, 실행 의존 추가를 금지한다.

## 활성 계약 구조

### 0. Shared UI Component Catalog

`component-catalog`는 shared UI primitive vocabulary를 고정하는 companion 계약이다.
활성 handoff 입력은 아니고, `screen manifest`와 `unity-map`을 읽는 사람이 같은 어휘를 쓰게 만드는 reference lane이다.

필수 top-level 필드는 아래만 가진다.

- `schemaVersion`
- `contractKind = "component-catalog"`
- `catalogId`
- `status`
- `tokenSource`
- `components`
- optional `notes`

각 component는 최소 아래 필드를 가진다.

- `componentId`
- `level`
- `role`
- `states`
- `slots`
- `childComponents`
- `tokenRefs`
- `unityBasePrefab`
- `intendedUses`
- optional `notes`

v1 규칙:

- `componentId`는 English kebab-case만 쓴다.
- `level`은 `atom | molecule`만 허용한다.
- block, surface, scene 의미는 다시 catalog로 끌어내리지 않는다.

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
- `children`
- optional `notes`

`blockId`는 `slot-selector`, `focus-bar`, `primary-cta` 같은 의미 이름이다.
여기에는 Unity child path나 layout 숫자를 적지 않는다.

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
- optional `notes`

한 줄 기준:

`manifest는 무엇을 만들지`, `unity-map은 어디에 붙일지`를 가진다.

## 금지 입력

아래는 활성 handoff에 다시 넣지 않는다.

- legacy override 문법
- legacy contract ref path
- set별 handoff md 의존
- `png/html export를 직접 handoff 입력으로 삼는 규칙`
- `component-catalog를 manifest/map 대신 generator 직접 입력처럼 취급하는 규칙`

Stitch 원본 html/png는 source artifact일 뿐이고, generator 직접 입력은 아니다.

## 작성 규칙

### Manifest

- `blocks[]`는 semantic block 순서를 그대로 가진다.
- 같은 surface에서 generator가 반드시 조립해야 하는 block만 남긴다.
- CTA는 `ctaPriority[]`와 대응되게 쓴다.
- `validation.firstReadOrder`는 가능한 한 `blocks[].blockId`와 같은 어휘를 쓴다.
- `validation.requiredChecks`는 prose가 아니라 semantic kebab-case id로 적는다.
- `notes`는 source fidelity 경고나 재해석 금지사항만 적는다.
- manifest는 path/layout/label literal을 소유하지 않는다.

### Unity Map

- `blocks` key는 manifest의 `blockId`와 같아야 한다.
- `hostPath`는 현재 Unity hierarchy 기준의 연결점만 적는다.
- layout 숫자, 시각 토큰, typography 값을 다시 적지 않는다.
- rename 과도기에는 `aliases`로만 흡수하고, caller migration 후 제거한다.

### Shared UI Component Catalog

- token 값과 layout 숫자를 다시 정의하지 않는다.
- shared component는 작은 재사용 primitive만 남긴다.
- 같은 surface 안에서만 의미가 있는 큰 덩어리는 catalog가 아니라 `blocks[]`에 둔다.
- `unityBasePrefab`는 docs-first reference path일 수 있지만, active runtime truth를 대신하지 않는다.

## generator 규칙

generator는 아래만 수행한다.

1. manifest를 읽고 semantic block 순서를 확정한다.
2. contract completeness를 검증한다.
3. contract에 적힌 값만 적용한다.
4. unity-map을 읽고 wiring 경로를 확인한다.
5. controller/view serialized reference를 연결한다.
6. preflight / translation / pipeline artifact를 남긴다.

generator가 legacy skeleton이나 override 문법을 다시 해석하는 경로는 활성 기준이 아니다.
v1에서 generator가 `component-catalog`를 읽는 경로도 아직 활성 기준이 아니다.
script-side constants나 fallback으로 계약 누락을 메우는 경로도 활성 기준이 아니다.

## handoff 완료 기준

handoff는 아래가 모두 참일 때 complete다.

- baseline Stitch source가 `source.projectId/screenId/url`로 고정돼 있다.
- semantic block 순서가 `blocks[]`로 바로 읽힌다.
- CTA hierarchy가 `ctaPriority[]`로 바로 읽힌다.
- 각 block이 Unity에서 어느 host에 붙는지 `unity-map`으로 바로 따라갈 수 있다.
- validation focus가 `requiredChecks`와 `firstReadOrder`에 적혀 있다.

위 항목 중 하나라도 빠지면 draft다.

## 읽기 순서

1. `screen manifest`
2. `shared component catalog` (있다면)
3. `unity-map`
4. 관련 presentation contract
5. `ops.stitch-to-unity-translation-guide`
6. `ops.unity-ui-authoring-workflow`
