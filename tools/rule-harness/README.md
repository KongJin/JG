# Rule Harness

`tools/rule-harness/` 는 코드와 SSOT 문서의 불일치를 찾고, 안전한 범위에서는 직접 수정까지 시도하는 로컬/CI 하네스다.

기본 기준 문서:

- `CLAUDE.md`
- `CLAUDE.md` 가 직접 가리키는 global owner docs
- `Assets/Scripts/Features/*/README.md`
- `Assets/Scripts/Shared/README.md`
- `Assets/Editor/UnityMcp/README.md`

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

기본 mutation mode는 config 기준 `code_and_rules` 이고, `-DryRun`이면 수정 없이 계획/검증 대상만 계산한다.

## 로컬 주기 실행

기본 1시간 주기 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1
```

30분 주기로 등록:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\register-rule-harness-scheduled-task.ps1 -IntervalMinutes 30
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

예약 해제:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\unregister-rule-harness-scheduled-task.ps1
```

작업 스케줄러에서 가장 안정적인 키 주입:

```powershell
[Environment]::SetEnvironmentVariable('RULE_HARNESS_API_KEY', 'YOUR_GLM_KEY', 'User')
```

## 주요 옵션

- `-DryRun`
  - patch batch를 실제 적용하지 않고 제안 상태로만 남긴다.
- `-DisableLlm`
  - API 키가 있어도 LLM 단계를 끈다.
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

- code/mixed batch는 이제 `validation-registry.json` 등록 여부와 무관하게 repair loop에 들어갈 수 있다.
- 검증 계획은 다음 우선순위로 자동 발견된다.
  1. `Tests/<Feature>/Run-*.ps1`
  2. `CLAUDE.md`, 현재 global owner docs, owner README 계약에서 추론한 structural check
  3. `Tests/<Feature>/` 아래 기존 test asset
  4. `tools/rule-harness/validation-registry.json` 의 optional hint
  5. 하네스 fixture test + static scan
- registry는 migration 동안 유지되는 hint 레이어다. 없다고 해서 `missing-validation-registry`로 막지 않는다.
- runner script가 없으면 하네스는 inferred structural check + static scan으로 계속 진행하고, report의 `discoveredValidationPlan`과 `actionItems`에 confidence/다음 조치를 남긴다.
- advisory memory는 `tools/rule-harness/memory/advisory-memory.json` 에 저장되며 SSOT가 아니다. prompt/판단 우선순위는 항상 `CLAUDE.md -> owner docs -> advisory memory -> current failure context` 순서다.

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
- `decisionTrace`
- `validationResults`
- `discoveredValidationPlan`
- `learningTrace`
- `memoryHits`
- `memoryUpdates`
- `promotionCandidates`
- `retryAttempts`
- `historySummary`
- `commit`
- `rollback`

문서 sync skip 이유나 batch 검증 실패 이유는 `decisionTrace`, `validationResults`, `rule-harness.log` 에서 확인할 수 있다.

`rule-harness-summary.md`는 개수 요약 뒤에 `Stage Status`, `Next Actions`를 붙여서 지금 막힌 지점과 다음 조치를 먼저 보여준다.
반복 실패가 누적되면 `Promotion Candidates`와 `promotionCandidates` 필드에서 어느 owner doc으로 규칙을 승격해야 하는지 볼 수 있다.
예약 실행에서는 `latest-status.json` 이 마지막 실행 경로와 상위 액션 아이템, promotion candidate, retry count를 바로 가리킨다.

## 운영 체크

- 마지막 예약 실행을 빨리 확인할 때는 `Temp/RuleHarnessScheduled/latest-status.json` 을 먼저 연다.
- stage별 성공/실패 흐름을 보려면 `rule-harness-summary.md` 의 `Stage Status`를 본다.
- 다음 액션만 빠르게 보려면 `rule-harness-summary.md` 의 `Next Actions` 또는 `latest-status.json.topActionItems`를 본다.
- 자기개선 루프가 이번 run에서 뭘 학습했는지는 `memoryUpdates`, `learningTrace`, `latest-status.json.learnedAnything`를 본다.
- 반복 실패를 owner doc 규칙으로 올릴 시점은 `promotionCandidates` 또는 `latest-status.json.topPromotionCandidates`를 본다.
- LLM 연결 실패가 보이면 `execution.llmApiBaseUrl`, `execution.logPath`, `actionItems`를 보고 API 키, 네트워크, endpoint를 함께 확인한다.
- runner script가 없어서 confidence가 낮게 나오면 `Tests/<Feature>/Run-*.ps1` 를 추가하고 필요하면 `validation-registry.json` 에 hint를 보강한다.
- 예약 작업 산출물은 `latest-run.txt`로 run 디렉터리를 찾고, 그 디렉터리의 report/summary/log를 순서대로 열면 된다.

## GLM 메모

- `GLM-5`는 공식 문서상 OpenAI-compatible chat completions 엔드포인트를 지원한다.
- 기본 URL은 `https://open.bigmodel.cn/api/paas/v4` 로 맞춰져 있다.
- LLM HTTP 호출에는 기본 `120초` timeout이 걸려 있다. 오래 멈춘 것처럼 보이면 `rule-harness.log` 에서 `static scan`, `LLM review`, `doc sync`, `apply-doc-edits` 단계 로그를 보면 된다.
- doc sync는 문서별 request timeout과 대상 문서 크기 제한으로 제어된다. 대상 문서가 기본 `20000`자를 넘으면 그 문서만 skip하고, run 전체는 계속 진행한다.
- 같은 commit에서 같은 batch가 반복 실패하면 `Temp/RuleHarnessState/history.json` 을 기준으로 suppression 하거나 `max-attempts-reached` 로 skip될 수 있다.
- 예약 작업은 `-RequireLlm -MutationMode code_and_rules` 로 실행되므로, 실제 작업 스케줄러 세션에서 `GLM_API_KEY`가 보이지 않으면 run log에 실패가 남는다.
- 스케줄러 등록 스크립트는 기본으로 `-Model glm-5 -ApiBaseUrl https://open.bigmodel.cn/api/paas/v4 -MutationMode code_and_rules` 를 task action에 직접 넣는다.
- 키는 `GLM_API_KEY`보다 `RULE_HARNESS_API_KEY`를 사용자 환경변수로 저장하는 편이 스케줄러 세션에서 더 예측 가능하다.
