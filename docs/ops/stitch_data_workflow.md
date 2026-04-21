# Stitch Data Workflow

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: ops.stitch-data-workflow
> role: ssot
> owner_scope: Stitch working data ownership, prompt brief lifecycle, design export storage, Unity handoff 운영
> upstream: design.ui-reference-workflow, plans.stitch-ui-ux-overhaul, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/contracts/`

이 문서는 JG에서 `Stitch` 산출물을 어떻게 읽고, 어디에 저장하고, 언제 Unity handoff로 넘길지 정하는 단일 운영 기준이다.
시각 탐색 원칙은 `design.ui-reference-workflow`, 실행 상태와 세트별 우선순위는 `plans.stitch-ui-ux-overhaul`가 소유한다.
여기서는 그 둘 사이의 **데이터 흐름과 파일 소유권**만 고정한다.
handoff completeness 판정은 `ops.stitch-handoff-completeness-checklist`를 사용한다.
구조화된 handoff 필드와 schema는 `ops.stitch-structured-handoff-contract`가 소유한다.

## 목적

- `Stitch prompt -> generated screen -> handoff -> Unity 구현` 루프의 파일 경계를 고정한다.
- 같은 역할의 문서와 skill이 여러 군데에서 따로 놀지 않게 한다.
- `Stitch` 산출물을 참고 자료와 working data로만 쓰고, runtime SSOT와 혼동하지 않게 한다.

## 역할 구분

### Stitch가 맡는 것

- visual language 탐색
- screen-level prompt brief 정리
- Unity 번역용 JSON contract 작성

### Unity가 맡는 것

- runtime hierarchy
- serialized reference
- 실제 layout, wiring, interaction contract
- 최종 smoke 및 acceptance evidence

한 줄 기준:

`Stitch는 방향과 handoff를 소유하고, Unity scene/prefab은 runtime truth를 소유한다.`

## 파일 소유권

### 문서 SSOT

- Stitch 활용 원칙 owner: `design.ui-reference-workflow`
- Stitch 실행 계획 owner: `plans.stitch-ui-ux-overhaul`
- Stitch 데이터 운영: 이 문서
- Unity UI authoring route owner: `ops.unity-ui-authoring-workflow`

### `.stitch/` working set

- `.stitch/DESIGN.md`
  - Stitch visual system의 working SSOT
  - 새 세트 공통 tone, typography, component posture를 여기서만 보정한다.
- `.stitch/prompt-briefs/*.md`
  - screen 단위 입력 브리프
  - screen 목적, reading order, CTA 우선순위, 상태 조건을 적는다.
- `.stitch/contracts/blueprints/*.json`
  - reusable surface family skeleton
  - 공통 block 구조, 기본 states, 기본 validation을 담는다.
- `.stitch/contracts/screens/*.json`
  - concrete Unity handoff manifest
  - target path, CTA, states, validation, screen-specific delta를 담는다.
- `.stitch/contracts/*.json`
  - full contract fallback
  - migration 또는 one-off surface에서만 제한적으로 사용한다.
- `.stitch/contracts/schema/*.json`
  - contract schema

### historical only

- `.stitch/designs/*.{html,png}`
- `.stitch/handoff/*.md`

위 두 경로는 기존 산출물 보관용으로만 남긴다.
새 파일 생성, 갱신, acceptance 증거 사용을 금지한다.

## 우선순위

### 제품/레이아웃 판단

1. `design.game-design`
2. `design.ui-foundations` 또는 관련 scene-facing SSOT
3. `design.ui-reference-workflow`
4. `.stitch/DESIGN.md`
5. relevant `.stitch/contracts/screens/*.json`
6. relevant `.stitch/contracts/blueprints/*.json`

### runtime truth

1. `CodexLobbyScene.unity`, `GameScene.unity`, 관련 prefab
2. scene contract / validator / smoke artifact
3. `.stitch/contracts/*.json`
4. historical `.stitch/designs/*`, `.stitch/handoff/*.md`

## 기본 작업 루프

### 1. Route를 먼저 고른다

- 방향 탐색만 필요하면 Stitch lane에 머문다.
- runtime 반영까지 필요하면 Stitch lane에서 handoff를 정리한 뒤 Unity lane으로 넘긴다.
- Unity lane에 들어가면 owner doc `ops.unity-ui-authoring-workflow`를 기준으로 바꾼다. current path는 `docs/index.md`에서 확인한다.

### 2. 기존 `.stitch` 자산을 먼저 읽는다

새로 생성하기 전에 아래를 확인한다.

- `.stitch/DESIGN.md`
- 해당 screen의 `.stitch/prompt-briefs/*.md`
- 기존 `.stitch/contracts/screens/*.json`
- 필요한 경우 해당 `.stitch/contracts/blueprints/*.json`
- legacy/full fallback `.stitch/contracts/*.json`

이미 있는 자산이 목적에 맞으면 **덮어쓰기보다 갱신**을 우선한다.

### 3. Prompt brief를 수정한다

새 버전을 별도 파일로 증식시키지 말고, 해당 screen의 기존 brief를 먼저 갱신한다.

brief에는 최소한 아래가 들어가야 한다.

- surface 목적
- first read / second read
- primary CTA / secondary CTA
- empty/loading/error state posture
- Unity에서 반드시 살아남아야 할 block

### 4. Stitch 산출물을 저장한다

새 활성 경로에서는 html/png export를 저장하지 않는다.
로컬 시각 탐색이 있었더라도 최종 repo handoff는 `.stitch/contracts/blueprints/*.json` 과 `.stitch/contracts/screens/*.json` 조합, 또는 제한적 full contract `.stitch/contracts/*.json` 으로만 남긴다.

### 5. Structured contract로 번역한다

채택된 Stitch 결과는 반드시 구조화된 JSON contract로 번역한다.
기본값은 `.stitch/contracts/blueprints/*.json` + `.stitch/contracts/screens/*.json` 조합이다.
one-off surface이거나 migration 중간 단계일 때만 full contract `.stitch/contracts/*.json`을 사용한다.
작성과 검토는 `ops.stitch-structured-handoff-contract`와
`ops.stitch-handoff-completeness-checklist`를 함께 본다.

contract에는 최소한 아래가 있어야 한다.

- target scene or prefab root
- block order
- CTA hierarchy
- root / repeat / independent / shared 역할
- required child path
- serialized contract owner
- validation focus

blueprint + manifest를 쓸 때 역할 분리는 아래와 같다.

- blueprint: 공통 block skeleton, 기본 state, 기본 validation
- manifest: concrete target, CTA, screen-specific state/validation, 필요한 delta

### 6. Unity 구현으로 넘긴다

Unity 반영 단계부터는 `jg-unity-workflow`와 `unity_ui_authoring_workflow`를 따른다.

즉:
- Stitch 결과를 그대로 복제하지 않는다
- scene hierarchy와 prefab contract에 맞게 다시 구성한다
- smoke와 gate는 Unity 쪽에서 증명한다

## Reset / Reimport Route

runtime scene 또는 UI prefab을 의도적으로 폐기했거나, 기존 결과물을 버리고 다시 가져와야 하는 경우에는
이전의 `scene repair` 흐름을 기본값으로 쓰지 않는다.

이 경우의 기준은 아래와 같다.

1. `accepted Stitch structured contract`를 유지한다.
   - handoff의 활성 형식은 JSON contract만 허용한다.
2. 기존 Unity smoke 캡처와 scene polish 이력은 참고 자료로만 본다.
3. Unity 반영은 `scene-first`가 아니라 `prefab-first`로 다시 시작한다.
4. prefab baseline이 서기 전에는 scene gate를 acceptance proof로 쓰지 않는다.

권장 순서:

1. set handoff에서 block order, CTA 우선순위, Unity translation target을 다시 확정한다.
2. presentation script가 요구하는 required reference와 view ownership을 읽는다.
3. surface별 baseline prefab을 다시 세운다.
4. prefab 단위에서 serialized ref와 hierarchy를 먼저 안정화한다.
5. 마지막에 새 scene에 조립하고 scene contract / smoke를 다시 붙인다.

현재 JG에서 우선 고려할 surface baseline 예시는 아래다.

- Lobby shell: `LobbyPageRoot`
- Garage shell: `GaragePageRoot`
- Overlay set: `RoomDetailPanel`, loading/error/confirm surfaces
- Battle HUD root
- Result overlay root

한 줄 기준:

`scene를 고치는 대신, handoff를 prefab baseline으로 먼저 번역하고 scene은 마지막 조립 단계에서만 다시 만든다.`

## 금지 규칙

- `.stitch/*`를 runtime SSOT처럼 취급하지 않는다.
- png/html export를 활성 handoff 입력처럼 취급하지 않는다.
- 동일 screen에 대한 prompt brief를 새 파일로 계속 복제하지 않는다.
- screen manifest 또는 full contract 없이 바로 Unity에 반영하지 않는다.
- baseline과 supporting state를 파일명만으로 추측하게 두지 않는다.
- handoff에서 absolute-position clone이나 presentation layout repair를 구현 기본값처럼 지시하지 않는다.
- 새 `.stitch/designs/*.{png,html}` 파일을 handoff 경로에 추가하지 않는다.
- 새 `.stitch/handoff/*.md` 파일을 활성 handoff 경로로 추가하지 않는다.
- JG 밖의 generic Stitch helper skill 이름을 repo workflow 기준처럼 남겨두지 않는다.
- runtime scene이 이미 폐기된 상태에서 과거 smoke artifact를 현재 acceptance proof처럼 재사용하지 않는다.

## Repo 기준 Skill Route

- `jg-stitch-workflow`
  - JG의 Stitch 작업 단일 진입점
  - prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, design export organization, handoff 정리, Unity lane handoff까지 맡는다.
- `jg-unity-workflow`
  - Unity 반영과 검증

## 작업 종료 체크

- `.stitch` 산출물이 최신 brief와 일치하는가
- screen manifest와 필요한 blueprint가 최신 Stitch 판단을 반영하는가
- active structured contract가 `ops.stitch-handoff-completeness-checklist` 기준으로 baseline, CTA, Unity target, validation focus를 모두 갖췄는가
- 관련 plan 또는 design SSOT에 남겨야 할 장기 판단이 있으면 repo 문서로 승격했는가
- Unity 반영까지 했다면 Unity evidence가 Stitch 결과보다 최신인가
