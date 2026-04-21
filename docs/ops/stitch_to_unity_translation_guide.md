# Stitch To Unity Translation Guide

> 마지막 업데이트: 2026-04-21
> 상태: reference
> doc_id: ops.stitch-to-unity-translation-guide
> role: reference
> owner_scope: accepted Stitch screen을 Unity prefab/scene contract로 한 장씩 번역할 때 따르는 실무 가이드
> upstream: docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract, ops.unity-ui-authoring-workflow, design.ui-foundations
> artifacts: `.stitch/contracts/intakes/*.json`, `.stitch/contracts/blueprints/*.json`, `.stitch/contracts/screens/*.json`, `Assets/Prefabs/`, `Assets/Scenes/`, `artifacts/unity/`

이 문서는 `Stitch -> Unity` 번역을 실제로 수행할 때 따라가는 작업 가이드다.
규칙 본문은 owner 문서가 가진다.
이 문서는 그 규칙들을 `한 장씩 어떻게 옮길지`, 그리고 `어디까지 source fidelity를 지켜야 하는지`만 실무 순서로 묶는다.

## 목적

- accepted Stitch screen을 빠짐없이 Unity로 옮긴다.
- `예쁜 시안`과 `구현 가능한 계약`을 섞지 않는다.
- 다음 세션이나 다른 작업자도 같은 순서로 재현 가능하게 만든다.
- SSOT를 다시 요약하지 않고, 실제 번역 판단과 비교 포인트만 제공한다.

## 언제 이 가이드를 쓰는가

- 새 accepted screen을 intake로 내릴 때
- 기존 manifest가 screen 의도를 제대로 반영하는지 비교할 때
- manifest를 기준으로 prefab-first translation을 시작할 때
- `번역 완료`를 어디까지로 볼지 정할 때

## 번역 완료의 정의

이 레포에서 `번역했다`는 말은 아래 네 단계를 모두 닫았다는 뜻이다.

1. accepted screen의 읽기 순서, block 의미, CTA, 상태가 intake에 남아 있다.
2. Unity target과 owner가 manifest에 명시돼 있다.
3. prefab/scene hierarchy와 Required serialized ref가 실제로 맞다.
4. source screen의 핵심 fidelity가 살아 있고, workflow policy, wiring review, smoke 또는 관련 evidence로 결과를 증명했다.

화면이 비슷해 보여도 `manifest`와 `serialized ref`가 비어 있으면 번역 완료가 아니다.
구조가 맞아도 source의 핵심 reading posture가 무너지면 번역 완료가 아니다.

## 먼저 읽을 것

1. [`../../AGENTS.md`](../../AGENTS.md)
2. [`../index.md`](../index.md)
3. [`./stitch_data_workflow.md`](./stitch_data_workflow.md)
4. [`./stitch_structured_handoff_contract.md`](./stitch_structured_handoff_contract.md)
5. [`./unity_ui_authoring_workflow.md`](./unity_ui_authoring_workflow.md)
6. [`../design/ui_foundations.md`](../design/ui_foundations.md)
7. 해당 `.stitch/contracts/screens/*.json`와 관련 `blueprint`

구조/필드/반자동 생성 규칙은 [`./stitch_structured_handoff_contract.md`](./stitch_structured_handoff_contract.md)를 기준으로 본다.
이 문서에서는 그 내용을 다시 정의하지 않는다.

## 기본 루프

### 1. Source를 고정한다

- accepted baseline screen 하나만 source로 잡는다.
- non-baseline candidate나 historical export를 섞지 않는다.
- set handoff md는 보조 reference로만 보고, 활성 입력은 `.stitch/contracts/*.json`으로 모은다.
- source comparison은 가능하면 accepted Stitch `png`를 옆에 두고 진행한다.

### 2. Screen Intake를 만든다

- accepted screen에서 읽히는 사실만 적는다.
- 필수 필드와 작성 규칙은 [`./stitch_structured_handoff_contract.md`](./stitch_structured_handoff_contract.md)의 `Screen Intake 구조`를 따른다.
- Unity path, serialized owner, smoke script는 이 단계에서 억지로 넣지 않는다.

