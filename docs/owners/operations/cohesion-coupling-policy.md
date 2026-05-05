# Cohesion Coupling Policy

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: ops.cohesion-coupling-policy
> role: ssot
> owner_scope: 응집도와 결합도 정의, 판정 단위, hard-fail과 review gate 경계
> upstream: docs.index
> artifacts: none

이 문서는 JG 레포에서 문서, 코드, 씬, 프리팹, 자동화를 볼 때 공통으로 적용하는 응집도/결합도 상위 기준이다.
응집도와 결합도의 정의, 판정 순서, 예외, 강제 경계는 여기 한 곳에만 둔다.
세부 lane 문서는 이 기준의 파생 규칙만 소유한다.

## 목적

- 같은 이유로 바뀌는 내용을 한 owner와 한 경계에 모은다.
- 한 변경의 여파가 여러 owner로 번지는 경로를 줄인다.
- fallback, runtime repair, hidden lookup으로 결합도를 숨기지 않는다.
- hard-fail lint와 review 판단의 경계를 고정한다.

repo-local owner 문서가 이 레포의 최종 기준이다.
general-purpose skill 문서는 배경 설명으로만 참고하고, 현재 owner 판단은 `docs/index.md`와 이 문서를 우선한다.

## 핵심 기준

1. 먼저 `같은 이유로 바뀌는가`를 본다.
2. 다음 `변경의 여파가 어디까지 번지는가`를 본다.
3. 줄 수 감소, 파일 수 감소, DRY는 보조 기준일 뿐 1차 기준이 아니다.
4. ripple을 줄이겠다고 서로 다른 변경 이유를 한 owner에 합치지 않는다.
5. fallback, runtime repair, hidden lookup으로 coupling을 가리는 방식은 금지한다.

## 판정 단위

- 문서: `entry`, `ssot`, `plan`, `reference`, `historical`, `skill-entry`
- 코드: feature, layer, class, scene root, setup, controller, adapter, contract
- 자산: scene/prefab serialized contract, Stitch contract, tool script

## 변경 이유 분류

- 제품 판단
- 진행 상태
- runtime contract와 wiring
- 상태 렌더와 UI chrome
- 도메인 규칙
- application orchestration
- 외부 연동과 transport
- 검증과 운영 workflow
- historical record

## 적용 규칙

- 먼저 같은 이유로 바뀌는 내용을 모으고, 그다음 seam을 둬 ripple을 줄인다.
- entry, registry, catalog, checklist, historical 문서는 링크와 탐색을 맡아도 본문 owner가 되지 않는다.
- composition root와 `*Setup`, `*Root`는 wiring 예외 지점일 수 있지만, 다른 변경 이유까지 흡수하는 만능 owner가 되면 안 된다.
- production `*PageController`나 runtime controller는 smoke host, style owner, fallback owner, transport owner를 동시에 겸하지 않는다.
- provider 구현 세부는 consumer owner까지 번지지 않게 port, adapter, contract seam에서 끊는다.
- 문서는 동일 결정의 장문 재서술을 만들지 않고 owner 문서로 위임한다.

## Review Procedure

응집도/결합도 리뷰는 줄 수나 파일 수보다 `reason-to-change`, caller impact, failure owner를 먼저 본다.
리뷰 skill은 이 절차로 라우팅만 하고, 판단 기준 본문은 이 문서가 소유한다.

1. 변경 이유를 한 문장으로 잠근다.
   - 한 문장 안에 서로 다른 이유가 섞이면 split-owner 후보로 본다.
2. 익숙하지 않은 영역은 먼저 지도를 만든다.
   - public entrypoint, direct caller, downstream consumer, provider/adapter, scene/prefab/tool contract, test/smoke surface를 확인한다.
   - 현재 caller와 likely failure owner를 말할 수 없으면 split/seam 결론 대신 `blocked` 또는 추가 탐색으로 둔다.
3. owner와 reference direction을 분리한다.
   - 문서는 `docs/index.md`와 stable `doc_id`로 찾는다.
   - 코드는 feature/layer/class/setup/root/presentation/adapter/contract 중 어디가 실패를 고칠 owner인지 본다.
   - Unity는 serialized scene/prefab contract와 runtime code를 분리한다.
