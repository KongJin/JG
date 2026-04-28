# LobbyScene UI/Prefab 관리 정리 계획

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: plans.lobby-scene-ui-prefab-management
> role: plan
> owner_scope: `LobbyScene` UI prefab instance 관리, scene override drift 점검, assembly helper 안전화, Garage preview placeholder 정리
> upstream: plans.progress, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Scenes/LobbyScene.unity`, `Assets/UI/`, `artifacts/unity/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 `LobbyScene`이 현재 동작하는 상태를 유지하면서 UI/prefab 관리 부채를 줄이던 reference 계획이다.
현재 새 UI 작업은 UI Toolkit candidate surface route가 맡는다.

## 현재 관찰

- `LobbyScene`은 scene serialized reference가 런타임 계약을 소유한다.
- Lobby scene rebuild fallback은 active route가 아니며, 현재 runtime truth는 scene serialized reference다.
- `LobbyScene.unity`에는 prefab root의 active state, anchors, size, position, name override가 다수 존재한다.
- Garage preview의 `PreviewFrameTemplate`, `PreviewWeaponTemplate`, `PreviewThrusterTemplate`은 asset prefab이 아니라 scene root의 inactive primitive template이다.
- `GaragePageController`는 현재 lint를 통과하지만, mobile tab state, settings overlay, save state, chrome orchestration이 한 클래스에 모여 있어 다음 UI 변경 때 책임이 커질 수 있다.

## 진행 메모

- 2026-04-25: 당시 destructive rebuild helper 메뉴와 실행 로그를 명확히 하고, 실행 전 확인 다이얼로그를 추가했다.
- 2026-04-25: `artifacts/unity/lobby-scene-prefab-override-audit.md`에 read-only prefab override audit 초안을 남겼다. 현재 visual/text/color override는 보이지 않고, `LobbyPageRoot`와 `GaragePageRoot`의 추가 active override만 review candidate로 분리했다.
- 2026-04-25: Garage preview scene template 이름을 `*Prefab`에서 `*Template`으로 정리했고, rebuild helper도 같은 이름을 생성하도록 맞췄다.
- 2026-04-27: 남은 Set B visual 판단은 Garage UI/UITK lane에서 본다.

## Lifecycle

- reference 전환 이유: 이 문서는 현재 실행 owner가 아니라 배경 기록이다.
- 남은 visual fidelity와 runtime replacement 판단은 Set B Garage / Non-Stitch UI migration lane에서 추적한다.

## 목표

- scene-owned 수정은 serialized scene contract 기준으로 유지한다.
- `LobbyScene` prefab instance override를 허용 override와 의심 override로 분류할 수 있게 한다.
- preview placeholder가 asset prefab인지 scene template인지 헷갈리지 않게 정리한다.
- `GaragePageController`는 당장 큰 분해를 하지 않고, 다음 변경 때 분리할 책임 경계를 먼저 정한다.
- 기존 `scene/prefab authoring` route와 workflow policy를 유지하고 새 hard-fail 기준은 만들지 않는다.

## 제외 범위

- 새 Stitch source 생성
- `LobbyScene` 런타임 구조 재조립
- `LobbyPageRoot`, `GaragePageRoot` 전체 재생성
- Garage 저장/로드, Firebase, WebGL 실기 동작 재설계
- `GaragePageController` 전면 리팩터링
- UI authoring workflow의 새 정책 또는 새 validation gate 추가

## 판단 기준

| 변경 유형 | 기본 owner | 처리 기준 |
|---|---|---|
| 페이지/오버레이의 source-derived visual 구조 | prefab asset 또는 Stitch translation lane | scene override로 장기 보관하지 않는다 |
| scene 배치용 root anchor, active default, parent binding | `LobbyScene.unity` | 허용 override로 본다 |
| 런타임 필수 reference wiring | `LobbyScene.unity` | scene contract로 유지하고 hidden runtime lookup으로 복구하지 않는다 |
| 반복 사용 가능한 Garage preview model/template | prefab asset 후보 | scene root template으로 남길 경우 이름과 주석을 명확히 한다 |
| page state, tab state, save/settings orchestration | presentation code | visual authoring 값은 prefab/scene/token owner로 둔다 |

## 실행 순서

1. **Current route**
   - 이후 Lobby/Garage UI 변경은 UI Toolkit candidate surface 또는 scene-owned MCP repair route로만 연다.

2. **Prefab override audit 초안**
   - `LobbyScene.unity`의 prefab instance override를 surface별로 추출한다.
   - 허용 후보: root name, root active default, root RectTransform placement, scene parent.
   - 검토 후보: prefab 내부 visual child의 active/size/text/color override, runtime binding을 숨기는 override.
   - 결과는 새 필수 artifact gate가 아니라 구현 closeout 표나 선택적 정리 artifact로 남긴다.

