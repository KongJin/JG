# Set B / Operation Memory Visual Improvement Plan

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: plans.set-b-operation-memory-visual-improvement
> role: plan
> owner_scope: Set B Garage와 Operation Memory Stitch 화면의 visual 개선, source 후보 판단, UI Toolkit candidate handoff 준비
> upstream: plans.progress, design.game-design, design.ui-foundations, plans.operation-record-world-memory, ops.stitch-data-workflow, ops.unity-ui-authoring-workflow
> artifacts: `artifacts/stitch/`, `Assets/UI/`, `Assets/Scenes/`, `artifacts/unity/`

이 문서는 두 화면의 **디자인 개선과 후보 고정 순서**만 소유한다.

- `Set B Garage`: 이미 존재하는 Garage baseline의 final visual fidelity mismatch를 줄인다.
- `Operation Memory`: 2026-04-27 Stitch에서 생성한 신규 화면을 GameDesign/Set B 기준에 맞게 다듬어 source 후보로 올릴지 판단한다.

런타임 교체, 저장/계정 검증, Firestore/WebGL 검증은 각 owner lane에 남긴다.

## Source Candidates

| surface | current source | current judgment |
|---|---|---|
| Set B Garage | `Tactical Unit Assembly Workspace` / `d440ad9223a24c0d8e746c7236f7ef27` | UI Toolkit pilot은 있으나 active runtime acceptance 전 visual mismatch가 남음 |
| Operation Memory | `Operation Memory` / `a2bac39c798b41098e9ece6f20465881` | visual candidate는 pass, handoff baseline은 CTA/copy/list structure 수정 필요 |

## Improvement Targets

### Set B Garage

- 첫 시선은 계속 `active slot -> part focus -> focused editor -> preview/summary -> save dock`로 읽혀야 한다.
- Unity 후보 capture는 source의 compact dark-panel hierarchy, card framing, icon treatment, border contrast를 보존해야 한다.
- Preview/Summary는 sparse placeholder가 아니라 조립 결과를 평가하는 완성된 카드처럼 보여야 한다.
- Save dock은 화면의 가장 명확한 commit action이어야 하며, page nav나 settings보다 강해야 한다.
- Garage는 수집/상점 화면이 아니라 `조립 작업대`처럼 보여야 한다.

### Operation Memory

- 화면 목적은 reward/leaderboard가 아니라 `한 판 뒤 남은 작전 흔적`이다.
- 하단은 tab bar보다 `RETURN TO LOBBY` fixed action dock이 우선이다.
- `OPEN GARAGE`는 보조 CTA로 두고, Operation Memory를 Garage editor처럼 만들지 않는다.
- 핵심 카피는 GameDesign 기준에 맞춰 Korean-first로 다듬는다.
  - `HELD` -> `버텨냄`
  - `BASE COLLAPSED` -> `거점 붕괴`
  - `W-08` -> `공세 8`
  - `KILLS` -> `제거`
  - `CORE UNITS TRACE` -> `기체 전적`
- 최근 기록은 5개 슬롯 구조가 보이게 하고, 부족한 항목은 finished empty state로 처리한다.

## Execution Plan

1. **Stitch revision**
   - Set B Garage는 accepted baseline을 유지하되, current UI Toolkit capture와 source mismatch 목록을 기준으로 수정안을 낸다.
   - Operation Memory는 같은 프로젝트에서 기존 화면 variant 또는 edit으로 CTA/copy/list structure를 수정한다.
   - 후보가 여러 개 생기면 화면당 하나만 source 후보로 고정한다.

2. **Source freeze readiness**
   - 후보별 `projectId`, `screenId`, `screen.html`, `screen.png`를 확인한다.
   - Operation Memory는 source freeze 전까지 runtime replacement 후보로 부르지 않는다.
   - historical `.stitch/handoff` 문서를 새 handoff 입력으로 늘리지 않는다.

