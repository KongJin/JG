# Prefab 관리 빈틈 closeout 계획

> 마지막 업데이트: 2026-04-27
> 상태: reference
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

- 새 UI prefab 기본 금지 정책은 유지하되, 신규 Stitch/Garage UI는 UI Toolkit candidate route로 이동했다.
- `Assets/Prefabs`에는 Garage generated preview prefab이 대량 존재하며, hash suffix가 붙은 중복 후보와 active/playable/deprecated 상태 구분이 명확하지 않다.
- prefab review/import와 scene rebuild fallback은 active UI route가 아니다.
- LobbyScene prefab override audit은 historical evidence이며 현재 workflow policy summary에서 제외됐다.
- `Assets/Resources`에 남은 native UI prefab migration 상태가 prefab 관리 inventory와 분리되어 있다.

## 진행 메모

- 2026-04-26: `tools/unity-mcp/Invoke-PrefabManagementInventory.ps1`를 추가해 `Assets/Prefabs`와 `Assets/Resources/*.prefab` baseline inventory를 생성했다.
  - inventory: `artifacts/unity/prefab-management-inventory.json`, `artifacts/unity/prefab-management-inventory.md`
  - approval manifest: `artifacts/unity/prefab-management-approved-new-prefabs.json`
  - 현재 수량: total prefab 362, generated preview prefab 336, Resources prefab 13, duplicate candidate group 18, approved new prefab target 1
- 2026-04-26: `SetCAccountSettingsOverlayRoot.prefab`는 `.stitch/contracts/mappings/set-c-account-settings-overlay.json`와 `artifacts/unity/set-c-account-settings-overlay-pipeline-result.json` 근거가 있는 `approved-declared` target으로 manifest에 기록했다.
- 2026-04-26: 당시 prefab review/import helper의 stale path와 destructive rebuild fallback을 점검했다.
- 2026-04-26: `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`는 prefab management inventory/approval manifest summary와 exact declared new prefab targets를 report에 포함한다.
- 2026-04-27: `LobbyScene` prefab override drift를 JSON/markdown으로 생성하고 workflow policy report에 summary를 연결했다.
  - override audit: `artifacts/unity/lobby-scene-prefab-override-audit.json`, `artifacts/unity/lobby-scene-prefab-override-audit.md`
  - current result: surface 6, allowed candidate 3, review candidate 2, warning 1
  - residual warning: `SetCRoomDetailPanelRoot` has one scene-owned `m_Color.a` override. This is warning evidence, not a workflow hard-fail.
- 2026-04-27: full workflow policy check succeeded and reports prefab inventory, approval manifest, and override drift summary together.
- 2026-04-27: Remaining Resources UI/feedback prefabs are limited to runtime-referenced compatibility surfaces.

## Closeout

Status: reference.

Closed acceptance:

- Declared Stitch reimport prefab target is machine-readable through `artifacts/unity/prefab-management-approved-new-prefabs.json`.
- New prefab policy still blocks unapproved prefab additions by default while reading explicit declared targets.
- Generated preview prefab count and duplicate candidate groups are in `artifacts/unity/prefab-management-inventory.json`.
- prefab review/import helpers are not active review routes.
- destructive LobbyScene rebuild fallback is not an active route.
- `Assets/Resources` prefab migration categories are connected to the prefab inventory.
- Battle HUD and effect UI surfaces are tracked through runtime compatibility or UI Toolkit replacement candidates.

Residual:

- Historical override findings remain useful as background, but they are no longer active workflow policy inputs.

## 목표

- 새 UI prefab 생성이 막혀야 할 때와 허용되어야 할 때를 machine-readable evidence로 구분한다.
- generated preview prefab을 `active`, `playable`, `deprecated`, `duplicate-candidate`, `not-ui/generated` 같은 운영 상태로 분류한다.
- prefab review/import tooling and destructive rebuild fallback stay outside the active route unless reintroduced under a UI Toolkit strategy.
- scene prefab instance override drift는 historical background로만 보관한다.
- `Assets/Resources` native UI prefab의 Stitch reimport/migration 상태를 prefab inventory와 연결한다.

## 제외 범위

- 실제 UI 시각 재작업
- 새 Stitch screen 생성 자체
- Nova1492 부품 밸런스, 이름, 사용권 판단
- `ops.unity-ui-authoring-workflow`의 정책 본문 재작성
- 기존 generated prefab 대량 정리
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
   - historical `artifacts/nova1492/nova_part_preview_prefab_report.md`는 참고만 하고, 실제 active asset path로 재승격하지 않는다.
   - hash suffix 중복 후보, missing renderer 후보, alignment review 후보를 별도 필드로 분리한다.
   - playable SO 또는 ModuleCatalog에서 참조되는 prefab과 단순 generated archive prefab을 구분한다.
   - 정리가 필요한 경우에도 이번 계획에서는 먼저 `deprecated` 또는 `duplicate-candidate` evidence를 남긴 뒤 별도 cleanup pass에서 처리한다.

