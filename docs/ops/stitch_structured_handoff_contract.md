# Stitch Structured Handoff Contract

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: ops.stitch-structured-handoff-contract
> role: ssot
> owner_scope: Stitch 산출물을 Unity 번역 계약 JSON으로 고정하는 구조, 필수 필드, 금지 입력
> upstream: docs.index, ops.stitch-data-workflow, design.ui-foundations, ops.unity-ui-authoring-workflow
> artifacts: `.stitch/contracts/*.json`, `.stitch/contracts/intakes/*.json`, `.stitch/contracts/blueprints/*.json`, `.stitch/contracts/screens/*.json`, `.stitch/contracts/schema/*.json`

이 문서는 JG에서 `Stitch -> Unity` 전달 형식을 **구조화된 JSON contract**로 고정하는 단일 기준이다.
이제 Unity handoff의 활성 경로는 `png/html export`나 set별 `handoff md`가 아니라 `.stitch/contracts/*.json` 계열 JSON이다.
기본값은 `screen intake -> blueprint + screen manifest` 조합이고, full contract는 one-off surface나 migration fallback일 때만 쓴다.

## 목적

- 사람 눈에 보이는 시안과 Unity가 실제로 필요한 번역 계약을 분리한다.
- `예쁘다`와 `구현 가능하다`를 같은 파일에 섞지 않는다.
- generator와 implementer가 같은 입력을 읽도록 구조를 고정한다.

## 활성 산출물

- 활성 handoff:
  - `.stitch/contracts/intakes/*.json`
  - `.stitch/contracts/blueprints/*.json`
  - `.stitch/contracts/screens/*.json`
  - 필요 시 full contract `.stitch/contracts/*.json`
- 활성 schema:
  - `.stitch/contracts/schema/stitch-screen-intake.schema.json`
  - `.stitch/contracts/schema/stitch-blueprint.schema.json`
  - `.stitch/contracts/schema/stitch-screen-manifest.schema.json`
  - legacy/full fallback: `.stitch/contracts/schema/stitch-handoff-contract.schema.json`

## 비활성 산출물

아래 경로는 새 handoff의 입력 또는 acceptance artifact로 사용하지 않는다.

- `.stitch/designs/*.{png,html}`
- `.stitch/handoff/*.md`

기존 파일이 남아 있어도 `historical reference`로만 취급한다.
새 파일 생성, 갱신, 의존 추가를 금지한다.

## 왜 JSON인가

Unity 번역 단계에서는 아래가 먼저 필요하다.

- surface 역할
- block order
- root / repeat / independent / shared 구분
- target prefab root / scene root
- required path
- serialized reference owner
- CTA 우선순위
- state 집합
- validation focus

이 정보는 screenshot보다 JSON이 훨씬 안정적으로 전달한다.

## 기본 구조

### 1. 기본값: Screen Intake -> Blueprint + Screen Manifest

재사용이 필요한 surface family는 아래 두 파일로 나눈다.

- `screen intake`
  - accepted screen만 보고 추출 가능한 block, CTA, state, validation focus를 가진다.
- `blueprint`
  - 공통 block skeleton, 기본 state, 기본 validation만 가진다.
- `screen manifest`
  - concrete Unity target, CTA, screen-specific state/validation, 필요한 delta만 가진다.

권장 대상:

- Lobby / Garage / Overlay / Battle HUD / Result 같은 family
- populated / empty / loading / error처럼 상태만 달라지는 screen

권장 흐름:

1. Stitch에서 screen accepted
2. accepted screen 기준으로 `screen intake` 작성
3. intake를 보고 reusable family면 `blueprint`와 `screen manifest`로 분해
4. one-off surface면 필요 시 full contract로 바로 내린다

### 2. fallback: Full Contract

one-off surface이거나 blueprint로 나눌 가치가 아직 없을 때는 full contract 하나로 작성할 수 있다.

최소 top-level 필드는 아래를 가진다.

- `schemaVersion`
- `setId`
- `surfaceId`
- `surfaceRole`
- `status`
- `targets`
- `blocks`
- `ctaPriority`
- `states`
- `validation`

## 필드 의미

### 식별

