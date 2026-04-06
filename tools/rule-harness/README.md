# Rule Harness

`tools/rule-harness/` 는 코드와 SSOT 문서의 불일치를 찾는 로컬/CI 하네스다.

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
$env:OPENAI_API_KEY='YOUR_KEY'
powershell -ExecutionPolicy Bypass -File .\tools\rule-harness\run-rule-harness.ps1 -DryRun -RequireLlm
```

결과는 기본적으로 `Temp/RuleHarness/rule-harness-report.json` 에 저장된다.

## 주요 옵션

- `-DryRun`
  - 문서 패치를 실제 적용하지 않고 제안만 계산한다.
- `-DisableLlm`
  - API 키가 있어도 LLM 단계를 끈다.
- `-RequireLlm`
  - LLM이 실제로 켜지지 않으면 바로 실패한다.
- `-ApiKey`
  - `OPENAI_API_KEY` 대신 직접 키를 넘긴다.
- `-Model`
  - `RULE_HARNESS_MODEL` 대신 모델을 직접 지정한다.
- `-ReviewJsonPath`
  - OpenAI 호출 대신 미리 저장한 리뷰 JSON을 읽는다.

## 보고서 확인

보고서 JSON의 아래 필드를 보면 실제로 LLM이 사용됐는지 확인할 수 있다.

- `execution.dryRun`
- `execution.llmEnabled`
- `execution.llmModel`
