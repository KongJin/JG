# Stitch UI/UX Overhaul Plan

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: plans.stitch-ui-ux-overhaul
> role: plan
> owner_scope: Stitch set inventory, 현재 상태, 다음 작업, 실행 순서
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow, plans.progress
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/handoff/`

이 문서는 JG의 `Stitch` 기반 전면 UI/UX 개편 실행 계획 SSOT다.
외부 메모나 세션 채팅에 흩어진 기준을 레포 안으로 모으고, `Stitch 시안 -> handoff -> Unity scene-owned layout` 루프를 같은 용어로 유지하는 목적을 가진다.
운영 규칙 본문은 `ops.stitch-data-workflow`, 활용 원칙은 `design.ui-reference-workflow`가 소유하고, 이 문서는 실행 상태와 우선순위만 소유한다.

## Goal

- `Lobby`, `Garage`, `Overlay`, `Battle HUD`, `Result`를 하나의 tactical hangar visual language로 재정렬한다.
- 산출물은 단순 시안이 아니라 `Unity scene 계약으로 바로 번역 가능한 handoff`까지 포함한다.
- `Stitch`는 방향 탐색과 visual handoff에 쓰고, 최종 runtime layout SSOT는 계속 scene/prefab과 contract가 가진다.

## Current Status

- Gate 0는 이미 통과했다.
  - Stitch 인증/프로젝트 접근 성공
  - 마스터 프로젝트 `11729197788183873077` 확정
  - `.stitch/DESIGN.md` 초안 작성 완료
- 세트 A~E의 concept pass와 handoff 문서 1차 정리는 완료됐다.
  - 산출물 위치: `.stitch/designs/`, `.stitch/prompt-briefs/`, `.stitch/handoff/`
- 현재 단계는 `concept generation`이 아니라 `scene-owned layout implementation + contract/smoke verification`이다.

## Working SSOT

- 제품 판단: `design.game-design`
- Lobby/Garage layout 기준: `design.ui-foundations`
- Stitch 활용 원칙: `design.ui-reference-workflow`
- Stitch visual system artifact: `.stitch/DESIGN.md`
- Stitch 세트별 번역 기준 artifact: `.stitch/handoff/INDEX.md`
- 진행 상태 SSOT: `plans.progress`

규칙:

- `Stitch` 산출물은 시안과 handoff 기준이다.
- Lobby/Garage runtime layout SSOT는 `CodexLobbyScene.unity`다.
- Battle HUD / Result runtime layout SSOT는 `GameScene.unity`다.
- 시안 복제 대신, scene hierarchy와 serialized contract에 맞는 block 재구성으로 번역한다.

## Tooling / Skill Route

- repo entry: `jg-stitch-workflow`
  - JG 기준 Stitch 단일 진입점
  - prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, design export organization, handoff 정리, Unity lane handoff를 맡는다.

프롬프트 작성 기본값:

- mobile-first
- validation frame `390x844`
- 한글 카피 우선
- `dark tactical sci-fi base`
- `Nova-style garage preparation`과 `short readable summon clarity`는 원칙 차용만 허용

## Set Inventory

### Set A - Lobby

- 범위: `Lobby main`, `Room list empty/list`, `Create room`, `Garage summary`
- handoff: [`../../.stitch/handoff/set-a-lobby.md`](../../.stitch/handoff/set-a-lobby.md)
- 현재 상태: concept/handoff 완료, runtime 반영은 기본 구조까지 진행됨

### Set B - Garage

- 범위: `Garage main workspace`, `slot selector`, `focused editor`, `preview`, `summary`, `save dock`, `Garage settings overlay`, `Account card`
- handoff: [`../../.stitch/handoff/set-b-garage.md`](../../.stitch/handoff/set-b-garage.md)
- 현재 상태: mobile-only 구조와 1차 polish 반영 완료, 다음 패스는 handoff 대비 남은 scene-level 밀도 조정

### Set C - Overlay

- 범위: `Room detail panel`, `Login loading overlay`, `Account delete confirm`, `Common modal/error dialog`
- handoff: [`../../.stitch/handoff/set-c-overlays.md`](../../.stitch/handoff/set-c-overlays.md)
- 현재 상태: concept/handoff 완료, auxiliary panel 문법 유지가 핵심

### Set D - Battle HUD

- 범위: `HUD`, `Unit summon bar`, `Core HP`, `Wave HUD`, `Placement feedback`, `Cannot afford overlay`
- handoff: [`../../.stitch/handoff/set-d-battle-hud.md`](../../.stitch/handoff/set-d-battle-hud.md)
- 현재 상태: concept/handoff 완료, visual 반영 전 runtime contract 안정화가 선행 과제

### Set E - Result / Feedback

- 범위: `Wave end / result overlay`, `toast`, `banner`, `feedback`
- handoff: [`../../.stitch/handoff/set-e-results.md`](../../.stitch/handoff/set-e-results.md)
- 현재 상태: concept/handoff 완료, Set D와 같은 battle runtime contract 위에서 이어서 반영

## Standard Execution Loop

각 세트는 아래 순서를 반복한다.

1. 현재 scene contract와 관련 SSOT 문서를 읽고 surface 목적, 읽기 순서, CTA 우선순위를 다시 적는다.
2. 필요하면 `jg-stitch-workflow` 기준으로 prompt brief를 갱신한다.
3. `Stitch`에서는 한 세트당 1개의 baseline만 유지하고, 기각안은 이유만 남긴다.
4. Unity 반영은 scene-owned layout 기준으로 수행한다.
5. 세트 종료 시 handoff와 repo 문서를 함께 동기화한다.
6. 검증은 더 싼 레이어부터 통과시킨다.

## Validation Order

### Lobby / Garage

1. contract
2. EditMode/unit tests
3. page-switch smoke
4. feature smoke

### Battle / Result

1. contract
2. summon or wave smoke
3. outcome overlay smoke

모든 시각 sanity check는 `390x844`를 기준으로 남긴다.

## Immediate Next Work

현재 기본 우선순위는 아래 둘이다.

1. `Set B Garage` scene-level polish를 handoff 밀도에 더 가깝게 맞춘다.
2. `Set D Battle HUD` visual 반영 전에 wave/outcome runtime contract를 다시 안정화한다.

보조 원칙:

- 승인된 세트의 visual language는 다음 세트의 기본값으로 이어간다.
- popup과 overlay는 메인 flow보다 더 시끄럽게 만들지 않는다.
- empty/loading/error state도 완성형 카드나 패널처럼 남긴다.

## Artifacts To Keep Updated

- `.stitch/DESIGN.md`
- `.stitch/prompt-briefs/*.md`
- `.stitch/handoff/*.md`
- 관련 `docs/design/*` 또는 `docs/plans/*` SSOT

외부 로컬 메모가 생겨도, 계속 참조해야 하는 결정은 이 문서나 해당 SSOT로 승격한다.
