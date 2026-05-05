# Document Management Workflow

> 마지막 업데이트: 2026-05-02
> 상태: active
> doc_id: ops.document-management-workflow
> role: ssot
> owner_scope: 문서 운영 상위 원칙, 역할 분리, 참조/리네임/삭제 관리 기준
> upstream: docs.index, ops.cohesion-coupling-policy
> artifacts: none

이 문서는 JG 레포의 문서 운영 헌장이다.
목표는 규칙을 늘리는 것이 아니라, 적은 상위 원칙으로 문서 판단을 시작하게 만드는 것이다.
응집도와 결합도 정의는 `ops.cohesion-coupling-policy`가 소유하고, 이 문서는 그 기준을 문서 운영에 적용한다.

## 상위 원칙

### SSOT

- 한 사실은 한 owner만 가진다.
- 현재 상태는 `plans.progress`를 우선한다.
- 설계 판단은 `design/*`, 운영 절차는 `ops/*`, 실행 순서는 `plans/*`가 맡는다.
- 같은 결정을 여러 문서에 장문으로 풀어 쓰지 않는다.

### Role

- `entry`: 길 안내와 registry만 맡는다.
- `ssot`: 규칙과 판단 기준만 맡는다.
- `plan`: 현재 실행 상태와 순서만 맡는다.
- `reference`: 필요할 때 보는 배경, 예시, 절차다.
- `historical`: 현재 판단 근거가 아니다.
- `skill-entry`: 실행 진입점이며, owner 본문을 재서술하지 않는다. 새 skill 생성, skill description/trigger 변경은 `ops.skill-routing-registry`와 `ops.skill-trigger-matrix`를 먼저 보고, 변경이면 `skill trigger checked`를 남긴다.

### Artifacts

- `artifacts:`는 참고 경로가 아니라 해당 문서가 closeout/evidence owner로 소유하는 경로다.
- `entry`, `skill-entry`, checklist/reference 문서의 기본값은 `artifacts: none`이다.
- active plan은 실제 evidence 전용 하위 경로만 소유한다.
- design/ops SSOT는 구현 디렉터리를 소유하지 않고, 필요한 경우 concrete schema/token/tool 파일만 소유한다.
- 예외적으로 실제 구현 구조를 소유하는 design SSOT는 `owner_scope`가 그 역할을 명시할 때 concrete implementation directory를 artifact로 가질 수 있다.
- 예외적으로 source freeze, generated source, virtual execution contract를 소유하는 ops workflow SSOT는 `owner_scope`와 본문이 경로 역할을 명시할 때 concrete artifact root나 virtual URI namespace를 artifact로 가질 수 있다.
- 허용 shape는 concrete file, concrete evidence directory, 해당 evidence directory 안의 좁은 glob, workflow가 실제로 만드는 virtual URI로 제한한다.
- `docs/plans/{current,active,reference,historical}/*.md`, `Assets/**`, `Build/**`처럼 넓은 glob은 closeout/evidence owner가 아니면 `artifacts: none`으로 둔다.

### Closeout

- 완료나 성공을 말하려면 기준, 증거, 남은 리스크가 있어야 한다.
- mechanical pass와 actual acceptance를 섞지 않는다.
- 막혔으면 `blocked`, 비교 결과가 다르면 `mismatch`로 남긴다.
- 작업 범위가 여러 owner를 건드렸다면 closeout에 `owner impact`를 남겨 primary, secondary, out-of-scope를 구분한다.
- 큰 문서 작업이면 closeout에 `doc lifecycle checked`를 남겨 active/reference/historical/delete 후보를 확인했다는 사실을 드러낸다.
- 작업 중 기준/도구/policy를 바꿔 blocked lane을 success로 만들지 않는다.
- 규칙을 개정했으면 active/current 기준에 남은 old trace를 수정, 제거, 또는 historical/reference로 격리하기 전에는 success로 닫지 않는다.
- 문서/skill/script 문제를 발견했으면 원인, 예방, 검증을 함께 남긴다.

### Recurrence Carryover