4. ripple을 추적한다.
   - 함께 바뀌는 docs/files/scenes/prefabs/scripts/artifacts가 같은 이유로 바뀌는지 확인한다.
   - 다른 owner의 세부가 새는 바람에 따라 바뀌는 항목은 coupling으로 표시한다.
5. seam을 점검한다.
   - code seam: interface, port, adapter, event, DTO, contract
   - Unity seam: serialized reference, scene/prefab contract, explicit bootstrap registration, scene-local registrar
   - document seam: registry, owner doc, stable `doc_id`, reference link
   - seam이 없으면 direct coupling으로 본다.
6. interface depth와 test surface를 본다.
   - interface는 type shape, ordering, invariants, error modes, config, performance expectation까지 포함한다.
   - deep module은 작은 public surface 뒤에 의미 있는 규칙이나 integration complexity를 숨긴다.
   - shallow module은 provider vocabulary, config knob, call order만 caller에게 전달한다.
   - 삭제 테스트를 적용한다: 없애면 pass-through가 사라지는지, 아니면 규칙이 caller로 퍼지는지 본다.
   - 테스트는 caller가 쓰는 interface를 우선 검증한다. 내부 collaborator mock이나 call-order 검증이 많으면 seam 또는 owner shape를 재검토한다.
   - mock은 network, filesystem, time, randomness, Unity runtime, third-party SDK 같은 hard external boundary에 우선 둔다.
7. verdict를 분리한다.
   - `keep together`: 같은 reason-to-change와 같은 failure owner
   - `split owner`: reason, success criteria, rhythm, failure owner가 다름
   - `add seam`: direct coupling은 실제지만 contract/adapter/registry로 ripple을 줄일 수 있음
   - `blocked`: local evidence가 부족함

## 허용 예외

- `AGENTS.md`, `docs/index.md` 같은 entry/registry 문서
- composition root 성격의 `*Setup`, `*Root`
- adapter, bridge, map, catalog, checklist, historical 문서

이 예외는 참조 집중이나 wiring 집중을 허용할 뿐, 여러 변경 이유의 본문 owner가 되는 것을 허용하지 않는다.

## 금지 예시

- 문서 A와 문서 B가 같은 결정을 각각 본문으로 설명한다.
- production controller가 smoke entrypoint, chrome styling, fallback 보정까지 함께 소유한다.
- scene/prefab contract 누락을 runtime lookup, `AddComponent`, script-side fallback으로 메운다.
- provider 구현 세부 변경 때문에 consumer owner 문서나 controller가 함께 흔들린다.

## 강제 기준

### Hard Fail

- `docs:lint`가 잡는 owner, path, metadata, index registry, `doc_id`, routing 위반
- `stitch-driven policy lint`
- repo에서 이미 금지한 fallback, style ownership, smoke host 위반
- Core cohesion/coupling lint는 관측 가능한 네 가지 흔적만 hard-fail로 둔다: active plan 상호 참조, active plan artifact owner shape drift, active plan concrete artifact owner collision, `plans.progress` concrete evidence overload.
- 의미 판단은 hard-fail로 만들지 않는다. "같은 이유로 바뀌는가", owner boundary 재설계, route 문서 장문화 여부는 Review Gate에 남긴다.

### Review Gate

- “같은 이유로 바뀌는가” 같은 의미 판단
- `*Setup`, `*Root`의 상태 소유 과다
- feature 분리 시점과 owner boundary 재설계
- 문서 간 장문 재서술 여부

### Out Of Scope

- 줄 수나 파일 수만으로 계산하는 일반화된 cohesion score
- repo 전역에 일괄 적용하는 추상 coupling score lint
- 현재 구조를 바로 깨뜨릴 수 있는 전역 `*Setup`/`*Root` hard-fail lint

## 연결 원칙

- 문서 운영 lane은 `ops.document-management-workflow`가 소유하되, 응집도/결합도 정의는 이 문서를 따른다.
- Unity UI authoring lane은 `ops.unity-ui-authoring-workflow`가 소유하되, owner 분리와 ripple 판단의 상위 기준은 이 문서를 따른다.
- Lobby/Garage UI 설계 lane은 `design.ui-foundations`가 소유하되, 책임 분리의 상위 기준은 이 문서를 따른다.
