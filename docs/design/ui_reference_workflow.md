# UI Reference Workflow

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: design.ui-reference-workflow
> role: ssot
> owner_scope: Stitch를 JG에서 어떤 철학과 판단 기준으로 사용할지에 대한 원칙
> upstream: design.game-design
> artifacts: none

이 문서는 JG UI/UX 작업에서 `Stitch`를 어떻게 활용할지 정리한 실무용 메모다.
목표는 빠르게 시안을 만들고, 그것을 JG 문맥에 맞게 UI Toolkit candidate surface로 번역할 수 있는지 판단하는 것이다.
저장 위치, prompt brief 수명, handoff 운영 같은 작업 절차는 `ops.stitch-data-workflow`와 `ops.stitch-structured-handoff-contract`가 소유한다.

## 채택 도구

### 1. Stitch

용도:

- `Lobby`, `Garage`, `Account`, `Result` 같은 화면의 시안 생성
- 정보 위계, 카드 밀도, 저장 CTA, 빈 상태를 빠르게 탐색
- 하나의 화면을 여러 방향으로 짧게 반복 탐색

추천 이유:

- 지금 JG처럼 "패턴 구경"보다 "바로 시안 만들기"가 중요한 작업에 더 잘 맞는다
- 같은 프롬프트를 여러 방향으로 빠르게 비교할 수 있다
- mobile-first game dashboard 감각을 짧은 반복으로 밀어붙이기 좋다

## 기본 원칙

- Stitch 산출물은 `최종 SSOT`가 아니라 `시안`이다.
- JG의 runtime SSOT는 Stitch 산출물이 아니라 Unity의 runtime surface와 scene contract다.
- 새 Stitch import의 실행 route는 `ops.stitch-data-workflow`, `ops.stitch-structured-handoff-contract`, `ops.unity-ui-authoring-workflow`가 소유한다.
- 시각 판단은 `design.ui-foundations`를 우선한다.
- Stitch 결과를 그대로 복제하지 말고, JG의 실제 flow와 serialized contract로 번역한다.
- 실제 반영과 검증은 Unity MCP와 scene contract 기준으로 한다.
- `.stitch` 자산의 저장 위치와 handoff 운영 규칙은 `ops.stitch-data-workflow`를 따른다.
- Unity 번역 순서는 owner workflow 문서를 따른다.
- stored `.stitch/contracts/*.json`은 source freeze를 건너뛰는 시작점이 되면 안 된다.

## 추천 사용 순서

1. Stitch에 현재 화면 목표를 짧고 강하게 넣는다.
2. 나온 시안 중 정보 위계가 가장 선명한 한 방향만 고른다.
3. `design.ui-foundations` 계약에 맞게 source에서 execution contract를 준비한다.
4. UI Toolkit candidate surface와 preview scene을 만든다.
5. fresh capture와 scoped workflow policy로 검증한다.

## JG Quick Test

### 테스트 주제

`Lobby / Garage`를 page switcher 구조로 유지하면서도, 각각의 화면이 더 분명한 "main workspace"처럼 읽히게 만들기

### Stitch 1차 프롬프트

```text
Create a mobile-first tactical sci-fi game lobby and garage UI.

Screen 1: Lobby
- room list should be the first thing the player reads
- create room is secondary
- garage entry should be visible but not louder than room actions
- empty room state should still feel finished, not blank

Screen 2: Garage
- slot selection comes first
- the body should feel like a single scroll workspace
- settings is auxiliary
- save roster must remain the clearest persistent action
- preview and summary should feel like finished cards, not empty placeholders

Style:
- dark tactical sci-fi dashboard
- practical, compact, readable
- avoid marketing-site layouts
- avoid generic SaaS card grids
```

### 테스트 결과로 채택한 판단

이번 quick test 기준으로 바로 유지할 판단은 아래 네 가지다.

1. Lobby는 `room list surface + create flow + garage entry`가 첫 시선이어야 한다.
2. Garage는 `roster strip / editor / right rail`의 역할 분리가 선명해야 한다.
3. `Save Roster`는 항상 우측 주요 액션처럼 읽혀야 한다.
4. preview/empty state는 "검은 빈칸"이 아니라 완성된 카드처럼 보여야 한다.

## Unity 반영 규칙

- Stitch에서 얻은 결과는 먼저 UI Toolkit candidate surface와 preview capture로 검토한다.
- `LobbyView`, `GarageSetBUitkPageController`에 runtime layout 보정 코드를 추가해서 해결하지 않는다.
- runtime scene/prefab 교체가 필요하면 candidate evidence 이후 별도 replacement pass로 분리한다.

## 현재 JG에 가장 잘 맞는 사용법

- Stitch: 시안 생성과 방향 탐색
- Unity MCP: preview scene/capture와 runtime replacement 검증
- repo 기준 진입점은 `jg-stitch-workflow`, `jg-stitch-unity-import`, `jg-unity-workflow`를 사용한다.
  - `jg-stitch-workflow`: source freeze와 contract 준비 라우팅
  - `jg-stitch-unity-import`: Stitch 화면을 Unity 후보 surface로 가져오는 반복 루틴
  - `jg-unity-workflow`: scene/prefab/MCP authoring과 검증 라우팅
