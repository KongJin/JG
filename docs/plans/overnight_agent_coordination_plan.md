# Overnight Agent Coordination Plan

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.overnight-agent-coordination
> role: plan
> owner_scope: 수면 중 여러 Codex 에이전트가 JG 레포에서 충돌 없이 작업을 고르고, blocked를 남기고, Unity MCP 단일 writer를 유지하는 실행 순서
> upstream: plans.progress, ops.document-management-workflow, ops.plan-authoring-review-workflow, ops.acceptance-reporting-guardrails, tools.unity-mcp-readme
> artifacts: none
>
> 생성일: 2026-04-26
> 근거: 여러 열린 에이전트 세션이 다음 작업 탐색, 정리 계획 작성, 계획 실행 루프를 돌 때 Unity MCP와 전역 문서 충돌을 줄이기 위한 운영 계획

이 문서는 overnight multi-agent 작업의 실행 순서만 다룬다.
문서 운영 기준은 [`document_management_workflow.md`](../ops/document_management_workflow.md)가 맡고, plan 작성 재리뷰는 [`plan_authoring_review_workflow.md`](../ops/plan_authoring_review_workflow.md)가 맡는다.
Unity MCP 실행 세부는 [`../../tools/unity-mcp/README.md`](../../tools/unity-mcp/README.md)가 맡는다.

## 현재 리스크

- Unity MCP는 Unity Editor, active scene, Play Mode, Prefab Mode, compile state, console log를 공유하는 전역 writer다.
- 여러 에이전트가 동시에 MCP mutation을 실행하면 scene/prefab save, Play Mode, screenshot/capture, smoke 결과가 서로 섞일 수 있다.
- `progress.md`, `docs/index.md`, scene/prefab YAML, generated evidence artifact는 여러 세션이 동시에 수정하면 충돌 비용이 크다.
- blocked 작업을 억지로 우회하면 mechanical pass를 acceptance success처럼 보고할 위험이 있다.

## 목표

- 수면 중에도 에이전트가 작은 작업을 계속 진행하되, shared state writer는 한 명으로 제한한다.
- 막힌 작업은 성공으로 포장하지 않고 `blocked`, `mismatch`, `residual`로 분리한다.
- 정리/간소화 plan은 owner 문서의 규칙 본문을 재정의하지 않고 실행 순서와 acceptance만 담는다.
- 아침에 사람이 봤을 때 변경 파일, 검증 결과, 남은 판단이 빠르게 보이게 한다.

## 제외 범위

- 자동 커밋, 자동 브랜치 정리, 원격 push
- Firebase Console, 배포, 외부 서비스 설정 변경
- 아키텍처 방향 변경 또는 새 hard-fail 규칙 추가
- scene/prefab 대수정 병렬 실행
- `progress.md`를 여러 에이전트가 동시에 수정하는 운영

## 운영 모델

### MCP Captain

한 번에 한 세션만 MCP Captain이 된다.
MCP Captain만 아래 작업을 수행한다.

- Unity MCP mutation route 사용
- scene open/save
- prefab stage open/set/save
- Play Mode start/stop
- UI invoke
- sceneview capture
- Unity 상태를 바꾸는 smoke script 실행

MCP Captain은 작업 전 `git status`, compile preflight, `/health` 상태를 확인하고, 작업 후 save, re-read, console/capture evidence를 남긴다.
Unity가 열어 둔 scene을 disk에서 직접 덮어쓰지 않는다.

### Non-MCP Worker

다른 에이전트는 non-MCP worker로 둔다.
기본 허용 범위는 아래로 제한한다.

- 문서와 코드 read/search
- 자기에게 배정된 작은 C# 또는 docs 변경
- 정적 분석, lint, compile preflight
- plan 초안 또는 blocked note 작성
- MCP Captain에게 넘길 request 작성

Non-MCP worker는 `.unity`, `.prefab` 직접 수정, Unity MCP mutation, Play Mode 제어, `progress.md` 동시 수정을 하지 않는다.

### 정리 세션

마지막 정리 세션 또는 아침 수동 세션이 전역 문서 정합성을 본다.
각 worker가 남긴 결과를 바탕으로 필요할 때만 `progress.md`와 `docs/index.md`를 갱신한다.

## MCP Request Template

Non-MCP worker가 Unity 상태 변경이 필요하다고 판단하면 직접 실행하지 않고 아래 형태로 넘긴다.

```md
MCP request:
- target:
- reason:
- expected route:
- files already changed:
- validation needed:
- risk:
```

MCP Captain은 request를 하나씩 처리한다.
요청이 capability expansion, 새 prefab policy 승인, 제품 UX 판단을 필요로 하면 실행하지 않고 `blocked`로 남긴다.

## Overnight Loop

각 에이전트는 한 사이클에 같은 이유로 바뀌는 작은 작업 하나만 잡는다.

