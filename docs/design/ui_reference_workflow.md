# UI Reference Workflow

> 마지막 업데이트: 2026-04-19
> 상태: active

이 문서는 JG UI/UX 작업에서 `Stitch`를 어떻게 활용할지 정리한 실무용 메모다.
목표는 빠르게 시안을 만들고, 그것을 JG 문맥에 맞게 scene-owned layout으로 번역하는 것이다.

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
- JG의 runtime SSOT는 항상 `CodexLobbyScene.unity`와 executable scene contract다.
- 시각 판단은 `ui_foundations.md`를 우선한다.
- Stitch 결과를 그대로 복제하지 말고, JG의 실제 flow와 serialized contract로 번역한다.
- 실제 반영과 검증은 Unity MCP와 scene contract 기준으로 한다.

## 추천 사용 순서

1. Stitch에 현재 화면 목표를 짧고 강하게 넣는다.
2. 나온 시안 중 정보 위계가 가장 선명한 한 방향만 고른다.
3. `ui_foundations.md` 계약에 맞게 블록 순서와 CTA 역할을 다시 적는다.
4. Unity MCP로 씬을 수정한다.
5. gate/smoke로 검증한다.

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

- Stitch에서 얻은 결과는 scene-owned layout으로 번역한다.
- `LobbyView`, `GaragePageController`에 runtime layout 보정 코드를 추가해서 해결하지 않는다.
- hierarchy, wiring, card 구조 변경 시 scene contract 기준 문서와 필요한 운영 문서를 같은 턴에 갱신한다.

## 현재 JG에 가장 잘 맞는 사용법

- Stitch: 시안 생성과 방향 탐색
- Unity MCP: 실제 씬 수정과 검증

한 줄로 정리하면:

`Stitch로 시안을 만들고, Unity MCP로 구현한다.`
