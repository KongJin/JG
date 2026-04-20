# Stitch Data Workflow

> 마지막 업데이트: 2026-04-20
> 상태: active
> doc_id: ops.stitch-data-workflow
> role: ssot
> owner_scope: Stitch working data ownership, prompt brief lifecycle, design export storage, Unity handoff 운영
> upstream: design.ui-reference-workflow, plans.stitch-ui-ux-overhaul, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/DESIGN.md`, `.stitch/prompt-briefs/`, `.stitch/designs/`, `.stitch/handoff/`

이 문서는 JG에서 `Stitch` 산출물을 어떻게 읽고, 어디에 저장하고, 언제 Unity handoff로 넘길지 정하는 단일 운영 기준이다.
시각 탐색 원칙은 `design.ui-reference-workflow`, 실행 상태와 세트별 우선순위는 `plans.stitch-ui-ux-overhaul`가 소유한다.
여기서는 그 둘 사이의 **데이터 흐름과 파일 소유권**만 고정한다.

## 목적

- `Stitch prompt -> generated screen -> handoff -> Unity 구현` 루프의 파일 경계를 고정한다.
- 같은 역할의 문서와 skill이 여러 군데에서 따로 놀지 않게 한다.
- `Stitch` 산출물을 참고 자료와 working data로만 쓰고, runtime SSOT와 혼동하지 않게 한다.

## 역할 구분

### Stitch가 맡는 것

- visual language 탐색
- screen-level prompt brief 정리
- html/png export 보관
- Unity 번역용 handoff 작성

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
- `.stitch/designs/*.{html,png}`
  - Stitch raw export
  - 결과 증거이며 handoff의 근거 자료다.
- `.stitch/handoff/*.md`
  - Unity 번역 문서
  - scene root, block mapping, CTA 유지 규칙, implementation notes를 적는다.

## 우선순위

### 제품/레이아웃 판단

1. `design.game-design`
2. `design.ui-foundations` 또는 관련 scene-facing SSOT
3. `design.ui-reference-workflow`
4. `.stitch/DESIGN.md`
5. `.stitch/handoff/*.md`
6. `.stitch/designs/*`

### runtime truth

1. `CodexLobbyScene.unity`, `GameScene.unity`, 관련 prefab
2. scene contract / validator / smoke artifact
3. `.stitch/*`는 참고 자료

## 기본 작업 루프

### 1. Route를 먼저 고른다

- 방향 탐색만 필요하면 Stitch lane에 머문다.
- runtime 반영까지 필요하면 Stitch lane에서 handoff를 정리한 뒤 Unity lane으로 넘긴다.
- Unity lane에 들어가면 owner doc `ops.unity-ui-authoring-workflow`를 기준으로 바꾼다. current path는 `docs/index.md`에서 확인한다.

### 2. 기존 `.stitch` 자산을 먼저 읽는다

새로 생성하기 전에 아래를 확인한다.

- `.stitch/DESIGN.md`
- 해당 screen의 `.stitch/prompt-briefs/*.md`
- 기존 `.stitch/designs/*.html`, `.png`
- 관련 `.stitch/handoff/*.md`

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

screen이 채택되면 html/png를 `.stitch/designs/`에 저장한다.

파일명 원칙:
- set 기준 + screen 목적이 바로 읽혀야 한다
- `v2`, `final-final`, 날짜 꼬리표 같은 임시 이름 금지

### 5. Handoff로 번역한다

채택된 Stitch 결과는 반드시 `.stitch/handoff/*.md`로 요약한다.

handoff에는 최소한 아래가 있어야 한다.

- target scene or prefab root
- block order
- CTA hierarchy
- 유지할 density / spacing 인상
- 버릴 장식 요소
- Unity에서 scene-owned layout으로 바꿀 때 주의할 serialized contract

### 6. Unity 구현으로 넘긴다

Unity 반영 단계부터는 `jg-unity-workflow`와 `unity_ui_authoring_workflow`를 따른다.

즉:
- Stitch 결과를 그대로 복제하지 않는다
- scene hierarchy와 prefab contract에 맞게 다시 구성한다
- smoke와 gate는 Unity 쪽에서 증명한다

## 금지 규칙

- `.stitch/*`를 runtime SSOT처럼 취급하지 않는다.
- Stitch screenshot만 보고 layout 결정을 확정하지 않는다.
- 동일 screen에 대한 prompt brief를 새 파일로 계속 복제하지 않는다.
- handoff 없이 바로 Unity에 반영하지 않는다.
- JG 밖의 generic Stitch helper skill 이름을 repo workflow 기준처럼 남겨두지 않는다.

## Repo 기준 Skill Route

- `jg-stitch-workflow`
  - JG의 Stitch 작업 단일 진입점
  - prompt-brief refinement, `.stitch/DESIGN.md` upkeep guidance, design export organization, handoff 정리, Unity lane handoff까지 맡는다.
- `jg-unity-workflow`
  - Unity 반영과 검증

## 작업 종료 체크

- `.stitch` 산출물이 최신 brief와 일치하는가
- handoff가 최신 design export를 반영하는가
- 관련 plan 또는 design SSOT에 남겨야 할 장기 판단이 있으면 repo 문서로 승격했는가
- Unity 반영까지 했다면 Unity evidence가 Stitch 결과보다 최신인가
