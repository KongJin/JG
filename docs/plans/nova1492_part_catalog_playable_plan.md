# Nova1492 Part Catalog Playable Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.nova1492-part-catalog-playable
> role: plan
> owner_scope: 변환된 Nova1492 UnitParts 모델을 JG 3-slot Garage 부품 카탈로그와 playable 데이터로 대량 승격하는 실행 순서와 검증 기준
> upstream: plans.progress, plans.nova1492-resource-integration, plans.lobby-scene-nova1492-model-application, design.unit-module-design, design.module-data-structure, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/nova1492/gx_asset_classification.csv`, `artifacts/nova1492/nova_part_catalog.csv`, `artifacts/nova1492/nova_part_catalog_summary.md`, `artifacts/nova1492/nova_part_preview_prefab_report.md`, `artifacts/nova1492/nova_part_playable_asset_report.md`, `Assets/Art/Nova1492/GXConverted/`, `Assets/Prefabs/Features/Garage/PreviewModels/Generated/`, `Assets/Data/Garage/NovaGenerated/`, `tools/nova1492/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 Nova1492 변환 모델 중 유닛 부품으로 분류된 모델을 JG의 Garage 부품 체계에 대량 연결하는 계획이다.
기존 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)는 `LobbyScene` preview 후보와 로비 장식 판단만 맡고, 이 문서는 전체 부품 catalog/playable 데이터 승격을 맡는다.
리소스 변환과 분류 결과 자체의 owner는 [`nova1492_resource_integration_plan.md`](./nova1492_resource_integration_plan.md)와 `artifacts/nova1492/`다.

## Lifecycle

- active 유지 이유: Nova1492 `UnitParts` Core 321은 catalog, preview prefab, playable ModuleCatalog까지 승격됐고, Garage Nova Parts panel과 runtime smoke/closeout 판단이 남아 있다.
- reference 전환 조건: generated catalog, preview prefab pack, playable ScriptableObject 생성, Garage Nova Parts panel smoke, validation report가 모두 닫히고 후속 밸런스/이름/권리 판단만 별도 owner로 이관된다.
- 전환 시 갱신: 이 문서 header와 `docs.index`, `plans.progress`의 Nova 후속 판단 줄을 함께 `reference` 기준으로 낮춘다.

## 현재 실행 결과

2026-04-26 실행 결과:

- manifest 생성 완료: `artifacts/nova1492/nova_part_catalog.csv`
- manifest summary 생성 완료: `artifacts/nova1492/nova_part_catalog_summary.md`
- preview prefab pack 생성 완료: Frame 100, Firepower 160, Mobility 61, failed 0
- playable SO 생성 완료: Frame 100, Firepower 160, Mobility 61, failed 0
- visual catalog 생성 완료: `Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset`, entries 321
- `ModuleCatalog.asset` append 완료: 기존 9개 유지, 총 Frame 103, Firepower 163, Mobility 64
- 생성 도구:
  - `tools/nova1492/BuildNovaPartCatalog.ps1`
  - `tools/nova1492/Invoke-Nova1492PartGeneration.ps1`
  - `Assets/Editor/AssetTools/Nova1492PartCatalogTool.cs`
