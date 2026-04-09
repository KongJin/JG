# Unit Feature

## Responsibility

`Unit`은 유닛 조합 결과 계산, 소환, 배틀 엔티티 초기화, 유닛 슬롯 UI와 관련된 계약을 소유한다.

## Entrypoints

* `UnitSetup.cs`
  순수 C# composition root
* `UnitBootstrap.cs`
  scene-level wiring, catalog/summon adapter 연결
* `BattleEntitySetup.cs`
  BattleEntity feature root

## Local Contract

* `UnitBootstrap`는 `ModuleCatalog`, `SummonPhotonAdapter`를 inspector로 받아야 한다.
* `BattleEntity` 초기화는 `UnitBootstrap.InitializeBattleEntity(...)` 경로로만 진행한다.
* Unit Presentation과 Application은 `Unit.Domain.Unit` 타입을 쓸 때 bare `Unit` 대신 alias를 기본값으로 사용한다.
* EventBus 계약은 실제 Shared 선언 기준으로 사용한다. 존재하지 않는 `IEventBus` 같은 이름을 새로 만들지 않는다.
* `PlacementArea`와 배치 판정/시각화는 Domain이 아니라 Presentation 소유다.
* `SummonPhotonAdapter`와 `BattleEntityPrefabSetup` wiring은 Bootstrap이 concrete `EventBus`를 조립하고, 나머지 레이어는 필요한 최소 계약만 받는다.

## Scene-owned / Prefab-owned Notes

* `SummonPhotonAdapter`가 Photon instantiate를 담당한다.
* `BattleEntityPrefabSetup`는 battle entity prefab 쪽 serialized references와 late-join 초기화를 소유한다.
* prefab reference 누락은 runtime fallback으로 고치지 않는다. 씬/프리팹에서 직접 연결한다.

## Cross-feature Dependencies

* `Garage`
* `Combat`
* `Wave`
* `Player`

## Validation Notes

* `Unit`은 namespace/type 충돌 위험이 높은 feature다. `Features.Unit` namespace와 `Features.Unit.Domain.Unit` type을 함께 쓸 때 compile-clean을 반드시 확인한다.
* `clean` 판정은 `/agent/validation_gates.md`를 따른다.
* compile hazard:
  `Unit` short-type shadowing, phantom shared contract, `PlacementArea`의 잘못된 레이어 배치, `Subscribe()` 반환형 오해를 먼저 점검한다.
