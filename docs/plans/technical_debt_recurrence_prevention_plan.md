# Technical Debt Recurrence Prevention Plan

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: plans.technical-debt-recurrence-prevention
> role: plan
> owner_scope: GameScene/BattleScene refactor 후 Setup/Root 책임 비대화, runtime lookup, dynamic component repair 재발 방지 실행 순서와 acceptance
> upstream: docs.index, ops.cohesion-coupling-policy, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: none

이 문서는 2026-04-28 기술부채 정리 패스 이후 같은 형태의 부채가 다시 쌓이지 않게 하는 실행 계획이다.
규칙 본문은 `ops.cohesion-coupling-policy`와 Unity lane owner 문서가 소유하고, 이 문서는 적용 순서와 acceptance만 소유한다.

## Scope

포함한다:

- `*Setup`, `*Root`가 wiring 밖의 per-frame 상태 진행을 다시 소유하지 않게 하는 확인 루프
- runtime `Find*`, `GetComponent*`, `AddComponent`가 feature 코드에 흩어지는 것을 막는 seam 점검
- scene/prefab serialized contract가 필요한 컴포넌트를 코드 fallback으로 복구하지 않게 하는 후속 정리
- compile-clean, static-clean, docs lint, asset hygiene를 한 묶음으로 유지하는 closeout 기준

제외한다:

- GameScene Phase 5 2-client acceptance 자체
- UI Toolkit candidate migration과 Stitch source freeze 판단
- Sound system의 제품 UX, 믹싱, WebGL 재생 acceptance
- 전역 hard-fail lint 신설. 필요하면 별도 tooling/rule owner pass에서 판단한다.

## Current Baseline

이미 처리된 기준선:

- `StatusSetup.Update()` 제거, `StatusTickDriver`로 status tick 책임 분리
- `BattleEntityPrefabSetup.Update()` 제거, `BattleEntityAttackDriver`로 battle entity 자동공격 루프 분리
- `GameSceneRoot.Update()` 제거, energy regen tick을 `EnergyBarView` runtime responsibility로 이동
- `GameSceneRuntimeSpawnRegistrar`의 polling/scene scan 제거, explicit arrival notification으로 전환
- pool binding seam 도입 후 projectile, skill effect, zone view, lifetime release가 `PooledObject`를 직접 lookup하지 않게 정리
- `ComponentAccess` seam으로 feature-level component access 분산을 축소
- `SoundPlayer` duplicate scan을 scene-wide `FindObjectsByType` 대신 active instance reference로 전환
- `SoundPlayerRuntimeHostFactory`의 host creation을 code-generated `GameObject` 대신 `Assets/Resources/Shared/Sound/SoundPlayer.prefab` contract로 전환

현재 검증 기준선:

- compile-clean: `tools/check-compile-errors.ps1`
- static-clean: `tools/validate-rules.ps1`
- docs-clean: `npm run --silent rules:lint`
- asset-clean: `npm run --silent unity:asset-hygiene`

## Recurrence Risks

| Risk | Severity | Prevention |
|---|---:|---|
| `*Setup`/`*Root`에 `Update()`가 다시 생김 | High | 변경 후 `rg -n "class .*Setup|class .*Root|private void Update\\(" Assets/Scripts/Features -g '*Setup.cs' -g '*Root.cs'` 확인 |
| scene/prefab reference 누락을 runtime `AddComponent`로 복구 | High | 새 runtime component는 prefab/scene contract에 serialized reference로 연결 |
| collision/projectile/zone 코드가 다시 `GetComponentInParent<EntityIdHolder>()`를 직접 호출 | Medium | `ComponentAccess.TryGetEntityIdHolder` 또는 registry/port seam으로만 접근 |
| pool rent/return 루프에서 매번 하위 component scan | Medium | pool instance binding cache 유지, 새 pool reset/bind handler는 `IPoolResetHandler`/`IPoolBindingHandler`로 등록 |
| sound/runtime host가 scene-wide scan이나 hidden repair 경로로 확장 | Medium | runtime config/prefab contract 후보를 별도 audio host pass에서 검토 |
| manual scene/prefab YAML 변경 후 Unity contract mismatch | Medium | compile/static/asset hygiene 후 가능하면 Unity editor smoke 또는 prefab inspection으로 재확인 |

## Execution Plan

### Phase 1: Setup/Root Drift Guard

목표:

- `*Setup`, `*Root`는 composition/wiring만 맡는 기준을 유지한다.
- per-frame 로직이 필요하면 `*Driver`, `*Controller`, view/runtime adapter 중 책임 이름이 맞는 컴포넌트로 분리한다.

작업:

- 각 refactor 후 Setup/Root Update scan을 실행한다.
- 발견 시 runtime owner 후보를 먼저 정하고, scene/prefab serialized reference를 연결한다.
- driver가 domain/application 포트를 직접 소유해야 하면 Setup이 생성해 driver에 주입한다.