- `setId`: `set-a`, `set-b` 같은 세트 식별자
- `surfaceId`: `lobby-root`, `garage-root`, `login-loading-overlay` 같은 surface 식별자
- `surfaceRole`: `root`, `repeat`, `independent`, `shared`
- `status`: `draft`, `accepted`, `historical`

### Targets

`targets`는 Unity 번역의 고정 목적지를 담는다.

- `prefabPath`
- `sceneRoots`
- `primaryContractPaths`
- `serializedOwners`

### Blocks

`blocks`는 surface 내부의 의미 블록을 순서대로 나열한다.
각 block은 최소한 아래를 가진다.

- `blockId`
- `role`
- `sourceName`
- `unityTargetPath`
- `layout`
- `children`

`role`은 아래 enum만 허용한다.

- `root`
- `section`
- `repeat-item`
- `overlay`
- `cta`
- `content`
- `status`
- `shared-chrome`

### Layout

`layout`은 시각 인상이 아니라 번역 규칙을 적는다.

- `axis`: `vertical`, `horizontal`, `overlay`, `free`
- `padding`
- `gap`
- `minHeight`
- `minWidth`
- `alignment`
- `sticky`
- `scroll`

### CTA Priority

CTA는 반드시 배열로 명시한다.

각 항목은 아래를 가진다.

- `id`
- `priority`: `primary`, `secondary`, `tertiary`
- `unityTargetPath`
- `outcome`

### States

화면이 가져야 하는 상태를 고정한다.

- `default`
- `empty`
- `loading`
- `error`
- `selected`
- `disabled`

surface마다 필요 없는 상태는 생략할 수 있지만,
필요 상태를 구현자 추측에 맡기면 안 된다.

### Validation

`validation`은 Unity acceptance에 필요한 기준을 적는다.

- `frame`
- `firstReadOrder`
- `requiredChecks`
- `smokeScripts`

## Screen Intake 구조

`screen intake`는 accepted screen을 보고 추출 가능한 정보만 가진다.
이 단계에서는 아직 Unity target, serialized owner, smoke script를 확정하지 않는다.

`screen intake`는 아래 정보를 가진다.

- `contractKind = "screen-intake"`
- `setId`
- `surfaceId`
- `surfaceRole`
- `status`
- `source`
- optional `familyHints`
- `blocks`
- `ctaPriority`
- `states`
- `validation`
- optional `notes`
- optional `openQuestions`

원칙:

- intake는 screen-derived 사실만 적는다.
- `targets`와 `serializedOwners`는 intake에 넣지 않는다.
- CTA는 label, priority, expected outcome까지만 적고 Unity path는 적지 않는다.
- validation도 screen이 요구하는 읽기 순서와 체크 포인트만 적고 smoke script는 적지 않는다.
- intake는 manifest보다 가볍고, accepted screen을 사람이 다시 읽지 않도록 충분히 꼼꼼해야 한다.

## Blueprint 구조

`blueprint`는 아래 정보를 가진다.

- `contractKind = "blueprint"`
- `blueprintId`
- `surfaceRole`
- `blocks`
- `defaults.states`
- `defaults.validation`

원칙:

- blueprint는 reusable skeleton만 가진다.
- concrete prefab path, scene root, serialized owner는 screen manifest가 가진다.
- block 이름과 hierarchy 의미는 blueprint에서 고정하고, manifest는 필요한 곳만 덮어쓴다.

## Screen Manifest 구조

`screen manifest`는 아래 정보를 가진다.

- `contractKind = "screen-manifest"`
- `setId`
- `surfaceId`
- `surfaceRole`
- `status`
- `extends`
- `targets`
- `ctaPriority`
- `states`
- `validation`
- optional `blockOverrides`
- optional `appendBlocks`

원칙:

- manifest는 concrete Unity target과 screen-specific delta만 가진다.
- 동일 family의 공통 block 구조를 매 screen마다 다시 복사하지 않는다.
- `targets`, `ctaPriority`, `states`, `validation`은 manifest에서 생략하지 않는다.
- intake에 있던 CTA/validation 정보는 manifest에서 Unity binding이 붙은 형태로 재기록한다.

