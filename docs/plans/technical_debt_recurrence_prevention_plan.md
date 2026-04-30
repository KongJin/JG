# Technical Debt Recurrence Prevention Plan

> 마지막 업데이트: 2026-04-30
> 상태: reference
> doc_id: plans.technical-debt-recurrence-prevention
> role: plan
> owner_scope: Setup/Root drift, runtime lookup, dynamic repair 재발 방지 reference gate
> upstream: ops.cohesion-coupling-policy, ops.document-management-workflow
> artifacts: none

이 문서는 이미 발생한 GameScene/BattleScene technical debt가 반복되지 않게 하는 reference gate만 소유한다.
제품 acceptance와 현재 실행 순서는 해당 active owner plan과 `plans.progress`를 우선한다.

## Baseline

- `DefaultPlayerSpecProvider`, non-master `EnemySetup`, `SoundPlayerRuntimeHostFactory`의 runtime `Resources.Load` dependency repair는 serialized/scene-owned contract로 옮겼다.
- `SoundPlayer` AudioSource/template residual은 WebGL audio product smoke가 아니라 runtime contract debt로 추적한다.
- `PlayerHealthHudView`, `EnemyHealthBar`, `DamageNumber`는 `Assets/Prefabs/RuntimeFeedback/` compatibility surface로 분리했다.
- Runtime lookup / dynamic repair 위반은 `tools/validate-rules.ps1` 기준 0이어야 한다.
- `compile-clean`, `rules-clean`, `asset-clean`은 각각 Unity compile check, `npm run --silent rules:lint`, `npm run --silent unity:asset-hygiene`로 확인한다.

## Reference Gate

| Gate | Verdict | Handling |
|---|---|---|
| Setup/Root drift | reference guard | 새 runtime object repair는 serialized reference 또는 owner setup으로 고정 |
| Runtime lookup containment | reference guard | feature code raw lookup은 `ComponentAccess`, registry, serialized reference 등 드러난 seam만 허용 |
| Prefab/scene contract | reference guard | Resources fallback을 dependency repair로 확장하지 않음 |
| Actual acceptance split | reference guard | GameScene, Account/Garage, UI Toolkit candidate evidence를 서로 다른 success로 묶지 않음 |

## Recurrence Risks

| Risk | Severity | Prevention |
|---|---|---|
| scene/prefab reference 누락을 runtime `AddComponent`나 lookup으로 복구 | High | scene/prefab contract 또는 setup owner에 serialized reference로 연결 |
| single-client smoke를 multiplayer success로 확장 | High | Phase 5는 2-client evidence 전까지 blocked/residual 유지 |
| UI Toolkit preview를 runtime replacement success로 확장 | Medium | candidate preview, runtime visibility, WebGL/product acceptance를 분리 |
| generated asset inventory를 product/balance approval로 오해 | Medium | Nova generated assets와 gameplay/content 승격 판단을 content owner lane으로 분리 |

## Validation

- `tools/check-compile-errors.ps1`
- `tools/validate-rules.ps1`
- `npm run --silent rules:lint`
- `npm run --silent unity:asset-hygiene`
- 관련 runtime owner plan의 smoke evidence

## Residual

- GameScene direct EditMode execution과 Phase 5 2-client smoke는 각각의 GameScene active owner lane이 소유한다.
- Account/Garage/WebGL product acceptance는 account WebGL owner lane이 소유하고, 실행 절차는 WebGL smoke checklist를 따른다.
- `SoundPlayer` AudioSource/template residual은 runtime contract guard 기준으로 보되, 실행이 필요하면 새 runtime owner pass 또는 해당 feature owner로 연다.

owner impact:

- primary: `plans.technical-debt-recurrence-prevention`
- secondary: `plans.progress`
- out-of-scope: 새 hard-fail lint, code/API 변경, Unity scene/prefab mutation

doc lifecycle checked:

- reference 유지. 새 runtime repair 재발이 실제 작업으로 열리면 해당 feature/runtime owner plan 또는 session checklist로 다시 판단한다.
- plan rereview: clean
