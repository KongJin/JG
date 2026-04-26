# Prefab 관리 빈틈 closeout 계획

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.prefab-management-gap-closeout
> role: plan
> owner_scope: 새 UI prefab 승인 잔여, generated prefab lifecycle, prefab review/import tooling, scene override drift, Resources prefab migration inventory
> upstream: plans.progress, ops.unity-ui-authoring-workflow, plans.lobby-scene-ui-prefab-management, plans.non-stitch-ui-stitch-reimport
> artifacts: `Assets/Prefabs/`, `Assets/Resources/*.prefab`, `Assets/Editor/SceneTools/`, `tools/unity-mcp/`, `artifacts/unity/`, `artifacts/nova1492/`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 현재 prefab 관리에서 확인된 부족한 부분을 실행 단위로 닫기 위한 계획이다.
규칙 본문은 [`../ops/unity_ui_authoring_workflow.md`](../ops/unity_ui_authoring_workflow.md)가 소유하고, 이 문서는 그 규칙을 만족시키기 위한 구현 순서와 closeout 기준만 소유한다.

## 배경

최근 점검에서 아래 상태가 확인됐다.

- 새 UI prefab 기본 금지 정책은 있으나, Stitch reimport 또는 prefab-first reset에서 생성되는 허용 대상과 무단 생성 대상을 구분하는 승인 기록이 약하다.
- `Assets/Prefabs`에는 Garage generated preview prefab이 대량 존재하며, hash suffix가 붙은 중복 후보와 active/playable/deprecated 상태 구분이 명확하지 않다.
- prefab review/import helper가 실제 prefab inventory와 어긋난 하드코딩 경로를 들고 있다.
- destructive scene rebuild helper가 missing prefab을 skeleton fallback으로 덮을 수 있다.
- LobbyScene prefab override audit은 존재하지만 선택 artifact라 drift 회귀를 자동으로 막지 못한다.
- `Assets/Resources`에 남은 native UI prefab migration 상태가 prefab 관리 inventory와 분리되어 있다.

## 진행 메모

- 2026-04-26: `tools/unity-mcp/Invoke-PrefabManagementInventory.ps1`를 추가해 `Assets/Prefabs`와 `Assets/Resources/*.prefab` baseline inventory를 생성했다.
  - inventory: `artifacts/unity/prefab-management-inventory.json`, `artifacts/unity/prefab-management-inventory.md`
  - approval manifest: `artifacts/unity/prefab-management-approved-new-prefabs.json`
  - 현재 수량: total prefab 362, generated preview prefab 336, Resources prefab 13, duplicate candidate group 18, approved new prefab target 1
- 2026-04-26: `SetCAccountSettingsOverlayRoot.prefab`는 `.stitch/contracts/mappings/set-c-account-settings-overlay.json`와 `artifacts/unity/set-c-account-settings-overlay-pipeline-result.json` 근거가 있는 `approved-declared` target으로 manifest에 기록했다.
- 2026-04-26: `TempScenePrefabImportTool`의 stale prefab path를 실제 existing prefab으로 정리하고, missing prefab을 warning으로 건너뛰지 않고 import 전에 fail 하도록 바꿨다.
- 2026-04-26: `LobbySceneRuntimeAssemblyTool`은 destructive rebuild 전에 required prefab/assets를 먼저 resolve하고, required UI surface prefab 누락 시 skeleton fallback을 만들지 않고 실패하도록 바꿨다.
- 2026-04-26: `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`는 prefab management inventory/approval manifest summary와 exact declared new prefab targets를 report에 포함한다.

## 목표

- 새 UI prefab 생성이 막혀야 할 때와 허용되어야 할 때를 machine-readable evidence로 구분한다.
- generated preview prefab을 `active`, `playable`, `deprecated`, `duplicate-candidate`, `not-ui/generated` 같은 운영 상태로 분류한다.
- review/import tooling이 stale prefab path를 조용히 통과하지 않게 만든다.
- scene rebuild helper가 missing required prefab을 skeleton UI로 덮지 않게 한다.
- scene prefab instance override drift를 최소 warning 수준으로 감지한다.
- `Assets/Resources` native UI prefab의 Stitch reimport/migration 상태를 prefab inventory와 연결한다.

## 제외 범위

- 실제 UI 시각 재작업
- 새 Stitch screen 생성 자체
- Nova1492 부품 밸런스, 이름, 사용권 판단
- `ops.unity-ui-authoring-workflow`의 정책 본문 재작성
- 기존 generated prefab 대량 삭제
- WebGL 실기 검증

## 판단 기준