출력:
- `.stitch/contracts/intakes/<surface>.intake.json`

### 3. Blueprint / Manifest에 반영한다

- reusable family면 `blueprint + screen manifest`
- one-off면 full contract fallback
- 기존 accepted manifest가 있으면 intake와 비교해 추측이 섞인 부분을 줄인다.
- 필드 의미, 변환 규칙, 반자동 생성 규칙은 [`./stitch_structured_handoff_contract.md`](./stitch_structured_handoff_contract.md)를 따른다.

출력:
- `.stitch/contracts/blueprints/*.json`
- `.stitch/contracts/screens/*.screen.json`

### 4. Presentation Contract를 읽는다

- Unity 구현 전에 관련 presenter/view의 `[Required]` field를 먼저 확인한다.
- screen이 요구하는 block이 실제로 어떤 view owner와 연결되는지 읽는다.
- 이 단계에서 `예쁘게 비슷함`보다 `어떤 참조가 반드시 살아야 하는가`를 우선한다.

주로 확인할 것:
- `*PageController`
- `*View`
- `RequiredFieldValidator`
- scene/prefab contract helper

### 5. Unity에서 Prefab-First Translation을 수행한다

- 기본 route는 `prefab-first reset`
- accepted handoff -> presentation contract -> baseline prefab wiring -> scene assembly 순서로 간다.
- scene repair보다 prefab baseline을 먼저 닫는다.

실행 원칙:
- Unity MCP를 우선 사용한다.
- direct YAML overwrite를 기본값으로 쓰지 않는다.
- 가장 작은 surface부터 만든다.
- root surface는 skeleton부터 세우고, decorative polish는 뒤로 미룬다.

### 6. Fidelity Check를 수행한다

이 단계는 이 문서의 핵심이다.
contract와 wiring이 맞아도 아래 항목이 source에서 크게 벗어나면 `done`으로 닫지 않는다.
평가는 `pass/fail`로만 적는다.
애매하면 `pass`로 넘기지 말고 `fail`로 적고, 무엇이 어긋났는지 한 줄 이유를 남긴다.

#### Structure Fidelity

- block order만 아니라 `방향`도 본다.
- `row`, `strip`, `stack`, `single scroll body`, `sticky footer` 같은 구조 인상을 보존한다.
- stretch/fixed-height 혼합 때문에 source의 레이아웃 posture가 바뀌지 않게 본다.

Pass:
- source의 주요 block order와 방향이 유지된다
- source의 root posture가 같은 화면으로 읽힌다

Fail:
- 상단 `slot strip`이 세로 list로 바뀐다
- `save dock`가 full-width persistent action에서 작은 inline button으로 줄어든다
- `single scroll body`가 여러 개의 분절된 dashboard panel처럼 읽힌다

#### Emphasis Fidelity

- first read가 무엇인지 유지한다.
- primary CTA, selected block, focused editor의 시선 우선순위를 source와 비교한다.
- auxiliary surface가 main workflow보다 더 크거나 강하게 보이면 실패다.

Pass:
- source에서 가장 먼저 읽히는 block과 Unity에서 가장 먼저 읽히는 block이 같다
- primary와 auxiliary의 시선 순서가 source와 같다

Fail:
- selected slot보다 settings가 더 먼저 보인다
- focused editor보다 다른 보조 card가 더 큰 주목을 받는다
- primary CTA가 secondary action과 비슷한 무게로 읽힌다

#### Completion Fidelity

- preview, summary, empty state가 placeholder처럼 보이면 안 된다.
- source가 `finished card`처럼 보이는 surface는 Unity에서도 최소한 완성된 평가 카드처럼 보여야 한다.
- `검은 빈칸`, `임시 텍스트 덩어리`, `레이블만 있는 박스` 상태로 두지 않는다.

Pass:
- preview / summary / empty state가 완성된 surface처럼 읽힌다
- source의 평가 card 인상이 Unity에서도 유지된다

Fail:
- preview가 비어 있는 박스나 임시 placeholder로 보인다
- summary가 단순 텍스트 몇 줄로 축소된다
- empty state가 미완성처럼 보여 source의 완결감을 잃는다

