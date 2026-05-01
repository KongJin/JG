# Technical Debt Recurrence Prevention Plan

> 마지막 업데이트: 2026-05-01
> 상태: reference
> doc_id: plans.technical-debt-recurrence-prevention
> role: plan
> owner_scope: Setup/Root drift, runtime lookup, dynamic repair, dead-code retention 재발 방지 reference gate
> upstream: ops.cohesion-coupling-policy, ops.document-management-workflow
> artifacts: none

이 문서는 이미 발생한 runtime repair debt와 dead-code retention이 반복되지 않게 하는 reference gate만 소유한다.
제품 acceptance와 현재 실행 순서는 해당 active owner plan과 `plans.progress`를 우선한다.

## Baseline

- `DefaultPlayerSpecProvider`, non-master `EnemySetup`, `SoundPlayerRuntimeHostFactory`의 runtime `Resources.Load` dependency repair는 serialized/scene-owned contract로 옮겼다.
- `SoundPlayer` AudioSource/template residual은 WebGL audio product smoke가 아니라 runtime contract debt로 추적한다.
- `PlayerHealthHudView`, `EnemyHealthBar`, `DamageNumber`는 `Assets/Prefabs/RuntimeFeedback/` compatibility surface로 분리했다.
- Runtime lookup / dynamic repair 위반은 `tools/validate-rules.ps1` 기준 0이어야 한다.
- 삭제 후보 정리는 code reference, Unity serialized GUID reference, test private reflection, registered entrypoint를 함께 확인해야 한다.
- `compile-clean`, `rules-clean`, `asset-clean`은 각각 Unity compile check, `npm run --silent rules:lint`, `npm run --silent unity:asset-hygiene`로 확인한다.

## Reference Gate

| Gate | Verdict | Handling |
|---|---|---|
| Setup/Root drift | reference guard | 새 runtime object repair는 serialized reference 또는 owner setup으로 고정 |
| Runtime lookup containment | reference guard | feature code raw lookup은 `ComponentAccess`, registry, serialized reference 등 드러난 seam만 허용 |
| Prefab/scene contract | reference guard | Resources fallback을 dependency repair로 확장하지 않음 |
| Actual acceptance split | reference guard | GameScene, Account/Garage, UI Toolkit candidate evidence를 서로 다른 success로 묶지 않음 |
| Locked source padding cleanup | reference guard | file lock 때문에 같은 바이트 길이로 제거한 코드는 임시 padding residual로만 남기고, `ArchitectureGuardrailReflectionTests.SourcePaddingMarkers_AreLimitedToKnownLockedResiduals`가 새 padding marker를 막는다 |
| Fallback ownership containment | reference guard | fallback/default는 production controller나 setup에 직접 흩뿌리지 않고 domain default, adapter/helper, serialized contract 중 한 owner로 고정하며, `ArchitectureGuardrailReflectionTests.ProductionFallbackReferences_AreExplicitlyReviewed`가 새 production fallback marker를 리뷰 대상으로 만든다 |
| Dead-code cleanup closeout | reference guard | 삭제 후보는 code grep만으로 닫지 않고 type/name refs, `.meta` GUID refs, scene/prefab refs, test-only reflection, registered tool entrypoints, generated project include를 함께 확인한다 |

## Recurrence Risks

| Risk | Severity | Prevention |
|---|---|---|
| scene/prefab reference 누락을 runtime `AddComponent`나 lookup으로 복구 | High | scene/prefab contract 또는 setup owner에 serialized reference로 연결 |
| single-client smoke를 multiplayer success로 확장 | High | Phase 5는 2-client evidence 전까지 blocked/residual 유지 |
| file lock 우회 과정에서 제거된 코드가 padding 주석으로 장기 잔류 | Medium | guardrail test와 `Padding retained`, `Removed procedural` 검색을 closeout checklist에 포함하고, 잠금 해제 후 별도 cleanup pass로 정상 diff를 만든다 |
| 임시 fallback/default가 contract 누락을 숨기는 runtime repair로 확장 | High | 새 fallback은 이유, owner, 제거 조건을 코드 근처 또는 테스트명에서 드러내고, fallback guardrail test와 `fallback|Fallback` 검색 결과를 리뷰한다 |
| UI Toolkit preview를 runtime replacement success로 확장 | Medium | candidate preview, runtime visibility, WebGL/product acceptance를 분리 |
| generated asset inventory를 product/balance approval로 오해 | Medium | Nova generated assets와 gameplay/content 승격 판단을 content owner lane으로 분리 |
| unused code가 feature cleanup 밖에 남아 다음 리팩터의 전제처럼 보임 | Medium | dead-code cleanup closeout에서 code reference, 등록 entrypoint, 테스트 리플렉션, Unity serialized GUID를 한 번에 닫는다 |

## Issue 2 Prevention: Locked Source Padding

