# Stitch Test Brief

> 마지막 업데이트: 2026-04-24
> 상태: active
> doc_id: design.stitch-test-brief
> role: reference
> owner_scope: Stitch 빠른 탐색용 테스트 브리프 템플릿
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow
> artifacts: `.stitch/prompt-briefs/`

이 문서는 JG에서 `Stitch`를 빠르게 시험할 때 바로 붙여넣어 쓸 수 있는 실전 브리프다.
목표는 예쁜 목업을 많이 만드는 것이 아니라, 현재 execution contracts와 새 baseline prefab으로 번역 가능한 시안을 빠르게 찾는 것이다.

## Test Goal

첫 테스트 목표:

- `Lobby`와 `Garage`를 각각 별도 화면처럼 읽히게 만들기
- `Lobby`에서는 room list가 가장 먼저 읽히는지 보기
- `Garage`에서는 slot first, single scroll body, fixed save action이 선명한지 보기
- 생성 결과가 Unity scene-owned layout으로 무리 없이 옮겨질 수 있는지 보기

## Prompt 1

```text
Create a mobile-first tactical sci-fi game lobby and garage UI.

Screen 1: Lobby
- room list must be the first thing the player reads
- create room is secondary
- garage entry is visible but quieter than room actions
- empty room state must still feel finished, not blank

Screen 2: Garage
- slot selection comes first
- the page should feel like one continuous scroll workspace
- settings is auxiliary, not a main destination
- save roster must remain the clearest persistent action
- preview and summary must feel like finished cards, not empty placeholders

Style
- dark tactical sci-fi dashboard
- practical, compact, readable
- bold but not flashy
- avoid marketing-site layouts
- avoid generic SaaS card grids
- mobile-first, game UI, not enterprise admin
```

## Prompt 2

```text
Redesign this as a mobile-first game garage workspace for repeated short play sessions.

Priorities:
- the player should immediately understand which slot is selected
- part editing should feel focused, not cluttered
- the body should read as slot first -> focused editor -> preview -> summary
- save roster should stay visually dominant
- settings should feel like an auxiliary overlay trigger

Visual direction:
- dark hangar atmosphere
- dense card layout
- strong hierarchy
- finished empty states
- minimal wasted space
```

## Prompt 3

```text
Create a paired Lobby and Garage interface for a tactical co-op game.

Lobby:
- emphasize room discovery first
- make create room clearly secondary
- include a compact garage summary card
- keep garage entry obvious but not louder than join/create flow

Garage:
- slot strip or slot grid first
- one scrollable body
- focused editor for the currently selected part
- clear preview and summary cards
- persistent save action dock

Constraints:
- mobile-first
- compact
- readable in one glance
- strong CTA hierarchy
- no decorative filler panels
```

## What To Judge

첫 테스트에서는 아래 다섯 가지만 본다.

1. `Lobby`에서 room list가 첫 시선을 가져가는가
2. `Garage`에서 slot selection이 첫 구조로 읽히는가
3. save action이 화면에서 가장 명확한 CTA인가
4. empty state와 preview가 공백처럼 보이지 않는가
5. 결과를 새 baseline prefab hierarchy로 번역할 때 불필요한 desktop/web 전용 구조가 없는가

## Translation Rule

- Stitch 산출물은 시안이다.
- 최종 SSOT는 `ui_foundations.md`와 새로 세울 serialized prefab/scene contract다.
- 실제 구현에서는 런타임 layout 보정 코드 대신 prefab-first authoring으로 번역한다.

## Next Step

첫 테스트에서 가장 괜찮은 시안 1개만 고른 뒤, 아래 순서로 이어간다.

1. 블록 순서를 `ui_foundations.md` 용어로 다시 적기
2. 필요한 hierarchy root를 적기
3. Unity MCP로 baseline prefab 재구성
4. prefab wiring review
5. 새 scene 조립 후 fresh contract/smoke
