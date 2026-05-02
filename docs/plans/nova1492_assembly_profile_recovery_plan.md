# Nova1492 Assembly Profile Recovery Plan

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: plans.nova1492-assembly-profile-recovery
> role: plan
> owner_scope: Nova1492 원본 조립 슬롯/형태/위치 규칙을 evidence 기반 assembly profile로 복원하는 실행 순서와 acceptance
> upstream: docs.index, plans.nova1492-content-residual, design.module-data-structure, ops.acceptance-reporting-guardrails
> artifacts: `artifacts/nova1492/assembly-profile/`, `artifacts/unity/garage-humanoid-weapon-current/`

이 문서는 Nova1492 원본의 숨은 조립 규칙을 Unity runtime 하드코딩이 아니라 검수 가능한 데이터 프로파일로 복원하기 위한 실행 owner다.
GX 변환 품질, 형태 분류, Garage preview 위치 보정은 서로 연결되어 있지만, 이 문서의 핵심은 “조립 규칙의 evidence -> profile -> capture -> manual review -> targeted fix” 루프다.

## Current Status

- 2026-05-02: `BuildNovaAssemblyProfile.ps1`로 144개 profile seed, manual review CSV, slot evidence report를 생성했다.
- 2026-05-02: profile metadata를 `NovaPartAlignmentCatalog.asset`에 promotion했고, 인간형은 부품 조립 형태로만 다룬다. 원본 `UnitModel` 증거 전까지 `Disabled`/`blocked` profile로 fail-closed 유지한다.
- 2026-05-02: 폐기된 인간형 offset 후보 capture는 product acceptance 증거로 쓰지 않고, humanoid sample은 원본 `UnitModel` 기준을 먼저 확보한다.
- 2026-05-02: 형태가 맞고 명시적 assembly anchor contract가 있는 조합은 `pending` profile이라도 Garage runtime preview에 표시한다. 단 `direction-only` 자체를 socket truth로 승격하지 않고, explicit anchor contract가 없으면 fail-closed로 남긴다.
- 2026-05-02: 최신 humanoid weapon capture 2건의 combo review CSV를 생성했고, 원본 비교 기준 부족으로 둘 다 `unsure`로 남겼다.
- acceptance: `blocked: manual visual review pending` (combo review has 2 `unsure`; no `match` accepted yet)

## Problem

현재 Garage preview는 `하단(기동) / 중단(프레임) / 상단(무장)` 세 부품을 모두 보여줄 수 있어도, `기동 + 인간형 프레임 + 인간형 무장` 조합군에서 위치가 규칙적으로 맞지 않는다.
인간형 프레임/무장은 XFI에 명시 transform이 거의 없고, 원본 클라이언트 내부의 슬롯명과 slot-specific GX 파일 흔적으로 볼 때 원본 조립은 단순한 mesh center 정렬보다 더 강한 UnitModel 조립 규칙을 사용한다.
`xfi_weapon_direction_only`처럼 방향만 있는 metadata는 장착 contract가 아니며, 이를 bounds 기반 shell 배치로 정상 preview처럼 보여주면 silent fallback이 된다.

이 문제를 조합별 runtime if문으로 막으면 새 조합마다 같은 문제가 반복된다.
목표는 hardcoding에 가까운 원본 규칙을 그대로 코드에 박는 것이 아니라, 근거와 검수 상태가 남는 `assembly profile` 데이터로 끌어내는 것이다.

## Confirmed Evidence

- `C:\Program Files (x86)\Nova1492\Nova1492.exe`에는 `legs`, `body`, `larm`, `rarm`, `top`, `lshd`, `rshd`, `front`, `lshd_addon`, `rshd_addon` 문자열이 남아 있다.
- `C:\Program Files (x86)\Nova1492\datan\common`에는 `_LArm.GX`, `_RArm.GX`, `_Front.GX`, `_Top.GX` 같은 slot-specific 원본 모델 파일이 존재한다.
- `arm32_sppoo.xfi`와 `arm39_hmsk.xfi`는 같은 direction range 형태이며, Unity에서 필요한 최종 조립 위치 transform을 직접 제공하지 않는다.
- `module_data_structure.md` 기준 사용자-facing 조립 구조는 `하단(기동) / 중단(프레임) / 상단(무장)`이며, C# 내부 legacy 타입명은 호환성 때문에 유지한다.

## Working Hypotheses

