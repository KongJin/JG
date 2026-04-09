# Wave Feature

## Responsibility

`Wave`는 PvE wave 흐름, 적 스폰, 게임 시작/종료 브리지, HUD/엔드뷰 wiring을 소유한다.

## Entrypoints

* `WaveBootstrap.cs`
  scene-level wiring and lifecycle owner

## Local Contract

* `WaveBootstrap`는 `WaveTableData`, `EnemySpawnAdapter`, `PlayerPositionQueryAdapter`, `UnitPositionQueryAdapter`, `WaveHudView`, `WaveEndView`, `WaveFlowController`, `WaveNetworkAdapter`, `CoreHealthHudView`를 inspector로 받아야 한다.
* `WaveGameEndBridge`는 Wave 승패 이벤트를 Player 쪽 `GameEndEvent` 계열로 변환하는 Application bridge다.
* `Wave`는 scene EventBus를 만들지 않는다. `GameSceneRoot`가 만든 버스를 주입받는다.
* `WaveGameEndBridge`가 소비하는 Player 종료 계약은 drift를 허용하지 않는다. Player 이벤트 payload가 바뀌면 bridge와 consumer를 함께 검토한다.

## Scene-owned Notes

* `WaveBootstrap.Initialize(...)`가 wave flow와 네트워크 hydration의 단일 진입점이다.
* enemy arrival fallback은 non-master late-join 경로에 한해 명시적으로 처리한다.

## Cross-feature Dependencies

* `Enemy`
* `Combat`
* `Player`
* `Skill`
* `Unit`

## Validation Notes

* 승패 흐름 변경 시 Wave 자체 정적 규칙뿐 아니라 compile 상태와 game end bridge 연결을 같이 확인한다.
* `clean` 판정은 `/agent/validation_gates.md`를 따른다.
* compile hazard:
  Player 종료 이벤트 계약 drift와 scene-owned helper type 위치 drift를 먼저 점검한다.