문제 해결 후 재발방지를 새 세션까지 이어야 할 때는 저장 위치를 단계별로 고른다.
목표는 모든 문제를 새 문서로 남기는 것이 아니라, 다음 세션이 필요한 예방 정보만 owner 경로에서 다시 찾게 하는 것이다.

- 세션 안에서만 필요한 판단은 최종 응답에 남긴다.
- 다음 세션의 작업 판단을 바꾸는 예방 정보는 해당 owner 문서, active plan, 또는 `plans.progress` residual에 짧게 남긴다.
- 규칙, repo-local skill, docs-lint, rule-harness, closeout artifact처럼 governance surface가 바뀐 경우에만 recurrence closeout shard를 함께 사용한다.
- 같은 예방 정보를 final, plan, ops 문서에 장문으로 중복하지 않는다.
- 새 문서가 필요할 때도 먼저 `plans.progress` 한 줄, 기존 owner 문서의 짧은 섹션, reference plan 압축 보존으로 충분한지 확인한다.

### Instruction Fit

- 사용자 지시가 기존 규칙과 충돌하면 충돌하는 기준을 짧게 밝히고, 규칙을 지키는 대안을 먼저 제안한다.
- 요청 범위가 과하면 같은 이유로 바뀌는 최소 범위로 줄이는 안을 제안한다.
- 요청 범위가 부족하면 성공 판정에 필요한 누락 항목을 짚고, 함께 처리할지 제안한다.
- 기존 규칙, owner 문서, 코드 선례로 안전하게 판단할 수 있으면 질문하지 않고 보수적으로 진행한다.

### Cohesion

- 같은 이유로 바뀌는 것만 한 문서나 한 패치에 둔다.
- 큰 작업을 시작할 때는 변경 이유, primary owner, secondary owner, 제외 범위를 먼저 짧게 선언한다.
- 본문 문서는 다른 owner의 규칙 본문을 재서술하지 않는다.
- 링크와 경로 탐색은 `AGENTS.md`와 `docs/index.md`에 모은다.
- fallback, hidden lookup, runtime repair를 정답처럼 문서화하지 않는다.

## 적용 범위

관리 대상:

- `docs/**`
- `AGENTS.md`
- `.codex/skills/jg-*/SKILL.md`
- `.codex/skills/jg-*/agents/*.yaml`
- `.codex/skills/jg-*/evals/*.json`
- `.codex/skills/jg-*/references/*.md`
- `tools/*/README.md`
- 사람이 직접 읽는 repo-maintained prompt/reference 문서

제외 대상:

- `.codex/skills/.system/**`
- `.codex/.tmp/**`
- `node_modules/**`, `Library/**`, `Temp/**`
- test fixture와 third-party bundle
- 사람이 repo 운영 기준으로 직접 읽지 않는 machine-only prompt

## 작업 크기 판단

작은 문서 작업:

- 단일 문서의 오탈자, 링크, 표현만 고친다.
- reference/historical 문서를 보존 목적으로 짧게 정리한다.
- 상태나 진행률을 한두 줄 갱신한다.

큰 문서 작업:

- 새 문서를 만든다.
- 문서 3개 이상을 같은 이유로 수정한다.
- active/reference/historical/draft 상태를 바꾼다.
- owner 문서, repo-local skill, 전역 skill trigger/body를 바꾼다.
- 문서를 삭제, 리네임, 이동한다.

작은 문서 작업은 기본 lint와 짧은 변경 요약으로 충분하다.
큰 문서 작업은 시작 시 변경 이유, primary/secondary owner, 제외 범위를 드러내고 closeout에 `owner impact`와 `doc lifecycle checked`를 남긴다.
전역 skill trigger나 body를 바꾼 경우 repo lint가 직접 보장하지 않으므로, `ops.skill-routing-registry`의 repo-facing route 이름과 `ops.skill-trigger-matrix`의 prompt-signal fixture, description budget을 확인하고 실제 파일 변경 여부를 별도로 확인한 뒤 closeout에 `skill trigger checked`를 남긴다.
전역 skill 파일은 repo 밖 사용자 Codex home에 있으므로 repo closeout artifact의 `changedPaths`에 포함되지 않는다. 수정했다면 absolute path, 변경 이유, repo owner 우선순위 유지 여부, 검증 한계를 closeout에 별도 residual/evidence로 적는다.

