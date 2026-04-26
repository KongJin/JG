# Rule Harness

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: tools.rule-harness-readme
> role: reference
> owner_scope: Rule Harness 실행 reference와 owner-doc policy ordering 설명
> upstream: repo.agents, docs.index, ops.document-management-workflow
> artifacts: `tools/rule-harness/`, `Temp/RuleHarness/`, `Temp/RuleHarnessScheduled/`

`tools/rule-harness/` 는 코드와 SSOT 문서의 불일치를 찾고, 안전한 범위에서는 직접 수정까지 시도하는 로컬/CI 하네스다.

기본 기준 문서:

- `AGENTS.md`
- `docs/index.md` 로 해석한 repo-local owner docs
- 실제 `Setup` / `Bootstrap` 와 관련 코드 경로

## 로컬 실행

정적 검사만:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\run-rule-harness.ps1 -DryRun -DisableLlm
```

LLM 리뷰 포함:

```powershell
$env:GLM_API_KEY='YOUR_KEY'
$env:RULE_HARNESS_MODEL='glm-5'
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\run-rule-harness.ps1 -DryRun -RequireLlm
```

결과는 기본적으로 `Temp/RuleHarness/rule-harness-report.json` 에 저장된다.
요약을 함께 남기면 `stageResults`, `actionItems`가 반영된 markdown summary를 바로 읽을 수 있다.
실행 전에 하네스는 가능하면 Unity MCP를 통해 `Temp/RuleHarnessState/compile-status.json` 을 갱신한다. 여기서 상태는 최소 `passed | failed | unavailable | blocked` 로 구분된다.
- `failed`: 실제 Unity compile error.
- `unavailable`: Unity MCP 미연결/비정상.
- `blocked`: play mode, compile 진행 중, timeout 같은 일시적 차단 상태.
- `unavailable` / `blocked` 는 `static-clean only` 로 남지만, `failed` 와는 별도 원인으로 보고된다.
실행 전에 `Assets/Editor/LayerDependencyValidator.cs` 가 있으면 `Temp/LayerDependencyValidator/feature-dependencies.json` 도 함께 갱신한다.
- `passed`: feature dependency graph가 DAG다.
- `failed`: feature dependency cycle이 있거나 report가 손상됐다.
- `unsupported`: 현재 repo snapshot에 LayerDependencyValidator 소스가 없어 gate를 적용하지 않았다.

`LayerDependencyAnalyzer` 는 구조 gate다. 즉 layer violation, feature edge, cycle 같은 `static-clean` 문제를 담당한다.
행동 회귀는 여기서 대체하지 않는다. 정책, mapper, serializer, presenter 로직은 direct EditMode 테스트가 별도로 보호해야 한다.
이 레포의 editor 테스트는 별도 test `asmdef`를 두지 않고 `Assets/Editor/`의 predefined editor assembly에 둔다.

Presentation responsibility static scan은 수동 리뷰에서 반복적으로 확인하던 냄새를 하네스 finding으로 올린다.
- Presentation의 `DllImport`, `UNITY_WEBGL`, `System.Runtime.InteropServices` platform bridge
- Presentation의 `transform.Find`, `GetComponentInChildren`, `Resources.Load`, `Find*Object*ByType` runtime lookup
- Presentation의 직접 `Debug.Log*`
- plain `*View.cs`가 UseCase를 직접 실행하는 흐름

`InputHandler`, `Controller`, `Flow`, `Presenter`, `Spawner`, `Adapter` 성격의 Presentation 파일은 입력 처리/연출 seam일 수 있으므로 plain View UseCase 규칙에서 제외한다.
Rule Harness는 `tools/presentation-lint/lint-presentation-responsibility.mjs`가 있으면 `presentation_policy_lint` stage로 실행한다. 이 stage는 PageController 크기, chrome/style 책임, smoke entrypoint, 과도한 dependency count를 `npm run --silent presentation:policy:lint`와 같은 기준으로 차단한다.

feature dependency repair는 문서 우선순위를 고정한 채 동작한다.
- 정책 우선순위: `AGENTS.md -> docs/index.md -> owner docs/doc_id -> code analysis`
- 문서 역할: `repair hint`, `safety constraint`, `preferred direction`
- 실제 patch 가능 여부: 코드 분석으로 최종 확정
- 문서가 모호하면 repair는 더 보수적으로 `unsupported` 로 남긴다.

현재 구조 repair v1은 `Port 역전`만 자동수정한다.
- consumer feature의 `Application/Ports`에 port interface 생성
- provider feature의 `Infrastructure`에 adapter 생성
- `Setup` / `Bootstrap` / composition root wiring 수정
- 생성자 시그니처와 call-site 연쇄 수정 허용
- `Shared 이동`, `event edge rewrite`, ambiguous edge는 아직 자동수정하지 않는다

runtime 검증 운영 모델은 아래로 고정한다.
- 하네스는 scene-specific runtime smoke를 자동 실행하지 않는다.
- Unity MCP는 compile/status refresh와 generic console/hierarchy 진단에만 사용한다.
- runtime 확인이 필요한 변경은 `manual-validation-required` 또는 `docs/playtest/runtime_validation_checklist.md` 기록으로 남긴다.
- scope가 `AGENTS.md`, `docs/*`, `.codex/skills/jg-*`, `.githooks/*`, `tools/docs-lint/*`, `tools/rule-harness/*`로 시작하면 하네스는 이를 `rules-only scope`로 취급한다.
- `rules-only scope`에서 feature code, scene/prefab, generated `.csproj` 같은 비규칙 target이 batch에 섞이면 `rules-scope-mutation-violation`으로 즉시 stop한다.
- `rules-only scope`에서 patch plan target이 남으면 하네스는 `artifacts/rules/issue-recurrence-closeout.json`을 함께 검사한다.
  - `verification`은 항상 비어 있으면 안 된다.
  - `issueDetected = true`면 `rootCause`, `prevention`, `verification`이 모두 채워져야 한다.
  - `changedPaths`는 rules-only target file을 포함해야 하고, 누락 시 `rules_closeout` stage가 failed가 된다.
- 정적 스캔은 `tools/`, `.github/workflows/`, `tools/rule-harness/` 아래에서 hardcoded MCP UI smoke 재유입을 차단한다.
  - 차단 예: `/ui/button/invoke`, `/input/click`, `/input/drag`, `/input/key`, `/input/text`, `Get-McpUiPath`, `Invoke-McpButton`
  - 예외: `tools/mcp-test-compile.ps1`, `tools/mcp-diagnose-scene-hierarchy.ps1`, `tools/mcp-hierarchy-diag.ps1`, `tools/unity-mcp/`, `tools/rule-harness/tests/`

기본 mutation mode는 config 기준 `code_and_rules` 이고, `-DryRun`이면 수정 없이 계획/검증 대상만 계산한다.

compile handoff만 수동으로 새로 쓰고 싶으면:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\write-compile-status.ps1
```

Unity Editor 없이 feature dependency DAG 산출물만 새로 쓰고 싶으면:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\write-feature-dependency-report.ps1
```

## 로컬 주기 실행

기본 1시간 주기 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1
```

기본 예약 실행은 deterministic static-only 모드다.
API 키나 GLM quota가 없어도 `static scan -> doc proposal/report -> compile/feature dependency gate -> latest-status.json`까지 진행한다.
LLM diagnose는 명시적으로 켠 경우에만 사용한다.
기본 예약 작업 이름은 `JG Rule Harness Static`이다.

30분 주기로 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1 -IntervalMinutes 30
```

LLM diagnose를 선택적으로 켜서 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1 -EnableLlm
```

LLM이 없으면 실패해야 하는 실험 run으로 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1 -RequireLlm
```

예약 작업이 실제로 호출하는 스크립트:

```powershell
.\tools\rule-harness\run-rule-harness-scheduled.ps1
```

출력 위치:

- `Temp/RuleHarnessScheduled/<timestamp>/rule-harness-report.json`
- `Temp/RuleHarnessScheduled/<timestamp>/rule-harness-summary.md`
- `Temp/RuleHarnessScheduled/<timestamp>/rule-harness.log`
- `Temp/RuleHarnessScheduled/latest-run.txt`
- `Temp/RuleHarnessScheduled/latest-status.json`
- 반복 실패/재시도 상태: `Temp/RuleHarnessState/history.json`
- compile handoff 상태: `Temp/RuleHarnessState/compile-status.json`

예약 해제:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\unregister-rule-harness-scheduled-task.ps1
```

작업 스케줄러에서 가장 안정적인 키 주입:

```powershell
[Environment]::SetEnvironmentVariable('RULE_HARNESS_API_KEY', 'YOUR_GLM_KEY', 'User')
```

키를 저장해도 기본 예약 작업은 LLM을 사용하지 않는다.
LLM diagnose가 필요하면 예약 등록 때 `-EnableLlm` 또는 `-RequireLlm`을 명시한다.

## 주요 옵션

- `-DryRun`
  - patch batch를 실제 적용하지 않고 제안 상태로만 남긴다.
- `-DisableLlm`
  - API 키가 있어도 LLM 단계를 끈다.
- `-EnableLlm`
  - 예약 wrapper에서만 쓰는 옵션이다. API 키가 있으면 LLM diagnose를 사용하고, 키가 없으면 static-only로 계속 진행한다.
- `-RequireLlm`
  - LLM이 실제로 켜지지 않으면 바로 실패한다.
- `-ApiKey`
  - `RULE_HARNESS_API_KEY`, `OPENAI_API_KEY`, `GLM_API_KEY` 대신 직접 키를 넘긴다.
- `-ApiBaseUrl`
  - provider base URL을 직접 지정한다. 비우면 `glm-*` 모델은 `https://open.bigmodel.cn/api/paas/v4`, 그 외는 OpenAI 기본 URL을 쓴다.
- `-Model`
  - `RULE_HARNESS_MODEL` 대신 모델을 직접 지정한다.
- `-MutationMode`
  - `report_only | doc_only | code_and_rules`
- `-EnableMutation`
  - config보다 우선해서 mutation loop를 켠다.
- `-DisableMutation`
  - config보다 우선해서 mutation loop를 끈다.
- `-ReviewJsonPath`
  - OpenAI 호출 대신 미리 저장한 리뷰 JSON을 읽는다.

## Validation Discovery

- code/mixed batch도 별도 runner 없이 repair loop에 들어갈 수 있다.
- 검증 계획은 이제 하네스 내부 추론만으로 계산된다.
  1. `AGENTS.md`, `docs/index.md`로 해석한 current owner docs, 실제 `Setup` / `Bootstrap`, 코드 구조에서 inferred check를 만든다.
  2. `Tests/<Feature>/` 아래 기존 test asset 존재 여부를 confidence 신호로만 쓴다.
  3. 하네스 fixture test와 static scan으로 결과를 확인한다.
- `discoveredValidationPlan` 은 스크립트 목록 대신 아래 정보만 남긴다.
  - `source`: `rule-only | inferred | feature_test_assets`
  - `confidence`: `high | medium | low`
  - `runnable`: 하네스 내장 check 중 실제 실행 가능한 check 존재 여부
  - `checks`: inferred check 목록
  - `featureTestAssets`: feature test asset 존재 신호
- code/mixed batch의 기본 confidence는 보수적으로 계산한다. feature test asset이 없거나, UnityMcp/scene/prefab 범위를 건드리거나, cross-feature 범위가 넓으면 `low` 로 내려간다.
- `low` confidence code/mixed batch는 auto-apply 대신 `manual-validation-required` 로 skip된다.
- `rules-only scope`가 비규칙 target을 건드리려 하면 confidence와 무관하게 `rules-scope-mutation-violation`으로 stop되고, action item에는 owner docs만 수정하거나 user intent를 다시 잠그라는 안내가 남는다.
- runtime smoke는 자동 하네스 범위 밖으로 공식 분리됐다. 관련 범위는 `manual-validation-required` 와 `docs/playtest/runtime_validation_checklist.md` 기록으로 넘긴다.
- advisory memory는 `tools/rule-harness/memory/advisory-memory.json` 에 저장되며 SSOT가 아니다. prompt/판단 우선순위는 항상 `AGENTS.md -> docs/index.md -> owner docs -> advisory memory -> current failure context` 순서다.

## 보고서 확인

보고서 JSON의 아래 필드를 보면 실제로 LLM이 사용됐는지 확인할 수 있다.

- `execution.dryRun`
- `execution.llmEnabled`
- `execution.llmModel`
- `execution.llmApiBaseUrl`
- `execution.llmTimeoutSec`
- `execution.mutationEnabled`
- `execution.mutationMode`
- `plannedBatches`
- `appliedBatches`
- `skippedBatches`
- `rollbackBatches`
- `stageResults`
- `actionItems`
- `execution.featureDependencyGateStatus`
- `execution.featureDependencyRepairStatus`
- `execution.featureDependencyRepairAttemptCount`
- `execution.featureDependencyUnsupportedCycleCount`
- `decisionTrace`
- `validationResults`
- `discoveredValidationPlan`
- `learningTrace`
- `memoryHits`
- `memoryUpdates`
- `promotionCandidates`
- `featureDependencyRepairSummaries`
- `featureDependencyRepairCodeCommits`
- `featureDependencyRepairDocCommits`
- `featureDependencyRepairPolicySnapshot`
- `retryAttempts`
- `historySummary`
- `commit`
- `rollback`

문서 sync skip 이유나 batch 검증 실패 이유는 `decisionTrace`, `validationResults`, `rule-harness.log` 에서 확인할 수 있다.

`rule-harness-summary.md`는 개수 요약 뒤에 `Stage Status`, `Next Actions`를 붙여서 지금 막힌 지점과 다음 조치를 먼저 보여준다.
반복 실패가 누적되면 `Promotion Candidates`와 `promotionCandidates` 필드에서 어느 owner doc으로 규칙을 승격해야 하는지 볼 수 있다.
예약 실행에서는 `latest-status.json` 이 마지막 실행 경로와 상위 액션 아이템, promotion candidate, retry count를 바로 가리킨다.
feature dependency repair가 켜진 run에서는 `feature_dependency_refresh`, `feature_dependency_gate`, `feature_dependency_repair`, `feature_dependency_gate_post_repair` stage를 순서대로 보면 된다.

## 운영 체크

- 마지막 예약 실행을 빨리 확인할 때는 `Temp/RuleHarnessScheduled/latest-status.json` 을 먼저 연다.
- stage별 성공/실패 흐름을 보려면 `rule-harness-summary.md` 의 `Stage Status`를 본다.
- 다음 액션만 빠르게 보려면 `rule-harness-summary.md` 의 `Next Actions` 또는 `latest-status.json.topActionItems`를 본다.
- 자기개선 루프가 이번 run에서 뭘 학습했는지는 `memoryUpdates`, `learningTrace`, `latest-status.json.learnedAnything`를 본다.
- 반복 실패를 owner doc 규칙으로 올릴 시점은 `promotionCandidates` 또는 `latest-status.json.topPromotionCandidates`를 본다.
- LLM 연결 실패가 보이면 `execution.llmApiBaseUrl`, `execution.logPath`, `actionItems`를 보고 API 키, 네트워크, endpoint를 함께 확인한다.
- `manual-validation-required` 가 보이면 하네스가 아직 충분히 강한 inferred signal을 못 찾은 것이다. 해당 feature의 구조 규칙, static coverage, feature test asset 유무를 먼저 보강한다.
- `remove-hardcoded-mcp-ui-smoke` 가 보이면 자동화 스크립트/워크플로우에 scene-specific UI flow가 다시 들어온 것이다. offending file을 제거하고 runtime 확인은 `docs/playtest/runtime_validation_checklist.md`로 옮긴다.
- 예약 작업 산출물은 `latest-run.txt`로 run 디렉터리를 찾고, 그 디렉터리의 report/summary/log를 순서대로 열면 된다.
- 예약 실행에서 `llmEnabled = false`는 정상 기본값이다. 하네스가 GLM 없이 돌며 남긴 static finding과 action item을 Codex 작업 큐처럼 읽는다.

## GLM 메모

- `GLM-5`는 공식 문서상 OpenAI-compatible chat completions 엔드포인트를 지원한다.
- 기본 URL은 `https://open.bigmodel.cn/api/paas/v4` 로 맞춰져 있다.
- LLM HTTP 호출에는 기본 `120초` timeout이 걸려 있다. 오래 멈춘 것처럼 보이면 `rule-harness.log` 에서 `static scan`, `LLM review`, `doc sync`, `apply-doc-edits` 단계 로그를 보면 된다.
- doc sync는 문서별 request timeout과 대상 문서 크기 제한으로 제어된다. 대상 문서가 기본 `20000`자를 넘으면 그 문서만 skip하고, run 전체는 계속 진행한다.
- 같은 commit에서 같은 batch가 반복 실패하면 `Temp/RuleHarnessState/history.json` 을 기준으로 suppression 하거나 `max-attempts-reached` 로 skip될 수 있다.
- 예약 작업은 기본적으로 `-MutationMode code_and_rules` 만 넘기며 LLM을 끈다.
- 스케줄러 등록 스크립트는 `-EnableLlm` 또는 `-RequireLlm`을 받은 경우에만 `-Model glm-5 -ApiBaseUrl https://open.bigmodel.cn/api/paas/v4` 를 task action에 넣는다.
- 키는 `GLM_API_KEY`보다 `RULE_HARNESS_API_KEY`를 사용자 환경변수로 저장하는 편이 스케줄러 세션에서 더 예측 가능하다.
