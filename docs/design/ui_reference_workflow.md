# UI Reference Workflow

> 마지막 업데이트: 2026-04-18
> 상태: active

이 문서는 JG UI/UX 작업에서 외부 레퍼런스 도구를 어떻게 참고할지 정리한 실무용 메모다.
목표는 "디자인 감각 보강"이지, 외부 화면을 그대로 복제하는 것이 아니다.

## 채택 도구

### 1. Mobbin

용도:

- 실전 제품 UI 패턴 참고
- 카드 밀도, 정보 위계, 빈 상태, 버튼 우선순위 확인
- `Lobby`, `Garage`, `Account`, `Result` 같은 화면 구조 비교

추천 이유:

- 무료 플랜으로도 시작 가능
- 실제 서비스 UI를 빠르게 많이 볼 수 있다
- 지금 JG처럼 "예쁜 한 장"보다 "실제로 읽히는 제품 UI"가 중요한 작업에 잘 맞는다

### 2. Relume

용도:

- 레이아웃 아이디어와 정보 구조 초안 뽑기
- 한 화면 안의 주요 블록 순서와 그룹화 정리
- 페이지 전환형 구조를 빠르게 상상하기

추천 이유:

- free tier가 있다
- 웹 출신 도구지만 `정보 구조 / 섹션 구조` 아이디어를 Unity UI에 옮겨오기 좋다
- "어떤 카드가 먼저 보여야 하는가"를 정리하는 데 유용하다

## 기본 원칙

- 외부 레퍼런스는 `복제 대상`이 아니라 `판단 보조 도구`다.
- JG의 runtime SSOT는 항상 `CodexLobbyScene.unity`와 executable scene contract다.
- 시각 판단은 `ui_foundations.md`를 우선한다.
- 외부 참고 후 실제 반영은 Unity MCP와 scene contract 기준으로 한다.

## 추천 사용 순서

1. Mobbin에서 비슷한 제품 패턴을 5~10개 훑는다.
2. 공통 패턴을 짧게 요약한다.
3. Relume에서 같은 문제를 레이아웃 수준으로 한 번 더 압축한다.
4. 그 결과를 JG 문맥으로 번역한다.
5. Unity MCP로 씬을 수정하고 gate/smoke로 검증한다.

## JG Quick Test

### 테스트 주제

`Lobby / Garage`를 page switcher 구조로 유지하면서도, 각각의 화면이 더 분명한 "main workspace"처럼 읽히게 만들기

### Mobbin에서 먼저 볼 키워드

- dashboard
- settings
- inventory
- multiplayer
- card list
- control panel

특히 아래를 본다.

- 방 목록이 비어 있을 때도 공백처럼 보이지 않는 예시
- 우측 요약 패널이 너무 답답하지 않게 구성된 예시
- 저장 CTA가 가장 쉽게 읽히는 패턴

### Relume에 넣을 1차 프롬프트

```text
Create a desktop game lobby interface with two separate pages:
1. Lobby page with room list, room creation input, room detail panel, and a clear Garage entry button.
2. Garage page with roster strip, unit editor, preview card, account card, stats summary, and a primary Save Roster action.

Use a dark tactical sci-fi UI tone.
Keep the layout practical, readable, and compact.
Avoid fancy marketing sections.
The result should feel like a real product dashboard, not a landing page.
```

### 테스트 결과로 채택한 판단

이번 quick test 기준으로 바로 유지할 판단은 아래 네 가지다.

1. Lobby는 `room list surface + create flow + garage entry`가 첫 시선이어야 한다.
2. Garage는 `roster strip / editor / right rail`의 역할 분리가 선명해야 한다.
3. `Save Roster`는 항상 우측 주요 액션처럼 읽혀야 한다.
4. preview/empty state는 "검은 빈칸"이 아니라 완성된 카드처럼 보여야 한다.

## Unity 반영 규칙

- 외부 도구에서 얻은 결과는 scene-owned layout으로 번역한다.
- `LobbyView`, `GaragePageController`에 runtime layout 보정 코드를 추가해서 해결하지 않는다.
- hierarchy, wiring, card 구조 변경 시 scene contract 기준 문서와 필요한 운영 문서를 같은 턴에 갱신한다.

## 현재 JG에 가장 잘 맞는 사용법

- Mobbin: 화면 단위 판단
- Relume: 레이아웃 압축과 우선순위 판단
- Unity MCP: 실제 씬 수정과 검증

한 줄로 정리하면:

`Mobbin으로 보고, Relume으로 압축하고, Unity MCP로 구현한다.`