## Owner 해석

- `doc_id`가 stable owner identifier다.
- 현재 file path는 `docs/index.md`에서 찾는다.
- 이름이 바뀌어도 가능한 한 `doc_id`와 역할 위임은 유지한다.
- entry 문서와 owner 본문이 충돌하면 owner 본문을 우선하고, entry는 같은 변경에서 맞춘다.

## 새 문서 생성 기준

작업 시작 전 계획은 기본적으로 repo 문서가 아니라 세션 계획으로 둔다.
새 `docs/plans/{current,active,reference,historical}/*.md` 문서는 multi-session handoff나 persistent acceptance가 필요할 때만 만든다.

새 문서를 만들기 전에 아래 순서로 판단한다.

1. `plans.progress` 한 줄 갱신으로 충분한가?
2. 기존 owner 문서의 짧은 섹션으로 충분한가?
3. 기존 reference 문서 갱신으로 충분한가?
4. 채팅/세션 체크리스트로 끝낼 수 있는 작업인가?
5. 실행 순서, acceptance, residual handling을 나중에 다시 찾아야 할 때만 새 plan을 만든다.

새 문서를 만들지 않기로 한 판단은 보통 최종 응답이나 작업 요약에 남긴다.
나중에 다시 찾아야 하는 owner 판단, 우선순위 판단, residual 이관 판단이면 `plans.progress`나 owner 문서에 짧게 남긴다.
세션 안에서만 의미가 있는 판단이면 문서 본문에 늘리지 않는다.

새 문서 위치는 먼저 owner tree로 결정한다.
운영/규칙은 `docs/owners/operations/`, Unity UI와 Stitch handoff는 `docs/owners/ui-workflow/`, 제품/세계관/UI/유닛 설계는 `docs/owners/design/`, 구조 분석은 `docs/owners/architecture/`, runtime/WebGL 검증 기준은 `docs/owners/validation/`에 둔다.
계획 문서는 lifecycle tree로 나누며 현재 상태는 `docs/plans/current/progress.md`, 실행 중인 multi-session 계획은 `docs/plans/active/`, 재사용 reference는 `docs/plans/reference/`, 현재 판단 근거가 아닌 보존 기록은 `docs/plans/historical/`에 둔다.

새 plan 문서가 필요한 예:

- 여러 세션에 걸쳐 이어질 작업이다.
- acceptance, residual, blocked 판단을 나중에 다시 찾아야 한다.
- 여러 owner가 영향을 받아 scope와 out-of-scope를 고정해야 한다.
- 사람이 나중에 실행 순서를 이어받아야 한다.
- `plans.progress` 한 줄이나 기존 owner 문서 섹션으로 부족하다.

## `plans.progress` 기록 기준

`plans.progress`는 현재 상태, 현재 포커스, active owner, 미완료 TODO, 다음 작업만 짧게 소유한다.
dated implementation log, 긴 evidence 목록, 완료된 pass의 세부 기록은 해당 owner plan/reference로 넘긴다.
현재 포커스가 오래된 active plan 목록에 묻히면, 진행률 본문을 늘리기보다 plan lifecycle을 먼저 재검토한다.
`progress.md`를 제외한 active plan budget은 5개이며, 초과하면 먼저 완료/blocked/residual-only plan을 reference 압축 또는 삭제 후보로 판정한다.
active execution 판단은 `plans.progress`와 active owner plan이 맡고, 세부 acceptance는 해당 active owner plan에 둔다.
완료 증거와 닫힌 blocker 설명은 reference plan이나 closeout artifact에 남기며, `plans.progress`에는 현재 판단을 바꾸는 residual만 짧게 남긴다.
reference로 닫힌 lane은 `plans.progress`에서 상세 evidence를 반복하지 않고 남은 residual owner 또는 다음 active owner만 한 줄로 남긴다.

