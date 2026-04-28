# LobbyScene Nova1492 모델 활용 계획

> 마지막 업데이트: 2026-04-28
> 상태: reference
> doc_id: plans.lobby-scene-nova1492-model-application
> role: plan
> owner_scope: `LobbyScene`에서 변환된 Nova1492 GX 모델을 Garage preview와 로비 장식 후보로 제한 적용하는 실행 순서와 검증 기준
> upstream: plans.progress, plans.nova1492-resource-integration, ops.unity-ui-authoring-workflow, design.game-design, design.unit-module-design
> artifacts: `Assets/Art/Nova1492/GXConverted/`, `artifacts/nova1492/gx_asset_classification.csv`, `Assets/Scenes/LobbyScene.unity`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 변환된 Nova1492 GX 모델을 `LobbyScene`에 어떻게 시험 적용할지 정한다.
리소스 인벤토리와 변환 이력은 [`nova1492_resource_integration_plan.md`](./nova1492_resource_integration_plan.md)와 `artifacts/nova1492/`가 맡고, 이 문서는 `LobbyScene` 적용 순서와 acceptance만 맡는다.

## Lifecycle

- reference 전환 이유: Garage preview mapping, scene template cleanup, required validation, Play Mode Garage smoke, `390x844` capture는 closeout evidence가 남았다. Phase 4 로비 장식 후보는 후속 decorative variant 판단 residual로만 남긴다.
- 남은 residual owner: Lobby 장식 후보와 BattleScene 전투 유닛 모델 교체는 각각 후속 visual/model pass에서 본다. Garage preview mapping 유지 판단은 `plans.progress`와 Garage/UI owner lane에서만 추적한다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

## 목적

- 현재 Garage preview의 primitive placeholder를 Nova1492식 조립 유닛 실루엣으로 바꿀 수 있는지 검증한다.
- Lobby/Garage 첫 화면의 “조립 작업대” 감각을 강화하되, 현재 LobbyScene runtime wiring과 Set B Garage visual fidelity 판단을 흔들지 않는다.
- 변환 모델 전체를 무차별 적용하지 않고, 카테고리별 대표 후보만 검증해 후속 asset mapping 기준을 만든다.

## 현재 관찰

- `LobbyScene`은 현재 열려 있으며 Unity MCP `/health` 기준 `activeScenePath = Assets/Scenes/LobbyScene.unity`, `isCompiling = false`다.
- 씬 루트에는 `/LobbyRuntime`, `/LobbyCanvas`, `/EventSystem`, `/LobbyPreviewCamera`가 있다.
- `/LobbyCanvas`는 `LobbyPageRoot`, `GaragePageRoot`, `Overlays`, `LobbyGarageNavBar`를 가진다.
- `GaragePageRoot`는 scene instance 기준으로 `GaragePageController`를 갖고, `GarageUnitPreviewView`는 `Camera + RawImage + RenderTexture` preview 구조를 이미 갖고 있다.
- `GarageUnitPreviewView`는 `_framePrefab`, `_weaponPrefab`, `_thrusterPrefab`을 참조하지만 현재 값은 scene root의 inactive primitive template이다.
- 씬 root의 preview fallback primitive template은 `PreviewFrameTemplate`, `PreviewWeaponTemplate`, `PreviewThrusterTemplate` 이름으로 정리됐고, 각 root는 하나씩만 남아 있다.
- 변환 결과는 `GX 871개 중 865개 OBJ 변환`, 카테고리 분류 `organized=865`, MTL texture link `missing_texture_refs=0` 상태다.

## 모델 후보 현황

2026-04-25 실행 시작 후 Phase 0 shortlist를 `artifacts/nova1492/lobby_model_shortlist.csv`와 `artifacts/nova1492/lobby_model_shortlist.md`로 고정했다.
선정은 `tools/nova1492/SelectLobbyModelShortlist.ps1`로 재생성 가능하며, slot별 5개씩 총 20개 후보를 둔다.

현재 분류 기준:

| category | count | LobbyScene 우선 용도 |
|---|---:|---|
| `UnitParts/ArmWeapons` | 160 | Garage 상단/무기 모듈 preview |
| `UnitParts/Bodies` | 72 | Garage 중단/프레임 또는 torso preview |
| `UnitParts/Legs` | 61 | Garage 하단/기동 모듈 preview |
| `UnitParts/Bases` | 28 | 로스터 카드/작업대 base silhouette |
| `UnitParts/Accessories` | 62 | accessory, upgrade, stat flavor 후보 |
| `Characters/MobAndBoss` | 128 | 로비/전투 후보 비교용, LobbyScene 직접 적용은 보류 |
| `Effects/CombatEffects` | 116 | hover/selection feedback 후보, 과사용 금지 |
| `Effects/Projectiles` | 17 | firepower 모듈 thumbnail/preview 후보 |
| `Environment/Props` | 65 | 로비 배경 장식 후보 |
| `ItemsAndUi/Icons` | 36 | UI icon 후보, 3D preview 우선순위 낮음 |
| `Unknown/Review` | 120 | 자동 적용 금지, 수동 review 후 이동 |

대표 후보는 먼저 아래 축에서 고른다.

| slot | 우선 폴더 | 선정 기준 |
|---|---|---|
| frame/body | `UnitParts/Bodies`, `UnitParts/Bases` | 정점 수 50~400, 중앙 실루엣이 읽히는 모델 |
| firepower | `UnitParts/ArmWeapons`, `Effects/Projectiles` | 위쪽 장착 시 방향성이 보이는 모델 |
| mobility | `UnitParts/Legs` | 아래쪽 장착 시 발/트랙/부스터 느낌이 나는 모델 |
| ambient prop | `Environment/Props` | UI 뒤에 둬도 정보 위계를 방해하지 않는 모델 |

## 적용 원칙

- 첫 적용 surface는 `GarageUnitPreviewView`로 제한한다.
- `LobbyPageRoot`와 `GaragePageRoot`의 UI layout은 첫 pass에서 변경하지 않는다.
- scene root primitive template 중복을 먼저 정리하고, preview asset route는 별도 pass에서 판단한다.
- 변환 모델을 domain data의 `unitPrefab` truth로 바로 승격하지 않는다. 처음에는 presentation preview 후보로만 쓴다.
- `Unknown/Review` 모델은 자동 후보에서 제외한다.
- 모델 수가 많으므로 Resources 전체 로드나 runtime name scan을 쓰지 않는다.
- 모델 매핑은 serialized config 또는 explicit table로 시작하고, hidden runtime lookup을 도입하지 않는다.

## 실행 순서

### Phase 0: 후보 shortlist 고정

상태: 완료

목표:

- `gx_asset_classification.csv`에서 slot별 3~5개 후보만 고른다.
- 각 후보의 원본 `source_relative_path`, OBJ path, texture 유무, vertex/triangle count를 표로 남긴다.

산출물:

- `artifacts/nova1492/lobby_model_shortlist.csv`
- `artifacts/nova1492/lobby_model_shortlist.md`
- 필요 시 후보 contact sheet 또는 SceneView capture

Acceptance:

- frame/firepower/mobility 후보가 각각 최소 3개 있다.
- 후보는 `Unknown/Review`를 포함하지 않는다.
- triangle count가 모바일 preview에 과한 모델은 첫 pass에서 제외된다.

결과:

- `frame_body`, `firepower`, `mobility`, `ambient_prop` slot에 각각 5개 후보를 선정했다.
- triangle range는 `frame_body 156~286`, `firepower 32~256`, `mobility 179~313`, `ambient_prop 49~117`이다.
- `Unknown/Review`는 포함하지 않았다.

### Phase 1: preview asset pack reference

상태: reference

목표:

- 이후 Nova preview는 scene-owned template, direct model reference, 또는 UI Toolkit candidate route에서 다시 설계한다.

주의:

- 새 prefab 생성은 workflow policy상 민감하므로, 구현 전 policy 판단을 먼저 확인한다. 2026-04-25 Phase 0 직후 `Invoke-UnityUiAuthoringWorkflowPolicy.ps1 -TimeoutSec 60`는 통과했다.
- prefab 생성이 blocked되면 scene root template 교체 대신 `blocked`로 남기고, 임시 scene-only 적용으로 성공 처리하지 않는다.

Acceptance:

- 후보 prefab은 frame/firepower/mobility slot별로 구분된다.
- prefab root scale과 pivot이 preview camera에서 비교 가능하다.
- material missing 또는 texture missing이 없다.

Historical result:

- `Tools/Nova1492/Create Garage Preview Model Prefabs` editor tool로 `frame_body`, `firepower`, `mobility` 각 5개씩 총 15개 preview prefab을 생성했었다.
- 생성 결과는 `artifacts/nova1492/lobby_preview_prefab_pack_report.md`에 남겼고, report 기준 `created=15`, `failed=0`, `missing materials=0`이다.
- `tools/check-compile-errors.ps1` 기준 `Assembly-CSharp-Editor.csproj` 빌드는 errors 0 / warnings 0이다.
- Historical note: 당시 `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`는 active plan과 prefab pack report에 선언된 정확한 15개 target만 허용했고, compile/reload도 통과했다.

### Phase 2: `GarageUnitPreviewView` model mapping 도입

상태: 완료

목표:

- 현재 primitive `_framePrefab`, `_weaponPrefab`, `_thrusterPrefab` 경로를 slot별 model mapping으로 확장한다.
- 최소 구현은 serialized 후보 배열 또는 작은 ScriptableObject catalog로 시작한다.
- `GarageSlotViewModel.FrameId`, `FirepowerId`, `MobilityId`와 preview prefab을 명시적으로 매핑한다.

제외:

- `UnitFrameData.unitPrefab`을 전투 prefab truth로 바꾸는 작업
- module domain balance 변경
- runtime filesystem/AssetDatabase lookup

Acceptance:

- 저장 유닛 preview가 선택 조합에 따라 frame/firepower/mobility 모델을 조합해 보여준다.
- mapping이 비어 있거나 후보가 없으면 현재 primitive fallback을 명시적으로 사용한다.
- console error 없이 Lobby/Garage tab 전환이 유지된다.

결과:

- `GarageUnitPreviewView`에 serialized `frame/firepower/mobility` ID -> prefab mapping 배열을 추가했다. 매핑이 없으면 기존 primitive `_framePrefab`, `_weaponPrefab`, `_thrusterPrefab` fallback을 유지한다.
- 과거 `Tools/Nova1492/Wire Garage Preview Model Prefabs` editor tool로 현재 `LobbyScene`의 `GarageUnitPreviewView`에 9개 runtime ID 매핑을 주입했다.
- 매핑 결과는 `artifacts/nova1492/lobby_preview_mapping_report.md`에 남겼다.
- `LobbyPreviewCamera`에 `AudioListener`를 추가했다.
- Play Mode에서 Lobby -> Garage tab invoke smoke는 console errors 0을 유지했다.
- preview root를 `LobbyPreviewCamera` 공간에 배치하도록 `GaragePreviewAssembler`로 분리했고, preview 전용 key light와 RawImage texture tint 보정을 추가해 모델이 GameView capture에서 읽히도록 정리했다.
- Play Mode에서 `/LobbyRuntime/LobbyView.OpenGaragePage` 호출 후 `GaragePageRoot`, `PreviewCard`, `MobileFirepowerTabButton` active/interactable 상태를 확인했다. `MobileFirepowerTabButton/Label`은 `무장`이며, GameView capture `artifacts/unity/lobby-scene-garage-nova-preview-phase2.png`에서 Nova1492 조립 preview가 보인다. console errors는 0건이다.

남은 일:

- Phase 2 범위에서는 없음. 다음은 Phase 3 scene template 중복 정리다.

### Phase 3: scene template 중복 정리

상태: 완료

목표:

- `PreviewFramePrefab`, `PreviewWeaponPrefab`, `PreviewThrusterPrefab` 중복 root를 audit한다.
- 실제 `GarageUnitPreviewView`가 참조하는 template만 남기거나, prefab pack reference로 대체한다.
- 남기는 scene template은 `PreviewFrameTemplate`, `PreviewWeaponTemplate`, `PreviewThrusterTemplate`처럼 scene template임을 드러내는 이름을 쓴다.

Acceptance:

- scene root에 같은 이름의 inactive preview template이 중복으로 남지 않는다.
- `GarageUnitPreviewView` required refs가 비지 않는다.
- required-field validation이 통과한다.

결과:

- Unity MCP로 `LobbyScene` root의 duplicate preview template을 정리해 `PreviewFrameTemplate`, `PreviewWeaponTemplate`, `PreviewThrusterTemplate`이 각각 1개만 남도록 했다.
- `GarageUnitPreviewView`의 fallback refs `_framePrefab`, `_weaponPrefab`, `_thrusterPrefab`은 남은 scene template root로 다시 연결했다.
- MCP `/scene/save`로 `Assets/Scenes/LobbyScene.unity`를 저장했다.
- Play Mode에서 `/LobbyRuntime/LobbyView.OpenGaragePage`를 호출해 Garage tab 진입을 확인했고, `GaragePageRoot`, `PreviewCard`, `GarageUnitPreviewView` active 상태를 확인했다.
- GameView capture `artifacts/unity/lobby-scene-garage-template-cleanup-smoke.png`는 `390x844` framing이며, Garage tab과 Nova1492 조립 preview가 표시된다.
- compile check는 errors 0 / warnings 0, console errors는 0건이다.

### Phase 4: 로비 장식 후보는 별도 variant로 검토

목표:

- `Environment/Props`와 `Characters/MobAndBoss`는 첫 pass에서 runtime UI에 직접 넣지 않는다.
- 필요하면 `LobbyModelDisplayVariant` 같은 별도 inactive parent 아래에서 SceneView 비교만 한다.
- 로비 첫 화면 정보 위계와 nav/tab affordance를 방해하지 않는 후보만 후속 적용으로 넘긴다.

Acceptance:

- Lobby tab 첫 화면의 room list와 bottom nav가 모델 장식에 가리지 않는다.
- 장식 후보는 decorative layer로 분리되고 input raycast/UI hierarchy를 방해하지 않는다.
- 적용 여부는 capture 비교 후 결정한다.

## 검증

구현 pass가 scene/prefab을 변경하면 아래 순서로 확인한다.