| 가설 | 맞다면 보여야 하는 증거 | 검증 방법 |
|---|---|---|
| 원본은 형태별 공통 anchor 외에 slot-specific 예외를 가진다 | 특정 형태에서 같은 offset 패턴이 반복된다 | 형태별 contact sheet와 profile diff 비교 |
| 인간형은 독립된 부품 조립 형태다 | 원본 연구소 preview에서 UnitModel 조립 실루엣의 일부로 배치된다 | 원본 연구소 exact combo capture와 Unity capture 비교 |
| XFI direction-only 무장은 조립 transform을 숨기지 않는다 | XFI 값이 방향/각도 범위뿐이고 위치 row가 없다 | XFI parser audit와 원본 파일 비교 |
| 일부 부품 문제는 조립 규칙이 아니라 GX 변환 누락이다 | 단품 capture도 원본 대비 깨져 있거나 block/count가 이상하다 | GX audit manifest와 단품 contact sheet 확인 |

가설은 profile seed를 만들기 위한 출발점일 뿐이다.
원인 확정은 최신 산출물, 테스트, capture 또는 원본 파일 evidence로 확인된 뒤에만 한다.

## Goal

- 원본 evidence를 바탕으로 모든 부품의 조립 형태와 anchor 후보를 데이터화한다.
- Garage preview runtime은 일반 anchor mode만 알고, 부품별 예외는 profile data에서 읽는다.
- profile에는 confidence와 evidence path를 남겨 나중에 “왜 이 위치인가”를 추적할 수 있게 한다.
- visual acceptance는 자동 pass가 아니라 최신 capture와 수동 검수 결과로 닫는다.

## Non-Goals

- 공개 배포 권리/이름 판단은 이 문서가 닫지 않는다. 해당 판단은 `plans.nova1492-content-residual`과 release gate가 맡는다.
- GX geometry 변환기 전체 품질을 이 문서에서 닫지 않는다. 단품이 깨진 부품은 GX audit lane으로 되돌린다.
- 조합별 runtime hardcoding을 늘리지 않는다.
- 전투 규칙, 밸런스, UI 레이아웃 acceptance를 이 문서에서 닫지 않는다.

## Assembly Profile Concept

`assembly profile`은 원본 조립 로직을 다시 코드로 추측하는 대신, 부품별 조립 결정을 명시적으로 기록하는 데이터 계층이다.
Unity runtime은 profile을 읽고 아래 같은 작은 anchor mode만 실행한다.

| Anchor mode | 의미 |
|---|---|
| `LegBodySocket` | 하단 위에 중단을 얹는 기본 기동-프레임 anchor |
| `FrameTopSocket` | 탑형 중단 위에 상단을 얹는 anchor |
| `ShoulderPair` | 어깨형 중단 좌우에 상단 또는 pair part를 붙이는 anchor |
| `FrontAddon` | 전면 보조/센서형 부품 anchor |
| `ManualOffset` | 원본 evidence나 visual review로 확정된 부품별 예외 |
| `Disabled` | 카탈로그에서는 유지하지만 조립 preview에서 제외하거나 후속 조사로 넘기는 상태. 인간형 행은 원본 `UnitModel` evidence 전까지 이 상태를 유지한다 |

## Proposed Schema

초기 산출물은 CSV로 시작하고, Unity 사용 시 ScriptableObject나 generated asset으로 변환한다.
필수 field는 `part_id`, `source_relative_path`, `display_name_ko`, `category`, `assembly_form`, `source_slot_code`, `slot_mode`, `anchor_mode`, `local_offset`, `local_rotation`, `local_scale`, `confidence`, `evidence_path`, `review_result`다.
`mobility_surface`와 `notes`는 필요할 때만 채운다.
`confidence`는 `source | derived | review | manual | blocked`, `review_result`는 `pending | match | mismatch | unsure`로 고정한다.

## Runtime Boundary

- `GxObjConverter`는 geometry/texture/material 변환과 audit를 맡고, 최종 조립 offset의 owner가 아니다.
- profile generator는 catalog, XFI, 원본 filename, manual mapping을 합쳐 후보 profile을 만든다.
- Garage preview assembly는 profile을 읽고 generic anchor mode를 실행한다.
- direction-only XFI는 source placement truth가 아니며, preview는 `FrameTopSocket`, `ShoulderPair`, 원본 evidence 기반 `ManualOffset` 같은 explicit assembly anchor contract만 정상 경로로 사용한다.
- product preview는 형태가 맞고 explicit assembly anchor contract가 있는 조합을 표시한다. fresh capture/manual review 전에는 visual acceptance success로만 승격하지 않는다.
- `pending/review/unsure` source-profile 값은 product visual acceptance가 아니며, 최신 capture/manual review 전에는 acceptance success로 닫지 않는다.
- 단품 자체가 깨져 있으면 assembly profile로 보정하지 않고 GX 변환 owner로 돌린다.

## Workflow

