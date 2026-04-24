# Stitch UI/UX Overhaul Plan

> 마지막 업데이트: 2026-04-25
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
- set inventory 자체는 남아 있지만, 현재 committed repo truth에서 active handoff가 직접 닫힌 surface는 `Set B Garage main workspace`, `Set C account-delete-confirm`, `Set C common-error-dialog` 정도다. 나머지 inventory는 아직 reference lane이다.
- active handoff artifact는 accepted source freeze와 in-memory execution contract만 허용한다.
- per-surface `screen/map/presentation` JSON file은 active route에서 제거했다.
- set별 전용 SceneTool과 set별 전용 review prep menu는 active route에서 제거했다.
- 하지만 새 surface translation이 완전히 generic한 상태는 아니다. 현재 병목은 source discovery보다 family detection / generic parser coverage다.
- `.stitch/contracts/components/shared-ui.component-catalog.json`은 shared UI vocabulary용 companion contract로 유지하고, active generator input으로 올리지 않는다.
- set별 md/png 기반 자산은 historical reference로만 남는다.
- `.stitch/handoff/*.md`와 set별 캡처/pass 산출물은 historical/reference lane으로만 남긴다.
- 현재 단계는 `concept generation`이 아니라 `active surface contract closure -> source-derived presentation closure -> prefab baseline recovery -> fresh translation evidence`다.

## Working SSOT

- 제품 판단: `design.game-design`
- Lobby/Garage layout 기준: `design.ui-foundations`
- Stitch 활용 원칙: `design.ui-reference-workflow`
- Stitch visual system artifact: `.stitch/DESIGN.md`
- Stitch 세트별 번역 기준 artifact: in-memory `screen manifest`
- Stitch Unity binding artifact: in-memory `unity-map`
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
- contract: `set-a Lobby` in-memory execution contract lane
- 현재 상태: set inventory에는 남아 있지만 active structured contract가 현재 repo에 닫혀 있지 않다. `Set B` recovery가 닫히기 전까지 reference lane으로 유지한다.

### Set B - Garage

- 범위: `Garage main workspace`, `slot selector`, `focused editor`, `preview`, `summary`, `save dock`, `Garage settings overlay`, `Account card`
- contract: `set-b Garage` in-memory execution contract lane
- 현재 상태: 현재 committed repo에서 실제 active handoff가 닫힌 유일한 surface다. 다음 패스는 시각 polish가 아니라 `compiled manifest -> compiled unity-map -> committed prefab target -> fresh translation evidence`를 다시 한 줄로 닫는 것이다.

### Set C - Overlay

- 범위: `Room detail panel`, `Login loading overlay`, `Account delete confirm`, `Common modal/error dialog`
- contract: `set-c overlay` in-memory execution contract lane
- 현재 상태: `account-delete-confirm`는 active execution contracts, committed prefab baseline, translation artifact, SceneView capture evidence까지 닫혔다. 남은 이슈는 `warning icon glyph` asset과 runtime/mobile framing fidelity이고, 나머지 Set C surface는 계속 reference lane이다.
- 현재 상태: `common-error-dialog`도 같은 loop로 닫혔다. execution contracts, translation artifact, SceneView capture evidence가 모두 있고, translation에서 `presentation.applied = true`를 유지한다.

### Set D - Battle HUD

- 범위: `HUD`, `Unit summon bar`, `Core HP`, `Wave HUD`, `Placement feedback`, `Cannot afford overlay`
- contract: `set-d Battle HUD` in-memory execution contract lane
- 현재 상태: set inventory와 source artifact는 남아 있지만, current repo truth에서 concrete runtime scene과 active handoff가 함께 닫혀 있지 않다. `Set B` recovery 이후에 다시 연다.

### Set E - Result / Feedback

- 범위: `Wave end / result overlay`, `toast`, `banner`, `feedback`
- contract: `set-e result/feedback` in-memory execution contract lane
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

## Generic Onboarding Plan

목표는 `Set B/C/D/E`와 이후 다시 여는 inventory set을 같은 route로 태우되, 새 set이나 새 surface를 추가할 때 per-surface script edit가 필요하지 않게 만드는 것이다.

적용 범위:

- accepted Stitch source freeze가 있는 surface
- `workspace`, `overlay`, 이후 추가 family로 분류 가능한 surface
- `source freeze -> in-memory execution contract -> translation output` route를 따르는 handoff

제외 범위:

- Unity target prefab/scene 자체가 아직 없는 lane
- visual fidelity final judgment와 runtime correctness closeout
- family-level generator capability 자체가 아직 정의되지 않은 완전히 새로운 UI grammar

핵심 기준:

- 새 per-surface JSON file을 만들지 않는다.
- 새 per-surface switch/case나 hardcoded source path를 기본 onboarding 수단으로 쓰지 않는다.
- 실패 시 script fallback 대신 `blockedReason`으로 멈춘다.

