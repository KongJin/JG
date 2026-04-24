# Stitch To Unity Translation Guide

> 마지막 업데이트: 2026-04-23
> 상태: reference
> doc_id: ops.stitch-to-unity-translation-guide
> role: reference
> owner_scope: accepted Stitch screen을 Unity prefab으로 번역할 때의 fidelity, wiring, evidence 기준
> upstream: docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.unity-ui-authoring-workflow, design.ui-foundations
> artifacts: `Assets/Prefabs/`, `artifacts/unity/`

이 문서는 `Stitch -> Unity` 번역에서 workflow를 다시 설명하지 않는다.
실행 순서와 파일 ownership은 `ops.stitch-data-workflow`, JSON 구조는 `ops.stitch-structured-handoff-contract`, 명령은 `tools/stitch-unity/README.md`를 기준으로 본다.
여기서는 `번역이 끝났는지 어떻게 판단하는가`만 다룬다.

## 번역 완료의 정의

아래 다섯 가지가 모두 닫혀야 `번역 완료`다.

1. semantic block 순서가 source와 같게 읽힌다.
2. block이 shared component vocabulary로 무리 없이 설명된다.
3. required serialized ref가 실제 prefab에 연결돼 있다.
4. primary CTA, selected state, completion posture가 source와 같은 등급으로 읽힌다.
5. preflight / translation / pipeline / workflow policy evidence가 남아 있다.

## 번역 전에 보는 것

- `screen manifest.blocks[]`
- `.stitch/contracts/components/shared-ui.component-catalog.json`
- `screen manifest.ctaPriority[]`
- `screen manifest.validation`
- `unity-map.blocks`
- 관련 `*PageController`, `*View`의 required ref

이 여섯 가지로 surface 의미와 wiring을 설명할 수 없으면 아직 번역 시작 상태가 아니다.

## Component Mapping

prefab authoring을 시작하기 전에 `block -> shared component` 대응을 먼저 닫는다.

- active generator input은 계속 `manifest + map`뿐이다.
- shared component catalog는 작은 재사용 primitive를 고르는 vocabulary reference다.
- block 자체를 catalog로 치환하지 않는다.

Garage baseline quick mapping:

- `slot-selector -> slot-card`
- `focus-bar -> tab-button`
- `editor-panel -> section-card + part-selector + stats-block`
- `preview-card -> section-card + status-text + stat-chip`
- `summary-card -> section-card + stats-block + toast`
- `save-dock -> primary-button + status-text`

## Fidelity Check

### Structure

- block order와 방향이 source와 같아야 한다.
- `strip`, `row`, `single scroll body`, `sticky dock` 같은 posture가 바뀌면 실패다.

### Emphasis

- first read가 source와 같아야 한다.
- selected block과 primary CTA가 auxiliary보다 약해지면 실패다.

### Completion

- preview, summary, empty state가 placeholder처럼 보이면 실패다.
- source가 완성 카드처럼 읽히면 Unity도 최소 그 등급이어야 한다.

### CTA

- primary CTA의 폭, 고정성, 반복 노출이 source와 같은 등급이어야 한다.
- secondary action이 primary를 이기면 실패다.

### State

- selected / empty / disabled / loading 구분이 source만큼 읽혀야 한다.

## Wiring Check

- `unity-map.target.assetPath`가 실제 target asset과 맞아야 한다.
- `unity-map.blocks.<blockId>.hostPath`가 실제 hierarchy에 존재해야 한다.
- required component가 빠지면 시각적으로 비슷해도 실패다.
- presentation code가 geometry나 typography를 다시 덮어쓰면 실패다.
- translator가 missing value를 script-side fallback으로 메우면 실패다.

## Evidence

최소 evidence는 아래 순서로 남긴다.

1. compile/reload
2. preflight
3. translation
4. pipeline
5. workflow policy

한 줄 기준:

`manifest와 map이 active input으로 유지되고, block이 shared component vocabulary로도 설명되면 번역이 닫힌다.`
