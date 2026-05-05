# C# Guardrails Migration Instructions

> 용도: C# source guardrails lint 위반을 파일 단위로 안전하게 고칠 때 5.3 계열 모델에게 전달하는 지시문.
> 상태: reference prompt

```text
C# source guardrails lint 위반을 수정해줘.

범위:
- Assets/Scripts/**/*.cs 안에서만 작업
- 한 번에 너무 많이 하지 말고 파일 단위로 작게 진행
- 먼저 1개 파일을 수정한 뒤 변경 내용을 보고해줘

중요 원칙:
- lint를 통과시키기 위해 기존 런타임 동작/계약을 바꾸면 안 됨
- optional serialized field를 임의로 [Required]로 바꾸면 안 됨
- Tooltip, 주석, fallback 로직, null 허용 의도가 있으면 optional 계약으로 보고 유지할 것
- 기존 fallback 경로를 제거하지 말 것
- null 체크를 없애서 NullReferenceException 가능성을 만들지 말 것

Required / SerializeField 규칙:
- MonoBehaviour의 필수 serialized field는 [Required, SerializeField] 순서로 맞출 것
- Required를 새로 붙인 파일에는 `using Shared.Attributes;`가 있는지 반드시 확인하고, 없으면 추가할 것
- [SerializeField, Required]는 [Required, SerializeField]로 순서만 바꿀 것
- [SerializeField]만 있는데 실제로 optional인 필드는 [Required]를 붙이지 말고 아래 예외 주석을 붙일 것:
  // csharp-guardrails: allow-serialized-field-without-required
- optional 여부 판단 근거:
  - Tooltip/주석에 "선택", "optional", "비우면", "없으면 fallback" 같은 표현이 있음
  - 코드에 fallback 로직이 있음
  - null일 때 다른 경로를 쓰도록 설계되어 있음

Null guard 규칙:
- null 방어는 생성자 또는 인자를 받는 함수에서만 허용
- null 방어 대상은 그 함수의 인자 자체여야 함
- 예: if (enemy is null) return; 은 OnEnemyArrived(EnemySetup enemy) 안에서 허용
- 예: if (_field == null) 은 금지. 단, 기존 동작상 필요하면 제거하지 말고 구조를 다시 생각하거나 예외/계약을 보고해줘
- 인자 null 가드는 함수 시작부에 두고, 인자를 사용하기 전에 검사할 것
- 내부 상태 필드의 null 체크를 그냥 삭제하지 말 것. Required 계약으로 보장 가능한지, optional 계약인지 먼저 판단할 것

절대 하지 말 것:
- optional 필드를 Required로 바꾸면서 fallback 제거
- Required를 붙이고 `using Shared.Attributes;` 확인 없이 끝내기
- ?. / ?? / == null 을 단순 삭제해서 동작 변경
- null이면 return하던 코드를 null에서 터지게 만들기
- 대량 자동수정 후 검토 없이 끝내기
- unrelated file 수정
- 빈 줄/포맷 대량 변경

검증:
- lint 통과만으로 완료 보고하지 말 것
- Required를 추가했다면 최소한 `powershell -ExecutionPolicy Bypass -File tools/check-compile-errors.ps1`로 C# 컴파일 에러를 확인할 것
- 전체 컴파일이 기존 이슈로 실패하면, 새로 수정한 파일에서 `RequiredAttribute`/`Required` 네임스페이스 에러가 생기지 않았는지 별도로 보고할 것

작업 후 보고:
- 수정한 파일 목록
- 각 파일에서 어떤 lint 위반을 어떻게 해결했는지
- 동작이 바뀌지 않았다고 판단한 근거
- 의도적으로 예외 주석을 남긴 위치와 이유
- 실행한 검증과 컴파일 결과. 실행하지 못했으면 이유
- 애매해서 손대지 않은 부분

주의 예시:
WaveSetup.cs의 _difficultySpawnScale은 Tooltip에 "선택 / 비우면 Room에서 직접 읽는다"라고 되어 있으므로 optional 필드다.
따라서 [Required]를 붙이거나 fallback을 제거하면 안 된다.
이 경우 [SerializeField] 앞에 예외 주석을 붙이고 기존 null fallback 동작을 유지해야 한다.
```