| 대상 | 기본 owner | 처리 기준 |
|---|---|---|
| 새 Stitch-derived UI prefab | source freeze / unity surface map / workflow policy | 승인 manifest 또는 기존 mapping에서 선언된 경우만 허용 |
| 수동으로 생긴 UI prefab | workflow policy | 기본 blocked, owner plan과 source evidence 없으면 scene/prefab authoring으로 되돌림 |
| Nova generated preview prefab | Nova/generated asset lane | UI prefab 금지와 분리하되 lifecycle inventory와 duplicate candidate를 기록 |
| LobbyScene prefab instance override | LobbyScene scene contract | root placement/default active는 허용 후보, 내부 text/color/layout override는 review candidate |
| Resources native UI prefab | non-Stitch UI migration lane | source freeze 있음/reimport 됨/blocked/not applicable 상태로 분류 |
| review/import helper path | tooling owner | 실제 repo inventory와 일치하지 않으면 fail 또는 report warning |

## 실행 순서

1. **Prefab inventory baseline 생성**
   - `Assets/Prefabs`와 `Assets/Resources/*.prefab` 전체를 수집한다.
   - prefab을 `ui-surface`, `scene-root`, `independent-overlay`, `generated-preview`, `resources-native-ui`, `gameplay-runtime`, `unknown`으로 1차 분류한다.
   - 결과를 `artifacts/unity/prefab-management-inventory.json`과 요약 markdown으로 남긴다.

2. **새 UI prefab 승인 manifest 정리**
   - `new-prefab-blocked`를 해결하기 위한 declared target 입력을 한 곳으로 모은다.
   - 최소 필드는 `assetPath`, `sourceEvidence`, `ownerPlan`, `reason`, `status`, `expiresOrResidual`로 둔다.
   - `SetCAccountSettingsOverlayRoot.prefab`처럼 source freeze와 mapping이 있는 prefab이 policy에서 허용 대상으로 인식되는지 검증한다.
   - 단, 허용 manifest를 wildcard로 만들지 않는다.

3. **Generated preview prefab lifecycle 분류**
   - `artifacts/nova1492/nova_part_preview_prefab_report.md`와 실제 `Assets/Prefabs/Features/Garage/PreviewModels/Generated/`를 대조한다.
   - hash suffix 중복 후보, missing renderer 후보, alignment review 후보를 별도 필드로 분리한다.
   - playable SO 또는 ModuleCatalog에서 참조되는 prefab과 단순 generated archive prefab을 구분한다.
   - 삭제가 필요한 경우에도 이번 계획에서는 먼저 `deprecated` 또는 `duplicate-candidate` evidence를 남긴 뒤 별도 cleanup pass에서 처리한다.

4. **Review/import tooling stale path 방지**
   - `TempScenePrefabImportTool`이 하드코딩된 과거 prefab path를 들고 있는지 검증한다.
   - review board는 실제 inventory 또는 승인 manifest에서 prefab 목록을 가져오도록 조정한다.
   - 존재하지 않는 prefab path는 silent warning이 아니라 report에 명시하고, review evidence로 사용하지 않는다.

5. **Destructive rebuild helper missing prefab 처리 강화**
   - `LobbySceneRuntimeAssemblyTool`의 required UI surface prefab이 없을 때 skeleton fallback으로 scene을 계속 만들지 않게 한다.
   - fallback이 꼭 필요한 경우에는 `dev-only placeholder`로 명시하고, closeout success에는 사용할 수 없게 report한다.
   - helper 실행 전 required prefab list와 resolved asset path를 report로 남긴다.

6. **Prefab override drift warning 추가**
   - 기존 `artifacts/unity/lobby-scene-prefab-override-audit.md`의 분류 기준을 scriptable audit으로 옮긴다.
   - root placement/default active override는 allowed candidate로 유지한다.
   - prefab 내부 `m_text`, `m_Color`, sprite/material, child layout override는 warning 또는 review candidate로 기록한다.
   - 처음에는 hard-fail이 아니라 workflow policy report의 warning evidence로 둔다.

7. **Resources native UI prefab migration 상태 연결**
   - `SkillBarCanvas`, `StartSkillSelectionCanvas`, `PlayerHealthHudView`, `EnemyHealthBar`, `DamageNumber` 등 `Assets/Resources` prefab을 `non_stitch_ui_stitch_reimport_plan.md`의 inventory와 연결한다.
   - 각 prefab을 `already Stitch-derived`, `native candidate`, `gameplay feedback`, `not UI`, `blocked`, `reimported`로 분류한다.
   - `tools/check-unity-asset-hygiene.ps1` allowlist와 migration status가 서로 다른 사실을 말하지 않게 한다.