## Intake -> Manifest 변환 규칙

`screen intake`와 `screen manifest`는 같은 정보를 두 번 적는 문서가 아니다.
intake는 accepted screen을 읽은 기록이고, manifest는 Unity translation binding이 붙은 구현 입력이다.

### 그대로 유지되는 것

아래 정보는 intake에서 manifest로 거의 그대로 유지된다.

| intake | manifest | 규칙 |
|---|---|---|
| `setId` | `setId` | 그대로 복사 |
| `surfaceId` | `surfaceId` | 그대로 복사 |
| `surfaceRole` | `surfaceRole` | 그대로 복사 |
| `status` | `status` | accepted/draft 상태 그대로 유지 |
| `states` | `states` | screen이 요구하는 상태를 유지 |
| `validation.frame` | `validation.frame` | frame 기준 그대로 유지 |
| `notes` | `notes` | 필요한 것만 정리 후 유지 |

### 해석해서 생성되는 것

아래 정보는 intake만으로는 부족하고, blueprint 선택 또는 Unity binding이 붙으면서 생성된다.

| intake 입력 | manifest 출력 | 규칙 |
|---|---|---|
| `familyHints.suggestedBlueprintId` | `extends` | reusable family가 맞으면 blueprint id로 승격 |
| `blocks` | `blockOverrides` 또는 `appendBlocks` | 공통 skeleton과 겹치는 내용은 override로 축소하고, 새 의미 블록만 append |
| `ctaPriority`의 `id/label/outcome/placement` | `ctaPriority`의 `unityTargetPath` 포함 항목 | CTA 우선순위는 유지하되 Unity target path를 추가 |
| `validation.firstReadOrder` | `validation.firstReadOrder` | 필요 시 blueprint block id에 맞춰 정규화 |
| `validation.requiredChecks` | `validation.requiredChecks` | screen-only 문장을 contract/implementation 기준 문장으로 정리 |
| `openQuestions` | manifest 미반영 또는 별도 확인 후 반영 | unresolved 질문은 자동 반영하지 않음 |

### manifest 단계에서 새로 생기는 것

아래 정보는 accepted screen만 보고 확정할 수 없으므로 intake에 넣지 않고, manifest 단계에서 처음 생긴다.

| manifest 전용 필드 | 생성 근거 |
|---|---|
| `targets.prefabPath` | feature별 Unity target path 결정 |
| `targets.sceneRoots` | scene composition contract |
| `targets.primaryContractPaths` | prefab/scene 필수 child path |
| `targets.serializedOwners` | presenter/view ownership |
| `ctaPriority[].unityTargetPath` | 실제 button/location binding |
| `validation.smokeScripts` | Unity lane validation route |

### intake에 남겨야 하고, manifest에서 압축되기 쉬운 것

아래 정보는 manifest로 내려가며 쉽게 사라지므로 intake에서 더 꼼꼼하게 적는다.

- screen이 실제로 보여주는 첫 읽기 순서
- CTA가 없다는 사실 자체
- support copy의 개수와 밀도
- overlay 뒤에 무엇이 보여야 하는지 같은 context requirement
- 아직 확정되지 않은 escape action, secondary action 같은 open question

### login-loading-overlay 예시

- intake:
  - `overlay-card`, `status-copy`, `status-subcopy`까지 읽기 순서를 적는다.
  - steady loading state에서 loud primary action이 없다는 사실을 적는다.
  - secondary CTA가 실제로 노출되는지 불확실하면 `openQuestions`로 남긴다.
- manifest:
  - `extends = overlay-modal`
  - `targets.*` 추가
  - 필요 시 secondary CTA를 Unity target path와 함께 확정
  - blueprint와 겹치는 block은 `blockOverrides`로만 남긴다

## Intake -> Manifest 반자동 생성 규칙

반자동 생성은 `screen intake`를 읽고 초벌 `screen manifest`를 만드는 단계다.
이 단계의 목표는 implementer가 바로 시작할 수 있는 초안을 만드는 것이지,
화면에 없던 결정을 새로 발명하는 것이 아니다.

### 자동으로 내려도 되는 것

아래는 intake에서 manifest로 자동 생성해도 된다.