#### Density Fidelity

- source가 compact tactical dashboard이면 Unity도 같은 밀도로 읽혀야 한다.
- card 내부 정보량, padding, chip/stat 압축감이 너무 느슨해지면 source와 다른 화면이 된다.
- skeleton 단계라도 first-screen density는 source와 크게 어긋나지 않게 맞춘다.

Pass:
- first-screen 정보량과 card 압축감이 source와 같은 등급으로 읽힌다
- 과도한 빈 공간 때문에 source와 다른 제품처럼 보이지 않는다

Fail:
- source는 compact card인데 Unity는 느슨하고 비어 보인다
- chip/stat row/action cluster가 사라져 정보 밀도가 눈에 띄게 낮아진다
- skeleton이라는 이유로 first-screen density가 source보다 크게 약해진다

#### CTA Fidelity

- primary CTA의 폭, 색, 고정성, 반복 노출 여부를 본다.
- `secondary`와 `tertiary` action이 primary action과 경쟁하지 않게 본다.
- action 부재가 의도인 surface는 임의 CTA를 추가하지 않는다.

Pass:
- primary CTA가 source와 같은 종류의 강도와 지속성으로 읽힌다
- auxiliary CTA가 primary CTA를 이기지 않는다

Fail:
- source의 persistent dock CTA가 inline button으로 약해진다
- source에 없던 CTA를 임의로 추가한다
- secondary CTA가 source보다 더 크게 부각된다

#### State Fidelity

- selected / empty / disabled / loading posture가 source에서 읽히는 방식으로 유지되는지 본다.
- selected state가 source의 핵심 인상인데 Unity에서 구분이 약하면 구조가 맞아도 실패다.

Pass:
- selected / empty / disabled / loading 상태가 source처럼 명확히 구분된다
- source가 전달하는 state hierarchy를 Unity에서도 같은 수준으로 읽을 수 있다

Fail:
- selected와 unselected 차이가 약해 핵심 state를 바로 읽을 수 없다
- empty / filled / disabled 구분이 source보다 흐려진다
- loading/error posture가 source와 다른 상호작용 의미를 만든다

### 7. Evidence를 남긴다

최소 검증 순서:

1. compile/reload 안정화
2. workflow policy check
3. prefab hierarchy / required ref 점검
4. scene-specific smoke 또는 screenshot evidence

대표 evidence:
- `artifacts/unity/*.json`
- smoke screenshot
- prefab wiring review 결과

### 8. 문서를 같이 맞춘다

- intake, manifest, blueprint, translation evidence 사이 날짜와 내용이 어긋나지 않게 맞춘다.
- 새 family 루프가 생기면 `tools/stitch/README.md` 또는 관련 reference에 짧게 남긴다.
- 규칙이 바뀐 게 아니면 SSOT를 늘리지 않고 reference와 artifact만 보강한다.

## 흔한 실패

- intake 없이 screen 기억만 믿고 바로 Unity를 만지는 것
- CTA가 안 보이는데 manifest에 임의 action을 추가하는 것
- Stitch screenshot의 absolute position을 그대로 Unity 구조로 복제하려는 것
- block order만 맞고 방향/비율/강조를 놓친 채 `비슷하게 구현됐다`고 닫는 것
- manifest 없이 “대충 맞는 hierarchy”만 만든 뒤 code에서 보정하려는 것
- Required ref가 비어 있는데 smoke부터 돌리는 것
- source가 finished card인데 Unity가 placeholder card로 남는 것

## 좋은 종료 조건

- intake를 다시 읽으면 accepted screen을 거의 복원할 수 있다
- manifest를 다시 읽으면 implementer가 Unity target을 추측하지 않아도 된다
- prefab/scene를 다시 읽으면 Required wiring이 계약대로 닫혀 있다
- source Stitch screen과 나란히 봤을 때 각 fidelity 항목이 `pass/fail`로 분명히 판정된다
- blocker 역할을 하는 fidelity 항목이 모두 `pass`다
- evidence가 해당 contract 이후 시점에 생성돼 있다