1. Unity compile/reload 안정화
2. `Tools/Validate Required Fields`
3. `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
4. Play Mode Lobby tab smoke
5. Garage tab invoke smoke
6. GameView 또는 SceneView capture
7. `npm run --silent rules:lint`

문서만 수정한 pass에서는 `npm run --silent rules:lint`를 기본 검증으로 둔다.

## Acceptance

- `GarageUnitPreviewView`가 primitive-only preview에서 Nova1492 model-backed preview 후보로 넘어갈 수 있는 실행 경로가 명확하다.
- 모델 적용은 scene/prefab serialized reference 또는 explicit mapping으로 닫히고, hidden runtime lookup을 만들지 않는다.
- LobbyScene 현재 acceptance인 첫 화면, overlay inactive state, Lobby/Garage tab 전환, console error 0을 유지한다.
- Set B Garage final fidelity 판단과 충돌하지 않도록, layout 변경과 model preview 변경을 분리한다.
- 변환 모델의 권리/배포 여부는 release gate 전까지 prototype/internal use로 제한된다.

## Blocked / Residual 처리

- 새 prefab 생성이 workflow policy에서 막히면 `blocked`로 남기고, scene-only 임시 모델을 성공으로 포장하지 않는다.
- OBJ import scale/pivot이 후보별로 크게 다르면 Phase 1에서 normalization prefab을 만들고, `GarageUnitPreviewView` 코드에서 보정값을 하드코딩하지 않는다.
- 일부 모델의 material이 Unity에서 기대대로 보이지 않으면 converter/material import issue로 분리하고 LobbyScene blocker로 보지 않는다.
- `Unknown/Review` 120개는 수동 명명/분류가 끝나기 전 자동 적용하지 않는다.

## 문서 재리뷰

- 과한점 리뷰: 이 문서는 `LobbyScene` 적용 순서만 다루고, GX 변환기나 전체 리소스 인벤토리 owner를 다시 정의하지 않는다.
- 부족한점 리뷰: 현재 씬 관찰, 후보 현황, 제외 범위, phase, acceptance, 검증, blocked/residual을 포함했다.
- 수정 후 재리뷰: 새 prefab 생성 리스크를 blocked 처리로 분리했고, Set B Garage visual fidelity 판단을 이 계획 acceptance로 섞지 않았다.
- 2026-04-25 실행 시작 후 재리뷰: Phase 0 shortlist 산출물을 계획에 연결했다. 과한점 없음. 부족한점으로 남았던 preview prefab pack 생성 전 workflow policy 재확인은 통과했고, 실제 prefab 생성/SceneView capture는 Phase 1 범위로 남겼다.
- 2026-04-25 Phase 1 생성 후 재리뷰: preview prefab pack은 생성됐지만 policy가 새 prefab 생성 자체를 blocked로 판정하므로 scene wiring으로 확장하지 않는다. 과한점은 scene root template 교체나 runtime mapping까지 밀고 가지 않은 점에서 정리됐고, 부족한점은 policy blocked 해소 또는 명시 승인 전까지 Phase 1 acceptance를 닫을 수 없다는 점이다.
- 2026-04-26 Phase 1 policy 정리 후 재리뷰: policy는 `lobby_preview_prefab_pack_report.md`에 기록된 정확한 Garage preview prefab 15개만 active plan 근거로 허용한다. presentation rotation write는 `GaragePreviewAssembler` runtime helper로 옮겨 ownership validator를 통과했다. 과한점은 wildcard 허용이나 scene wiring까지 확장하지 않은 점에서 정리됐고, 부족한점은 Phase 2 scene/reference mapping과 runtime smoke가 아직 남아 있다는 점이다.
- 2026-04-26 Phase 2 매핑 후 재리뷰: mapping은 serialized reference로 닫았고 hidden runtime lookup을 만들지 않았다. AudioListener 누락은 scene/build tool 양쪽에서 보정했다. 과한점은 domain data나 battle prefab truth까지 건드리지 않은 점에서 정리됐고, 부족한점은 visual capture/framing acceptance가 아직 남아 있다는 점이다.
- 2026-04-26 Phase 2 visual closeout 재리뷰: preview root 배치, lighting, RawImage tint 보정은 `GarageUnitPreviewView`의 표시 책임과 `GaragePreviewAssembler`의 runtime 배치 책임으로 분리했고, scene/domain truth나 Set B fidelity 기준을 확장하지 않았다. active-state/capture evidence가 생겼으므로 Phase 2 부족점은 해소됐다.
- 2026-04-26 Phase 3 template cleanup 재리뷰: scene template 정리는 fallback root 중복 제거와 serialized reference 유지에만 제한했고, Garage layout이나 Set B visual fidelity 판단으로 확장하지 않았다. Required field validation, Play Mode Garage smoke, `390x844` capture, compile check가 남아 있어 acceptance evidence는 충분하다.
- owner impact: primary `plans.lobby-scene-nova1492-model-application`; secondary `plans.progress`, `plans.nova1492-resource-integration`, `docs.index`; out-of-scope scene/prefab mutation, converter rewrite, GameScene runtime model application.
- 2026-04-28 lifecycle cleanup 재리뷰: 과한점은 로비 장식 후보 판단을 새 active plan으로 확장하지 않고 residual owner로 이관했다. 부족한점은 Garage preview mapping closeout evidence와 남은 후속 범위를 Lifecycle에 남겨 해소했다.
- doc lifecycle checked: active 실행 계획에서 reference 기록으로 전환한다. 기존 Nova resource integration plan은 reference로 유지하고, LobbyScene UI prefab management plan은 대체하지 않는다.
- plan rereview: clean