### Workstream 1. Generic Source Discovery

- 목표: surface id를 코드에 등록하지 않고 accepted source freeze에서 `screen.html/png`를 찾는다.
- 방법: active source inventory 또는 source freeze metadata를 generic lookup으로 읽고, `projectId/screenId/url/html/png`를 같은 구조로 반환한다.
- 완료 조건: 새 source freeze를 추가해도 `Get-SupportedSurfaceSourceMetadata` 같은 per-surface table 편집 없이 source를 찾을 수 있다.

### Workstream 2. Family Detection From Source

- 목표: `garage-main-workspace`, `account-delete-confirm` 같은 이름 분기 대신 html/source pattern으로 `workspace`, `overlay` family를 판별한다.
- 방법: first-read order, block composition, CTA posture, dialog viewport 같은 source signal을 읽어 family를 정한다.
- 완료 조건: 새 surface가 기존 family 중 하나에 속하면 family 판별을 위해 per-surface script edit가 필요 없다.

### Workstream 3. Generic Manifest / Unity Map Compile

- 목표: per-surface block table를 코드에 직접 늘리지 않고 family-level rule로 in-memory `screen manifest`와 `unity-map`을 만든다.
- 방법: family별 required block set, block role mapping, target binding 규칙만 남기고 surface별 차이는 source-derived semantic block으로 흡수한다.
- 완료 조건: 새 surface는 source freeze와 target binding만 있으면 manifest/map compile이 같은 script route로 닫힌다.

### Workstream 4. Generic Review Route

- 목표: review capture route를 surface별 switch가 아니라 family/target rule로 준비한다.
- 방법: `workspace`는 workspace staging route, `overlay`는 overlay staging route처럼 route kind를 줄이고, request file을 읽는 generic review tool만 사용한다. 없는 경우에는 translation과 분리된 warning으로만 남긴다.
- 완료 조건: 새 surface 때문에 set별 review prep menu나 set별 SceneTool을 다시 만들지 않는다.

### Workstream 5. Failure And Acceptance Guard

- 목표: generic화 과정에서도 조용한 우회 실행을 막는다.
- 방법: source discovery 실패, family 판별 실패, required block 누락, target binding 불가를 전부 explicit blocked로 남긴다.
- 완료 조건: 새 surface가 supported family에 속하지 않으면 성공처럼 지나가지 않고 `blockedReason`으로 종료된다.

## Acceptance For Generic Onboarding

- 새 surface onboarding 때 per-surface PowerShell edit가 없다.
- 새 per-surface `screen/map/presentation` JSON file이 생기지 않는다.
- 같은 script route로 `workspace`와 `overlay` family가 모두 돈다.
- `Set B`와 `Set C` current surface를 generic route로 다시 통과시킨다.
- 그 다음 `Set D`, `Set E`, 또는 이후 다시 여는 inventory set 중 최소 1개 surface를 코드 수정 없이 source freeze 추가만으로 translation까지 태운다.
- 실패 surface는 `blockedReason`을 남기고 멈추며, fallback success를 만들지 않는다.

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

1. `Set B`와 `Set C` current surface를 기준 샘플로 삼아 generic onboarding route를 먼저 닫는다.
2. source discovery, family detection, review route의 per-surface hardcode를 family/generic rule로 줄인다.
3. generic route로 `Set B Garage`와 `Set C overlay`를 다시 통과시킨다.
4. 그 다음 `Set D/E` 또는 이후 다시 여는 inventory set 중 하나를 코드 수정 없이 source freeze만 추가해 태운다.
5. visual fidelity final judgment와 shared runtime correctness는 각 lane owner 계획에서 계속 별도로 닫는다.

현재 truth 메모:

- set별 전용 SceneTool은 제거했고, review route는 generic family tool만 남긴 상태다.
- 이제 남은 핵심은 `Set A/D/E`를 위한 새 전용 도구 추가가 아니라, 공통 parser가 해당 source structure를 읽도록 넓히는 것이다.

보조 원칙:

- 승인된 세트의 visual language는 다음 세트의 기본값으로 이어간다.
- popup과 overlay는 메인 flow보다 더 시끄럽게 만들지 않는다.
- empty/loading/error state도 완성형 카드나 패널처럼 남긴다.
- active surface가 닫히기 전에는 같은 책임의 보조 문서, pass 캡처, handoff md를 새로 늘리지 않는다.

## Artifacts To Keep Updated

- `.stitch/DESIGN.md`
- 관련 `docs/design/*` 또는 `docs/plans/*` SSOT
- active surface의 `preflight / translation / pipeline` artifact

reference로만 유지:

- `.stitch/prompt-briefs/*.md`
- `.stitch/designs/*`
- `.stitch/handoff/*`
- pass 캡처와 sceneview 이미지 묶음

외부 로컬 메모가 생겨도, 계속 참조해야 하는 결정은 이 문서나 해당 SSOT로 승격한다.