Acceptance:

- `Assets/Scripts/Features/**/*Setup.cs`, `*Root.cs` 범위에서 production `Update()`가 없다.
- 예외가 필요하면 owner plan 또는 code comment가 아니라 owner 문서/ADR 후보로 분리되어 있다.

### Phase 2: Runtime Lookup Containment

목표:

- feature code에 raw `Find*`, `GetComponent*`, `AddComponent`가 흩어지지 않는다.
- 허용된 seam은 `ComponentAccess`, pool binding, scene registry, serialized reference 중 하나로 드러난다.

작업:

- feature code raw scan을 refactor closeout마다 실행한다.
- 반복되는 lookup은 scene/prefab contract 또는 registry/port로 승격할지 판단한다.
- temporary seam은 다음 owner pass에서 제거 가능한 이름과 위치로 둔다.

Acceptance:

- `tools/validate-rules.ps1`가 위반 0이다.
- raw scan 결과가 남더라도 shared seam, editor-only smoke, or explicitly scoped runtime factory로 설명 가능하다.

### Phase 3: Prefab/Scene Contract Hardening

목표:

- 새 driver/helper는 runtime repair가 아니라 serialized contract로 붙는다.
- prefab/scene 변경은 작고 추적 가능하다.

작업:

- 새 MonoBehaviour 추가 시 `.cs`, `.meta`, prefab/scene reference를 한 변경 이유로 묶는다.
- `Required` reference는 inspector/serialized reference로 연결한다.
- prefab pool 대상은 `PooledObject`/binding 대상 컴포넌트 유무를 inventory로 확인한다.

Acceptance:

- `npm run --silent unity:asset-hygiene` 통과
- 새 script `.meta` 누락 없음
- prefab/scene의 `m_EditorClassIdentifier`와 serialized reference가 새 class 역할과 일치

### Phase 4: Shared Runtime Debt Follow-Up

목표:

- 남은 shared runtime dynamic creation을 제품 계약으로 바꿀지 판단한다.

후속 후보:

- `SoundPlayerRuntimeHostFactory`: host prefab contract는 도입됨. 남은 검토는 approval/policy evidence와 WebGL audio product smoke로 분리
- `SoundPlayer`: SFX/BGM `AudioSource` runtime 생성이 pool prefab contract로 대체 가능한지 검토
- `RoundedRectGraphic`, `ButtonSoundEmitter`: 실제 사용처가 생기면 serialized reference 또는 editor-time requirement로 정리
- `GameObjectPool`: `PooledObject` dynamic ensure를 prefab contract로 고정할 수 있는 대상부터 migration

Acceptance:

- 후보별로 code-only 정리인지 prefab/scene contract migration인지 분리되어 있다.
- audio/WebGL acceptance는 이 문서가 아니라 Audio 또는 Account/Garage validation lane으로 이관한다.

## Validation Stack

기본 closeout 명령:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1
powershell -ExecutionPolicy Bypass -File .\tools\validate-rules.ps1
npm run --silent rules:lint
npm run --silent unity:asset-hygiene
```

보조 scan:

```powershell
rg -n "class .*Setup|class .*Root|private void Update\(" Assets/Scripts/Features -g '*Setup.cs' -g '*Root.cs'
rg -n "GetComponent<|GetComponentInParent|AddComponent|FindObjectsByType|FindFirstObjectByType" Assets/Scripts/Features Assets/Scripts/Shared -g '*.cs'
```

## Closeout

Success 조건:

- Phase 1~3 acceptance가 모두 충족된다.
- 남은 shared runtime 후보가 Phase 4 residual로 분리되어 있다.
- compile-clean, static-clean, docs-clean, asset-clean이 모두 통과한다.

Residual handling:

- 2-client sync, actual gameplay smoke, WebGL audio 같은 제품 acceptance는 기존 active owner plan으로 이관한다.
- hard-fail lint 신설이 필요하다고 판단되면 이 문서에서 성공 처리하지 않고 별도 rule/tooling owner pass로 연다.

owner impact:

- primary: `plans.technical-debt-recurrence-prevention`
- secondary: `docs.index`, `plans.progress`
- out-of-scope: GameScene Phase 5 acceptance, UI Toolkit migration, Audio/WebGL product validation

doc lifecycle checked:

- 현재는 active plan으로 유지한다.
- Phase 1~3이 반복 closeout 기준으로 정착되고 Phase 4 residual이 다른 owner로 이관되면 reference 압축 보존 또는 삭제 후보로 재검토한다.

- 2026-04-28 실행 후 재리뷰: 과한점은 audio/WebGL acceptance나 UI Toolkit migration을 이 plan 성공 조건으로 끌어오지 않고 residual owner로 분리했다. 부족한점은 SoundPlayer host prefab contract 완료와 남은 AudioSource/template residual을 현재 기준선에 반영해 해소했다.
plan rereview: clean