1. `AGENTS.md`, `docs/index.md`, `docs/plans/progress.md`를 읽고 현재 priority를 확인한다.
2. `git status`로 기존 변경을 확인하고, 다른 세션 변경을 되돌리지 않는다.
3. active plan 또는 `progress.md`의 다음 작업에서 작은 task 하나를 고른다.
4. MCP가 필요한지 판단한다.
5. MCP가 필요하면 MCP Captain에게 request를 남긴다.
6. MCP가 필요 없으면 code/docs/static 작업만 진행한다.
7. 작업 후 가장 가까운 검증을 실행한다.
8. 문서가 틀려졌으면 같은 owner 범위 안에서만 동기화한다.
9. 막히면 blocked reason, 다음 재시도 조건, 건드린 파일을 남긴다.
10. 다음 independent low-risk task가 있으면 새 사이클을 시작한다.

## Task Selection

우선순위는 아래 순서로 고른다.

1. `progress.md`의 현재 포커스와 다음 작업
2. active `docs/plans/*.md`의 low-risk cleanup
3. validation-only task
4. 문서 정합성 또는 residual 정리
5. 새 plan 작성은 기존 문서 한 줄 갱신으로 부족할 때만 선택

여러 task가 가능하면 MCP가 필요 없고, 파일 owner가 좁고, 검증이 빠른 작업을 먼저 고른다.

## Allowed Overnight Work

- unused parameter 제거, 명확한 null 정리, 작은 naming cleanup
- plan의 residual을 작은 단계로 처리
- lint, compile, rules validation
- read-only audit
- blocked/mismatch 원인 분리
- 기존 active plan의 acceptance를 바꾸지 않는 문서 정합성 보정

## Stop Conditions

아래 상황이면 해당 task를 멈추고 blocked로 남긴다.

- 사용자 제품 판단이 필요하다.
- 아키텍처 방향 또는 owner boundary를 바꿔야 한다.
- Unity MCP writer가 이미 다른 세션에서 작업 중이다.
- 같은 file 또는 scene/prefab에 충돌 가능성이 있다.
- compile error가 있어 MCP 결과가 misleading timeout이 될 수 있다.
- 새 tool, policy, parser, workflow gate를 만들어야 기존 task가 성공한다.
- visual fidelity나 runtime correctness를 실제 비교하지 못했다.

## Acceptance

- 동시에 Unity MCP mutation을 수행하는 세션은 하나뿐이다.
- non-MCP worker는 Unity state를 바꾸지 않고 request만 남긴다.
- 각 사이클은 변경 파일, 검증 결과, blocked/residual 여부를 남긴다.
- blocked 작업은 success로 보고하지 않는다.
- `progress.md`는 여러 세션이 동시에 수정하지 않는다.
- 새 plan을 만든 경우 plan authoring 재리뷰를 남기고 `docs/index.md`에 등록한다.
- 실제 scene/prefab 작업은 MCP Captain이 compile preflight와 `/health` 확인 뒤 수행한다.

## Blocked / Residual 처리

- MCP Captain이 unavailable이면 Unity 작업은 blocked로 두고 code/docs/static task로 전환한다.
- conflict 가능성이 있으면 해당 파일을 강제로 덮지 않고 conflict residual로 남긴다.
- verification 실패는 mechanical failure와 acceptance blocked/mismatch를 분리한다.
- 새 cleanup plan이 과해 보이면 draft 또는 residual로 남기고 실행하지 않는다.
- capability expansion이 필요하면 기존 task closeout과 섞지 않고 별도 lane으로 다시 선언한다.

## 검증 명령

- `git status --short`
- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- Unity MCP `/health`
- 필요한 경우 `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- `npm run --silent rules:lint`

## 문서 재리뷰

- 새 문서 생성 판단: 이 계획은 특정 feature 구현 plan이 아니라 overnight multi-agent 운영 순서를 다룬다. 기존 MCP README나 document workflow에 넣으면 실행 절차와 owner 규칙이 섞이므로 별도 active plan으로 둔다.
- 과한점 리뷰: Unity MCP 세부 규칙, acceptance 판정 기준, 문서 운영 원칙을 새로 정의하지 않고 기존 owner 문서에 위임했다. 새 artifact gate나 hard-fail을 만들지 않았다.
- 부족한점 리뷰: 현재 리스크, 목표, 제외 범위, 역할, 실행 루프, task 선택, stop condition, acceptance, blocked/residual, 검증 명령을 포함했다.
- 반영: `progress.md` 동시 수정 금지, MCP request template, MCP Captain 단일 writer, capability expansion stop condition을 명시했다.
- 수정 후 재리뷰: obvious 과한점/부족한점 없음.
- owner impact: primary `plans.overnight-agent-coordination`; secondary `docs.index`, `plans.progress`, `ops.document-management-workflow`, `ops.plan-authoring-review-workflow`, `ops.acceptance-reporting-guardrails`, `tools.unity-mcp-readme`; out-of-scope Unity UI authoring policy, feature-specific implementation plans, deployment.
- doc lifecycle checked: 새 문서는 active plan으로 등록한다. 기존 `tools/unity-mcp/README.md`와 ops owner 문서는 대체하지 않는다. overnight 운영이 안정화되면 reference 전환 후보로 본다.
- plan rereview: clean
