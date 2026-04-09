# Player Feature

## Responsibility

`Player`는 플레이어 도메인 상태, 입력, 이동, 에너지, 전투 타깃화, 게임 종료 시점의 로컬 플레이어 관찰을 소유한다.

## Entrypoints

* `PlayerSetup.cs`
  로컬/원격 플레이어 prefab composition root
* `GameSceneRoot.cs`
  게임 씬 루트. scene-owned wiring, cross-feature bootstrap, scene EventBus 생성

## Local Contract

* `PlayerSetup`는 `_networkAdapter`, `_motorAdapter`, `_inputHandler`, `_view`, `_entityIdHolder`, `_statusNetworkAdapter`를 inspector로 받아야 한다.
* 로컬 플레이어 초기화는 `GameSceneRoot -> PlayerSetup.InitializeLocal(...)` 경로로만 시작한다.
* 원격 플레이어 초기화는 Photon instantiate 후 `PlayerSetup.RemoteArrived`를 통해 이어진다.
* Application은 Unity API를 직접 사용하지 않는다. 시간/로그/SDK 접근은 Bootstrap 또는 Infrastructure에서 주입한다.
* `GameEndEvent`와 `GameEndReportRequestedEvent`는 Player feature가 소유하는 종료 계약이다. payload 변경 시 producer, consumer, bridge를 함께 갱신한다.
* `UnitEnergyAdapter`, scene loader 같은 cross-feature bridge는 Infrastructure에서만 연결한다.

## Scene-owned Notes

* `GameSceneRoot`가 scene EventBus를 만들고 Player, Combat, Status, Unit, Wave, Garage를 같은 씬 버스로 조립한다.
* local player prefab 획득은 현재 Photon instantiate 직후 `GetComponent<PlayerSetup>()` 예외를 사용한다. 이 예외는 runtime fallback wiring이 아니라 네트워크 생성 객체 진입점으로만 허용한다.

## Cross-feature Dependencies

* `Combat`
* `Status`
* `Garage`
* `Unit`
* `Wave`
* `Projectile`
* `Zone`

## Validation Notes

* `clean` 판정은 `/agent/validation_gates.md`를 따른다.
* `Player` 변경 후에는 정적 규칙뿐 아니라 Unity compile 상태를 같이 확인한다.
* compile hazard:
  `GameEndEvent` stale field 참조, moved symbol import drift, cross-feature adapter 계약 drift를 먼저 점검한다.