1. Evidence inventory: 원본 slot 문자열, slot-specific 파일, catalog, XFI 분류를 profile seed로 연결한다.
2. Profile seed generation: 사용자-confirmed 형태 mapping을 우선하고, direction-only 인간형 행은 원본 `UnitModel` evidence가 생기기 전까지 `Disabled`/`blocked`로 둔다.
3. Unity data import: CSV profile을 generated asset으로 변환하고, profile fallback 사용 여부를 report나 capture overlay에 남긴다.
4. Capture and contact sheet: 단품과 조립 capture를 분리하고, 조립 capture는 최소 `front`, `side`, `iso`를 생성한다.
5. Manual review: 사용자는 `match | mismatch | unsure`를 기록하고, `mismatch`는 원본 파일, XFI, 단품 capture, 조립 anchor 중 어느 단계 문제인지 다시 분류한다.
6. Rule promotion: 같은 형태/slot 보정이 3개 이상 반복될 때만 form-level rule 후보로 승격하고, 아니면 part-specific profile entry로 유지한다.
7. Expansion: 인간형 무장-프레임부터 닫고, 이후 어깨형 pair, 탑형 top, 하단-중단 순서로 넓힌다.

## Initial High-Risk Queue

초기 queue는 특정 무장 하나가 아니라 `기동 + 인간형 프레임 + 인간형 무장` 조합군 전체의 UnitModel profile이다. 대표 샘플은 coverage를 위한 예시일 뿐이며, acceptance는 조합군 규칙이 확보될 때까지 blocked로 남긴다.
하단 단품 품질 문제는 profile로 덮지 않고 GX audit lane으로 분리한다.

## Mechanical Verification

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\check-compile-errors.ps1`
- profile generator tests: slot/file evidence 연결, direction-only XFI 오판 방지, evidence 없는 `ManualOffset` 생성 금지
- Garage preview direct tests: valid form 조합 profile anchor 적용, invalid form 조합 차단, explicit anchor 없는 direction-only 조합 fail-closed, 인간형 disabled profile 차단 확인
- capture check: 최신 `front`, `side`, `iso` PNG/contact sheet 생성과 overlay의 anchor mode/confidence/review result 표시
- combo review check: 최신 capture sample별 `match | mismatch | unsure` CSV 기록과 next queue 분리

## Acceptance

Mechanical pass:

- assembly profile CSV 또는 generated asset이 만들어진다.
- compile과 targeted direct tests가 통과한다.
- 최신 capture/contact sheet와 part-level manual review CSV, combo review CSV가 생성된다.

Actual acceptance:

- 사용자가 최신 capture 기준으로 `match`를 기록한 조합만 accepted로 본다.
- `mismatch` 또는 `unsure`는 다음 targeted fix queue로 넘긴다.
- 단품 변환이 깨진 부품은 assembly profile acceptance로 닫지 않는다.

Closeout wording:

- 최신 capture 전: `blocked: fresh evidence pending`
- capture는 있으나 수동 검수 전: `blocked: manual visual review pending`
- 수동 검수 결과 불일치: `mismatch`
- 수동 검수 결과 일치: `success`

## Guardrails

- 과거 capture와 최신 capture를 섞어 현재 판정하지 않는다.
- 원본 evidence와 가설을 같은 문장 안에서 root cause처럼 쓰지 않는다.
- 조합별 하드코딩은 profile data로만 표현한다.
- profile data도 evidence 없는 숫자 보정은 `manual` 또는 `review` confidence로 둔다.
- `direction-only` XFI는 normal socket success가 아니다. explicit assembly anchor contract가 있을 때만 붙이고, contract가 없으면 fail-closed로 남긴다.
- `pending`, `review`, `unsure` source-profile 값은 product visual success가 아니다. 깨진 조립을 bounds fallback으로 그럴듯하게 보여주지 않는다.
- GX 변환 문제, 조립 profile 문제, UI preview camera 문제를 한 closeout에서 success로 묶지 않는다.

## Residual

- 원본 클라이언트 내부의 실제 조립 함수는 아직 역추적되지 않았다.
- `assembly_form`, `mobility_surface`, 원본 slot code mapping은 사용자 검수와 파일 evidence를 합쳐 단계적으로 확정해야 한다.
- 하단 일부 단품 품질 문제는 별도 GX audit lane에서 처리해야 한다.

owner impact:

- primary: `plans.nova1492-assembly-profile-recovery`
- secondary: `plans.progress`, `plans.nova1492-content-residual`, `design.module-data-structure`, `ops.acceptance-reporting-guardrails`
- out-of-scope: GX converter geometry fixes, Garage UI layout changes, combat balance, release/legal approval

doc lifecycle checked:

- active 유지. Assembly profile schema, latest capture loop, manual review 결과가 다른 owner로 이관되거나 완료되면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - owner scope, acceptance, fresh evidence guardrail, and residual split checked
