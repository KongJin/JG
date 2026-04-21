# Document Management Workflow

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: ops.document-management-workflow
> role: ssot
> owner_scope: 문서 역할, 참조 규칙, 리네임과 삭제 관리 원칙
> upstream: docs.index
> artifacts: none

이 문서는 JG 레포에서 문서를 어떻게 나누고, 서로 어떻게 참조하고, 이름 변경/삭제를 어떻게 관리할지 정하는 운영 기준이다.
목표는 문서 역할을 분리한 채로 **중복 서술과 경로 결합을 낮추는 것**이다.

## 목적

- 문서 간 결합도를 낮춘다.
- 같은 규칙이나 판단이 여러 문서에 중복되지 않게 한다.
- 파일명 변경이나 삭제가 문서 체계 전체를 흔들지 않게 한다.
- 엔트리 문서, SSOT 문서, 계획 문서, 스킬 문서의 책임을 분리한다.

## 적용 범위

### 관리 대상

- `docs/*`
- 루트 `AGENTS.md`
- repo-local skill: `.codex/skills/jg-*/SKILL.md`
- repo-local skill reference: `.codex/skills/jg-*/references/*.md`
- repo-maintained operator README: `tools/*/README.md`
- 위 문서들이 직접 사용하는 human-facing prompt/reference 문서

### 제외 대상

- `.codex/skills/.system/*`
- `.codex/.tmp/*`
- `node_modules/*`
- `Library/*`
- `Temp/*`
- test fixture와 번들된 third-party 문서
- 현재 사람이 repo 운영 기준으로 직접 읽지 않는 machine-only prompt

### owner 해석 규칙

- `doc_id`가 stable owner identifier다.
- 현재 file path는 항상 `docs/index.md`에서 찾는다.
- 이름이 바뀌어도 본문 문서는 가능한 한 `doc_id`와 역할 위임을 유지한다.

## 문서 역할

### `entry`

찾아가는 길과 읽기 순서만 제공한다.

예:
- `AGENTS.md`
- `docs/index.md`

허용:
- 실제 경로 링크
- "무엇을 먼저 읽을지" 요약

금지:
- 규칙 본문 재서술
- 상태/계획/운영 규칙 장문 설명

### `skill-entry`

repo-local `SKILL.md`의 기본 역할이다.

허용:
- 읽기 순서
- owner doc의 `doc_id`
- artifact entrypoint
- `docs/index.md` 경유 규칙

금지:
- SSOT 본문 재서술
- plan 상태 복제
- 여러 owner 문서를 합쳐 새 기준처럼 설명

### `ssot`

한 주제의 단일 기준을 가진다.

예:
- `ops.unity-ui-authoring-workflow`
- `ops.stitch-data-workflow`
- `design.game-design`

허용:
- 자기 주제의 규칙 본문
- 관련 artifact ownership
- 필요한 경우 상위 owner 문서로의 짧은 위임

금지:
- 다른 SSOT의 본문을 다시 설명
- 상태 추적과 backlog까지 같이 소유

### `plan`

현재 실행 상태와 순서만 가진다.

예:
- `plans.progress`
- `plans.stitch-ui-ux-overhaul`

허용:
- 현재 상태
- 다음 작업
- 실행 순서
- 어떤 SSOT를 기준으로 삼는지 명시

금지:
- 규칙 본문을 새로 정의
- 운영 절차의 단일 기준 역할 수행

### `reference`

필요할 때 다시 보는 보조 자료다.

허용:
- 예시
- 배경 설명
- 참고 루틴

금지:
- 현재 구현 기준의 유일한 근거 역할

### `historical`

당시 판단 기록만 보존한다.

허용:
- 배경
- 당시 의사결정

금지:
- 현재 작업 기준으로 직접 인용

## 참조 규칙

### 원칙

- 경로 참조는 `entry` 문서로 몰아준다.
- 본문 문서는 다른 문서를 **경로**보다 **역할**로 위임한다.
- 문서 간 참조는 가능한 한 `entry -> ssot -> plan/reference/artifact` 방향으로만 둔다.
- `ssot <-> ssot` 상호 장문 참조는 만들지 않는다.

### 허용 참조

- `entry -> ssot`
- `entry -> plan`
- `entry -> reference`
- `plan -> ssot`
- `skill -> entry/ssot/plan`
- `ssot -> artifact`

### 피할 참조

- `ssot -> ssot` 상세 상호 참조
- `plan -> plan` 상호 의존
- `skill` 문서가 SSOT 본문을 재서술하는 것
- 동일 사실을 두 문서가 각각 풀어 쓰는 것

## 경로 결합을 낮추는 기본값

### 1. 엔트리 문서만 실제 링크를 많이 가진다

- `AGENTS.md`
- `docs/index.md`

이 두 문서는 실제 파일 경로를 많이 가져도 된다.
대신 다른 문서는 링크 수를 줄이고 "이 주제의 owner는 어디인가"만 짧게 위임한다.

### 2. 본문 문서는 역할 위임만 한다

좋은 예:

- "저장 위치와 handoff 운영은 Stitch 데이터 운영 SSOT를 따른다."
- "실행 상태는 progress 문서를 따른다."

나쁜 예:

- 다른 문서 내용을 여기서 다시 10줄로 요약
- 같은 체크리스트를 두 문서에 모두 유지

### 3. 경로 변경 가능성이 큰 문서는 index에서 먼저 찾게 한다

