# Rule Harness

`tools/rule-harness/` 는 코드와 SSOT 문서의 불일치를 찾고, 안전한 범위에서는 직접 수정까지 시도하는 로컬/CI 하네스다.

기본 기준 문서:

- `CLAUDE.md`
- `agent/*.md`
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
- `decisionTrace`
- `validationResults`
- `commit`
- `rollback`

## GLM 메모

- `GLM-5`는 공식 문서상 OpenAI-compatible chat completions 엔드포인트를 지원한다.
- 기본 URL은 `https://open.bigmodel.cn/api/paas/v4` 로 맞춰져 있다.
- LLM HTTP 호출에는 기본 `120초` timeout이 걸려 있다. 오래 멈춘 것처럼 보이면 `rule-harness.log` 에서 `static scan`, `LLM review`, `doc sync`, `apply-doc-edits` 단계 로그를 보면 된다.
- doc sync는 병목 방지를 위해 hard guard가 있다. owner doc 수가 기본 `3`개를 넘거나, 대상 문서가 기본 `20000`자를 넘으면 해당 run에서는 문서 sync를 건너뛰고 report-only로 남긴다.
- 예약 작업은 `-RequireLlm -MutationMode code_and_rules` 로 실행되므로, 실제 작업 스케줄러 세션에서 `GLM_API_KEY`가 보이지 않으면 run log에 실패가 남는다.
- 스케줄러 등록 스크립트는 기본으로 `-Model glm-5 -ApiBaseUrl https://open.bigmodel.cn/api/paas/v4 -MutationMode code_and_rules` 를 task action에 직접 넣는다.
- 키는 `GLM_API_KEY`보다 `RULE_HARNESS_API_KEY`를 사용자 환경변수로 저장하는 편이 스케줄러 세션에서 더 예측 가능하다.