Scope:

- Unity/IDE가 source file을 memory-mapped 상태로 잡아 일반 patch가 실패한 경우.
- 같은 바이트 길이 우회로 제거한 코드가 block comment padding으로 남은 경우.

Prevention:

- padding은 `success`가 아니라 `residual`로만 본다.
- padding 파일에는 새 기능을 얹지 않는다. 새 작업 전에 먼저 에디터 잠금 해제 여부를 확인하고 padding을 정상 삭제한다.
- padding 삭제 pass는 code behavior 변경과 섞지 않고 `padding cleanup only` 이유로 분리한다.
- cleanup pass의 최소 검증은 compile, `git diff --check`, 그리고 padding marker 검색이다.

Closeout check:

- `rg -n "Padding retained|Removed procedural" Assets/Scripts`
- `ArchitectureGuardrailReflectionTests.SourcePaddingMarkers_AreLimitedToKnownLockedResiduals`
- 남는 결과가 있으면 파일 경로와 잠금/정리 owner를 final 또는 owner plan residual에 분리해 남긴다.

## Issue 5 Prevention: Fallback Containment

Scope:

- runtime 데이터 누락을 `fallback`, 기본값, 임시 prefab/effect, 임시 stat 값으로 메우는 코드.
- 정상 도메인 기본값은 허용하되, scene/prefab/catalog/network contract 누락을 숨기는 fallback은 review gate로 본다.

Prevention:

- 새 fallback은 세 가지 중 하나로만 허용한다: domain default, feature-owned adapter/helper, serialized/asset contract.
- production controller, setup, page controller는 fallback owner가 되지 않는다.
- fallback이 transport/protocol 호환 목적이면 key/default parsing을 helper로 빼고 direct test를 둔다.
- fallback이 visual/prefab 목적이면 catalog/contract completeness test를 우선하고, preview 실패를 조용한 success로 보고하지 않는다.
- fallback이 combat/stat 목적이면 도메인 default인지 runtime contract repair인지 테스트명에서 구분한다.

Review check:

- `rg -n "fallback|Fallback" Assets/Scripts/Features Assets/Editor/DirectTests`
- `ArchitectureGuardrailReflectionTests.ProductionFallbackReferences_AreExplicitlyReviewed`
- 검색 결과마다 `domain default`, `compat adapter`, `test-only`, `residual` 중 하나로 분류한다.
- 분류가 안 되는 production fallback은 별도 cleanup 후보로 남긴다.

## Issue 6 Prevention: Dead-Code Retention

Scope:

- 코드 이름 검색에는 안 잡히지만 `.meta` GUID, scene/prefab serialization, generated `.csproj`, tool registry, 또는 테스트 리플렉션이 붙잡는 삭제 후보.
- 새 구조로 대체됐지만 구 adapter/helper/tool 파일이 미등록 상태로 남아 있는 경우.
- behavior가 없는 compatibility overload, 빈 validation method, 미사용 serialized field가 테스트 fixture 때문에 유지되는 경우.

Confirmed facts from 2026-05-01 cleanup:

- `Shared.Ui`의 UGUI-era stack/palette/graphic/sound helper는 code reference와 scene/prefab GUID reference가 없어서 삭제 가능했다.
- `UiHandlers.cs`와 `GameObjectHandlers.cs`는 canonical `Improved*Handlers`가 `UnityMcpBridge`에 등록된 뒤 미등록 duplicate handler로 남아 있었다.
- `UnitSlotInputHandlerDirectTests`가 private `_dragGhostPrefab` field를 reflection으로 세팅해서 production의 빈 `ValidateGhostPrefab`, `legacyCanvas` overload, 미사용 serialized field 제거를 늦췄다.
- script 삭제 뒤 local `dotnet build`는 generated `.csproj` stale include 때문에 `CS2001`을 냈고, Unity/IDE가 잡은 source lock은 stale comment 정리를 막을 수 있다.

Root cause:

- cleanup closeout 기준이 compile 중심이라 unused-code deletion의 실제 owner surface를 모두 보지 않았다.
- Unity script는 C# symbol reference, `.meta` GUID serialization, generated project include가 서로 다른 경로로 남는데, 삭제 체크가 한 종류의 참조 검색에 치우쳤다.
- tests가 public behavior가 아니라 private implementation detail을 고정해 dead field/method를 살아 있게 만들었다.
- tool handlers는 registry가 canonical entrypoint인데, 파일 존재 자체를 tool availability로 착각하기 쉬운 구조였다.

Prevention:

- 삭제 후보마다 owner surface를 먼저 분류한다: runtime script, MonoBehaviour serialized component, editor tool handler, generated/project-only include, test-only helper.
- MonoBehaviour/script 삭제 전후로 type/name search와 `.meta` GUID search를 둘 다 실행한다.
- editor tool handler는 file grep이 아니라 registry/bootstrap entrypoint를 기준으로 live/dead를 판정한다.
- test가 private field/method를 reflection으로 만지면 production API 유지 근거로 보지 않고, public behavior test로 전환한 뒤 삭제한다.
- generated `.csproj` stale include는 code regression이 아니라 Unity project-file sync residual로 분리하되, CLI build closeout 전에는 regenerate 또는 local include cleanup으로 빌드를 통과시킨다.
- file lock 때문에 주석/old reference가 남으면 behavior success로 포장하지 않고 locked residual로 기록하고, 잠금 해제 후 cleanup-only pass로 닫는다.

Closeout check:

- `rg -n "<DeletedType>|<DeletedFile>|<DeletedField>|<DeletedMethod>" Assets/Scripts Assets/Editor Assets/Scenes Assets/Prefabs Assets/UI docs`
- deleted script `.meta` GUID를 `Assets/Scenes Assets/Prefabs Assets/UI`에서 검색한다.
- editor tool 삭제는 registry/bootstrap 파일에서 해당 type이 등록되지 않는지 확인한다.
- tests에서 `BindingFlags.NonPublic`, `SetPrivateField`, 삭제 대상 field/method 문자열을 검색한다.
- `dotnet build JG.slnx -v:minimal`
- 필요한 경우 scoped Unity EditMode direct test를 실행하고, Unity Editor가 열려 batchmode가 막히면 blocked evidence를 분리해 보고한다.

## Applied Guardrails

2026-05-01 적용:

- `ArchitectureGuardrailReflectionTests`에 locked source padding residual allowlist와 production fallback review allowlist를 추가했다.
- production 코드의 오탐 fallback 명명은 behavior 변경 없이 `placeholder`, `default`, `current`, `reported`, `catalog missing` 의미로 정리했다.
- serialized field rename은 `FormerlySerializedAs`로 마이그레이션 경로를 유지했다.
- dead-code retention cleanup gate를 추가하고, 미사용 `Shared.Ui` 잔여 코드, 미등록 MCP 구 핸들러, `UnitSlotInputHandler` private-field test coupling을 cleanup evidence로 고정했다.

## Validation

- `tools/check-compile-errors.ps1`
- `tools/validate-rules.ps1`
- `npm run --silent rules:lint`
- `npm run --silent unity:asset-hygiene`
- `tools/unity-mcp/Invoke-UnityMcpEditModeTests.ps1 -TestName Tests.Editor.ArchitectureGuardrailReflectionTests`
- `rg -n "Padding retained|Removed procedural" Assets/Scripts`
- `rg -n "fallback|Fallback" Assets/Scripts/Features Assets/Editor/DirectTests`
- 삭제 후보 closeout 시 type/name search, deleted `.meta` GUID search, registry/bootstrap entrypoint search, private reflection search
- 관련 runtime owner plan의 smoke evidence

## Residual

- GameScene direct EditMode execution은 GameScene actual-flow active owner가 소유하고, Phase 5 2-client smoke는 `plans.progress` multiplayer residual이 소유한다.
- Account/Garage/WebGL product acceptance는 `plans.progress` WebGL account residual과 WebGL smoke checklist 기준으로 추적한다.
- `SoundPlayer` AudioSource/template residual은 runtime contract guard 기준으로 보되, 실행이 필요하면 새 runtime owner pass 또는 해당 feature owner로 연다.
- `WaveSetup.TryGetFirstEnemyData`와 `SummonPhotonAdapter`의 `fallbackInstanceId`는 production fallback residual이다. behavior removal은 별도 runtime contract cleanup으로 연다.
- `fallback|Fallback` 검색 결과는 review gate다. guardrail allowlist에 없는 production fallback은 다음 cleanup 후보로 승격한다.
- `Assets/Editor/UnityMcp/Handlers/ImprovedUiHandlers.cs`의 stale `UiHandlers.cs` 주석은 Unity/IDE source lock으로 남은 locked residual이다. 잠금 해제 후 cleanup-only pass로 제거한다.

owner impact:

- primary: `plans.technical-debt-recurrence-prevention`
- secondary: `Assets/Editor/Tests/ArchitectureGuardrailReflectionTests.cs`, narrow production naming cleanup, dead-code cleanup files, `artifacts/rules/issue-recurrence-closeout.json`
- out-of-scope: `WaveSetup`/`SummonPhotonAdapter` behavior removal, Unity scene/prefab mutation beyond deleted GUID checks, `plans.progress` 현재 우선순위 변경

doc lifecycle checked:

- reference 유지. 새 runtime repair 또는 dead-code cleanup 실행 lane이 열리면 해당 feature/runtime owner plan 또는 session checklist로 다시 판단한다.
- plan rereview: clean - reference guard, dead-code cleanup gate, residual routing checked
