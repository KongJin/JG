# Player

## Responsibility

`Player`는 GameScene에서 로컬/원격 플레이어 스폰, 카메라 추적, 에너지 HUD, 전투 씬 bootstrap을 조립한다.

## GameScene Contract

`GameSceneRoot`가 `Assets/Scenes/GameScene.unity`의 composition root다. 이 씬은 런타임 fallback 탐색에 의존하지 않고 Inspector wiring을 기준으로 유지한다.

필수 scene-owned 참조:

- `Camera` + `CameraFollower`
- `Canvas` HUD
- `SceneErrorPresenter`
- `ProjectileSpawner`
- `CombatSetup`
- `ZoneSetup`
- `StatusSetup`
- `PlayerSceneRegistry`
- `EnergyRegenTicker`
- `EnergyBarView`
- `UnitSetup`
- `GarageSetup`
- `WaveSetup`
- `CoreObjectiveSetup`
- `UnitSlotsContainer`

런타임 주입 참조:

- `_localPlayerSetup`는 씬에 미리 두지 않는다.
- 로컬 플레이어는 `PhotonNetwork.Instantiate("PlayerCharacter", ...)`로 생성된다.
- `GameSceneRoot`는 Photon instantiation 시점의 `PlayerSetup.LocalArrived` 이벤트로 로컬 `PlayerSetup` 참조를 먼저 받고, 그 뒤 `InitializeLocal()`을 수행한다.

## Ordering Notes

`GameSceneRoot.CompleteLocalPlayerInitialization()` 기준 순서:

1. `StatusSetup`
2. `PlayerSetup.InitializeLocal`
3. `CombatSetup`
4. `CoreObjectiveSetup.RegisterCombatTarget`
5. `CoreObjectiveSetup.InitializePlacementArea`
6. `ProjectileSpawner`, `ZoneSetup`
7. `UnitSetup` + `GarageSetup`
8. `WaveSetup`
9. `UnitSlotsContainer`

배치 영역 시각화와 summon UI는 `CoreObjectiveSetup.InitializePlacementArea()` 이후에만 초기화한다.

## Smoke Notes

- Unity MCP smoke can target `/HudCanvas/UnitSummonUi/SlotRow/UnitSlotTemplate(Clone)` with `/ui/invoke` to verify summon flow without relying on fragile GameView pixel dragging.
- `tools/unity-mcp/Invoke-GameSceneSummonSmoke.ps1` is the baseline lobby-to-game summon smoke for this scene.
