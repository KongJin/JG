# Unity UI Authoring Workflow

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: ops.unity-ui-authoring-workflow
> role: ssot
> owner_scope: Unity UI/UX authoring route, evidence gate, 금지 경로, 종료 proof
> upstream: docs.index, ops.cohesion-coupling-policy
> artifacts: `tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`

이 문서는 이 레포에서 Unity UI/UX 작업을 시작할 때 에이전트가 가장 먼저 따라야 하는 단일 기준이다.
정책 본문은 여기 한 곳에만 둔다.
`AGENTS.md`, `docs/index.md`, skill 문서, `tools/unity-mcp/README.md`는 이 문서를 가리키는 엔트리 또는 실행 reference로만 유지한다.
응집도와 결합도의 상위 정의, 예외, hard-fail/review 경계는 `ops.cohesion-coupling-policy`를 따른다.
이 문서는 Unity UI authoring lane의 파생 규칙과 evidence gate만 소유한다.

## 목적

- Unity UI/UX 작업의 허용 route를 먼저 고정한다.
- 금지된 authoring 경로를 작업 시작 전에 차단한다.
- 작업 종료 시 무엇을 증명해야 하는지 artifact 기준으로 고정한다.

## 허용 Route

| route | 언제 쓰는가 | 기본 작업 방식 | 최소 증거 |
|---|---|---|---|
| `uitk-candidate` | Stitch source freeze를 Unity 후보 surface로 가져올 때 | UXML/USS/preview scene/capture report를 만들고 runtime 교체와 분리 | compile/reload + workflow policy check + capture/report |
| `scene/prefab authoring` | `Assets/Scenes/*.unity`, `Assets/**/*.prefab` 중심 UI 작업 | MCP로 현재 hierarchy와 serialized ref를 읽고, scene/prefab을 직접 authoring | compile/reload + workflow policy check |
| `mixed` | scene/prefab과 legacy runtime UI script를 함께 건드릴 때 | scene contract를 먼저 읽고, scene/prefab authoring을 주인으로 유지 | compile/reload + workflow policy check + 필요한 scene-specific evidence |
| `battle-scene-ui` | legacy battle scene가 아직 실제 authoring 대상일 때만 | scene가 실제 존재할 때만 MCP repair로 직접 정리 | compile/reload + workflow policy check + route-specific verification |

## 금지 작업

- 새 UX/UI 개발은 Stitch source freeze와 UI Toolkit candidate surface에서 시작한다
- 새 UI prefab 생성 기본 금지
- Unity가 열어 둔 scene의 디스크 파일 직접 overwrite 금지
- code-driven builder 또는 rebuild route를 UI authoring 기본 경로로 재도입 금지
- capture나 runtime proof를 layout authoring의 대체 수단으로 사용 금지
- scene registry를 `FindFirstObjectByType<*SceneRegistry>` 또는 `AddComponent<*SceneRegistry>` 같은 runtime repair 경로로 복구하는 것 금지

## 허용 작업

- scene/prefab authoring은 Unity MCP repair로 수행
- 새 화면과 visual system은 Stitch source freeze를 기준으로 UI Toolkit candidate surface에서 먼저 만든다
- 기존 runtime UI script는 runtime 참조를 깨지 않는 범위의 유지보수 대상으로만 둔다
- scene contract owner는 serialized scene/prefab 상태로 유지
- visual handoff는 `Stitch`를 참고하되, 새 Stitch import는 UI Toolkit candidate surface로 먼저 가져온다
- runtime scene/prefab은 실제 교체 pass에서만 건드리고, pilot 성공을 runtime acceptance로 보고하지 않는다
- scene registry 같은 runtime wiring dependency는 scene/prefab contract로만 유지하고, 숨은 runtime lookup/add-component fallback은 재도입하지 않는다

## Stitch / UITK Translation Caveats

Stitch source를 Unity UI Toolkit 후보 surface로 옮길 때는 semantic hierarchy와 source-derived presentation을 분리한다.
세부 실행 명령은 `tools/stitch-unity/README.md`와 `tools/unity-mcp/README.md`가 담당하지만, authoring 판단은 이 문서가 소유한다.

