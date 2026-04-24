# Stitch UI/UX Overhaul Plan

> 마지막 업데이트: 2026-04-24
> 상태: active
> doc_id: plans.stitch-ui-ux-overhaul
> role: plan
> owner_scope: Stitch set inventory, 현재 상태, 다음 작업, 실행 순서
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow, plans.progress
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/contracts/`

이 문서는 JG의 `Stitch` 기반 UI/UX 개편 실행 계획 SSOT다.
외부 메모나 세션 채팅에 흩어진 기준을 레포 안으로 모으고, `Stitch 판단 -> structured contract -> Unity prefab-first reset` 루프를 같은 용어로 유지하는 목적을 가진다.
운영 규칙 본문은 `ops.stitch-data-workflow`, 활용 원칙은 `design.ui-reference-workflow`가 소유하고, 이 문서는 실행 상태와 우선순위만 소유한다.

## Goal

- `Lobby`, `Garage`, `Overlay`, `Battle HUD`, `Result`를 하나의 tactical hangar visual language로 재정렬한다.
- 산출물은 단순 시안이 아니라 `Unity scene 계약으로 바로 번역 가능한 structured contract`까지 포함한다.
- `Stitch`는 방향 탐색과 visual handoff에 쓰고, reset 중의 committed runtime layout SSOT는 계속 `contract + prefab target + fresh translation evidence`가 가진다.

## Current Status

- Gate 0는 이미 통과했다.
  - Stitch 인증/프로젝트 접근 성공
  - 마스터 프로젝트 `11729197788183873077` 확정
  - `.stitch/DESIGN.md` 초안 작성 완료
- set inventory 자체는 남아 있지만, 현재 committed repo truth에서 active surface로 바로 읽을 수 있는 handoff는 `Set B Garage main workspace` 하나다.
- active handoff artifact는 `.stitch/contracts/screens/*.json`, `.stitch/contracts/mappings/*.json`만 허용한다.
- `.stitch/contracts/components/shared-ui.component-catalog.json`은 shared UI vocabulary용 companion contract로 유지하고, active generator input으로 올리지 않는다.
- set별 md/png 기반 자산은 historical reference로만 남는다.
- `.stitch/handoff/*.md`와 set별 캡처/pass 산출물은 historical/reference lane으로만 남긴다.
- 현재 단계는 `concept generation`이 아니라 `active surface contract closure -> source-derived presentation closure -> prefab baseline recovery -> fresh translation evidence`다.

## Working SSOT

- 제품 판단: `design.game-design`
- Lobby/Garage layout 기준: `design.ui-foundations`
- Stitch 활용 원칙: `design.ui-reference-workflow`
- Stitch visual system artifact: `.stitch/DESIGN.md`
- Stitch 세트별 번역 기준 artifact: `.stitch/contracts/screens/*.json`
- Stitch Unity binding artifact: `.stitch/contracts/mappings/*.json`
- Stitch shared vocabulary artifact: `.stitch/contracts/components/shared-ui.component-catalog.json`
- 진행 상태 SSOT: `plans.progress`

규칙:

- `Stitch` 산출물은 시안과 structured contract 기준이다.
- shared component catalog는 세트 공통 vocabulary를 고정하는 reference lane이다.
- reset 중에는 concrete scene보다 `accepted execution contracts + committed prefab target + fresh translation evidence`를 먼저 본다.
- `TempScene.unity`는 staging surface일 뿐 committed runtime SSOT가 아니다.
- 시안 복제 대신, prefab hierarchy와 serialized contract에 맞는 block 재구성으로 번역한다.

## Tooling / Skill Route

- repo entry: `jg-stitch-workflow`
  - JG 기준 Stitch 단일 진입점
  - prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, structured contract 정리, Unity lane handoff를 맡는다.

프롬프트 작성 기본값:

- mobile-first
- validation frame `390x844`
- 한글 카피 우선
- `dark tactical sci-fi base`
- `Nova-style garage preparation`과 `short readable summon clarity`는 원칙 차용만 허용

## Set Inventory

### Set A - Lobby

- 범위: `Lobby main`, `Room list empty/list`, `Create room`, `Garage summary`
- contract: `set-a Lobby manifests under .stitch/contracts/screens/`, Unity binding under `.stitch/contracts/mappings/`
- 현재 상태: set inventory에는 남아 있지만 active structured contract가 현재 repo에 닫혀 있지 않다. `Set B` recovery가 닫히기 전까지 reference lane으로 유지한다.

### Set B - Garage

- 범위: `Garage main workspace`, `slot selector`, `focused editor`, `preview`, `summary`, `save dock`, `Garage settings overlay`, `Account card`
- contract: `set-b Garage manifests under .stitch/contracts/screens/`, Unity binding under `.stitch/contracts/mappings/`
- 현재 상태: 현재 committed repo에서 실제 active handoff가 닫힌 유일한 surface다. 다음 패스는 시각 polish가 아니라 `manifest -> unity-map -> committed prefab target -> fresh translation evidence`를 다시 한 줄로 닫는 것이다.

### Set C - Overlay

- 범위: `Room detail panel`, `Login loading overlay`, `Account delete confirm`, `Common modal/error dialog`
- contract: `set-c overlay manifests under .stitch/contracts/screens/`, Unity binding under `.stitch/contracts/mappings/`
- 현재 상태: `account-delete-confirm`는 active execution contracts, committed prefab baseline, translation artifact, SceneView capture evidence까지 닫혔다. 남은 이슈는 `warning icon glyph` asset과 runtime/mobile framing fidelity이고, 나머지 Set C surface는 계속 reference lane이다.
- 현재 상태: `common-error-dialog`도 같은 loop로 닫혔다. execution contracts, translation artifact, SceneView capture evidence가 모두 있고, translation에서 `presentation.applied = true`를 유지한다.

### Set D - Battle HUD

- 범위: `HUD`, `Unit summon bar`, `Core HP`, `Wave HUD`, `Placement feedback`, `Cannot afford overlay`
- contract: `set-d Battle HUD manifests under .stitch/contracts/screens/`, Unity binding under `.stitch/contracts/mappings/`
- 현재 상태: set inventory와 source artifact는 남아 있지만, current repo truth에서 concrete runtime scene과 active handoff가 함께 닫혀 있지 않다. `Set B` recovery 이후에 다시 연다.

### Set E - Result / Feedback

- 범위: `Wave end / result overlay`, `toast`, `banner`, `feedback`
- contract: `set-e result/feedback manifests under .stitch/contracts/screens/`, Unity binding under `.stitch/contracts/mappings/`
- 현재 상태: 일부 brief와 source artifact만 남아 있다. `Set D`와 함께 reference lane으로 유지한다.

## Standard Execution Loop

각 세트는 아래 순서를 반복한다.

1. 현재 scene contract와 관련 SSOT 문서를 읽고 surface 목적, 읽기 순서, CTA 우선순위를 다시 적는다.
2. 필요하면 `jg-stitch-workflow` 기준으로 prompt brief를 갱신한다.
3. `Stitch`에서는 한 세트당 1개의 baseline만 유지하고, 기각안은 이유만 남긴다.
4. accepted 판단은 기본값으로 `execution contracts`로 구조화한다.
5. prefab authoring 전에는 `block -> shared component` 대응을 shared catalog vocabulary로 먼저 점검한다.
6. Unity 반영은 `prefab-first reset` 기준으로 수행한다.
7. 세트 종료 시 structured contract와 repo 문서를 함께 동기화한다.
8. 검증은 더 싼 레이어부터 통과시킨다.

## Validation Order

### Lobby / Garage

1. contract
2. EditMode/unit tests
3. preflight / translation / pipeline evidence
4. page-switch 또는 feature smoke

### Battle / Result

1. contract
2. summon or wave smoke
3. outcome overlay smoke

모든 시각 sanity check는 `390x844`를 기준으로 남긴다.

## Immediate Next Work

현재 기본 우선순위는 아래 셋이다.

1. `Set B Garage`만 active surface로 고정한다.
2. `Set B Garage`의 committed prefab target과 fresh translation evidence를 다시 맞춘다.
3. `Set C account-delete-confirm`에서 source 기반 execution contract 준비 흐름을 안정화하고, 나머지 `Set A/C/D/E`는 active handoff가 다시 닫힐 때까지 reference lane으로 유지한다.

보조 원칙:

- 승인된 세트의 visual language는 다음 세트의 기본값으로 이어간다.
- popup과 overlay는 메인 flow보다 더 시끄럽게 만들지 않는다.
- empty/loading/error state도 완성형 카드나 패널처럼 남긴다.
- active surface가 닫히기 전에는 같은 책임의 보조 문서, pass 캡처, handoff md를 새로 늘리지 않는다.

## Artifacts To Keep Updated

- `.stitch/DESIGN.md`
- `.stitch/contracts/mappings/*.json`
- `.stitch/contracts/presentations/*.json`
- `.stitch/contracts/screens/*.json`
- 관련 `docs/design/*` 또는 `docs/plans/*` SSOT
- active surface의 `preflight / translation / pipeline` artifact

reference로만 유지:

- `.stitch/prompt-briefs/*.md`
- `.stitch/designs/*`
- `.stitch/handoff/*`
- pass 캡처와 sceneview 이미지 묶음

외부 로컬 메모가 생겨도, 계속 참조해야 하는 결정은 이 문서나 해당 SSOT로 승격한다.