3. **UI Toolkit candidate handoff**
   - source freeze가 잡힌 화면만 UI Toolkit candidate 대상으로 넘긴다.
   - candidate는 `390x844` 모바일 기준으로 capture/report evidence를 남긴다.
   - Set B Garage와 Operation Memory는 각각 별도 surface로 다루고, 한쪽 pass를 다른 쪽 acceptance로 재사용하지 않는다.

4. **Acceptance review**
   - `visual fidelity`: source와 candidate capture가 같은 first-read hierarchy로 보이는가.
   - `CTA fidelity`: Garage는 Save, Operation Memory는 Return To Lobby가 가장 강한가.
   - `GameDesign fit`: 편성/기체/작전 흔적이 보상/랭킹/상점처럼 읽히지 않는가.
   - `translation readiness`: block 구조가 header, primary card, list/editor, summary/trace, dock으로 분리되는가.

## Validation

- Stitch 화면 review: 390x844 screenshot에서 겹침, 잘림, CTA 위계, list density를 확인한다.
- Source freeze review: 후보 화면 하나만 html/png로 고정되어 있는지 확인한다.
- UI Toolkit candidate review: fresh capture와 report를 남기고, runtime 교체와 pilot success를 분리한다.
- Unity 변경이 생긴 경우:
  - `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
  - `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- 문서 변경 검증:
  - `npm run --silent rules:lint`

## Blocked / Residual Handling

- Operation Memory의 current screen은 `visual candidate`일 뿐이며, CTA/copy/list 수정 전에는 accepted source로 닫지 않는다.
- Set B Garage의 UI Toolkit pilot은 runtime Garage acceptance가 아니다.
- WebGL save/load, settings interaction, account sync는 shared `Account/Garage` residual로 유지한다.
- translator capability나 evidence route가 부족하면 해당 화면을 `blocked: capability-expansion-required`로 남기고, capability 변경을 같은 화면 개선 success에 섞지 않는다.
- 기체 전적 태그의 실제 통계 source가 부족하면 fake data를 만들지 않고 `전적 기록 대기중`류 empty state로 둔다.

## Closeout Criteria

- Set B Garage는 source와 fresh candidate capture의 first-read hierarchy, preview completeness, save dock posture가 일치하거나, 명확한 `mismatch`/`blocked` reason으로 닫힌다.
- Operation Memory는 수정된 Stitch 화면이 `RETURN TO LOBBY` fixed dock, Korean-first copy, recent 5 record structure를 만족한다.
- 두 화면 모두 source 후보와 screen identity가 기록된다.
- UI Toolkit candidate로 넘긴 경우 fresh capture/report와 workflow policy evidence가 남는다.
- runtime replacement 여부는 별도 pass로 분리된다.

## Lifecycle

- active 유지 이유: `Set B Garage` visual fidelity residual과 새 `Operation Memory` source 후보 판단이 진행 중이다.
- reference 전환 조건: 두 화면의 source 후보와 visual verdict가 pass, mismatch, 또는 blocked로 닫히고, 남은 runtime/Account/WebGL 항목이 각 owner lane으로 이관된다.
- 전환 시 갱신: 이 문서 header와 `docs.index` 상태 라벨을 함께 `reference`로 맞춘다.

## Plan Rereview

- 과한점 리뷰: 이 문서는 GameDesign, UI Foundations, Stitch workflow 규칙을 새로 정의하지 않고 두 화면의 개선 순서와 acceptance만 소유한다.
- 부족한점 리뷰: owner/scope, source candidates, 화면별 개선 대상, 실행 순서, validation, blocked/residual, closeout, lifecycle을 포함했다.
- 수정 후 재리뷰: existing `garage_ui_ux_improvement_plan.md`의 Set B recovery owner를 대체하지 않고, 두 화면 visual 개선과 source 후보 판단으로 범위를 좁혔다.
- plan rereview: clean
