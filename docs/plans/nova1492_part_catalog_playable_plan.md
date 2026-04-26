# Nova1492 Part Catalog Playable Closeout

> 마지막 업데이트: 2026-04-26
> 상태: reference
> doc_id: plans.nova1492-part-catalog-playable
> role: plan
> owner_scope: 변환된 Nova1492 UnitParts 모델의 Garage 부품 catalog/playable 승격 closeout 기록
> upstream: plans.progress, plans.nova1492-resource-integration, plans.lobby-scene-nova1492-model-application, design.unit-module-design, design.module-data-structure, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/nova1492/nova_part_catalog.csv`, `artifacts/nova1492/nova_part_catalog_summary.md`, `artifacts/nova1492/nova_part_preview_prefab_report.md`, `artifacts/nova1492/nova_part_playable_asset_report.md`, `artifacts/nova1492/nova_part_validation_closeout_report.md`, `artifacts/nova1492/nova_part_alignment_report.md`, `artifacts/nova1492/nova_part_alignment.csv`, `Assets/Prefabs/Features/Garage/PreviewModels/Generated/`, `Assets/Data/Garage/NovaGenerated/`, `tools/nova1492/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 Nova1492 변환 모델 중 `UnitParts` Core 321개를 JG Garage 3-slot 부품 체계에 연결한 결과만 남긴 reference다.
리소스 변환과 분류 근거는 [`nova1492_resource_integration_plan.md`](./nova1492_resource_integration_plan.md)와 `artifacts/nova1492/`가 맡고, Lobby 장식 후보 판단은 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)가 맡는다.

## Closeout

- playable 대상: Core 321개
  - Frame 100: `UnitParts/Bodies` 72 + `UnitParts/Bases` 28
  - Firepower 160: `UnitParts/ArmWeapons`
  - Mobility 61: `UnitParts/Legs`
- 제외 대상: `UnitParts/Accessories`, `Effects/Projectiles`, `Unknown/Review`
- generated assets:
  - preview prefabs: `Assets/Prefabs/Features/Garage/PreviewModels/Generated/`
  - playable SOs: `Assets/Data/Garage/NovaGenerated/`
  - visual catalog: `Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset`
  - module catalog: `Assets/Data/Garage/ModuleCatalog.asset`
- `ModuleCatalog.asset` 최종 수: Frame 103, Firepower 163, Mobility 64
- 기존 수동 9개 id는 save compatibility 때문에 유지했다.
- socket/pivot/scale 자동 1차 데이터는 `NovaPartAlignmentCatalog.asset`로 준비했고, `auto_ok` 부품은 Garage preview runtime 조립에 적용했다. `needs_review_*`, `missing_*`, alignment 누락 부품은 기존 고정 오프셋 fallback을 유지한다.
- BattleScene 전투 유닛 모델 교체는 이 closeout 범위에 포함하지 않는다.

## Evidence

- catalog manifest: `artifacts/nova1492/nova_part_catalog.csv`
- catalog summary: `artifacts/nova1492/nova_part_catalog_summary.md`
- preview prefab report: `artifacts/nova1492/nova_part_preview_prefab_report.md`
- playable asset report: `artifacts/nova1492/nova_part_playable_asset_report.md`
- Garage panel install report: `artifacts/unity/garage-nova-parts-panel-install-report.md`
- Garage panel smoke report: `artifacts/unity/garage-nova-parts-panel-smoke.json`
- Garage panel smoke screenshot: `artifacts/unity/garage-nova-parts-panel-smoke.png`
- closeout validation report: `artifacts/nova1492/nova_part_validation_closeout_report.md`
- alignment report: `artifacts/nova1492/nova_part_alignment_report.md`
- alignment csv: `artifacts/nova1492/nova_part_alignment.csv`

최종 검증 결과:

- manifest rows 321, duplicate id 0, missing model path 0
- preview prefab generation failed 0, missing material 0
- playable SO generation failed 0
- Garage Nova Parts panel search/apply smoke passed
- generated Firepower x Mobility 9,760 combinations passed `UnitComposition.Validate`
- generated smoke roster passed `ValidateRosterUseCase`
- Firestore garage mapper roundtrip passed
- local `GarageJsonPersistence` save/load roundtrip passed, original local file restored after smoke
- alignment catalog entries 321, duplicate id 0, missing prefab 0, missing renderer 2, static balance pass
- Garage preview runtime alignment wiring passed: `GarageSetup` references `NovaPartAlignmentCatalog.asset`, `GarageUnitPreviewView` attaches firepower/mobility by frame socket + child socket for `auto_ok` alignments, Garage panel smoke passed with console error 0
- latest scoped Unity UI authoring workflow policy passed
- `dotnet build .\Assembly-CSharp.csproj`, `dotnet build .\Assembly-CSharp-Editor.csproj`, `tools/check-compile-errors.ps1`, `npm run --silent rules:lint` passed

## Residual

- generated stats are deterministic baseline only, not final balance.
- source-derived names and 4 alignment review candidates still need review before final product polish.
- BattleScene combat unit model assembly remains a separate follow-up from Garage preview alignment.
- Nova1492 original resource public release rights remain a separate release gate.
- WebGL account/cloud mutation smoke belongs to the shared `Account/Garage` validation lane, not this closeout.

## Rereview

- 과한점 재리뷰: 상세 Phase 로그와 반복 리뷰 기록은 closeout reference에 불필요해 제거했다. 실행 증거는 artifact 링크와 최종 판정만 남긴다.
- 부족한점 재리뷰: owner, scope, excluded categories, acceptance evidence, residual, lifecycle 상태가 남아 있다. 부족한점 없음.
- owner impact: primary `plans.nova1492-part-catalog-playable`; secondary `plans.progress`, `docs.index`; out-of-scope public release rights, final balance pass, WebGL account cloud mutation smoke.
- doc lifecycle checked: reference 기록으로 유지한다.
- plan rereview: clean
