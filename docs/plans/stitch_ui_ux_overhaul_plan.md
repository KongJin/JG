# Stitch UI/UX Overhaul Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.stitch-ui-ux-overhaul
> role: plan
> owner_scope: Stitch set inventory와 zero-touch onboarding 실행 기준 reference
> upstream: design.ui-reference-workflow, ops.stitch-data-workflow, plans.progress
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/contracts/`

이 문서는 JG의 `Stitch` 기반 UI/UX 개편 실행 기준 reference다.
외부 메모나 세션 채팅에 흩어진 기준을 레포 안으로 모으고, `Stitch 판단 -> structured contract -> Unity prefab-first reset` 루프를 같은 용어로 유지하는 목적을 가진다.
세부 운영 규칙은 `ops.stitch-data-workflow`, 활용 원칙은 `design.ui-reference-workflow`를 따르고, 현재 실행 상태와 우선순위는 `plans.progress`를 우선한다.

## Goal

- `Lobby`, `Garage`, `Overlay`, `Battle HUD`, `Result`를 하나의 tactical hangar visual language로 재정렬한다.
- 산출물은 단순 시안이 아니라 `Unity scene 계약으로 바로 번역 가능한 structured contract`까지 포함한다.
- `Stitch`는 방향 탐색과 visual handoff에 쓰고, reset 중의 committed runtime layout SSOT는 계속 `contract + prefab target + fresh translation evidence`가 가진다.

## Route Snapshot

- Gate 0는 이미 통과했다.
  - Stitch 인증/프로젝트 접근 성공
  - 마스터 프로젝트 `11729197788183873077` 확정
  - `.stitch/DESIGN.md` 초안 작성 완료
- set inventory 자체는 남아 있지만, 현재 zero-touch route의 active rehearsal surface는 `Set B Garage main workspace`, `Set C account-delete-confirm`, `Set C common-error-dialog` 정도다. 나머지 inventory는 아직 reference lane이다.
- active handoff artifact는 accepted source freeze와 in-memory execution contract만 허용한다.
- per-surface `screen/map/presentation` JSON file은 active route에서 제거했다.
- set별 전용 SceneTool과 set별 전용 review prep menu는 active route에서 제거했다.
- 하지만 새 surface translation이 아직 `zero-touch source-to-prefab` 상태는 아니다. 현재 병목은 source discovery보다 source grammar 해석, target capability 매칭, generic parser coverage다.
- 2026-04-25 Set B deleted-target rehearsal 기준, target prefab이 없는 상태에서 source-to-prefab route를 실행하면 prefab file은 생성될 수 있지만 command completion, fresh translation/pipeline evidence, Korean text preservation, review capture framing이 아직 한 줄로 닫히지 않는다.
- `.stitch/contracts/components/shared-ui.component-catalog.json`은 shared UI vocabulary용 companion contract로 유지하고, active generator input으로 올리지 않는다.
- set별 md/png 기반 자산은 historical reference로만 남는다.
- `.stitch/handoff/*.md`와 set별 캡처/pass 산출물은 historical/reference lane으로만 남긴다.
- 현재 단계는 `concept generation`이 아니라 `active surface contract closure -> source-derived presentation closure -> prefab baseline recovery -> fresh translation evidence`다.

이 snapshot은 route 판단을 위한 기준 메모다.
실제 현재 우선순위와 완료/미완료 판정은 `plans.progress`를 우선한다.

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
- 현재 상태: zero-touch route의 primary rehearsal surface다. 다음 패스는 시각 polish가 아니라 deleted-target 상태에서도 `compiled manifest -> compiled unity-map -> generated prefab target -> fresh translation/pipeline evidence -> review proof`를 다시 한 줄로 닫는 것이다.

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

## Zero-Touch Source-To-Prefab Plan

목표는 `Set A/B/C/D/E`와 이후 다시 여는 inventory set을 같은 route로 태우되, 새 screen을 추가할 때 screen별 layer, script, JSON, switch/case, hardcoded source path를 추가하지 않고 `source freeze 추가 -> in-memory execution contract compile -> prefab translation`만으로 닫는 것이다.
이 목표는 기존 prefab이 있을 때의 patch만 뜻하지 않는다. target prefab이 의도적으로 삭제된 상태에서도 같은 route가 `from-scratch generate -> fresh evidence -> review proof`로 닫혀야 한다.

한 줄 기준:

`새 screen 추가`를 `새 실행 로직 추가`로 취급하지 않는다.

적용 범위:

- accepted Stitch source freeze가 있는 surface
- 현재 shared component vocabulary와 target capability 안에서 표현 가능한 surface
- `source freeze -> in-memory execution contract -> translation output` route를 따르는 handoff

source freeze 추가 허용 범위:

- `artifacts/stitch/<project>/<screen>/screen.html`
- `artifacts/stitch/<project>/<screen>/screen.png`
- `artifacts/stitch/<project>/<screen>/meta.json`

`.stitch/designs/` copied file은 historical/reference convenience로만 볼 수 있고, active source freeze owner는 아니다.
위 source freeze를 등록하기 위해 per-screen script, helper, stored manifest/map/presentation JSON, review menu를 추가하면 zero-touch onboarding으로 보지 않는다.

제외 범위:

- Unity target capability/root contract 자체가 아직 정의되지 않은 lane
- visual fidelity final judgment와 runtime correctness closeout
- 현재 shared grammar / target capability 밖에 있는 완전히 새로운 UI grammar

단, `target prefab file이 없음`은 그 surface가 지원 target kind와 capability 안에 있을 때 제외 사유가 아니다.
`generate-or-patch` route에서는 missing target을 정상 from-scratch 입력으로 처리하거나, 생성 capability가 없으면 explicit blocked로 종료해야 한다.

capability gate:

- 기존 `workspace-root`, `overlay-root`, `hud-root`, `result-root` target capability 안에 들어가면 source freeze 추가만으로 시도한다.
- 기존 capability 밖이면 해당 작업은 zero-touch onboarding이 아니라 capability expansion이다.
- capability expansion은 공통 grammar/target binding을 넓히는 별도 작업으로 분리하고, 그 surface 하나만을 위한 분기나 fallback으로 닫지 않는다.
- zero-touch onboarding 중 새 공통 tool, policy, parser, review route 수정이 필요해지는 순간 해당 실행은 `blocked`로 멈춘다. 그 수정은 같은 surface를 닫기 위한 후속 패치가 아니라 별도 capability expansion lane으로 다시 선언해야 한다.

핵심 기준:

- 새 per-surface JSON file을 만들지 않는다.
- 새 per-surface script edit, switch/case, hardcoded source path를 onboarding 수단으로 쓰지 않는다.
- 새 per-surface layer나 runtime fallback을 추가해 translation 성공처럼 위장하지 않는다.
- onboarding evidence와 공통 Stitch/Unity MCP capability 또는 evidence policy 수정을 같은 closeout에 섞지 않는다.
- 지원 grammar/capability 밖의 화면은 `blockedReason`으로 멈추고, 성공처럼 보고하지 않는다.
- command가 timeout/hang 상태로 남아 prefab만 부분 생성하는 상태를 success로 취급하지 않는다.
- source text, 특히 Korean-first copy가 `???` 또는 placeholder glyph로 손실되면 presentation mismatch로 본다.
- review capture가 source와 비교 가능한 `390x844` framing을 제공하지 못하면 visual fidelity judgment를 닫지 않는다.

추가 경계:

- 이 plan의 구현 과정에서는 generic compiler / extractor / target capability route를 닫기 위한 공통 infra 수정이 있을 수 있다.
- 하지만 route acceptance 이후의 새 screen onboarding은 `source freeze 추가` 외의 repo-tracked logic mutation을 요구하지 않는 상태를 목표로 한다.

### Workstream 1. Source-Freeze-Only Input

- 목표: 새 screen onboarding 입력을 accepted `screen.html/png + metadata`로만 고정한다.
- 방법: source inventory를 generic lookup으로 읽고, `projectId/screenId/url/html/png`를 같은 구조로 반환한다.
- 완료 조건: 새 source freeze를 추가해도 per-surface table, manual JSON, source path helper 편집 없이 source를 찾는다.

### Workstream 2. Source Grammar Extraction

- 목표: screen 이름이나 set 이름이 아니라 source structure만으로 semantic block graph를 뽑는다.
- 방법: first-read order, section composition, CTA posture, list/form/dialog/shell 패턴을 읽어 `header`, `body`, `section`, `list`, `form`, `dialog`, `cta-dock`, `status`, `feedback` 같은 공통 grammar로 정규화한다.
- 완료 조건: `Set A/B/C/D/E` surface가 이름 분기 없이 같은 extractor route를 통과하고, 지원 grammar면 같은 contract 구조로 compile된다.

### Workstream 3. Target Capability Binding

- 목표: surface별 hostPath 테이블 대신 target prefab이 제공하는 slot/capability에 semantic block를 매칭한다.
- 방법: target은 `workspace-root`, `overlay-root`, `hud-root`, `result-root`처럼 수용 가능한 block 역할과 required binding만 선언하고, compiler는 source grammar를 읽어 target capability에 배치한다.
- 완료 조건: 새 screen은 기존 target capability 안에 있으면 per-surface binding edit 없이 `unity-map`이 compile된다.

### Workstream 4. Grammar-To-Contract Compile

- 목표: per-surface block table를 코드에 직접 늘리지 않고 grammar rule로 in-memory `screen manifest`, `unity-map`, `presentation-contract`를 만든다.
- 방법: semantic block graph, CTA priority, validation focus, target capability를 합쳐 contract를 compile하고, source-derived 값만 presentation contract로 올린다.
- 완료 조건: 지원 grammar의 새 screen은 source freeze만 추가하면 같은 compile route로 translation-ready contract가 만들어진다.

### Workstream 5. Generic Translation / Review Route

- 목표: translator와 review prep이 screen별 분기 없이 같은 route를 사용한다.
- 방법: translator는 `manifest semantic order -> binding check -> presentation apply -> controller wiring`만 수행하고, review route는 target kind별 generic staging rule만 사용한다.
- 완료 조건: 새 surface 때문에 set별 SceneTool, review prep menu, translation helper를 다시 만들지 않는다.

### Workstream 6. Command Completion And Evidence Atomicity

- 목표: from-scratch generate가 성공/blocked/timeout 중 하나로 반드시 종료되고, 부분 생성된 prefab만 남아 success처럼 보이지 않게 한다.
- 방법: translation stage, Unity MCP call, asset save/import, review capture를 stage 단위로 timeout 처리하고 pipeline artifact에 terminal verdict를 쓴다.
- 완료 조건: target prefab 삭제 상태에서 `source freeze -> compile -> generate -> pipeline`이 한 실행에서 fresh하게 닫히거나, 아래 최소 shape로 닫힌다.

timeout / blocked 최소 artifact shape:

- `success = false`
- `terminalVerdict = blocked`
- `blockedReason = <stage>-timeout` 또는 구체적 blocked reason
- `stageStatus.<stage> = timeout | blocked | not-run`
- `artifacts.pipelineResult`가 stale artifact를 fresh success처럼 가리키지 않음

### Workstream 7. Source Text And Encoding Fidelity

- 목표: source-derived presentation 값이 generation 과정에서 손실되지 않게 한다.
- 방법: HTML extraction, PowerShell JSON serialization, MCP payload, TextMeshPro assignment 단계에서 Korean copy를 fixture로 검증한다.
- 완료 조건: Set B 기준 fixture text set이 prefab text values와 review output에서 `???` 없이 보존된다.

Set B fixture text set:

- `격납고 관리`
- `저장 및 배치`
- `HV-42 레일건`
- `주무장`

### Workstream 8. Failure Guard And Honest Exit

- 목표: zero-touch route에서도 조용한 우회 실행을 막는다.
- 방법: source discovery 실패, grammar 추출 실패, target capability 불일치, required block 누락, presentation unresolved, command timeout, encoding loss, review capture unsupported를 모두 explicit blocked 또는 mismatch로 남긴다.
- 완료 조건: 새 surface가 현재 route 밖이면 fallback success 없이 `blockedReason`으로 종료되고, 그 사유만 다음 capability 확장 backlog로 넘긴다.

## Acceptance For Zero-Touch Onboarding

- 새 screen onboarding 때 per-surface PowerShell/script edit가 없다.
- 새 per-surface `screen/map/presentation` JSON file이 생기지 않는다.
- 새 per-surface layer, helper, scene tool, review prep menu가 생기지 않는다.
- target prefab 삭제 상태에서도 같은 compile/translation route가 from-scratch generate를 완료하거나 explicit blocked/timeout artifact로 종료한다.
- 같은 compile/translation route로 `Set B/C` current surface를 다시 통과시킨다.
- `Set B Garage` from-scratch rehearsal에서 Korean source text가 prefab에 `???` 없이 보존된다.
- `Set B Garage` review capture가 source와 비교 가능한 `390x844` framing으로 갱신된다.
- 그 다음 `Set A`, `Set D`, `Set E` 중 최소 1개 surface를 코드 수정 없이 source freeze 추가만으로 translation까지 태운다.
- 최종 acceptance는 inventory 밖의 신규 screen 1개를 같은 조건으로 태워 닫는다.
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

## Route Hardening Order

zero-touch route를 닫을 때의 권장 실행 순서는 아래다.
현재 repo 전체 우선순위는 `plans.progress`를 우선한다.

1. `Set B Garage` deleted-target rehearsal에서 translation command가 hang/timeout 없이 terminal verdict를 남기게 한다.
2. prefab 부분 생성과 stale translation/pipeline artifact가 함께 남는 상태를 막는다.
3. Korean source text가 `???`로 손실되는 extraction/apply 경로를 고친다.
4. review capture가 `390x844` source comparison에 쓸 수 있는 framing으로 갱신되게 한다.
5. `Set B`와 `Set C` current surface를 기준 샘플로 삼아 zero-touch compile route를 다시 통과시킨다.
6. source discovery, grammar extraction, target capability binding에서 per-surface hardcode를 제거한다.
7. 그 다음 `Set A`, `Set D`, `Set E` 중 최소 1개를 코드 수정 없이 source freeze 추가만으로 태운다.
8. 마지막으로 inventory 밖 신규 screen 1개를 같은 조건으로 태워 zero-touch acceptance를 닫는다.
9. visual fidelity final judgment와 shared runtime correctness는 각 lane owner 계획에서 계속 별도로 닫는다.

Route notes:

- set별 전용 SceneTool은 제거했고, review route는 generic tool만 남긴 상태다.
- 이제 남은 핵심은 `Set A/D/E`를 위한 새 전용 도구 추가가 아니라, 공통 grammar extractor와 target capability route가 어떤 screen도 같은 구조로 읽게 만드는 것이다.

Incident guard:

- 2026-04-25 Set A 재개 시 `Set A를 끝까지 닫자`는 압력으로 onboarding 실행 중 공통 Stitch/Unity MCP 코드와 evidence policy를 함께 수정했다. 원인은 `unsupported surface -> blocked report -> stop` 경계를 지키지 않고, capability expansion을 같은 closeout 안으로 끌어온 것이다.
- 재발 방지 기준: 기본 `UnityUiAuthoringWorkflowPolicy`는 screen onboarding evidence와 공통 Stitch/Unity MCP capability/policy edit가 같은 작업에 섞이면 `stitch-onboarding-mixed-with-capability-expansion`으로 실패한다.
- capability expansion이 실제 목표라면 먼저 작업 lane을 capability expansion으로 선언하고, zero-touch onboarding acceptance와 별도로 검증한다. 이 경우에만 policy를 명시적으로 capability expansion 허용 모드로 실행할 수 있다.

보조 원칙:

- 승인된 세트의 visual language는 다음 세트의 기본값으로 이어간다.
- popup과 overlay는 메인 flow보다 더 시끄럽게 만들지 않는다.
- empty/loading/error state도 완성형 카드나 패널처럼 남긴다.
- active surface가 닫히기 전에는 같은 책임의 보조 문서, pass 캡처, handoff md를 새로 늘리지 않는다.

## Route Artifacts

route 기준이 바뀌면 아래 artifact와 owner 문서를 함께 점검한다.

- `.stitch/DESIGN.md`
- 관련 `docs/design/*` 또는 `docs/plans/*` owner 문서
- active surface의 `preflight / translation / pipeline` artifact

reference로만 유지:

- `.stitch/prompt-briefs/*.md`
- `.stitch/designs/*`
- `.stitch/handoff/*`
- pass 캡처와 sceneview 이미지 묶음

외부 로컬 메모가 생겨도, 계속 참조해야 하는 결정은 이 문서나 해당 SSOT로 승격한다.