8. **Policy/validation closeout**
   - compile/reload를 안정화한다.
   - `tools/check-unity-asset-hygiene.ps1`를 실행한다.
   - `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`가 새 prefab 승인, generated preview 분류, override drift report를 한 번에 설명하는지 확인한다.
   - 필요한 경우 Lobby/Garage prefab review board capture 또는 MCP prefab hierarchy capture를 갱신한다.

## Acceptance

- `SetCAccountSettingsOverlayRoot.prefab` 같은 declared Stitch reimport prefab은 policy에서 근거 있는 허용 또는 명확한 residual로 분류된다.
- 무단 새 UI prefab은 여전히 `new-prefab-blocked`로 막힌다.
- generated preview prefab 전체 수량과 duplicate candidate 목록이 inventory artifact에서 확인된다.
- `TempScenePrefabImportTool` 또는 대체 review tool이 존재하지 않는 prefab path를 조용히 리뷰하지 않는다.
- destructive rebuild helper는 required prefab 누락을 skeleton fallback으로 success 처리하지 않는다.
- LobbyScene prefab override audit이 root override와 내부 visual/layout override를 분리해 report한다.
- `Assets/Resources` native UI prefab의 migration 상태가 `non_stitch_ui_stitch_reimport_plan.md`와 충돌하지 않는다.
- compile check, asset hygiene check, workflow policy check 결과가 success 또는 residual reason을 명확히 남긴다.

## Blocked / Residual 처리

- policy가 capability expansion과 surface onboarding mix 때문에 blocked이면, feature acceptance와 별도로 policy/tooling lane residual로 남긴다.
- generated prefab 중복이 실제 source asset 차이를 반영한 것인지 판정이 어려우면 삭제하지 않고 `duplicate-candidate`로만 남긴다.
- Resources UI prefab이 world-space feedback인지 screen UI인지 애매하면 `gameplay feedback candidate`로 낮은 우선순위 residual에 둔다.
- review tool을 inventory 기반으로 바꾸는 데 범위가 커지면, 우선 stale path fail만 넣고 자동 board 재구성은 후속 pass로 넘긴다.
- 기존 dirty worktree의 unrelated prefab/policy blocker는 이번 계획의 acceptance와 분리해 보고한다.

## 검증 명령

- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-PrefabManagementInventory.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\check-unity-asset-hygiene.ps1`
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- Unity MCP prefab hierarchy / scene capture as needed
- `npm run --silent rules:lint`

## 문서 재리뷰

- 과한점 리뷰: 기존 `ops.unity-ui-authoring-workflow`의 규칙 본문을 재정의하지 않고, 실행 순서와 acceptance만 담았다. 새 hard-fail은 바로 추가하지 않고 warning/report 단계에서 시작하도록 범위를 낮췄다.
- 부족한점 리뷰: owner, scope, 배경, 목표, 제외 범위, 판단 기준, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다. `docs.index`와 `progress.md` 등록이 필요하다.
- 수정 후 재리뷰: generated preview, new UI prefab, Resources migration, review tool, destructive helper가 서로 다른 owner로 흩어지지 않도록 이 계획은 조정/closeout owner만 맡고, 실제 규칙 본문은 기존 owner에 남긴다.
- owner impact: primary `plans.prefab-management-gap-closeout`; secondary `plans.progress`, `docs.index`, `plans.lobby-scene-ui-prefab-management`, `plans.non-stitch-ui-stitch-reimport`; out-of-scope `ops.unity-ui-authoring-workflow` rule rewrite.
- doc lifecycle checked: 새 active plan이 필요하다. 여러 세션에서 inventory, tooling, policy residual을 이어받아야 하며 `progress.md` 한 줄만으로는 acceptance와 residual을 보존하기 어렵다.
- plan rereview: clean
- 2026-04-26 반복 리뷰: 과한점 없음. 새 hard-fail이나 정책 본문을 이 plan에 추가하지 않았고, generated prefab 삭제도 실행 범위에서 제외했다.
- 2026-04-26 반복 리뷰: 부족한점 없음. 새 prefab 승인, generated lifecycle, stale review path, missing prefab fallback, override drift, Resources migration, 검증과 residual 처리까지 실행 가능 단위로 보인다.
- plan rereview: clean
- 2026-04-26 작업 시작 후 과한점 리뷰: inventory/approval evidence와 helper fail-fast만 추가했고, `ops.unity-ui-authoring-workflow` 정책 본문이나 generated prefab 삭제 범위를 늘리지 않았다.
- 2026-04-26 작업 시작 후 부족한점 리뷰: override drift warning의 scriptable audit 연결과 full workflow policy default run은 아직 남아 있어 이 plan은 active로 유지한다.
- plan rereview: residual - implementation started; override drift warning integration and full workflow policy closeout remain.