1. `setId`, `surfaceId`, `surfaceRole`, `status` 복사
2. `familyHints.suggestedBlueprintId`를 `extends`로 승격
3. `states` 복사
4. `validation.frame` 복사
5. `validation.firstReadOrder`를 blueprint block id 체계에 맞춰 정규화
6. `notes`를 manifest notes 또는 `blockOverrides.*.notes`로 재배치

### 자동으로 내려면 추가 매핑이 필요한 것

아래는 intake만으로는 부족하고, repo mapping table 또는 family 규칙이 있어야 자동화할 수 있다.

1. `targets.prefabPath`
2. `targets.sceneRoots`
3. `targets.primaryContractPaths`
4. `targets.serializedOwners`
5. `ctaPriority[].unityTargetPath`
6. `validation.smokeScripts`

이 값들은 `feature area + blueprint family + surfaceId` 조합으로 결정한다.
즉 `screen`만 보고 생성하지 않고, repo mapping을 붙여서 생성한다.

### 자동 생성이 멈춰야 하는 경우

아래 경우에는 manifest를 자동으로 완성하지 말고 `draft`로 멈춘다.

1. `familyHints.suggestedBlueprintId`가 비어 있고 one-off 여부도 불명확할 때
2. CTA label은 보이는데 실제 노출 여부나 우선순위가 확정되지 않았을 때
3. 같은 intake block이 어떤 blueprint block으로 매핑되는지 모호할 때
4. presenter/view ownership 후보가 둘 이상일 때
5. `openQuestions`가 남아 있는데 그 답이 `targets`나 CTA binding에 직접 영향을 줄 때

### CTA 생성 규칙

CTA는 가장 추측이 섞이기 쉬우므로 아래 규칙을 고정한다.

1. intake에 CTA가 없으면 manifest에서 임의 CTA를 생성하지 않는다.
2. intake에 CTA가 있어도 `label`, `placement`, `outcome`만으로는 `unityTargetPath`를 추측하지 않는다.
3. screen에서 action 존재가 불확실하면 CTA를 만들지 말고 `openQuestions`를 유지한다.
4. steady loading state처럼 action 부재가 의도인 surface는 manifest에 `primary-cta.enabled = false` 또는 빈 `ctaPriority`로 명시한다.

### Block 생성 규칙

1. intake block이 blueprint skeleton과 의미적으로 같으면 새 block을 만들지 않고 `blockOverrides`만 쓴다.
2. intake block이 blueprint에 없는 새 의미 단위면 `appendBlocks`에 추가한다.
3. intake의 세부 copy block은 Unity에서 독립 hierarchy가 꼭 필요할 때만 manifest block으로 승격한다.
4. 단순한 읽기 순서 보조 단위는 intake에만 남고 manifest에서는 상위 block validation으로 흡수할 수 있다.

### Validation 생성 규칙

1. intake의 `requiredChecks`는 manifest에서도 버리지 않는다.
2. 다만 Unity translation 단계에서는 `contract`, `binding`, `visibility`, `state posture` 같은 구현 체크 문장으로 정리한다.
3. intake에 적힌 context requirement는 manifest에서도 유지한다.
예:
`dimmed lobby visible behind scrim`
4. smoke script는 화면에서 보인다고 자동 선택하지 않는다.
surface family에 이미 정의된 script가 있을 때만 추가한다.

### 기본 출력 상태

반자동 생성기가 만드는 기본 manifest 상태는 아래를 따른다.

- `status = draft`
- unresolved `openQuestions`는 사라지지 않는다
- 미확정 CTA는 생략한다
- 미확정 target은 placeholder로 두지 않고 생성 실패로 돌린다

즉:
`빈 값을 채운 manifest`보다 `멈춘 draft manifest`가 낫다.

## 반자동 생성 우선순위

반자동 생성은 모든 surface family에 동시에 적용하지 않는다.
먼저 `screen truth`와 `Unity binding` 사이 변동이 적은 family부터 시작한다.