## 완료 plan lifecycle

완료된 `docs/plans/{current,active,reference,historical}/*.md`는 삭제가 기본값이다.
다만 residual, blocker, acceptance 근거가 아직 다른 owner로 이관되지 않았으면 삭제하지 않고 짧은 reference 기록으로 압축 보존한다.

삭제 가능한 조건:

1. acceptance가 `success`, `blocked`, 또는 `mismatch`로 닫혔다.
2. 남은 residual이 `plans.progress`, 다른 active plan, owner 문서, 또는 artifact로 이관됐다.
3. `docs/index.md`, active skill, tool README, active 문서에 old path, filename, `doc_id`, owner id 참조가 남지 않았다.

reference 압축 보존 조건:

- residual이나 blocker가 아직 해당 plan에만 남아 있다.
- multi-session handoff 근거가 아직 다른 owner로 이관되지 않았다.
- 당시 판단 기록이 필요하지만 현재 실행 기준은 아니다.

reference로 보존할 때는 `Closeout`, `Residual owner`, `Evidence links` 수준으로 줄이고, 실행 로그와 dated changelog는 제거한다.
완료된 plan에 새 TODO를 계속 붙여 재사용하지 않는다. 새 이유로 바뀌는 작업이면 새 세션 계획, 기존 active owner, 또는 새 plan 생성 기준으로 다시 판단한다.

## 역할 전이

- `draft -> active`: 현재 작업에서 직접 기준으로 쓰이고, owner scope와 upstream이 안정적일 때만 올린다.
- `active -> reference`: 현재 구현 기준이 아니지만 배경/절차로 유용할 때 내린다. 남은 residual이 `plans.progress`, 다른 active plan, 또는 owner 문서로 이관되어 원 plan이 더 이상 직접 실행 기준이 아닐 때도 내린다.
- `active/reference -> historical`: 당시 판단 기록만 남길 때 내린다.
- 완료된 plan은 active/draft로 방치하지 않고, 삭제 또는 짧은 reference 보존 후보로 판정한다.

## 참조 규칙

허용 기본값:

- `entry -> ssot/plan/reference`
- `plan -> ssot`
- `skill-entry -> entry/owner`
- `ssot -> artifact`

피할 것:

- `ssot <-> ssot` 장문 상호 참조
- `plan -> plan` 상호 의존
- skill 문서의 owner 본문 재서술
- 같은 체크리스트를 여러 문서에 복제

## 리네임과 삭제

리네임할 때:

1. 역할 분리를 개선하는 이름인지 확인한다.
2. `docs/index.md`와 주요 entry 링크를 먼저 갱신한다.
3. 본문 문서의 상세 경로 참조를 가능한 한 줄인다.
4. 필요한 경우에만 짧은 moved stub를 둔다.
5. `rg`로 old path, filename, `doc_id`, owner id 참조가 active 문서, active skill, tool README에 남았는지 확인한다.

삭제할 때:

1. 문서 역할을 확인한다.
2. 현재 owner가 다른 문서로 이동했는지 확인한다.
3. `docs/index.md`, active skill, tool README의 참조를 제거한다.
4. 필요하면 `progress.md`나 owner 문서에 owner 이동을 짧게 남긴다.
5. `rg`로 deleted path, filename, `doc_id`, owner id 참조가 active 문서, active skill, tool README에 남았는지 확인한다.

## 자동 검증

