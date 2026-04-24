# Stitch Data Workflow

> 마지막 업데이트: 2026-04-23
> 상태: active
> doc_id: ops.stitch-data-workflow
> role: ssot
> owner_scope: Stitch working data ownership, source freeze, Unity handoff 운영
> upstream: design.ui-reference-workflow, plans.stitch-ui-ux-overhaul, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/screens/`, `.stitch/contracts/mappings/`, `artifacts/stitch/`

이 문서는 JG에서 `Stitch` 산출물을 어떻게 저장하고 Unity handoff로 넘길지 정하는 단일 운영 기준이다.
현재 활성 흐름은 `Stitch source freeze -> screen manifest -> unity-map -> Unity generator`다.

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

### 4. Unity Generator 실행

실행 입력은 아래 두 개뿐이다.

- `screen manifest`
- `unity-map`

generator는:

1. manifest semantic block 순서를 읽고
2. contract completeness를 먼저 검증하고
3. contract에 적힌 값만 적용해 prefab을 생성하고
4. controller wiring을 연결하고
5. prefab을 저장한다

추가 규칙:

- Stitch-driven script는 layout/style/text 상수나 fallback을 소유하지 않는다.
- 계약에 필요한 값이 없으면 script가 기본값으로 메우지 않고 즉시 실패한다.
- `source-derived implementation`처럼 script가 시각 결정을 대신하는 경로는 활성 기준이 아니다.

### 5. Translation Evidence 남기기

translation 뒤에는 아래 artifact를 남긴다.

- preflight
- translation
- pipeline

## Reset / Reimport 기준

target prefab이 없어도 정상 케이스다.
이 경우에도 `patch-only`가 아니라 `generate` 또는 `generate-or-patch`로 surface를 실행한다.

권장 순서:

1. source freeze 확인
2. manifest 확인
3. unity-map 확인
4. presentation contract 확인
5. generator 실행
6. preflight / translation / pipeline 확인

한 줄 기준:

`기존 prefab 복구`가 아니라 `contract에서 새 prefab 생성`이 기본값이다.

## 금지사항

- legacy contract 보조 파일을 활성 generator 입력으로 전제하는 것
- handoff md를 실행 계약처럼 쓰는 것
- html/png를 직접 구현 입력처럼 쓰는 것
- legacy override 문법을 새 기준으로 계속 쓰는 것
- script-side constants나 fallback으로 contract 누락을 보정하는 것

## 읽기 순서

1. `docs/index.md`
2. `docs/ops/stitch-structured-handoff-contract.md`
3. 관련 `screen manifest`
4. 관련 `unity-map`
5. `docs/ops/stitch-to-unity-translation-guide.md`