| 우선순위 | family | 현재 판단 | 이유 |
|---|---|---|---|
| 1 | `overlay-modal` | 높음 | block 구조가 단순하고, `loading/error/confirm` 계열은 intake 기반 비교가 이미 가능하다 |
| 2 | `result-overlay` / `feedback` | 중간-높음 | overlay 계열과 비슷하게 독립 surface가 많고 CTA/read order가 비교적 명확하다 |
| 3 | simple `repeat-item` family | 중간 | 카드 구조는 반복되지만, 어떤 item contract로 묶을지 먼저 안정화가 필요하다 |
| 4 | `lobby-root` | 낮음 | root shell은 section 간 우선순위, shared chrome, auxiliary panel 경계가 많아 block 압축 손실이 크다 |
| 5 | `garage-workspace` | 낮음 | scroll body, fixed save dock, focused editor, summary, settings overlay가 함께 엮여 binding 의존성이 높다 |
| 6 | `battle-hud` | 매우 낮음 | runtime state와 HUD contract가 강하게 결합돼 있고 visual 반영보다 runtime contract 안정화가 먼저다 |

### 시작 기준

- 가장 먼저 자동화할 family는 `overlay-modal`이다.
- 이 family에서 `loading overlay`, `error dialog`, `confirm dialog`를 먼저 검증한다.
- 다음 후보는 `result-overlay`와 `feedback` 계열이다.

### 아직 미루는 기준

아래 family는 intake는 계속 만들되, 반자동 manifest 생성 기본값으로 올리지 않는다.

- `lobby-root`
- `garage-workspace`
- `battle-hud`

이유:

- section 수가 많고
- shared chrome이 섞여 있고
- presenter/view ownership 또는 runtime contract 의존성이 크기 때문이다.

## 언제 full contract를 쓰는가

- screen이 truly one-off라 재사용 가치가 거의 없을 때
- blueprint 도입 전 migration 중간 단계일 때
- implementer가 screen 하나만 빠르게 실험해야 하고 reuse 가치가 아직 검증되지 않았을 때

그 외에는 `blueprint + screen manifest`를 기본값으로 권장한다.

## 작성 원칙

- 시각 묘사보다 번역 결정을 먼저 적는다.
- absolute position 복제를 요구하지 않는다.
- `적당히`, `비슷하게`, `상황에 따라` 같은 문장을 쓰지 않는다.
- block order와 CTA 우선순위는 배열 순서로 고정한다.
- repeat item은 독립 surface처럼 쓰지 않고 item contract로 분리한다.
- shared chrome은 page root 안에 숨기지 않고 명시적 surface로 뺀다.
- reusable family는 full contract 복제보다 blueprint 추출을 먼저 검토한다.
- manifest는 thin delta를 유지하고, 공통 skeleton을 새로 복사해 넣지 않는다.

## Generator 사용 원칙

- generator는 JSON contract를 읽어 seed hierarchy와 refs 초벌 연결까지만 수행한다.
- generator가 spacing, visual density, accent 사용량을 최종 결정하면 안 된다.
- 최종 미감과 구조 품질은 hand-authored prefab pass에서 닫는다.
- generator가 blueprint를 읽는 경우 block skeleton까지만 펼치고, manifest가 주는 concrete target/delta만 적용한다.

## Unity Lane 전달 규칙

Unity lane으로 넘길 때 implementer는 아래만 읽어도 시작할 수 있어야 한다.

1. `docs/index.md`
2. `docs/ops/unity_ui_authoring_workflow.md`
3. `docs/design/ui_foundations.md`
4. 해당 `.stitch/contracts/screens/*.json`
5. 해당 manifest가 가리키는 `.stitch/contracts/blueprints/*.json`

## Schema 위치

- blueprint schema: [stitch-blueprint.schema.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/schema/stitch-blueprint.schema.json)
- screen manifest schema: [stitch-screen-manifest.schema.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/schema/stitch-screen-manifest.schema.json)
- full contract fallback schema: [stitch-handoff-contract.schema.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/schema/stitch-handoff-contract.schema.json)
- full contract template: [contract.template.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/contract.template.json)
- blueprint template: [blueprint.template.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/blueprints/blueprint.template.json)
- screen manifest template: [screen-manifest.template.json](/C:/Users/SOL/Documents/JG/.stitch/contracts/screens/screen-manifest.template.json)