- 검증 완료:
  - `dotnet build .\Assembly-CSharp.csproj -v:minimal`: errors 0, warnings 0
  - `dotnet build .\Assembly-CSharp-Editor.csproj -v:minimal`: errors 0, warnings 0
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`: errors 0, warnings 0
  - `npm run --silent rules:lint`: 통과
  - Garage part asset id scan: existing 9 + generated 321 = 330 ids, duplicate 0
- 정책/closeout residual:
  - `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`: blocked
  - blocked reason: generated model preview prefabs 321개가 generic `new-prefab-blocked` guard에 걸림
  - 판단: 이번 계획이 명시적으로 요구한 generated model preview prefab pack이므로 산출물은 의도 범위다. closeout 전 policy whitelist 또는 non-UI generated prefab 분류 보완이 필요하다.

## 결정

- playable 승격 대상은 `Core 321`로 제한한다.
  - `UnitParts/Bodies` 72개와 `UnitParts/Bases` 28개는 `Frame`으로 매핑한다.
  - `UnitParts/ArmWeapons` 160개는 `Firepower`로 매핑한다.
  - `UnitParts/Legs` 61개는 `Mobility`로 매핑한다.
- `UnitParts/Accessories`, `Effects/Projectiles`, `Unknown/Review`는 이번 playable 승격에서 제외한다. 필요하면 visual catalog metadata에는 남길 수 있지만 `ModuleCatalog.asset`에는 넣지 않는다.
- 표시 이름은 원본 file stem 기반으로 만든다. 깨진 이름, 비ASCII 이름, 충돌 이름은 `needsNameReview=true`로 남기고 자동 별칭으로 의미를 추측하지 않는다.
- Garage에는 `Nova Parts` 개발용 panel/tab을 항상 보이게 둔다. 단, public release gate 전에는 별도 사용권 판단이 필요하다.
- 스탯은 확정 밸런스가 아니라 deterministic generated tier baseline이다. 후속 밸런스 pass 전까지 generated임을 report와 asset naming에서 드러낸다.

## 실행 순서

### Phase 0: catalog manifest 생성

목표:

- `artifacts/nova1492/gx_asset_classification.csv`에서 Core 321만 추출해 `artifacts/nova1492/nova_part_catalog.csv`를 만든다.
- 각 row는 `partId`, `slot`, `category`, `source_relative_path`, `model_path`, `vertices`, `triangles`, `tier`, `displayName`, `needsNameReview`, `playableStatus`를 가진다.
- `partId` 규칙은 `nova_frame_<stem>`, `nova_fire_<stem>`, `nova_mob_<stem>`를 기본으로 한다. 충돌 시 짧은 hash suffix를 붙인다.

Acceptance:

- manifest row count가 321이다.
- `partId`가 unique다.
- Core 321 밖의 category가 playable row로 들어오지 않는다.
- 모든 `model_path`가 존재한다.

2026-04-26 결과: 완료. Core catalog rows 321, duplicate id 0, missing model path 0.

### Phase 1: generated tier/stat baseline

목표:

- slot별 triangle count quantile로 tier `1..5`를 산출한다.
- generated stat은 playable smoke와 조합 검증용 baseline이며, 수동 밸런스 완료를 뜻하지 않는다.

기본 공식:

- Frame: `baseHp = 420 + tier * 45`, `baseAttackSpeed = 1.25 - tier * 0.05`, `baseMoveRange = 4`, `passiveTrait = null`
- Firepower: `attackDamage = 16 + tier * 8`, `attackSpeed = 1.45 - tier * 0.13`, `range = 4.0 + tier * 0.75`
- Mobility: `hpBonus = 80 + tier * 50`, `moveRange = 6.2 - tier * 0.55`, `anchorRange = moveRange`

Acceptance:

- 모든 generated numeric field가 양수다.
- 모든 generated Firepower x Mobility 조합이 `UnitComposition.Validate`를 통과한다.
- generated stat report가 생성되어 후속 밸런스 대상임을 명시한다.

2026-04-26 결과: baseline 생성 완료. 단, 전체 Firepower x Mobility 조합 검증은 Phase 5 closeout에서 runtime/static validation으로 재확인한다.

### Phase 2: preview prefab pack 생성

목표:

- Core 321 모델을 normalized preview prefab으로 만든다.
- 출력 경로는 `Assets/Prefabs/Features/Garage/PreviewModels/Generated/Frames`, `Firepower`, `Mobility`로 나눈다.
- 기존 15개 shortlist prefab은 보존하고, generated pack은 별도 폴더에 둔다.

Acceptance:

- preview prefab created count가 Frame 100, Firepower 160, Mobility 61이다.
- missing material count가 0이거나, 모델별 residual로 분리된다.
- prefab root scale/pivot normalization이 기존 `GarageUnitPreviewView` camera에서 비교 가능하다.

2026-04-26 결과: 완료. `nova_part_preview_prefab_report.md` 기준 Frame 100, Firepower 160, Mobility 61, failed 0, missing material 0.

### Phase 3: playable ScriptableObject 생성

목표:

- `Assets/Data/Garage/NovaGenerated/Frames`, `Firepower`, `Mobility` 아래에 generated SO를 만든다.
- 각 SO는 generated stat, source display name, `PreviewPrefab` reference를 가진다.
- 기존 수동 9개 부품은 save compatibility 때문에 제거하지 않는다.
- `ModuleCatalog.asset`에는 기존 9개 뒤에 generated 321개를 append한다.

Acceptance:

- generated SO count가 Frame 100, Firepower 160, Mobility 61이다.
- `ModuleCatalog.asset`에 duplicate id가 없다.
- 기존 `frame_striker`, `fire_scatter`, `mob_treads` 등 기존 저장 데이터 ID가 유지된다.
- `dotnet build .\Assembly-CSharp.csproj`가 통과한다.

2026-04-26 결과: 완료. `nova_part_playable_asset_report.md` 기준 generated SO 321개, visual catalog 321 entries, `ModuleCatalog.asset` 총 Frame 103 / Firepower 163 / Mobility 64.

### Phase 4: Garage Nova Parts panel

목표:

- Garage에 항상 보이는 `Nova Parts` panel/tab을 추가한다.
- slot filter, search, scroll list, selected model preview, 현재 편집 슬롯에 적용하는 `Apply` 동작을 제공한다.
- 기존 prev/next cycling은 유지하되, 321개 탐색의 주 경로는 catalog panel로 둔다.

Acceptance:

- Lobby -> Garage 진입 후 `Nova Parts` panel이 보인다.
- Frame/Firepower/Mobility filter가 동작한다.
- 검색으로 source/display/id 기반 후보를 찾을 수 있다.
- 후보 선택 후 preview가 model-backed assembly로 갱신된다.
- Apply 후 현재 draft slot의 part id가 바뀌고 save가 성공한다.

### Phase 5: validation and closeout

목표:

- generated catalog와 playable 승격이 기존 Lobby/Garage/Account 저장 흐름을 깨지 않는지 검증한다.
- public release 판단은 이 계획에서 닫지 않고 residual로 남긴다.

Acceptance:

- Unity required-field validation 통과
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` 통과
- Play Mode Garage smoke에서 console errors 0
- save/load smoke에서 generated part id가 보존된다.
- `npm run --silent rules:lint` 통과

