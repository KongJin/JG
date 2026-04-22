# Stitch Handoff Completeness Checklist

> 마지막 업데이트: 2026-04-23
> 상태: reference
> doc_id: ops.stitch-handoff-completeness-checklist
> role: reference
> owner_scope: Stitch contract handoff completeness quick check
> upstream: docs.index, ops.stitch-data-workflow, ops.stitch-structured-handoff-contract
> artifacts: `.stitch/contracts/screens/*.json`, `.stitch/contracts/blueprints/*.json`, `.stitch/contracts/mappings/*.json`

이 문서는 Stitch handoff가 Unity translation에 들어가기 전에 빠르게 completeness를 확인하는 reference checklist다.
데이터 ownership은 `ops.stitch-data-workflow`, JSON 구조 기준은 `ops.stitch-structured-handoff-contract`가 소유한다.
여기서는 owner 문서를 다시 쓰지 않고, handoff를 넘겨도 되는 최소 확인 항목만 본다.

## 체크 순서

1. baseline source가 고정돼 있는지 확인한다.
2. `screen manifest`가 semantic block과 CTA 우선순위를 잃지 않았는지 확인한다.
3. 필요하면 blueprint가 중복 없이 재사용 경계를 설명하는지 확인한다.
4. `unity-map`이 각 block의 host/wiring 경로를 직접 따라갈 수 있게 적혀 있는지 확인한다.
5. contract가 비어 있는 값을 script fallback으로 메우도록 방치하지 않았는지 확인한다.

## 필수 확인 항목

- `source.projectId`, `source.screenId`, `source.url`이 채워져 있다.
- `blocks[]`만 읽어도 first read order와 주요 UI 덩어리가 따라간다.
- `ctaPriority[]`만 읽어도 primary/secondary CTA posture가 따라간다.
- `states`, `requiredChecks`, `firstReadOrder`가 검증 초점을 설명한다.
- Unity host 연결은 `.stitch/contracts/mappings/*.json` 또는 active `unity-map`으로 바로 추적된다.
- contract가 빠진 값을 script-side constants나 fallback으로 대신하지 않는다.

## 드래프트로 남겨야 하는 신호

- source가 accepted baseline인지 불분명하다.
- `blocks[]` 설명만으로 구조를 재구성할 수 없다.
- CTA hierarchy가 문서마다 다르게 읽힌다.
- block 하나 이상이 Unity host 또는 verification path와 연결되지 않는다.
- open question이 남아 있는데도 accepted처럼 다루고 있다.

## handoff 가능 기준

아래가 모두 참이면 handoff 가능으로 본다.

- accepted source와 active contract가 1:1로 묶여 있다.
- semantic block 순서와 CTA hierarchy가 owner 문서 기준으로 바로 읽힌다.
- Unity translation이 필요한 host/wiring/verification 경로가 빠지지 않았다.
- 누락 시 script가 보정하는 대신 실패하도록 설계돼 있다.
