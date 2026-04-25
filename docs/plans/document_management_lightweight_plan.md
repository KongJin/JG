# Document Management Lightweight Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.document-management-lightweight
> role: plan
> owner_scope: 문서관리 절차를 더 가볍게 만들기 위한 후속 정리 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `docs/ops/document_management_workflow.md`, `docs/ops/plan_authoring_review_workflow.md`

이 문서는 문서관리 규칙을 더 늘리지 않고, 실제 작업자가 매번 보는 판단 기준을 줄인 후속 계획의 실행 기록이다.
규칙 본문은 `ops.document-management-workflow`와 `ops.plan-authoring-review-workflow`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

## Goal

- 빠른 체크를 실제로 5~6개 수준으로 압축한다.
- `큰 문서 작업`과 `작은 문서 작업`의 차이를 짧게 정의한다.
- 새 문서를 만들기 전에 기존 owner, `progress.md`, reference 갱신으로 충분한지 먼저 판단하게 한다.
- 복잡한 작업용 기준을 모든 문서 작업의 기본 부담으로 만들지 않는다.

## Scope

- primary owner: `ops.document-management-workflow`
- secondary owner: `ops.plan-authoring-review-workflow`
- registry owner: `docs.index`

이 plan이 직접 다루는 것:

- `빠른 체크` 문항 압축
- `큰 문서 작업` 정의 추가
- 새 문서 생성 전 판단 순서 추가
- plan 재리뷰 기준의 기본/복잡 작업 구분
- 전역 skill 변경 확인 문구의 최소화

범위 밖:

- 새 lint rule 추가
- 새 artifact 추가
- status/role 체계 변경
- 기존 reference/historical 문서 재분류
- product, Unity, Stitch lane 진행 상태 변경

## Problem 기록

- `document_management_workflow.md`는 "다섯 가지만 본다"고 말했지만, 실제 빠른 체크는 11개였다.
- `doc lifecycle checked`는 유용하지만, 언제 필요한지 기준이 모호했다.
- 문서를 만든 뒤 관리하는 기준은 강했지만, 문서를 만들지 않는 기준은 약했다.
- `plan_authoring_review_workflow.md`의 일부 기준은 복잡한 blocked/capability 작업에는 필요하지만, 일반 계획 작업에는 무겁게 읽힐 수 있었다.
- 전역 skill 변경은 repo lint가 직접 보장하지 못하므로, 확인 사실을 closeout에 남기는 약한 기준이면 충분하다고 판단했다.

## Target Shape

`document_management_workflow.md`의 빠른 체크는 아래 수준으로 줄인다.

1. 역할이 하나인가?
2. owner가 맞는가?
3. `docs/index.md`로 찾을 수 있는가?
4. 같은 사실을 다른 문서가 장문으로 소유하지 않는가?
5. closeout에 기준, 증거, 남은 리스크가 맞게 붙는가?
6. 큰 문서 작업이면 owner impact와 lifecycle을 확인했는가?

상세 기준은 빠른 체크 밖으로 내려서, 필요할 때만 읽게 한다.

## Big Document Work

`큰 문서 작업` 후보:

- 새 문서를 만든다.
- 문서 3개 이상을 같은 이유로 수정한다.
- active/reference/historical/draft 상태를 바꾼다.
- owner 문서, repo-local skill, 전역 skill trigger를 바꾼다.
- 문서를 삭제, 리네임, 이동한다.

작은 작업 후보:

- 단일 문서의 오탈자, 링크, 표현만 고친다.
- reference/historical 문서를 보존 목적으로 짧게 정리한다.
- 상태나 진행률을 한두 줄 갱신한다.

큰 문서 작업이면 closeout에 `owner impact`와 `doc lifecycle checked`를 남긴다.
작은 작업이면 기본 lint와 짧은 변경 요약으로 충분하게 둔다.

## New Document Gate

새 문서를 만들기 전에 아래 순서로 판단한다.

1. `plans.progress` 한 줄 갱신으로 충분한가?
2. 기존 owner 문서의 짧은 섹션으로 충분한가?
3. 기존 reference 문서 갱신으로 충분한가?
4. 실행 순서, acceptance, residual handling이 따로 필요할 때만 새 plan을 만든다.

## Phases

### Phase 1 - 빠른 체크 압축

상태: 완료

- `document_management_workflow.md`의 빠른 체크를 5~6개로 줄인다.
- 기존 11개 문항 중 closeout, instruction fit, old trace 관련 세부 기준은 상위 원칙이나 상세 섹션으로 내린다.
- "다섯 가지만 본다" 문구와 실제 문항 수를 맞춘다.