4. **Helper route 정리**
   - legacy prefab import/rebuild helpers are outside the active UI route.
   - 같은 목적의 helper가 필요하면 UI Toolkit candidate strategy와 fresh preview evidence를 전제로 새로 만든다.

5. **Resources native UI prefab migration 상태 연결**
   - 남은 `PlayerHealthHudView`, `EnemyHealthBar`, `DamageNumber` 등 `Assets/Resources` prefab을 inventory와 연결한다.
   - 각 prefab을 `already Stitch-derived`, `native candidate`, `gameplay feedback`, `not UI`, `blocked`, `reimported`로 분류한다.
   - `tools/check-unity-asset-hygiene.ps1` allowlist와 migration status가 서로 다른 사실을 말하지 않게 한다.

6. **Policy/validation closeout**
   - compile/reload를 안정화한다.
   - `tools/check-unity-asset-hygiene.ps1`를 실행한다.
   - `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`가 새 prefab 승인과 generated preview 분류를 설명하는지 확인한다.
   - 필요한 경우 Lobby/Garage prefab review board capture 또는 MCP prefab hierarchy capture를 갱신한다.

## Acceptance

- `SetCAccountSettingsOverlayRoot.prefab` 같은 declared Stitch reimport prefab은 policy에서 근거 있는 허용 또는 명확한 residual로 분류된다.
- 무단 새 UI prefab은 여전히 `new-prefab-blocked`로 막힌다.
- generated preview prefab 전체 수량과 duplicate candidate 목록이 inventory artifact에서 확인된다.
- prefab review/import helper와 destructive rebuild helper가 active UI route에 속하지 않는다.
- `Assets/Resources` native UI prefab의 migration 상태가 `non_stitch_ui_stitch_reimport_plan.md`와 충돌하지 않는다.
- compile check, asset hygiene check, workflow policy check 결과가 success 또는 residual reason을 명확히 남긴다.

## Blocked / Residual 처리

- policy가 capability expansion과 surface onboarding mix 때문에 blocked이면, feature acceptance와 별도로 policy/tooling lane residual로 남긴다.
- generated prefab 중복이 실제 source asset 차이를 반영한 것인지 판정이 어려우면 `duplicate-candidate`로만 남긴다.
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
- 2026-04-26 반복 리뷰: 과한점 없음. 새 hard-fail이나 정책 본문을 이 plan에 추가하지 않았고, generated prefab 대량 정리도 실행 범위에서 제외했다.
- 2026-04-26 반복 리뷰: 부족한점 없음. 새 prefab 승인, generated lifecycle, stale review path, missing prefab fallback, override drift, Resources migration, 검증과 residual 처리까지 실행 가능 단위로 보인다.
- plan rereview: clean
- 2026-04-26 작업 시작 후 과한점 리뷰: inventory/approval evidence와 helper fail-fast만 추가했고, `ops.unity-ui-authoring-workflow` 정책 본문이나 generated prefab 정리 범위를 늘리지 않았다.
- 2026-04-26 작업 시작 후 부족한점 리뷰: override drift warning의 scriptable audit 연결과 full workflow policy default run은 아직 남아 있어 이 plan은 active로 유지한다.
- plan rereview: residual - implementation started; override drift warning integration and full workflow policy closeout remain.
- 2026-04-27 closeout 후 과한점 리뷰: plan을 새 정책 본문으로 키우지 않고, 구현 결과와 residual evidence만 기록했다. 새 hard-fail은 추가하지 않았다.
- 2026-04-27 closeout 후 부족한점 리뷰: acceptance 항목별 evidence, residual warning, 검증 결과, lifecycle 전환 사유가 보인다.
- doc lifecycle checked: active 실행 계획에서 reference closeout 기록으로 전환한다. 남은 drift는 audit artifact와 관련 surface pass에서 추적한다.
- plan rereview: clean
- 2026-04-27 route 리뷰: generated/runtime-referenced prefab 대량 정리로 확장하지 않고 UI Toolkit replacement 후보와 runtime compatibility surface를 구분했다.
- 2026-04-27 부족한점 리뷰: 유지 대상과 replacement 후보가 Closeout/Residual에 구분된다.
- plan rereview: clean
