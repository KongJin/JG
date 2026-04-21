# Stitch Handoff Completeness Checklist

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: ops.stitch-handoff-completeness-checklist
> role: reference
> owner_scope: Stitch handoff contract가 implementer에게 decision-complete하게 전달되었는지 점검하는 공통 rubric
> upstream: docs.index, ops.stitch-data-workflow, design.ui-reference-workflow
> artifacts: `.stitch/contracts/blueprints/*.json`, `.stitch/contracts/screens/*.json`, `.stitch/contracts/*.json`

이 문서는 Stitch handoff contract를 작성하거나 검토할 때 쓰는 completeness checklist다.
새 SSOT를 만들지 않고, 기존 owner 문서가 요구하는 판단을 implementer 관점에서 빠뜨리지 않게 점검하는 용도만 맡는다.

## 언제 쓰는가

- `.stitch/contracts/screens/*.json` screen manifest를 새로 작성할 때
- 기존 manifest 또는 blueprint를 최신 Stitch 판단 기준으로 갱신할 때
- Unity 구현 전에 handoff가 바로 실행 가능한지 검토할 때

## 이 문서가 소유하지 않는 것

- Stitch visual 원칙: `design.ui-reference-workflow`
- Stitch 데이터 흐름과 파일 소유권: `ops.stitch-data-workflow`
- Stitch JSON 구조와 schema: `ops.stitch-structured-handoff-contract`
- Unity runtime truth와 authoring route: `ops.unity-ui-authoring-workflow`

## 필수 기입 항목

각 active handoff contract는 최소한 아래 질문에 JSON만으로 답할 수 있어야 한다.

1. 어떤 Stitch screen이 baseline인가?
2. 어떤 screen이 supporting state 또는 overlay인가?
3. 첫 읽기와 CTA 우선순위는 무엇인가?
4. Unity에서 어느 root, 어느 path, 어느 serialized contract를 맞춰야 하는가?
5. 시안에서 무엇을 살리고, 무엇을 버리고, 무엇을 재해석해야 하는가?
6. 구현이 끝났을 때 무엇을 검증해야 하는가?

## 권장 섹션 순서

모든 contract가 완전히 같은 서술문을 가질 필요는 없지만, 아래 구조는 유지한다.

1. 식별 필드
2. `targets`
3. `blocks`
4. `ctaPriority`
5. `states`
6. `validation`

## Baseline 표기 규칙

- `surfaceRole`과 `blocks[*].role`이 역할 표기의 기준이다.
- 파일명이나 export 이름으로 역할을 추측하게 두지 않는다.
- implementer는 `role`과 `unityTargetPath`를 먼저 읽어야 한다.

## Unity 번역에 꼭 있어야 할 것

- target scene 또는 prefab root
- first read를 구성하는 핵심 block order
- primary CTA와 fallback CTA
- required child path 또는 interaction path
- serialized contract를 건드릴 때 주의할 view/component ownership

경로가 확정되지 않았으면 handoff를 완료로 보지 않는다.

## handoff에 쓰지 말아야 할 것

- Stitch absolute positioning을 Unity에서 그대로 복제하라는 지시
- scene/prefab runtime truth를 덮어쓰는 새로운 SSOT 선언
- presentation code가 geometry/layout를 보정할 것이라는 전제
- smoke artifact 없이도 “대충 비슷하게” 구현하라는 표현
- 구현자가 다시 결정을 내려야 하는 애매한 문장
- png/html export 의존
- set별 handoff md 의존

## 완료 판정

handoff는 아래가 모두 참이면 complete로 본다.

- baseline / supporting state가 구분되어 있다
- first read와 CTA hierarchy가 배열 순서로 바로 보인다
- Unity translation root와 핵심 path가 명시되어 있다
- root / repeat / independent / shared 역할이 추측 없이 읽힌다
- validation focus가 최소 한 화면 sanity 기준으로 적혀 있다

위 항목 중 하나라도 빠지면 handoff는 draft로 취급한다.
