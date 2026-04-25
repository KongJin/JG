# Document Management Follow-up Tuning Plan

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.document-management-followup-tuning
> role: plan
> owner_scope: 문서관리 경량화 이후 남은 작은 과함/부족함을 보정하는 실행 계획
> upstream: docs.index, ops.document-management-workflow, ops.plan-authoring-review-workflow
> artifacts: `docs/ops/document_management_workflow.md`, `docs/ops/plan_authoring_review_workflow.md`

이 문서는 문서관리 경량화 이후 남은 작은 마찰을 줄인 후속 실행 기록이다.
규칙 본문은 `ops.document-management-workflow`와 `ops.plan-authoring-review-workflow`가 소유하고, 이 문서는 실행 순서와 acceptance만 가진다.

## Goal

- 새 문서를 만들지 않기로 한 판단을 어디에 남길지 정한다.
- 전역 skill trigger 변경은 repo lint 밖 확인 대상이라는 점을 짧게 드러낸다.
- `owner impact`와 `doc lifecycle checked` 용어 장벽을 낮춘다.

## Scope

- primary owner: `ops.document-management-workflow`
- secondary owner: `ops.plan-authoring-review-workflow`
- registry owner: `docs.index`

이 plan이 직접 다루는 것:

- 새 문서 생성 gate의 결과 기록 기준
- 전역 skill 변경 확인 기준의 짧은 보강
- 빠른 체크 용어의 괄호 설명 또는 짧은 풀이
- 작은 plan 수정에서 owner/scope 확인 부담을 낮추는 문구 조정

범위 밖:

- 새 hard-fail lint 추가
- 새 artifact 추가
- status/role 체계 변경
- 기존 문서 재분류
- 전역 skill 파일 직접 수정

## Gap 기록

- `새 문서 생성 기준`은 있었지만, 새 문서를 만들지 않기로 한 결론을 최종 응답에만 남겨도 되는지 문서에 남겨야 하는지 애매했다.
- `전역 skill trigger`는 큰 문서 작업 후보에 들어가 있었지만, repo lint가 전역 skill 파일을 직접 보장하지 못한다는 점이 owner 문서에서 약하게만 보였다.
- 빠른 체크의 `owner impact`와 `doc lifecycle checked`는 유용하지만 처음 보는 사람에게는 용어 장벽이 있었다.
- `plan_authoring_review_workflow.md`의 핵심 루프는 작은 plan 보정에도 primary/secondary owner와 제외 범위를 요구하는 것처럼 읽힐 수 있었다.

## Target Shape

- 새 문서를 만들지 않기로 한 판단은 보통 최종 응답이나 작업 요약에 남기고, 현재 상태나 우선순위가 바뀔 때만 owner 문서나 `plans.progress`에 남긴다.
- 전역 skill을 바꾸면 repo lint와 별도로 실제 파일 변경 여부를 확인하고, closeout에 `skill trigger checked`를 남긴다.
- 빠른 체크의 마지막 문항은 `owner impact`, `doc lifecycle checked` 뒤에 짧은 괄호 풀이를 붙인다.
- 작은 plan 보정은 owner/scope를 간단히 확인하고, 큰 plan이나 여러 owner 작업에서만 primary/secondary/out-of-scope를 명시하게 한다.

## Phases

### Phase 1 - 새 문서 미생성 판단 기록 기준

상태: 완료

- `document_management_workflow.md`의 새 문서 생성 기준 아래에 결과 기록 기준을 추가한다.
- 기본값은 최종 응답이나 작업 요약 기록으로 둔다.
- 현재 상태, 우선순위, owner 이동이 바뀌는 경우에만 문서 본문에 남긴다.

결과:

- 새 문서를 만들지 않기로 한 판단은 보통 최종 응답이나 작업 요약에 남기도록 했다.
- 현재 상태, 우선순위, owner 이동이 바뀌는 경우에만 `plans.progress`나 owner 문서에 남기도록 했다.

### Phase 2 - 전역 skill 확인 기준 보강

상태: 완료

