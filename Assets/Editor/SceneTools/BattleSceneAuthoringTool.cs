#if UNITY_EDITOR
using System.Linq;
using Features.Combat;
using Features.Combat.Infrastructure;
using Features.Combat.Presentation;
using Features.Enemy;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Player;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Projectile.Infrastructure;
using Features.Status;
using Features.Unit;
using Features.Unit.Infrastructure;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Features.Zone;
using Features.Zone.Presentation;
using Shared.Kernel;
using Shared.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class BattleSceneAuthoringTool
    {
        private const string ScenePath = "Assets/Scenes/BattleScene.unity";
        private const string FullHudPrefabPath = "Assets/Prefabs/Features/Battle/Root/SetDGameSceneHudFullRoot.prefab";
        private const string LowCoreWarningPrefabPath = "Assets/Prefabs/Features/Battle/Independent/SetDLowCoreWarningRoot.prefab";
        private const string UnitStatsPopupPrefabPath = "Assets/Prefabs/Features/Battle/Independent/SetDUnitStatsPopupRoot.prefab";
        private const string VictoryOverlayPrefabPath = "Assets/Prefabs/Features/Result/Independent/SetEMissionVictoryOverlayRoot.prefab";
        private const string DefeatOverlayPrefabPath = "Assets/Prefabs/Features/Result/Independent/SetEMissionDefeatOverlayRoot.prefab";

        [MenuItem("Tools/Scene/Create Or Rebuild Battle Scene")]
        private static void CreateOrRebuild()
        {
            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isDirty && active.path != ScenePath)
            {
                Debug.LogError($"[BattleSceneAuthoringTool] Active scene is dirty: {active.path}. Save or discard it before rebuilding BattleScene.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene();
            EditorSceneManager.SaveScene(scene, ScenePath, true);
            RegisterBuildScene(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BattleSceneAuthoringTool] Rebuilt {ScenePath}");
        }

        private static void BuildScene()
        {
            var camera = CreateCamera();
            CreateLight();
            CreateEventSystem();
            CreateWorld(out var coreObjective, out var placementAreaView);
            CreateRuntimeRoots(out var battleEntityRoot, out var projectileRoot, out var zoneRoot);
            CreateHud(
                camera,
                out var hudCanvas,
                out var energyBarView,
                out var unitSlotsContainer,
                out var damageNumberSpawner,
                out var sceneErrorPresenter,
                out var waveHudView,
                out var waveEndView,
                out var coreHealthHudView);

            var systems = new GameObject("BattleSceneSystems");
            var root = systems.AddComponent<GameSceneRoot>();
            var playerRegistry = systems.AddComponent(typeof(PlayerSceneRegistry)) as PlayerSceneRegistry;
            var enemyRegistry = systems.AddComponent(typeof(EnemySceneRegistry)) as EnemySceneRegistry;
            var runtimeRegistrar = systems.AddComponent<GameSceneRuntimeSpawnRegistrar>();

            var combatSetup = systems.AddComponent<CombatSetup>();
            var combatTargetAdapter = systems.AddComponent<CombatTargetAdapter>();
            var statusSetup = systems.AddComponent<StatusSetup>();
            var unitSetup = systems.AddComponent<UnitSetup>();
            var summonAdapter = systems.AddComponent<SummonPhotonAdapter>();
            var garageSetup = systems.AddComponent<GarageSetup>();
            var garageNetwork = systems.AddComponent<GarageNetworkAdapter>();
            var waveSetup = systems.AddComponent<WaveSetup>();
            var enemySpawnAdapter = systems.AddComponent<EnemySpawnAdapter>();
            var playerPositionQuery = systems.AddComponent<PlayerPositionQueryAdapter>();
            var unitPositionQuery = systems.AddComponent<UnitPositionQueryAdapter>();
            var waveNetworkAdapter = systems.AddComponent<WaveNetworkAdapter>();
            var waveFlowController = systems.AddComponent<WaveFlowController>();
            var difficultyScale = systems.AddComponent<RoomDifficultySpawnScaleProvider>();
            var projectileSpawner = systems.AddComponent<ProjectileSpawner>();
            var zoneSetup = systems.AddComponent<ZoneSetup>();
            var zoneEffectAdapter = systems.AddComponent<ZoneEffectAdapter>();

            WireRuntimeRegistrar(runtimeRegistrar, playerRegistry, enemyRegistry);
            WireCombat(combatSetup, combatTargetAdapter);
            WireUnit(unitSetup, summonAdapter, battleEntityRoot);
            WireGarage(garageSetup, garageNetwork);
            WireWave(
                waveSetup,
                enemySpawnAdapter,
                playerPositionQuery,
                unitPositionQuery,
                waveHudView,
                waveEndView,
                waveFlowController,
                waveNetworkAdapter,
                coreHealthHudView,
                enemyRegistry,
                difficultyScale);
            WireProjectile(projectileSpawner, projectileRoot);
            WireZone(zoneSetup, zoneEffectAdapter, zoneRoot);
            WireGameRoot(
                root,
                camera,
                hudCanvas,
                combatSetup,
                zoneSetup,
                sceneErrorPresenter,
                playerRegistry,
                energyBarView,
                unitSetup,
                garageSetup,
                unitSlotsContainer,
                damageNumberSpawner,
                statusSetup,
                waveSetup,
                coreObjective,
                projectileSpawner);

            SetObjectField(coreObjective, "_placementAreaView", placementAreaView);
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(new Vector3(0f, 12f, -10f), Quaternion.Euler(55f, 0f, 0f));

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.05f, 0.07f);
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraFollower>();
            return camera;
        }

        private static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.88f, 0.95f, 1f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void CreateWorld(out CoreObjectiveSetup coreObjective, out PlacementAreaView placementAreaView)
        {
            var battlefield = new GameObject("Battlefield");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(battlefield.transform, false);
            ground.transform.localScale = new Vector3(1.8f, 1f, 1.8f);
            SetRendererColor(ground, new Color(0.08f, 0.11f, 0.13f));

            var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "ObjectiveCore";
            core.transform.SetParent(battlefield.transform, false);
            core.transform.position = new Vector3(0f, 0.75f, 0f);
            core.transform.localScale = new Vector3(1.35f, 1.5f, 1.35f);
            SetRendererColor(core, new Color(0.16f, 0.58f, 0.96f));

            var coreId = core.AddComponent<EntityIdHolder>();
            coreObjective = core.AddComponent<CoreObjectiveSetup>();
            SetObjectField(coreObjective, "_coreAnchor", core.transform);
            SetObjectField(coreObjective, "_entityIdHolder", coreId);
            SetFloatField(coreObjective, "_maxHp", 1500f);
            SetFloatField(coreObjective, "_placementForwardOffset", 1.3f);
            SetFloatField(coreObjective, "_placementWidth", 8.5f);
            SetFloatField(coreObjective, "_placementDepth", 5.5f);

            var placement = new GameObject("PlacementAreaView", typeof(MeshFilter), typeof(MeshRenderer));
            placement.transform.SetParent(battlefield.transform, false);
            placement.transform.position = Vector3.zero;
            placementAreaView = placement.AddComponent<PlacementAreaView>();
        }

        private static void CreateRuntimeRoots(out Transform battleEntityRoot, out Transform projectileRoot, out Transform zoneRoot)
        {
            var runtime = new GameObject("RuntimeSpawnRoots");
            battleEntityRoot = CreateChild("BattleEntities", runtime.transform).transform;
            projectileRoot = CreateChild("Projectiles", runtime.transform).transform;
            zoneRoot = CreateChild("Zones", runtime.transform).transform;
        }

        private static void CreateHud(
            Camera camera,
            out Canvas hudCanvas,
            out EnergyBarView energyBarView,
            out UnitSlotsContainer unitSlotsContainer,
            out DamageNumberSpawner damageNumberSpawner,
            out SceneErrorPresenter sceneErrorPresenter,
            out WaveHudView waveHudView,
            out WaveEndView waveEndView,
            out CoreHealthHudView coreHealthHudView)
        {
            var canvasGo = new GameObject("BattleHudCanvas", typeof(RectTransform));
            hudCanvas = canvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.6f;

            var visualLayer = CreateRect("StitchBattleVisualLayer", canvasGo.transform, Vector2.zero, Vector2.one);
            InstantiateBattleVisualPrefabs(visualLayer);

            var bindingLayer = CreateRect("RuntimeBindingLayer", canvasGo.transform, Vector2.zero, Vector2.one);
            var bindingGroup = bindingLayer.gameObject.AddComponent<CanvasGroup>();
            bindingGroup.alpha = 0f;
            bindingGroup.interactable = false;
            bindingGroup.blocksRaycasts = false;

            var topBar = CreatePanel("WaveHudPanel", bindingLayer, new Vector2(0.04f, 0.90f), new Vector2(0.64f, 0.985f), new Color(0.05f, 0.09f, 0.14f, 0.86f));
            waveHudView = topBar.gameObject.AddComponent<WaveHudView>();
            var waveText = CreateText("WaveText", topBar, "Wave 1/3", 30, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.95f, 0.97f, 1f), new Vector2(0.05f, 0.48f), new Vector2(0.62f, 0.95f));
            var countdownText = CreateText("CountdownText", topBar, "", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.5f, 0.85f, 1f), new Vector2(0.74f, 0.22f), new Vector2(0.95f, 0.90f));
            var statusText = CreateText("StatusText", topBar, "", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.73f, 0.83f, 0.94f), new Vector2(0.05f, 0.06f), new Vector2(0.70f, 0.44f));
            var hintText = CreateText("FirstWaveHintText", topBar, "", 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.77f, 0.88f, 1f, 0.95f), new Vector2(0.05f, -0.35f), new Vector2(0.95f, 0.03f));
            SetObjectField(waveHudView, "waveText", waveText);
            SetObjectField(waveHudView, "countdownText", countdownText);
            SetObjectField(waveHudView, "statusText", statusText);
            SetObjectField(waveHudView, "backgroundImage", topBar.GetComponent<Image>());
            SetObjectField(waveHudView, "firstWaveDeckHintText", hintText);

            var corePanel = CreatePanel("CoreHealthPanel", bindingLayer, new Vector2(0.66f, 0.90f), new Vector2(0.96f, 0.985f), new Color(0.10f, 0.14f, 0.19f, 0.92f));
            coreHealthHudView = corePanel.gameObject.AddComponent<CoreHealthHudView>();
            var coreText = CreateText("CoreHpText", corePanel, "CORE HP", 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.92f, 0.95f, 1f), new Vector2(0.07f, 0.36f), new Vector2(0.94f, 0.93f));
            var coreSlider = CreateSlider("CoreHpSlider", corePanel, new Vector2(0.07f, 0.12f), new Vector2(0.94f, 0.32f), out var coreFill, out var coreBackground);
            SetObjectField(coreHealthHudView, "healthSlider", coreSlider);
            SetObjectField(coreHealthHudView, "fillImage", coreFill);
            SetObjectField(coreHealthHudView, "hpText", coreText);
            SetObjectField(coreHealthHudView, "panelImage", corePanel.GetComponent<Image>());
            SetObjectField(coreHealthHudView, "sliderBackgroundImage", coreBackground);

            var commandDock = CreatePanel("CommandDock", bindingLayer, new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.20f), new Color(0.03f, 0.07f, 0.11f, 0.94f));
            var commandController = commandDock.gameObject.AddComponent<SummonCommandController>();
            SetObjectField(commandController, "_worldCamera", camera);

            var slotRow = CreateRect("SlotRow", commandDock, new Vector2(0.02f, 0.22f), new Vector2(0.72f, 0.95f));
            var slotLayout = slotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 14f;
            slotLayout.childAlignment = TextAnchor.MiddleLeft;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = true;

            var energyRect = CreateRect("EnergyBar", commandDock, new Vector2(0.74f, 0.56f), new Vector2(0.98f, 0.93f));
            energyBarView = energyRect.gameObject.AddComponent<EnergyBarView>();
            var energySlider = CreateSlider("EnergySlider", energyRect, new Vector2(0f, 0.02f), new Vector2(1f, 0.42f), out var energyFill, out var energyBackground);
            var energyLabel = CreateText("Label", energyRect, "ENERGY", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.87f, 0.94f, 1f), new Vector2(0f, 0.50f), new Vector2(1f, 1f));
            SetObjectField(energyBarView, "_energySlider", energySlider);
            SetObjectField(energyBarView, "_energyFillImage", energyFill);
            SetObjectField(energyBarView, "_energyText", energyLabel);
            SetObjectField(energyBarView, "_sliderBackgroundImage", energyBackground);

            var feedbackRect = CreatePanel("PlacementErrorView", commandDock, new Vector2(0.74f, 0.10f), new Vector2(0.98f, 0.48f), new Color(0.08f, 0.22f, 0.33f, 0.92f));
            var feedbackGroup = feedbackRect.gameObject.AddComponent<CanvasGroup>();
            var feedbackText = CreateText("MessageText", feedbackRect, "", 18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));
            var placementErrorView = feedbackRect.gameObject.AddComponent<PlacementErrorView>();
            SetObjectField(placementErrorView, "_errorText", feedbackText);
            SetObjectField(placementErrorView, "_canvasGroup", feedbackGroup);
            SetObjectField(placementErrorView, "_backgroundImage", feedbackRect.GetComponent<Image>());

            var templates = CreateChild("Templates", bindingLayer);
            templates.SetActive(false);
            var slotTemplate = CreateUnitSlotTemplate(templates.transform);
            var inputTemplate = CreateUnitSlotInputTemplate(templates.transform, canvasGo.GetComponent<Canvas>(), camera);

            unitSlotsContainer = commandDock.gameObject.AddComponent<UnitSlotsContainer>();
            SetObjectField(unitSlotsContainer, "_slotPrefab", slotTemplate);
            SetObjectField(unitSlotsContainer, "_inputHandlerPrefab", inputTemplate);
            SetObjectField(unitSlotsContainer, "_slotsParent", slotRow);
            SetObjectField(unitSlotsContainer, "_canvas", hudCanvas);
            SetObjectField(unitSlotsContainer, "_worldCamera", camera);
            SetObjectField(unitSlotsContainer, "_errorView", placementErrorView);
            SetObjectField(unitSlotsContainer, "_commandController", commandController);

            SetObjectField(commandController, "_dockRoot", commandDock);
            SetObjectField(commandController, "_slotRowRect", slotRow);
            SetObjectField(commandController, "_energyBarRect", energyRect);
            SetObjectField(commandController, "_feedbackRect", feedbackRect);
            SetObjectField(commandController, "_slotRowLayout", slotLayout);
            SetObjectField(commandController, "_dockBackgroundImage", commandDock.GetComponent<Image>());

            damageNumberSpawner = canvasGo.AddComponent<DamageNumberSpawner>();
            SetObjectField(damageNumberSpawner, "damageNumberPrefab", Resources.Load<GameObject>("DamageNumber"));

            sceneErrorPresenter = CreateSceneErrorPresenter(bindingLayer);
            waveEndView = CreateWaveEndView(bindingLayer);
        }

        private static UnitSlotView CreateUnitSlotTemplate(Transform parent)
        {
            var root = CreatePanel("UnitSlotTemplate", parent, Vector2.zero, Vector2.zero, new Color(0.08f, 0.14f, 0.21f, 0.96f));
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 188f;
            layout.preferredHeight = 118f;
            layout.flexibleHeight = 1f;

            var icon = CreatePanel("Icon", root, new Vector2(0.06f, 0.18f), new Vector2(0.34f, 0.82f), new Color(0.63f, 0.86f, 1f, 1f)).GetComponent<Image>();
            var name = CreateText("NameText", root, "Unit", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.88f, 0.94f, 1f), new Vector2(0.40f, 0.48f), new Vector2(0.92f, 0.90f));
            var cost = CreateText("CostText", root, "0", 22, FontStyle.Bold, TextAnchor.MiddleRight, Color.white, new Vector2(0.52f, 0.12f), new Vector2(0.92f, 0.44f));
            var cannotAfford = CreatePanel("CannotAffordOverlay", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.02f, 0.03f, 0.05f, 0.58f));

            var view = root.gameObject.AddComponent<UnitSlotView>();
            SetObjectField(view, "_iconImage", icon);
            SetObjectField(view, "_nameText", name);
            SetObjectField(view, "_costText", cost);
            SetObjectField(view, "_cannotAffordOverlay", cannotAfford.gameObject);
            SetObjectField(view, "_backgroundImage", root.GetComponent<Image>());
            SetObjectField(view, "_layoutElement", layout);
            return view;
        }

        private static UnitSlotInputHandler CreateUnitSlotInputTemplate(Transform parent, Canvas canvas, Camera camera)
        {
            var ghost = CreatePanel("DragGhostTemplate", parent, Vector2.zero, Vector2.zero, new Color(0.55f, 0.82f, 1f, 0.72f));
            ghost.gameObject.AddComponent<CanvasGroup>();
            SetRectSize(ghost, new Vector2(96f, 96f));

            var inputGo = CreateChild("UnitSlotInputTemplate", parent);
            var input = inputGo.AddComponent<UnitSlotInputHandler>();
            SetObjectField(input, "_worldCamera", camera);
            SetObjectField(input, "_dragGhostPrefab", ghost.gameObject);
            SetObjectField(input, "_ghostCanvasGroup", ghost.GetComponent<CanvasGroup>());
            return input;
        }

        private static SceneErrorPresenter CreateSceneErrorPresenter(Transform parent)
        {
            var root = CreateRect("SceneErrorPresenter", parent, Vector2.zero, Vector2.one);
            var presenter = root.gameObject.AddComponent<SceneErrorPresenter>();

            var banner = CreatePanel("ErrorBanner", root, new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.88f), new Color(0.54f, 0.18f, 0.16f, 0.95f));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            var bannerText = CreateTmpText("BannerMessage", banner, "Error", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.90f));

            var modal = CreatePanel("ErrorModal", root, new Vector2(0.16f, 0.38f), new Vector2(0.84f, 0.62f), new Color(0.06f, 0.09f, 0.13f, 0.98f));
            var modalGroup = modal.gameObject.AddComponent<CanvasGroup>();
            var modalText = CreateTmpText("ModalMessage", modal, "Error", 26, FontStyles.Bold, TextAlignmentOptions.TopLeft, Color.white, new Vector2(0.07f, 0.36f), new Vector2(0.93f, 0.88f));
            var dismiss = CreateButton("DismissButton", modal, "OK", new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.28f));

            SetObjectField(presenter, "_bannerGroup", bannerGroup);
            SetObjectField(presenter, "_bannerMessageText", bannerText);
            SetObjectField(presenter, "_modalGroup", modalGroup);
            SetObjectField(presenter, "_modalMessageText", modalText);
            SetObjectField(presenter, "_modalDismissButton", dismiss);
            return presenter;
        }

        private static WaveEndView CreateWaveEndView(Transform parent)
        {
            var root = CreateRect("WaveEndOverlay", parent, Vector2.zero, Vector2.one);
            var panel = CreatePanel("ResultPanel", root, new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.72f), new Color(0.04f, 0.07f, 0.10f, 0.96f));
            var result = CreateText("ResultText", panel, "Victory!", 44, FontStyle.Bold, TextAnchor.UpperCenter, Color.white, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.94f));
            var stats = CreateText("StatsText", panel, "", 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.84f, 0.92f, 1f), new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.68f));
            var button = CreateButton("ReturnToLobbyButton", panel, "Return To Lobby", new Vector2(0.24f, 0.08f), new Vector2(0.76f, 0.24f));

            var view = root.gameObject.AddComponent<WaveEndView>();
            SetObjectField(view, "panel", panel.gameObject);
            SetObjectField(view, "resultText", result);
            SetObjectField(view, "statsText", stats);
            SetObjectField(view, "returnToLobbyButton", button);
            return view;
        }

        private static void WireRuntimeRegistrar(GameSceneRuntimeSpawnRegistrar registrar, PlayerSceneRegistry playerRegistry, EnemySceneRegistry enemyRegistry)
        {
            SetObjectField(registrar, "_playerSceneRegistry", playerRegistry);
            SetObjectField(registrar, "_enemySceneRegistry", enemyRegistry);
        }

        private static void WireCombat(CombatSetup setup, CombatTargetAdapter targetAdapter)
        {
            SetObjectField(setup, "_targetAdapter", targetAdapter);
        }

        private static void WireUnit(UnitSetup setup, SummonPhotonAdapter summonAdapter, Transform spawnParent)
        {
            SetObjectField(setup, "_moduleCatalog", AssetDatabase.LoadAssetAtPath<ModuleCatalog>("Assets/Data/Garage/ModuleCatalog.asset"));
            SetObjectField(setup, "_summonAdapter", summonAdapter);
            SetObjectField(summonAdapter, "_battleEntityPrefab", Resources.Load<GameObject>("BattleEntity"));
            SetObjectField(summonAdapter, "_spawnParent", spawnParent);
        }

        private static void WireGarage(GarageSetup setup, GarageNetworkAdapter network)
        {
            SetObjectField(setup, "_networkAdapter", network);
        }

        private static void WireWave(
            WaveSetup setup,
            EnemySpawnAdapter spawnAdapter,
            PlayerPositionQueryAdapter playerPositionQuery,
            UnitPositionQueryAdapter unitPositionQuery,
            WaveHudView hudView,
            WaveEndView endView,
            WaveFlowController flowController,
            WaveNetworkAdapter networkAdapter,
            CoreHealthHudView coreHealthView,
            EnemySceneRegistry enemyRegistry,
            RoomDifficultySpawnScaleProvider difficultyScale)
        {
            SetObjectField(setup, "_waveTable", AssetDatabase.LoadAssetAtPath<WaveTableData>("Assets/Resources/Wave/DefaultWaveTable.asset"));
            SetObjectField(setup, "_spawnAdapter", spawnAdapter);
            SetObjectField(setup, "_playerPositionQuery", playerPositionQuery);
            SetObjectField(setup, "_unitPositionQuery", unitPositionQuery);
            SetObjectField(setup, "_hudView", hudView);
            SetObjectField(setup, "_endView", endView);
            SetObjectField(setup, "_flowController", flowController);
            SetObjectField(setup, "_networkAdapter", networkAdapter);
            SetObjectField(setup, "_coreHealthView", coreHealthView);
            SetObjectField(setup, "_enemySceneRegistry", enemyRegistry);
            SetObjectField(setup, "_difficultySpawnScale", difficultyScale);
        }

        private static void WireProjectile(ProjectileSpawner spawner, Transform spawnRoot)
        {
            SetObjectField(spawner, "_projectilePrefab", LoadResourceComponent<ProjectilePhysicsAdapter>("ProjectilePhysicsAdapter"));
            SetObjectField(spawner, "_spawnRoot", spawnRoot);
        }

        private static void WireZone(ZoneSetup setup, ZoneEffectAdapter effectAdapter, Transform spawnRoot)
        {
            SetObjectField(setup, "_zoneEffectAdapter", effectAdapter);
            SetObjectField(effectAdapter, "_zonePrefab", LoadResourceComponent<ZoneView>("ZoneEffect"));
            SetObjectField(effectAdapter, "_spawnRoot", spawnRoot);
        }

        private static void WireGameRoot(
            GameSceneRoot root,
            Camera camera,
            Canvas hudCanvas,
            CombatSetup combatSetup,
            ZoneSetup zoneSetup,
            SceneErrorPresenter sceneErrorPresenter,
            PlayerSceneRegistry playerRegistry,
            EnergyBarView energyBarView,
            UnitSetup unitSetup,
            GarageSetup garageSetup,
            UnitSlotsContainer unitSlotsContainer,
            DamageNumberSpawner damageNumberSpawner,
            StatusSetup statusSetup,
            WaveSetup waveSetup,
            CoreObjectiveSetup coreObjective,
            ProjectileSpawner projectileSpawner)
        {
            SetStringField(root, "_playerPrefabName", "PlayerCharacter");
            SetObjectField(root, "_camera", camera);
            SetObjectField(root, "_cameraFollower", camera.GetComponent<CameraFollower>());
            SetObjectField(root, "_healthHudPrefab", Resources.Load<GameObject>("PlayerHealthHudView"));
            SetObjectField(root, "_hudCanvas", hudCanvas);
            SetObjectField(root, "_projectileSpawner", projectileSpawner);
            SetObjectField(root, "_combatSetup", combatSetup);
            SetObjectField(root, "_zoneSetup", zoneSetup);
            SetObjectField(root, "_sceneErrorPresenter", sceneErrorPresenter);
            SetObjectField(root, "_playerSceneRegistry", playerRegistry);
            SetObjectField(root, "_energyBarView", energyBarView);
            SetObjectField(root, "_unitSetup", unitSetup);
            SetObjectField(root, "_garageSetup", garageSetup);
            SetObjectField(root, "_unitSlotsContainer", unitSlotsContainer);
            SetObjectField(root, "_damageNumberSpawner", damageNumberSpawner);
            SetObjectField(root, "_statusSetup", statusSetup);
            SetObjectField(root, "_waveSetup", waveSetup);
            SetObjectField(root, "_coreObjective", coreObjective);
            SetStringField(root, "_lobbySceneName", "LobbyScene");
        }

        private static void InstantiateBattleVisualPrefabs(Transform parent)
        {
            InstantiateBattleVisualPrefab(parent, FullHudPrefabPath, "BattleHudVisualShell", true);
            InstantiateBattleVisualPrefab(parent, LowCoreWarningPrefabPath, "LowCoreWarningVisual", false);
            InstantiateBattleVisualPrefab(parent, UnitStatsPopupPrefabPath, "UnitStatsPopupVisual", false);
            InstantiateBattleVisualPrefab(parent, VictoryOverlayPrefabPath, "MissionVictoryOverlayVisual", false);
            InstantiateBattleVisualPrefab(parent, DefeatOverlayPrefabPath, "MissionDefeatOverlayVisual", false);
        }

        private static void InstantiateBattleVisualPrefab(Transform parent, string prefabPath, string instanceName, bool active)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[BattleSceneAuthoringTool] Missing Stitch prefab: {prefabPath}");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                return;

            instance.name = instanceName;
            instance.SetActive(active);
            var rect = instance.transform as RectTransform;
            if (rect != null)
                Stretch(rect);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = GetBuiltInFont();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI CreateTmpText(string name, Transform parent, string text, int fontSize, FontStyles style, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.18f, 0.43f, 0.68f, 1f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            CreateText("Label", rect, text, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.90f));
            return button;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, out Image fillImage, out Image backgroundImage)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transition = Selectable.Transition.None;

            var background = CreatePanel("Background", rect, Vector2.zero, Vector2.one, new Color(0.18f, 0.09f, 0.11f, 0.92f));
            backgroundImage = background.GetComponent<Image>();

            var fillArea = CreateRect("Fill Area", rect, Vector2.zero, Vector2.one);
            var fill = CreatePanel("Fill", fillArea, Vector2.zero, Vector2.one, new Color(0.28f, 0.77f, 0.95f, 1f));
            fillImage = fill.GetComponent<Image>();

            slider.fillRect = fill;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRectSize(RectTransform rect, Vector2 size)
        {
            rect.sizeDelta = size;
        }

        private static Font GetBuiltInFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static T LoadResourceComponent<T>(string path) where T : Component
        {
            var prefab = Resources.Load<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static void SetRendererColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
        }

        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[BattleSceneAuthoringTool] Missing field '{fieldName}' on {target.GetType().Name}");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatField(Object target, string fieldName, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
                return;

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringField(Object target, string fieldName, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
                return;

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterBuildScene(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == path))
                return;

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
