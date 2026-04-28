# Nova1492 UI Flow Guide

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: design.nova1492-ui-flow-guide
> role: reference
> owner_scope: Nova1492 Garage UI 자산 톤을 기준으로 Stitch/UI Toolkit 후보 화면을 만들 때 참고하는 flow guide
> upstream: design.game-design, design.world-design, design.ui-foundations, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Art/Nova1492/UI/Garage/`, `artifacts/stitch/`, `Assets/UI/`, `artifacts/unity/`

이 문서는 JG의 비전투 UI를 만들 때 `Assets/Art/Nova1492/UI/Garage` 자산 톤과 최근 Stitch 후보 화면 흐름을 함께 참고하기 위한 가이드다.
정책이나 실행 절차의 owner가 아니며, 새 UI 작업은 계속 `Stitch source freeze -> UI Toolkit candidate surface -> preview capture/report` 흐름을 따른다.

## Visual Source

기본 시각 기준은 아래 로컬 자산이다.

| asset group | examples | use |
|---|---|---|
| Garage backgrounds | `Backgrounds/lab.bmp`, `Backgrounds/WAITINGROOM.bmp`, `Backgrounds/main.BMP` | 두꺼운 금속 프레임, 검정 content well, 중앙 기계 구조, 하단 기계식 버튼 감각 |
| Garage accents | `Accents/mainlight-1.BMP` through `mainlight-4.BMP` | 미세한 청록/청색 계기 조명, deck light accent |
| Common panels | `CommonPanels/base-lab.BMP`, `base-shop.BMP`, `exchange.bmp` | inset black-blue display, steel/chrome panel framing |

Stitch prompt에는 아래 말을 우선 넣는다.

- thick industrial metal frame
- dark black-blue inset display wells
- electric command-blue Korean labels
- mechanical seams and bolted steel borders
- compact old tactical hangar UI
- subtle blue/green instrument glow

피한다:

- generic SaaS settings card
- soft web dashboard card grid
- profile hero, trophy, rank, currency, shop tone
- decorative gradient/orb/background filler
- Nova Garage editor controls를 page별 목적 없이 깊게 복제하는 것

## Current Stitch Flow Candidates

아래 후보들은 runtime replacement가 아니라 source candidate다.

| flow role | Stitch title | screen id | current use |
|---|---|---|---|
| Garage baseline | `Tactical Unit Assembly Workspace` | `d440ad9223a24c0d8e746c7236f7ef27` | Garage 조립 작업대 visual 기준 |
| Operation memory | `Operation Memory - Accepted Dark Dock` | `753d889cc0874d69858fd17d98c66f7f` | 작전 기록/세계 기억 후보 |
| Shared shell | `Nova1492 Shared Shell / Navigation Only` | `7a083f26ec05412ca84188517d17d13f` | top shell과 shared NavigationBar 후보 |
| Shared feedback | `JG Shared Feedback / Status` | `998e5bdf3a734f3d873a3d90f19bd6a8` | status chip, toast, dialog 후보 |
| Shared controls | `JG Shared Components / Controls` | `660a91efb63346a1a68e5612ca1c1608` | atom/molecule control 후보 |
| Account/sync | `Nova1492 Compact Sync Console` | `7bc5b4ca92ca45559d4207a067057b57` | 계정/동기화 제어 패널 후보 |
| Connection/reconnect | `JG Connection / Reconnect Control` | `4e2da1df82fe4c619de57a4133a527dc` | 연결/재접속 상태 후보 |

긴 Stitch screenshot은 보통 `390x844` 모바일 viewport를 2x 스케일로 캡처한 scroll surface다.
길이 자체보다 첫 viewport 안에서 primary state, blocked reason, primary action이 보이는지를 먼저 판단한다.

## Flow Rules

비전투 UI는 아래 역할로 나눈다.

1. `Lobby`: 방 목록과 출격 흐름의 시작점이다.
2. `Garage`: 기체 편성과 저장 상태를 다루는 조립 작업대다.
3. `Account / Sync`: 계정, 로컬 상태, 클라우드 동기화 상태를 명시적으로 보여주는 control panel이다.
4. `Connection / Reconnect`: 연결 대기, 차단, 재시도 필요 상태를 숨기지 않는 operational surface다.
5. `Operation Memory`: 한 판 뒤 남은 작전 흔적과 기체 기억을 보여주는 기록 surface다.

공용 UI와 page UI를 섞지 않는다.

- 공용: top shell, shared NavigationBar, status chip, toast/dialog, base controls.
- page-owned: Garage save dock, Operation Memory return dock, Account sync action, Connection manual retry, Lobby room action.

색상 역할:

- command blue: navigation selected, focus, waiting/processing, system labels.
- signal orange: page-owned primary CTA 또는 명시적 warning/action.
- red: destructive, blocked, expired, failed state.

## Explicit State Rule

Fallback은 안티패턴이다.

화면 문구와 구조에서 아래를 금지한다.

- 숨은 자동 복구
- 조용한 로컬/클라우드 대체
- silent restore
- silent downgrade
- hidden alternate route
- background repair처럼 읽히는 표현

대신 아래처럼 명시한다.

- `LOCAL PRIMARY`
- `CLOUD PENDING`
- `CLOUD WRITE BLOCKED / 로그인 필요`
- `ROOM SYNC PENDING`
- `SESSION EXPIRED`
- `MANUAL RETRY`
- `사용자 확인 필요`

사용자가 누르는 행동도 명확해야 한다.

- `상태 다시 확인`
- `수동 재시도`
- `계정 확인`
- `로비로 돌아가기`

`로비로 돌아가기`는 숨은 대체 경로가 아니라 현재 시도를 떠나는 명시적 행동으로 표현한다.

## Stitch Prompt Checklist

새 비전투 화면을 Stitch에 요청할 때 최소로 확인한다.

- `Assets/Art/Nova1492/UI/Garage`의 `lab.bmp`, `WAITINGROOM.bmp`, `main.BMP` 톤을 반영한다.
- `390x844` mobile baseline을 명시한다.
- 첫 viewport reading order를 적는다.
- page-owned primary CTA와 shared navigation의 역할을 분리한다.
- orange를 navigation selected state에 쓰지 않는다.
- blocked/waiting/error state를 explicit copy로 적는다.
- finished empty/loading/error state를 요청한다.
- runtime replacement가 아니라 source candidate임을 유지한다.

## Handoff Notes

- Stitch 화면은 먼저 source candidate로 판단한다.
- accepted 후보 하나만 source freeze 대상으로 고정한다.
- source freeze 뒤 Unity 반영은 UI Toolkit candidate surface로 시작한다.
- runtime replacement, scene wiring, account/cloud 실제 동작 검증은 별도 pass다.
- per-surface manifest/map/presentation file을 새 active execution owner로 늘리지 않는다.
- script-side constants나 fallback으로 누락된 presentation 값을 메우지 않는다.

## Review Checklist

- 첫 화면에서 가장 중요한 상태와 CTA가 보이는가.
- 화면이 shop/reward/profile/SaaS로 읽히지 않는가.
- Nova1492 Garage 자산의 금속 프레임, 검정 패널, 파란 라벨 감각이 살아 있는가.
- 공용 UI와 page-owned UI가 분리되어 있는가.
- fallback처럼 읽히는 문구나 구조가 없는가.
- `GameDesign` 기준의 기체, 공세, 거점, 버텨냄/붕괴 말맛과 충돌하지 않는가.