- 문서 관리 변경 후 기본 검증은 `npm run --silent rules:lint`다.
- `rules:lint`는 metadata, relative links, `doc_id`, index registry, status mismatch, owner reference, Plan Mode routing, repo-local skill routing, external/global `rule-*` skill registry, global `rule-*` markdown link integrity, skill trigger matrix coverage, recurrence closeout, presentation/stitch policy lint를 함께 본다.
- `rules:lint`의 hard-fail은 broken link, stale owner path, `doc_id` 문제, active plan budget 초과, entry 문서의 owner policy body (`entry-policy-body`), recurrence closeout 누락처럼 SSOT나 자동 판정을 깨는 항목에 둔다.
- `rules:lint` advisory는 오래된 active 문서 (`stale-active-plan`, `stale-active-doc`), 긴 active plan/progress/entry, skill-entry policy density 후보 (`*-size-advisory`)처럼 즉시 실패는 아니지만 lifecycle 재검토가 필요한 항목에 둔다.
- advisory가 있어도 lint exit code는 실패가 아니며, closeout에서는 필요한 경우 남은 문서 lifecycle risk로 분리해 보고한다.
- `npm run --silent docs:health`는 blocking issue와 advisory를 text 또는 `-- --json` 출력으로 보여주는 비차단 리포트이며, 정리 후보를 보는 용도이지 acceptance gate가 아니다.
- 단순 docs-only plan 작성, playtest/checklist/design/reference 보정, 상태 한두 줄 갱신은 `rules:lint`와 authoring review로 충분하다.
- recurrence closeout artifact는 recurrence-tracked rule/tooling scope에만 요구한다: `AGENTS.md`, `docs/index.md`, `docs/owners/operations/*, docs/owners/ui-workflow/*`, `.codex/skills/jg-*`, `tools/docs-lint/*`, `tools/rule-harness/*`, `.githooks/*`, `.github/workflows/docs-lint.yml`, `artifacts/rules/issue-recurrence-closeout.json`, `artifacts/rules/issue-recurrence-closeout.d/*.json`.
- feature tool implementation, 일반 `tools/*/README.md`, `docs/plans/*`, `docs/owners/validation/*`, `docs/owners/design/*` 변경은 그 변경 자체가 rule/tooling recurrence 예방을 건드리지 않는 한 closeout artifact를 요구하지 않는다.
- closeout artifact는 declared lane, mutation class, acceptance evidence class, escalation 필요 여부를 함께 남겨 silent lane escalation을 기계적으로 점검할 수 있어야 한다.
- closeout artifact 동기화는 `npm run --silent rules:sync-closeout`를 사용한다. 기본값은 현재 changed rules-only set 기반 shard를 만들거나 갱신한다. legacy aggregate를 직접 갱신해야 하면 `npm run --silent rules:sync-closeout -- --primary`를 쓴다.
- `rules:lint`와 `rules:sync-closeout`는 `RULES_LINT_CHANGED_FILES`가 없으면 unstaged, staged, untracked 변경 목록을 git에서 직접 계산한다. plain `npm run --silent rules:lint`가 closeout artifact 누락을 잡아야 한다.
- Repo 밖 external/global skill 파일 변경은 git changed list와 closeout shard에 자동 포함되지 않는다. `rules:lint`는 현재 설치된 global `rule-*` markdown 링크 무결성만 검사한다. 해당 파일을 수정했다면 repo lint 결과와 별도로 파일 path와 검증 한계를 보고한다.
- closeout artifact가 이미 다른 작업의 dirty state라면 덮어쓰지 않는다. 병렬 작업이면 별도 shard로 분리하고, 이번 작업의 변경 목록으로 분리해 동기화할 수 없으면 residual 또는 blocked로 남긴다.

## 빠른 체크

문서를 만들거나 고칠 때는 아래 항목을 먼저 본다.

1. 이 문서의 역할은 하나인가?
2. 이 사실의 owner는 여기인가?
3. `docs/index.md`로 찾을 수 있는가?
4. 같은 내용을 다른 문서가 장문으로 갖고 있지 않은가?
5. closeout 표현에 기준, 증거, 남은 리스크가 맞게 붙어 있는가?
6. 큰 문서 작업이면 `owner impact`(여러 owner 영향)와 `doc lifecycle checked`(상태/삭제 후보 확인)를 남겼는가?
7. active plan을 새로 만들거나 phase를 분리했다면 기존 active parent가 여전히 직접 실행 owner인지 확인했는가?
8. `plan rereview: clean`을 남긴다면 실제 확인한 scope를 한 줄에 붙였는가?
