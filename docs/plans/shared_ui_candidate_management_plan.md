# Shared UI Candidate Management Plan

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: plans.shared-ui-candidate-management
> role: plan
> owner_scope: Stitch 공용 UI 후보의 owner 분리, source 후보 판단, UI Toolkit handoff 준비
> upstream: plans.progress, design.ui-foundations, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/stitch/`, `Assets/UI/`, `artifacts/unity/`

이 문서는 JG 공용 UI 후보를 큰 한 장짜리 board가 아니라 작은 source 후보 단위로 관리하기 위한 실행 계획이다.
공용 UI의 디자인 토큰과 런타임 변환 원칙은 `design.ui-foundations`, Stitch source freeze와 handoff 절차는 `ops.stitch-data-workflow`가 계속 소유한다.

## Source Candidates

| role | Stitch screen | screen id | judgment |
|---|---|---|---|
| reference board | `JG Shared UI Kit / Navigation Shell` | `333995bda474421ab1c3e2a0214ab15c` | 한 화면에 shell, nav, CTA 예시, feedback, components가 섞여 reference candidate로만 둔다 |
| shell/navigation | `JG Shared Shell / Navigation` | `bc2df08502bb4211ae740da1b2a78fbc` | 공용 top shell과 shared NavigationBar 후보 |
| feedback/status | `JG Shared Feedback / Status` | `998e5bdf3a734f3d873a3d90f19bd6a8` | 상태 chip, toast, dialog 후보 |
| components/controls | `JG Shared Components / Controls` | `660a91efb63346a1a68e5612ca1c1608` | atom/molecule controls 후보 |

## Owner Boundaries

- `Shared Shell / Navigation`은 top shell과 shared NavigationBar만 소유한다.
- shared NavigationBar의 selected state는 command blue를 사용하고, signal orange는 쓰지 않는다.
- `Shared Feedback / Status`는 save/sync/connect/error 계열 상태 표현만 소유한다.
- `Shared Components / Controls`는 button, icon button, stat chip, section card, slot card, empty/disabled/loading state 같은 atom/molecule 후보만 소유한다.
- page-owned UI는 공용 UI source에 올리지 않는다.
  - Garage: `저장 및 배치`, Save Dock, part focus/editor workspace
  - Operation Memory: `RETURN TO LOBBY`, latest result, recent operations, unit trace
  - Lobby: room list, create room, garage summary

## Execution Plan

1. **Candidate selection**
   - 기존 큰 shared board는 삭제하지 않고 reference candidate로 둔다.
   - 실제 source 후보는 split 3화면을 기준으로 review한다.
   - 후보별로 하나의 변경 이유만 남는지 먼저 확인한다.

2. **Source freeze readiness**
   - accepted 후보가 정해지면 화면별 `projectId`, `screenId`, `screen.html`, `screen.png`를 하나씩 고정한다.
   - 여러 후보 화면을 섞어 하나의 handoff input처럼 쓰지 않는다.
   - 새 per-surface manifest/map/presentation file을 active input으로 늘리지 않는다.

3. **UI Toolkit candidate handoff**
   - 공용 UI는 이후 `Assets/UI/UIToolkit/Shared/` 계열 candidate surface로 분리한다.
   - runtime replacement나 scene wiring은 별도 pass에서 판단한다.
   - Garage와 Operation Memory는 shared shell 후보를 참고하되, page CTA는 각 화면이 계속 소유한다.

## Validation

- Stitch review:
  - NavigationBar selected state에 orange가 없어야 한다.
  - Shell 화면은 page-owned primary CTA보다 조용해야 한다.
  - Feedback 화면은 generic web/SaaS alert kit처럼 보이지 않아야 한다.
  - Components 화면은 Garage/Operation Memory에 재사용 가능한 작은 단위로 보여야 한다.
- UI Toolkit handoff review:
  - source freeze artifact가 화면별로 분리되어 있어야 한다.
  - candidate surface capture/report는 `390x844` 모바일 기준으로 확인한다.
  - runtime replacement success와 candidate surface readiness를 섞지 않는다.
- 문서 변경 검증:
  - `npm run --silent rules:lint`

## Residual Handling

- split 3화면 중 하나라도 owner가 커지면 해당 화면만 다시 나누고, 나머지 후보 판단은 유지한다.
- feedback/status가 account settings나 page CTA를 흡수하려 하면 page owner로 돌린다.
- components 후보가 organism/block/surface 레벨로 커지면 `.stitch/contracts/components/shared-ui.component-catalog.json`에 추가하지 않고 screen manifest block으로 남긴다.
- translator capability나 review route가 부족하면 `blocked: capability-expansion-required`로 분리하고, 공용 UI 후보 closeout success에 섞지 않는다.

## Closeout Criteria

- reference board와 split 3화면의 역할 차이가 문서와 `docs.index`에서 확인된다.
- shared NavigationBar의 blue selected rule과 orange page CTA boundary가 `design.ui-foundations`에 짧게 연결된다.
- Garage와 Operation Memory 후속 개선은 shared shell만 참고하고 page-owned CTA를 공용 UI에 편입하지 않는다.
- source freeze로 넘어갈 때 화면별 artifact가 하나씩 고정되거나, 명확한 blocked/residual reason이 남는다.

## Lifecycle

- active 유지 이유: split shared UI 후보를 accepted source로 볼지와 UI Toolkit candidate handoff 준비가 아직 진행 중이다.
- reference 전환 조건: split 3화면이 pass, mismatch, 또는 blocked로 닫히고, runtime replacement와 page-specific CTA 작업이 각 owner lane으로 이관된다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

## Plan Rereview

- 과한점 리뷰: 이 문서는 `ui_foundations`나 Stitch workflow 규칙을 재정의하지 않고, split source 후보의 관리 순서와 owner boundary만 소유한다.
- 부족한점 리뷰: source candidates, reference candidate, 제외 범위, 실행 순서, validation, residual, closeout, lifecycle을 포함했다.
- 수정 후 재리뷰: 새 schema, hard-fail, runtime replacement, per-surface active contract file을 만들지 않는 것으로 범위를 줄였다.
- owner impact: primary `plans.shared-ui-candidate-management`, secondary `design.ui-foundations` note와 `plans.progress` registry, out-of-scope runtime replacement.
- doc lifecycle checked: 새 active plan으로 등록하며 기존 큰 shared board는 historical/delete가 아니라 reference candidate로 유지한다.
- plan rereview: clean