3. **Preview placeholder 정리**
   - 현재 scene root inactive primitives가 필요한 이유를 확인한다.
   - 유지한다면 현재처럼 `PreviewFrameTemplate`, `PreviewWeaponTemplate`, `PreviewThrusterTemplate` 이름으로 scene template임이 드러나야 한다.
   - asset화가 더 낫다면 prefab route를 다시 열지 말고 UI Toolkit candidate 또는 scene-owned model reference로 설계한다.
   - 둘 중 하나를 선택한 뒤 Required field audit과 Garage preview smoke로 검증한다.

4. **GaragePageController 책임 경계 표시**
   - 당장 분해하지 않고, 다음 변경 때 분리할 후보를 먼저 확인한다.
   - 우선 후보는 settings overlay, mobile focus/tab state, chrome/save-state orchestration이다.
   - 분리 시 기존 public interaction과 serialized refs를 깨지 않도록 작은 collaborator 단위로 진행한다.

5. **검증과 문서 동기화**
   - compile/reload 안정화 후 required-field validation을 실행한다.
   - `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`로 route와 policy evidence를 확인한다.
   - 변경이 실제 scene/prefab에 닿으면 Lobby/Garage tab smoke와 필요한 capture를 갱신한다.
   - 결과에 따라 이 문서를 reference 또는 historical로 내릴지 판단한다.

## Acceptance

- Lobby/Garage UI 변경 route가 UI Toolkit candidate surface 또는 scene-owned MCP repair로 정리되어 있다.
- `LobbyScene` prefab override 중 허용/검토 후보가 한 번 이상 분류되어 있다.
- Garage preview placeholder가 scene template인지 asset prefab인지 이름과 위치로 구분된다.
- `GaragePageController`의 다음 분리 후보가 문서 또는 코드 구조에서 확인 가능하다.
- compile check, required-field validation, workflow policy check가 통과한다.
- scene 또는 prefab을 변경했다면 Lobby/Garage tab smoke에서 console error가 없다.

## Blocked / Residual 처리

- Unity MCP가 prefab override 목록을 안정적으로 제공하지 못하면, YAML 기반 임시 audit로 분류하고 tooling 개선은 별도 residual로 남긴다.
- preview primitive를 asset prefab화했을 때 reference churn이 과하면, 이번 계획에서는 rename/comment 정리만 하고 asset화는 residual로 남긴다.
- `GaragePageController` 분리가 visual fidelity나 save/load 검증과 섞이면, controller 분리는 별도 implementation lane으로 넘긴다.
- Set B Garage visual fidelity 판단은 이 계획의 acceptance가 아니라 기존 Garage/Stitch lane residual로 유지한다.

## 검증 명령

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- Unity MCP `Tools/Validate Required Fields`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- 필요한 경우 Unity MCP Play Mode Lobby/Garage tab smoke
- `npm run --silent rules:lint`

## 문서 재리뷰

- 과한점 리뷰: 새 authoring 규칙이나 hard-fail을 만들지 않고, 기존 `ops.unity-ui-authoring-workflow` route 안에서 관리 부채 정리만 다룬다.
- 부족한점 리뷰: 현재 관찰, 목표, 제외 범위, 판단 기준, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 수정 후 재리뷰: `LobbyScene` runtime/completion 계획과 역할을 분리했고, Set B visual fidelity를 이 계획 acceptance로 섞지 않았다.
- 반복 재리뷰 반영: obvious 과한점/부족한점 없음.
- owner impact: primary `plans.lobby-scene-ui-prefab-management`; secondary `plans.progress`, `docs.index`; out-of-scope `ops.unity-ui-authoring-workflow`.
- doc lifecycle checked: 새 active plan으로 등록한다. 이 계획은 관리 정리 closeout 뒤 reference 또는 historical 전환 후보로 본다.
- plan rereview: clean
- 2026-04-25 반복리뷰: override audit 결과가 새 required artifact처럼 읽힐 수 있는 표현을 구현 closeout evidence 또는 선택적 정리 artifact로 좁혔다.
- 반복리뷰 후 과한점 리뷰: 새 hard-fail, 새 validation gate, 새 owner 규칙을 추가하지 않았다.
- 반복리뷰 후 부족한점 리뷰: acceptance와 실행 순서는 유지되고, audit evidence 위치의 애매함만 해소했다.
- plan rereview: clean
- 2026-04-25 작업 시작 후 재리뷰: helper 안전화와 read-only audit 초안만 반영했고, scene/prefab runtime state는 변경하지 않았다.
- 작업 시작 후 과한점 리뷰: optional audit artifact를 새 gate로 승격하지 않았고, helper는 bounded fallback 역할만 더 분명히 했다.
- 작업 시작 후 부족한점 리뷰: preview placeholder 정리와 `GaragePageController` 책임 경계 구현은 아직 residual로 남아 있다.
- plan rereview: residual - preview placeholder 정리와 controller 책임 경계 표시는 다음 implementation pass에서 처리한다.
- 2026-04-27 route 리뷰: 이 문서는 현재 route를 다시 정의하지 않고 residual owner 이동만 기록한다.
- 2026-04-27 부족한점 리뷰: reference 전환 이유와 남은 owner가 보인다.
- doc lifecycle checked: active plan에서 reference 기록으로 전환한다.
- plan rereview: clean