- 전역 skill trigger 변경은 repo-local lint가 직접 보장하지 않는다는 점을 짧게 추가한다.
- 전역 skill을 바꾼 경우 closeout에 `skill trigger checked`를 남긴다는 기존 기준을 유지한다.
- 새 자동화나 lint는 만들지 않는다.

결과:

- 전역 skill trigger 변경은 repo lint가 직접 보장하지 않으므로 실제 파일 변경 여부를 별도로 확인하도록 했다.
- 전역 skill을 바꾼 경우 closeout에 `skill trigger checked`를 남기는 기준을 유지했다.
- 새 자동화나 lint는 만들지 않았다.

### Phase 3 - 빠른 체크 용어 풀이

상태: 완료

- `owner impact`는 여러 owner가 흔들렸는지 확인하는 표현임을 괄호로 붙인다.
- `doc lifecycle checked`는 active/reference/historical/delete 후보를 확인했다는 표현임을 괄호로 붙인다.
- 빠른 체크 문항 수는 6개를 유지한다.

결과:

- 빠른 체크의 `owner impact`에 여러 owner 영향이라는 짧은 풀이를 붙였다.
- `doc lifecycle checked`에 상태/삭제 후보 확인이라는 짧은 풀이를 붙였다.
- 빠른 체크 문항 수는 6개를 유지했다.

### Phase 4 - 작은 plan 보정 문구 완화

상태: 완료

- `plan_authoring_review_workflow.md`의 핵심 루프를 작은 수정과 큰 작업으로 구분해 읽히게 한다.
- 작은 plan 보정은 owner/scope 확인으로 충분하게 둔다.
- 여러 owner나 큰 문서 작업에서만 primary/secondary owner와 제외 범위를 명시한다.

결과:

- 작은 수정은 owner/scope 확인으로 충분하게 읽히도록 핵심 루프를 바꿨다.
- 큰 문서 작업이나 여러 owner 작업에서만 변경 이유, primary/secondary owner, 제외 범위를 확인하도록 했다.
- 부족한점 체크도 같은 기준으로 완화했다.

### Phase 5 - 검증과 정리

상태: 완료

- 새 hard-fail, 새 artifact, 새 script가 없는지 확인한다.
- `npm run --silent rules:sync-closeout`
- `npm run --silent rules:lint`
- 완료 시 이 plan을 `reference`로 전환하고 `docs.index` 상태 라벨을 맞춘다.

결과:

- 새 hard-fail, 새 artifact, 새 script를 추가하지 않았다.
- 이 plan을 `reference`로 전환했고, `docs.index` 상태 라벨을 맞췄다.
- 검증 결과는 closeout에 남긴다.

## Acceptance

- 새 문서를 만들지 않는 판단의 기록 위치가 보인다.
- 전역 skill 변경이 repo lint 밖 확인 대상임이 짧게 보인다.
- `owner impact`와 `doc lifecycle checked`가 처음 보는 사람에게도 덜 낯설게 읽힌다.
- 작은 plan 보정이 큰 plan 작업처럼 무겁게 읽히지 않는다.
- 새 hard-fail, 새 artifact, 새 script를 추가하지 않는다.
- rules lint가 통과한다.

## Risks

- 기록 기준을 너무 약하게 두면 새 문서를 만들지 않은 이유가 사라질 수 있다.
- 전역 skill 확인을 너무 강하게 쓰면 repo 밖 파일까지 매번 검증하려는 부담이 생길 수 있다.
- 용어 풀이가 길어지면 빠른 체크가 다시 무거워질 수 있다.

## Authoring Review

- 과한점: 세 가지 작은 보강만 다뤘고, 새 lint나 artifact를 만들지 않았다.
- 부족한점: 전역 skill 파일 직접 수정은 범위 밖으로 유지했다.
- doc lifecycle checked: completed plan reference 전환, 새 삭제 후보 없음.

## Closeout

- status: completed
- owner impact: primary `ops.document-management-workflow`; secondary `ops.plan-authoring-review-workflow`; registry `docs.index`
- out-of-scope: 새 hard-fail lint, 새 artifact, 새 script, 전역 skill 파일 직접 수정
- doc lifecycle checked: completed plan reference 전환, 새 삭제 후보 없음
- skill trigger checked: not changed
- residual: none
- plan rereview: clean
