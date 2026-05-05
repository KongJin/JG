# Nova1492 Content Residual Plan

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: plans.nova1492-content-residual
> role: plan
> owner_scope: Nova1492 generated content residual routing, rights/naming release gate, owner handoff for balance/UI/model follow-up
> upstream: design.world-design, design.module-data-structure, design.ui-foundations
> artifacts: `artifacts/nova1492/`

이 문서는 Nova1492 기반 generated content 후속 판단의 routing owner다.
세계관/권리 경계는 [`../../owners/design/world_design.md`](../../owners/design/world_design.md)가, 실제 유닛/모듈 데이터 구조는 [`../../owners/design/module_data_structure.md`](../../owners/design/module_data_structure.md)가 소유한다.

## Current Judgment

- UnitParts playable 승격 자체는 닫혔지만, 공개 배포 권리/이름 판단은 release gate로 남아 있다.
- Balance, Lobby decoration, BattleScene model replacement는 같은 Nova1492 출처에서 나온 residual이지만, 이 문서가 각 product success를 직접 소유하지 않는다.
- Generated asset inventory나 prefab preview evidence를 공개 배포 권리 승인이나 gameplay balance approval로 확장하지 않는다.

## Decision Routes

| Item | Decision owner | This plan owns |
|---|---|---|
| Rights/naming | `design.world-design` plus release/legal decision outside repo docs | keep `blocked: rights-or-naming-unresolved` until owner decision exists |
| Balance promotion | `design.module-data-structure` and gameplay balance owner | route generated part candidates; do not claim balance success here |
| Lobby decoration | `design.ui-foundations` and UI replacement owner | route decoration candidates; do not claim UI/layout success here |
| BattleScene model replacement | scene/prefab runtime owner | route model candidates; do not claim runtime readability success here |

## Execution Rule

- Use `artifacts/nova1492/current-index.json` as the current evidence entrypoint. Historical bulk source inventory and staging reports live under `artifacts/nova1492/archive/` and are not current acceptance evidence unless explicitly reopened.
- Generated asset presence, prefab creation, preview screenshot은 mechanical evidence일 뿐 product/content acceptance가 아니다.
- 권리 또는 이름 판단이 불명확하면 `blocked: rights-or-naming-unresolved`로 남긴다.
- Balance, UI decoration, model replacement 실행은 해당 owner로 넘기고, 이 문서에서는 handoff 상태만 남긴다.

## Release Gate Checklist

| Gate | Verdict | Required owner/evidence |
|---|---|---|
| Rights/naming | blocked: rights-or-naming-unresolved | release/legal decision or explicit rename policy |
| Manual visual audit | blocked | arm/body/leg comparison record against source-derived expectations |
| Balance handoff | blocked | balance owner accepts candidate IDs and stat review scope |
| UI/model handoff | blocked | UI/runtime owner accepts replacement candidate scope |

## HITL Gate

- Decision owner: rights/naming은 repo 밖 release/legal 판단 또는 explicit rename policy가 필요하다.
- Next action: rights/naming verdict를 먼저 확정하고, 그 결과에 따라 balance/UI/model 후보를 각 owner가 받을지 정한다.
- Stale handling: release/legal 판단을 이번 pass에서 진행하지 않기로 하면 이 plan을 계속 active TODO로 늘리지 말고, `blocked: rights-or-naming-unresolved` residual만 `plans.progress` 또는 `design.world-design`에 짧게 이관한 뒤 reference 압축 후보로 본다.

## Residual

- Rights/naming release gate가 남아 있다.
- Balance, Lobby decoration, BattleScene model replacement 후보는 owner handoff가 남아 있다.

owner impact:

- primary: `plans.nova1492-content-residual`
- secondary: `plans.progress`, `design.world-design`, `design.module-data-structure`, `design.ui-foundations`
- out-of-scope: new asset conversion tooling, gameplay code changes, UI route policy changes, legal approval proof

doc lifecycle checked:

- active 유지. Rights/naming gate가 닫히고 balance/UI/model 후보가 각 owner로 이관되면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - rights/naming gate and owner handoff residual checked