결과:

- 빠른 체크를 6개로 줄였다.
- 시작 범위, `owner impact`, `doc lifecycle checked`, lane 변경, old trace 관련 문항은 상위 원칙과 작업 크기 판단으로 이동했다.

### Phase 2 - 큰 문서 작업 기준 추가

상태: 완료

- `큰 문서 작업` 후보와 제외 후보를 `document_management_workflow.md`에 짧게 추가한다.
- `doc lifecycle checked`를 모든 작업의 필수 부담이 아니라 큰 문서 작업의 closeout 기준으로 둔다.

결과:

- 새 문서 생성, 3개 이상 문서 수정, status 변경, owner/skill trigger 변경, 삭제/리네임/이동을 큰 문서 작업 후보로 정리했다.
- 단일 문서 오탈자/링크/표현 수정, reference/historical의 짧은 보존 정리, 한두 줄 상태 갱신은 작은 작업으로 분리했다.

### Phase 3 - 새 문서 생성 gate 추가

상태: 완료

- 새 문서 생성 전 `progress.md`, 기존 owner, 기존 reference 갱신으로 충분한지 확인하는 순서를 추가한다.
- 새 plan은 실행 순서, acceptance, residual handling이 따로 필요할 때만 만든다는 기준을 남긴다.

결과:

- 새 문서 생성 전 `plans.progress`, 기존 owner 문서, 기존 reference 문서를 먼저 확인하는 순서를 추가했다.
- 별도 실행 순서, acceptance, residual handling이 필요할 때만 새 plan을 만들도록 정리했다.

### Phase 4 - plan 재리뷰 기준 경량화

상태: 완료

- `plan_authoring_review_workflow.md`의 기본 체크는 owner, scope, acceptance, 검증, residual 중심으로 유지한다.
- capability expansion, lane 재선언, blocked 처리 기준은 복잡한/blocked 작업에서만 보는 기준으로 낮춘다.

결과:

- 기본 재리뷰 기준은 owner, scope, acceptance, 실행 순서, 검증, residual 중심으로 유지했다.
- blocked/unsupported success 전환 방지와 capability expansion 기준은 복잡하거나 blocked/capability 작업일 때만 추가로 보게 분리했다.

### Phase 5 - 검증과 재리뷰

상태: 완료

- 새 hard-fail, 새 artifact, 새 script가 생기지 않았는지 확인한다.
- `npm run --silent rules:sync-closeout`
- `npm run --silent rules:lint`
- closeout에는 owner impact, doc lifecycle checked, residual 여부를 남긴다.

결과:

- 새 hard-fail, 새 artifact, 새 script를 추가하지 않았다.
- 검증 결과는 closeout에 남긴다.

## Acceptance

- 빠른 체크가 실제로 5~6개다.
- `큰 문서 작업`과 작은 문서 작업의 차이가 보인다.
- 새 문서 생성 전 판단 순서가 보인다.
- 복잡한 lane 기준이 일반 계획 작업의 기본 부담처럼 읽히지 않는다.
- 새 hard-fail, 새 artifact, 새 script를 추가하지 않는다.
- rules lint가 통과한다.

## Risks

- 과하게 줄이면 closeout 안전장치가 약해질 수 있다.
- 큰 작업 기준을 너무 넓게 잡으면 다시 모든 문서 작업이 무거워질 수 있다.
- 새 문서 생성 gate를 너무 강하게 쓰면 필요한 plan까지 만들기 어려워질 수 있다.

## Authoring Review

- 과한점: 새 lint, artifact, script를 만들지 않고 owner 문서의 기존 문구를 압축하는 범위로 끝냈다.
- 부족한점: 전역 skill trigger 자체는 바꾸지 않았다. 이번 작업은 repo owner 문서의 기본 판단 부담을 낮추는 데 한정했다.
- doc lifecycle checked: 새 plan은 완료 후 reference로 전환했고, active/reference/historical/delete 후보 중 즉시 삭제할 문서는 없었다.

## Closeout

- status: completed
- owner impact: primary `ops.document-management-workflow`; secondary `ops.plan-authoring-review-workflow`; registry `docs.index`
- out-of-scope: 새 lint rule, 새 artifact, 새 script, 기존 reference/historical 문서 재분류
- doc lifecycle checked: completed plan reference 전환, 새 삭제 후보 없음
- residual: none
- plan rereview: clean
