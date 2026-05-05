---
name: rule-operations
description: "운영/SSOT 규칙. Triggers: Plan 모드, 규칙 충돌, 문서 동기화, closeout, stale path/rule, blocked/residual 보고, skill trigger."
---

# Operations

운영/작업 절차 route. JG repo에서는 `AGENTS.md`와 `docs/index.md`로 current owner를 확인하고, repo owner docs를 최종 기준으로 우선한다.
이 skill의 하위 문서는 repo owner가 없거나 배경 확인이 필요할 때만 fallback/reference로 사용한다.

---

## 빠른 찾기

| 주제 | 상세 |
|-------|--------|
| 문서 소유권 · SSOT | JG repo: `docs/owners/operations/document_management_workflow.md`; fallback: [work_principles.md](work_principles.md) |
| Codex 패치 운영 | JG repo: `docs/owners/operations/codex_coding_guardrails.md`; fallback: [codex_patching.md](codex_patching.md) |
| 계획 문서 작성 · 재리뷰 | `rule-plan-authoring` skill - docs/plans 작성/수정 시 과한점/부족한점 반복 재리뷰 |
| Acceptance · closeout 보고 | JG repo: `docs/owners/operations/acceptance_reporting_guardrails.md` - blocked/mismatch/success/residual 보고 기준 |

---

## 하위 문서 읽어오기

사용자가 다음 주제에 대해 질문하면 해당 하위 문서를 읽어와서 답변하세요:

| 사용자 질문 키워드 | 읽어올 하위 문서 |
|-------------------|-------------------|
| SSOT, 문서 소유권, 작업 레벨별 응집도, 문서 동기화 | JG repo owner 문서 `docs/owners/operations/document_management_workflow.md`; fallback은 [work_principles.md](work_principles.md) |
| Codex 패치, 한 패치 = 한 책임, 파일 종류별 기본값, 적용 순서 | JG repo owner 문서 `docs/owners/operations/codex_coding_guardrails.md`; fallback은 [codex_patching.md](codex_patching.md) |
| 작업 절차, 진행 상태, Plan 모드 | `docs/index.md`, `docs/plans/current/progress.md`, repo owner 문서 우선; fallback은 [work_principles.md](work_principles.md) |
| 사용자 지시가 기존 규칙과 충돌/위배, 요청 범위가 과함/부족함, 대안 제안 | repo owner 문서 `docs/owners/operations/document_management_workflow.md`를 읽기 |
| 규칙 개정 후 stale path/rule, 이전 흔적 정리 | `docs/index.md`로 current owner를 확인한 뒤 repo owner 문서 `docs/owners/operations/document_management_workflow.md`, `docs/owners/operations/cohesion-coupling-policy.md`, 관련 repo-local skill을 읽기 |
| 계획 문서 작성, docs/plans, Phase, Acceptance, TODO, closeout, 과한점/부족한점 재리뷰 | `rule-plan-authoring` skill도 함께 사용 |
| acceptance, blocked, mismatch, success, residual, closeout 보고 | repo owner 문서 `docs/owners/operations/acceptance_reporting_guardrails.md`를 읽기 |
| 새 규칙 추가, 행동 트리거, skill trigger checked | `rule-plan-authoring` skill도 함께 사용하고 `docs/index.md`에서 current owner와 관련 repo-local skill route를 확인 |