새 문서 이름을 외우게 하기보다 `docs/index.md`에서 탐색하게 만든다.
`docs/index.md`는 이 레포에서 사람이 읽는 path-resolution registry를 겸한다.

## 문서 메타 기본값

모든 관리 대상 텍스트는 상단에 아래 메타를 둔다.

- 마지막 업데이트 날짜
- 상태: `active`, `draft`, `reference`, `historical`, `paused`
- `doc_id`
- `owner_scope`
- `upstream`
- `artifacts`

단, 메타를 도입하더라도 **본문 중복 제거가 먼저**다.
메타만 늘리고 내용이 중복되면 결합도는 그대로 높다.

## 충돌 처리와 상태 전이 최소 규칙

- `entry` 문서 요약과 본문 문서 메타가 충돌하면, 해당 주제의 owner 본문 문서를 우선으로 본다. entry 문서는 같은 변경에서 함께 맞춘다.
- `draft -> active`는 현재 작업에서 직접 기준으로 쓰이고, `owner_scope`와 `upstream`이 안정적으로 채워졌을 때만 올린다.
- `active -> reference/historical`는 더 이상 현재 구현 기준이 아니고, 대체 owner 또는 index 경로가 준비됐을 때만 내린다.
- 관리 대상 문서를 수정한 뒤에는 최소한 `docs/index.md`, active skill entry, 기존 경로/이전 이름 잔존 참조를 함께 확인한다.

## 리네임 규칙

문서 이름을 바꿀 때는 아래 순서를 따른다.

1. 새 이름이 정말 역할 분리를 개선하는지 먼저 확인한다.
2. `docs/index.md`와 주요 entry 문서의 링크를 먼저 갱신한다.
3. 본문 문서의 상세 경로 참조는 가능한 한 같이 줄인다.
4. 필요한 경우 기존 문서 자리에 짧은 이동 안내 문서를 잠시 둔다.

이동 안내 문서는 아래처럼 짧게만 둔다.

```md
# moved

이 문서는 이동되었다.
현재 owner 문서는 `docs/index.md`에서 찾는다.
```

안정화되면 제거한다.

## 삭제 규칙

문서를 삭제할 때는 아래를 먼저 확인한다.

1. 이 문서가 `entry`, `ssot`, `plan`, `reference`, `historical` 중 어떤 역할인지 확인
2. 현재 owner가 다른 문서로 이미 승격되었는지 확인
3. `docs/index.md`와 active skill 문서에서 먼저 참조를 제거
4. 필요하면 progress나 관련 SSOT에 "owner moved"를 짧게 남김

`historical`이 아닌 active 문서를 바로 삭제하는 것보다, 먼저 owner를 옮기고 entry를 정리한 뒤 제거하는 편이 안전하다.

## 스킬 문서 규칙

`.codex/skills/*/SKILL.md`는 `skill-entry`다.

허용:
- 읽기 순서
- 어떤 문서가 owner인지
- 어떤 artifact를 다루는지
- `docs/index.md`에서 current path를 찾도록 안내

금지:
- 운영 규칙 본문 재서술
- plan 상태 재서술
- 문서 여러 개의 내용을 합쳐 새 기준처럼 설명

repo-local skill reference 문서는 `reference`로 본다.
system/global skill은 이 레포 거버넌스 범위에 포함하지 않는다.

## 체크리스트

문서를 새로 만들거나 수정할 때 아래를 먼저 본다.

1. 이 문서의 역할은 하나인가?
2. 같은 사실을 이미 owner 문서가 가지고 있지 않은가?
3. 이 링크가 없어도 `docs/index.md`를 통해 찾을 수 있는가?
4. 이 참조가 문서 순환을 만들지 않는가?
5. skill 문서가 규칙 본문을 다시 말하고 있지 않은가?

## 자동 검증

- 문서 관리 변경 후 기본 lint는 `npm run rules:lint`로 확인한다.
- `rules:lint`는 `docs:lint`와 repo-local 운영 policy lint를 함께 실행해 메타, 링크, routing, skill-entry inspection 규칙을 같이 점검한다.
- 현재 clone에서 로컬 git hook을 쓰려면 `git config core.hooksPath .githooks`로 repo-tracked hook 경로를 활성화한다.
- 활성화된 `pre-commit` hook은 커밋 전에 `rules:lint`를 실행해 메타 누락, 상대 링크 오류, `doc_id` 중복, `docs/index.md` 상태 라벨 불일치, repo-local skill의 deprecated historical path 재서술, active 문서의 historical Stitch 링크, 존재하지 않는 concrete contract artifact 참조, Plan Mode routing 누락을 막는다.
- 원격 기준 검증은 `.github/workflows/docs-lint.yml`의 PR lint와 함께 유지한다.

## 현재 JG에 바로 적용할 기본 원칙

- `AGENTS.md`, `docs/index.md`는 엔트리 문서로 유지한다.
- `docs/ops/*`는 운영 기준을 소유한다.
- `docs/design/*`는 원칙과 판단 기준을 소유한다.
- `docs/plans/*`는 실행 상태와 다음 작업만 소유한다.
- `.codex/skills/jg-*`는 실행 진입점으로만 유지한다.
- `tools/*/README.md`는 owner가 아니라 operator reference로 유지한다.
- `.stitch/*`, `artifacts/*`는 산출물 경로로만 취급한다.