- Stitch/Tailwind/CSS를 USS로 그대로 복사하지 않는다.
- UI Toolkit이 지원하지 않거나 현재 workflow에서 검증하지 않는 `display: grid`, gradient, `box-shadow`, `calc()`, pseudo-element, `z-index`류 패턴에 기대지 않는다.
- source의 first-read order, block direction, emphasis posture, preview/summary completion, primary CTA persistence를 후보 surface에서 다시 확인한다.
- `screen manifest`는 semantic meaning, `unity-map`은 binding, `presentation-contract`는 source-derived presentation만 맡는다.
- `extractionStatus != resolved`이거나 `unresolvedDerivedFields`가 남아 있으면 translation-ready success로 보고하지 않는다.
- semantic contract와 map이 맞아도 skeleton처럼 보이면 presentation extraction이 아직 닫히지 않은 것으로 보고 pass를 열어 둔다.
- pilot/candidate evidence는 runtime replacement acceptance가 아니다. runtime target, binding, fresh runtime capture/smoke가 잠기기 전에는 `pilot success` 이상으로 보고하지 않는다.

## Unity Execution Checkpoints

Unity UI authoring 변경은 실행 전에 scene/prefab/runtime contract를 확인하고, 실행 후에는 현재 evidence를 다시 읽는다.

- script edit가 재컴파일을 필요로 하면 Play Mode를 먼저 멈추고 compile/reload 안정화를 기다린다.
- Unity Editor가 이미 열려 있고 사용자가 scene/prefab/UI를 보고 있을 가능성이 있으면, auxiliary EditMode 검증은 기본적으로 Play Mode를 보존하거나 `blocked`로 보고한다. open-scene dirty prompt 가능성이 있는 자동 Play Mode stop은 explicit lane ownership 또는 사용자 의도 없이 실행하지 않는다.
- scene/prefab authoring은 변경 전 target hierarchy와 component type을 확인하고, 변경 후 같은 node/component를 다시 읽는다.
- 새 wrapper/helper visual을 추가할 때 input을 가리거나 intended node를 숨기지 않는지 확인한다.
- console/log 판단은 timestamp나 최신 run 기준으로 stale error와 current failure를 분리한다.
- generated `.csproj`가 의심되면 Unity/Bee compile truth와 Unity Test Runner inclusion을 먼저 확인하고 generated project file을 직접 patch하지 않는다.
- 구조/static/direct EditMode 검증을 먼저 쓰고, wiring/runtime smoke는 값싼 검증이 통과한 뒤에 둔다.
- runtime smoke나 capture는 active contract 또는 translation pipeline이 존재한 뒤에 acceptance evidence로 취급한다.

## Route별 필수 증거

### `BattleScene`

- compile/reload 안정화
- route-specific verification artifact

### 일반 Unity UI 변경

- compile/reload 안정화
- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1` 결과

## 작업 종료 순서

1. compile/reload를 안정화한다.
2. workflow policy check를 실행한다.
3. route-specific verification을 실행한다.
4. artifact freshness를 확인한다.
5. 관련 문서를 동기화한다.

## 강제 장치

- `Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
  - 현재 변경 파일을 읽고 route를 판정한다.
  - `Assets/UI/UIToolkit/**`의 UXML/USS/asset 변경은 `uitk-candidate` route로 판정한다.
  - compile/reload, artifact freshness, prefab 금지 규칙을 검사한다.
  - policy가 blocked면 결과 artifact에 `blockedReason`을 남긴다.
## 실행 Entry

- 엔트리: [`../../../AGENTS.md`](../../../AGENTS.md)
- 문서 인덱스: [`../../index.md`](../../index.md)
- MCP 실행 reference: [`../../../tools/unity-mcp/README.md`](../../../tools/unity-mcp/README.md)
- policy script: [`../../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1`](../../../tools/unity-mcp/Invoke-UnityUiAuthoringWorkflowPolicy.ps1)