2026-04-26 결과: static/build/docs validation은 통과했으나 `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`는 generated model preview prefab pack을 generic new-prefab block으로 분류해 blocked다. Phase 5 closeout에서 정책 분류 보완 또는 명시 예외를 닫아야 한다.

## 검증

- Manifest/static:
  - row count 321
  - category allowlist
  - unique `partId`
  - model path exists
  - generated stat range
  - all generated Firepower x Mobility validation
- Editor/Unity:
  - preview prefab generation report
  - generated SO count report
  - `ModuleCatalog.asset` duplicate id check
  - compile check
  - required-field validation
  - Unity UI authoring workflow policy
- Runtime:
  - Lobby -> Garage open
  - Nova Parts panel visible
  - generated Frame/Firepower/Mobility select/apply
  - preview renders
  - roster save/load
  - console errors 0

## Blocked / Residual 처리

- Unity prefab generation policy가 blocked이면 generated prefab/SO 생성까지 확장하지 않고 blocked로 남긴다.
- material missing이 일부 모델에만 발생하면 해당 모델을 `playableStatus=blocked_material`로 분리하고 전체 pipeline 성공으로 포장하지 않는다.
- 깨진 source name은 자동 의미 추측을 하지 않고 `needsNameReview`로 남긴다.
- generated stats는 밸런스 완료가 아니다. 밸런스/이름/권리 검토는 후속 owner로 이관한다.
- Nova1492 원본 리소스의 public release 사용권은 이 계획의 acceptance가 아니며 release gate 전 별도 판단이 필요하다.

## 문서 재리뷰

- 초안 과한점 리뷰: 최초 범위가 `Visual Catalog + Full Promotion + Garage panel`을 한 문서에 모두 담아 넓지만, 같은 이유로 바뀌는 pipeline이며 multi-session handoff가 필요하므로 새 active plan으로 분리하는 편이 맞다. 다만 converter rewrite, Projectiles/Accessories playable, public release 권리 판단은 제외했다.
- 초안 부족한점 리뷰: generated stat 공식, category allowlist, 기존 9개 save compatibility, Garage panel acceptance, validation stack이 필요했다. 초안에 명시해 구현자가 즉석 결정을 하지 않게 했다.
- 수정 후 과한점 재리뷰: `lobby_scene_nova1492_model_application_plan.md`의 LobbyScene 장식 판단과 이 계획의 playable pipeline을 분리했고, design 문서의 유닛 규칙을 재정의하지 않고 generated baseline만 둔다. 과한점 없음.
- 수정 후 부족한점 재리뷰: row count, folder, ID 규칙, generated stat, validation, blocked/residual, lifecycle, owner impact가 보인다. 부족한점 없음.
- owner impact: primary `plans.nova1492-part-catalog-playable`; secondary `plans.progress`, `docs.index`, `plans.lobby-scene-nova1492-model-application`, `plans.nova1492-resource-integration`, `design.unit-module-design`; out-of-scope converter rewrite, public release rights decision, final balance pass, GameScene runtime combat model replacement.
- doc lifecycle checked: 새 active plan이 필요하다. 기존 `lobby_scene_nova1492_model_application_plan.md`는 LobbyScene preview/decoration owner로 유지하고, `nova1492_resource_integration_plan.md`는 resource staging reference로 유지한다.
- plan rereview: clean
- 실행 후 과한점 재리뷰: Core 321 전체를 한 번에 생성했지만 산출물이 generated 전용 폴더와 report로 분리되어 기존 수동 9개 save compatibility를 침범하지 않는다. 현재 과한 범위는 Garage panel/runtime smoke까지 한 문서에 남아 있는 점인데, 같은 playable catalog acceptance라 유지한다.
- 실행 후 부족한점 재리뷰: catalog/prefab/SO 생성은 닫혔지만, Garage Nova Parts panel, 전체 조합 validation, save/load smoke, public release 권리 판단은 아직 닫히지 않았다. 다음 pass는 UI panel과 runtime evidence에 집중한다.
- 실행 후 plan rereview: partial clean. Phase 0~3 clean, Phase 4~5 active.
