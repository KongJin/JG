# Nova1492 Assembly Profile Recovery Record

> 마지막 업데이트: 2026-05-02
> 상태: historical
> doc_id: plans.nova1492-assembly-profile-recovery
> role: plan
> owner_scope: Nova1492 원본 조립 슬롯/형태/위치 규칙 복원 시도 기록
> upstream: docs.index, plans.nova1492-content-residual, design.module-data-structure, ops.acceptance-reporting-guardrails
> artifacts: none

이 문서는 2026-05-02 조립 위치 복구 시도의 과거 기록이다.
현재 구현 기준이나 다음 작업 owner가 아니다.

## Closeout

- `BuildNovaAssemblyProfile.ps1`는 현재 generated playable catalog와 같은 114개 지원 행만 profile로 생성한다.
- `NovaPartAlignmentCatalog.asset` promotion은 114개 asset entry와 114개 profile row가 정확히 맞을 때만 통과한다.
- direction-only XFI는 source placement truth로 승격하지 않는다.
- shell/bounds 기반 보정이나 unproven adapter로 정상 preview를 흉내 내는 경로는 닫힌다.

## Residual Owner

- Nova1492 content release gate, rights/naming 판단, 모델 후보 handoff는 [`nova1492-content-residual-plan.md`](../active/nova1492-content-residual-plan.md)가 맡는다.
- 새 조립 형태를 다시 제품 범위로 열려면 기존 preview 보정 기록을 재사용하지 말고 `plans.progress`에서 새 owner를 먼저 연다.

## Evidence

- Generated profile: `artifacts/nova1492/assembly-profile/nova_assembly_profile.csv`
- Promotion report: `artifacts/nova1492/assembly-profile/nova_assembly_profile_unity_promotion_report.md`
- EditMode evidence: archived in `artifacts/unity/archive/flat-legacy-20260505.zip`; historical entry `editmode-humanoid-parts-removed-20260502.xml`.
