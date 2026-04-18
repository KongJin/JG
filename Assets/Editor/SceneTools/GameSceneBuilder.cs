#if UNITY_EDITOR
using System;
using Features.Combat;
using Features.Combat.Infrastructure;
using Features.Combat.Presentation;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Player;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Projectile.Infrastructure;
using Features.Status;
using Features.Status.Presentation;
using Features.Unit;
using Features.Unit.Infrastructure;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Features.Zone;
using Photon.Pun;
using Shared.Kernel;
using Shared.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    public static class GameSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string BattleEntityPrefabPath = "Assets/Resources/BattleEntity.prefab";
        private const string PlayerHealthHudPrefabPath = "Assets/Resources/PlayerHealthHudView.prefab";
        private const string ProjectilePrefabPath = "Assets/Resources/ProjectilePhysicsAdapter.prefab";
        private const string DamageNumberPrefabPath = "Assets/Resources/DamageNumber.prefab";
        private const string ZoneEffectPrefabPath = "Assets/Resources/ZoneEffect.prefab";
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string WaveTablePath = "Assets/Resources/Wave/DefaultWaveTable.asset";

        [MenuItem("Tools/Codex/Build Game Scene")]
        private static void BuildGameScene()
        {
            BuildGameSceneForAutomation();
        }

        public static void BuildGameSceneForAutomation()
        {
            EnsureSceneExists();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildGameSceneCore();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }

        private static void BuildGameSceneCore()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Open {ScenePath} before running the builder.");

            EnsureBattleEntityPrefab();

            ClearScene(scene);

            var environmentRoot = CreateGameObject("EnvironmentRoot");
            var systemsRoot = CreateGameObject("SystemsRoot");
            var runtimeRoot = CreateGameObject("RuntimeRoot");
            var templatesRoot = CreateGameObject("UiTemplates");
            templatesRoot.SetActive(false);

            var camera = CreateMainCamera();
            CreateDirectionalLight();
            CreateEventSystem();
            CreateArena(environmentRoot.transform);

            var hudCanvas = CreateCanvas();
            var errorPresenter = CreateSceneErrorPresenter(hudCanvas.transform);
            var energyBar = CreateEnergyBar(hudCanvas.transform);
            var placementError = CreatePlacementErrorView(hudCanvas.transform);
            var summonUi = CreateUnitSummonUi(hudCanvas, camera, placementError, templatesRoot.transform);
            var waveUi = CreateWaveUi(hudCanvas.transform);

            var projectileRoot = CreateGameObject("ProjectileRoot", runtimeRoot.transform).transform;
            var zoneRoot = CreateGameObject("ZoneRoot", runtimeRoot.transform).transform;
            var unitsRoot = CreateGameObject("UnitsRoot", runtimeRoot.transform).transform;

            var coreContext = CreateCoreObjective(environmentRoot.transform);
            var combatContext = CreateCombatSetup(systemsRoot.transform, coreContext.TargetView);
            var zoneSetup = CreateZoneSetup(systemsRoot.transform, zoneRoot);
            var statusSetup = CreateStatusSetup(systemsRoot.transform);
            var playerSceneRegistry = CreateGameObject("PlayerSceneRegistry", systemsRoot.transform).AddComponent<PlayerSceneRegistry>();
            var energyTicker = CreateGameObject("EnergyRegenTicker", systemsRoot.transform).AddComponent<EnergyRegenTicker>();
            var projectileSpawner = CreateProjectileSpawner(systemsRoot.transform, projectileRoot);
            var damageNumberSpawner = CreateDamageNumberSpawner(systemsRoot.transform);
            var unitSetup = CreateUnitSetup(systemsRoot.transform, unitsRoot);
            var garageSetup = CreateGarageSetup(systemsRoot.transform);
            var waveSetup = CreateWaveSetup(systemsRoot.transform, waveUi);

            var rootGo = CreateGameObject("GameSceneRoot", systemsRoot.transform);
            var gameSceneRoot = rootGo.AddComponent<GameSceneRoot>();
            SetObject(gameSceneRoot, "_camera", camera);
            SetObject(gameSceneRoot, "_cameraFollower", camera.GetComponent<CameraFollower>());
            SetObject(gameSceneRoot, "_healthHudPrefab", LoadAsset<GameObject>(PlayerHealthHudPrefabPath));
            SetObject(gameSceneRoot, "_hudCanvas", hudCanvas);
            SetObject(gameSceneRoot, "_projectileSpawner", projectileSpawner);
            SetObject(gameSceneRoot, "_combatSetup", combatContext.Setup);
            SetObject(gameSceneRoot, "_zoneSetup", zoneSetup);
            SetObject(gameSceneRoot, "_sceneErrorPresenter", errorPresenter);
            SetObject(gameSceneRoot, "_playerSceneRegistry", playerSceneRegistry);
            SetObject(gameSceneRoot, "_energyRegenTicker", energyTicker);
            SetObject(gameSceneRoot, "_energyBarView", energyBar);
            SetObject(gameSceneRoot, "_unitSetup", unitSetup);
            SetObject(gameSceneRoot, "_garageSetup", garageSetup);
            SetObject(gameSceneRoot, "_unitSlotsContainer", summonUi.Container);
            SetObject(gameSceneRoot, "_damageNumberSpawner", damageNumberSpawner);
            SetObject(gameSceneRoot, "_statusSetup", statusSetup);
            SetObject(gameSceneRoot, "_waveSetup", waveSetup.Setup);
            SetObject(gameSceneRoot, "_coreObjective", coreContext.Setup);
            SetString(gameSceneRoot, "_lobbySceneName", "CodexLobbyScene");

            EnsureBuildSettingsContainsScene(ScenePath);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[GameSceneBuilder] GameScene rebuilt.");
        }

        private static void EnsureSceneExists()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Unable to create {ScenePath}");
        }

        private static void EnsureBuildSettingsContainsScene(string scenePath)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var scene in existing)
            {
                if (string.Equals(scene.path, scenePath, StringComparison.Ordinal))
                    return;
            }

            var next = new EditorBuildSettingsScene[existing.Length + 1];
            Array.Copy(existing, next, existing.Length);
            next[^1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
        }

        private static void EnsureBattleEntityPrefab()
        {
            var root = BuildBattleEntityPrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(root, BattleEntityPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject BuildBattleEntityPrefabRoot()
        {
            var root = CreateGameObject("BattleEntity", null, typeof(PhotonView));

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);

            var renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateSurfaceMaterial(new Color(0.36f, 0.82f, 0.93f, 1f));

            var entityIdHolder = root.AddComponent<EntityIdHolder>();
            var photonController = root.AddComponent<BattleEntityPhotonController>();
            var battleEntityView = root.AddComponent<BattleEntityView>();
            var prefabSetup = root.AddComponent<Features.Unit.Infrastructure.BattleEntityPrefabSetup>();

            SetObject(battleEntityView, "_entityIdHolder", entityIdHolder);
            SetObject(battleEntityView, "_bodyRenderer", renderer);

            SetObject(prefabSetup, "_view", battleEntityView);
            SetObject(prefabSetup, "_photonController", photonController);
            SetObject(prefabSetup, "_entityIdHolder", entityIdHolder);

            var photonView = root.GetComponent<PhotonView>();
            SetInt(photonView, "Synchronization", 3);
            SetInt(photonView, "OwnershipTransfer", 0);
            SetInt(photonView, "observableSearch", 2);
            SetObjectArray(photonView, "ObservedComponents", photonController);

            return root;
        }

        private static Camera CreateMainCamera()
        {
            var cameraGo = CreateGameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 13f, -10f);
            cameraGo.transform.rotation = Quaternion.Euler(46f, 0f, 0f);

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 29, 40, 255);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CameraFollower>();
            return camera;
        }

        private static void CreateArena(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateSurfaceMaterial(new Color(0.22f, 0.28f, 0.34f, 1f));

            var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "LaneStripe";
            lane.transform.SetParent(parent, false);
            lane.transform.position = new Vector3(0f, 0.02f, 0f);
            lane.transform.localScale = new Vector3(2.5f, 0.02f, 18f);
            lane.GetComponent<Renderer>().sharedMaterial = CreateSurfaceMaterial(new Color(0.18f, 0.43f, 0.51f, 1f));
        }

        private static void CreateDirectionalLight()
        {
            var lightGo = CreateGameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color32(255, 244, 226, 255);
            lightGo.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            var eventSystemGo = CreateGameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();

            var inputSystemUiType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiType != null)
            {
                eventSystemGo.AddComponent(inputSystemUiType);
                return;
            }

            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var go = CreateGameObject("HudCanvas", null, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return canvas;
        }

        private static SceneErrorPresenter CreateSceneErrorPresenter(Transform parent)
        {
            var root = CreateGameObject("SceneErrorPresenter", parent, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var banner = CreatePanel("Banner", root.transform, new Color32(193, 80, 86, 235));
            Stretch(banner.rectTransform, new Vector2(0.32f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            var bannerMessage = CreateTmpText("BannerMessage", banner.transform, "Scene errors will appear here.", 16, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            Stretch(bannerMessage.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

            var modalOverlay = CreatePanel("Modal", root.transform, new Color32(4, 8, 15, 210));
            Stretch(modalOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var modalGroup = modalOverlay.gameObject.AddComponent<CanvasGroup>();

            var card = CreatePanel("ModalCard", modalOverlay.transform, new Color32(28, 35, 58, 255));
            Stretch(card.rectTransform, new Vector2(0.29f, 0.33f), new Vector2(0.71f, 0.63f), Vector2.zero, Vector2.zero);

            var modalMessage = CreateTmpText("ModalMessage", card.transform, "Modal errors will appear here.", 20, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(244, 247, 255, 255));
            Stretch(modalMessage.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);

            var dismissButton = CreateButton("DismissButton", card.transform, "Dismiss", new Color32(74, 122, 255, 255), Color.white);
            Stretch(dismissButton.GetComponent<RectTransform>(), new Vector2(0.32f, 0.1f), new Vector2(0.68f, 0.24f), Vector2.zero, Vector2.zero);

            var presenter = root.AddComponent<SceneErrorPresenter>();
            SetObject(presenter, "_bannerGroup", bannerGroup);
            SetObject(presenter, "_bannerMessageText", bannerMessage);
            SetObject(presenter, "_modalGroup", modalGroup);
            SetObject(presenter, "_modalMessageText", modalMessage);
            SetObject(presenter, "_modalDismissButton", dismissButton);
            return presenter;
        }

        private static EnergyBarView CreateEnergyBar(Transform parent)
        {
            var root = CreateGameObject("EnergyBar", parent, typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 72f);
            rt.sizeDelta = new Vector2(360f, 28f);

            var slider = CreateSlider(root.transform, new Color(0.08f, 0.11f, 0.17f, 0.92f), new Color(0.24f, 0.55f, 0.98f, 1f));
            Stretch(slider.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var label = CreateLegacyText("Label", root.transform, "ENERGY", 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color32(235, 242, 255, 255));
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var view = root.AddComponent<EnergyBarView>();
            SetObject(view, "_energySlider", slider);
            SetObject(view, "_energyFillImage", slider.fillRect.GetComponent<Image>());
            return view;
        }

        private static PlacementErrorView CreatePlacementErrorView(Transform parent)
        {
            var root = CreatePanel("PlacementErrorView", parent, new Color32(168, 63, 63, 220));
            Stretch(root.rectTransform, new Vector2(0.36f, 0.17f), new Vector2(0.64f, 0.23f), Vector2.zero, Vector2.zero);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var text = CreateLegacyText("Text", root.transform, "배치 영역 밖입니다!", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));

            var view = root.gameObject.AddComponent<PlacementErrorView>();
            SetObject(view, "_errorText", text);
            SetObject(view, "_canvasGroup", group);
            return view;
        }

        private static SummonUiContext CreateUnitSummonUi(Canvas canvas, Camera worldCamera, PlacementErrorView placementErrorView, Transform templatesRoot)
        {
            var root = CreateGameObject("UnitSummonUi", canvas.transform, typeof(RectTransform));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 118f);
            rootRt.sizeDelta = new Vector2(560f, 112f);

            var row = CreateGameObject("SlotRow", root.transform, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Stretch(row.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 18f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlHeight = false;
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;

            var dragGhost = CreateGameObject("DragGhostTemplate", templatesRoot, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            dragGhost.GetComponent<RectTransform>().sizeDelta = new Vector2(82f, 82f);
            var ghostImage = dragGhost.GetComponent<Image>();
            ApplyDefaultSprite(ghostImage);
            ghostImage.color = new Color(0.34f, 0.82f, 0.97f, 0.8f);

            var slotTemplate = BuildUnitSlotTemplate(templatesRoot);
            var inputHandlerTemplate = CreateGameObject("UnitSlotInputHandlerTemplate", templatesRoot, typeof(RectTransform));
            var inputHandler = inputHandlerTemplate.AddComponent<UnitSlotInputHandler>();
            SetObject(inputHandler, "_worldCamera", worldCamera);
            SetObject(inputHandler, "_dragGhostPrefab", dragGhost);
            SetObject(inputHandler, "_ghostCanvasGroup", dragGhost.GetComponent<CanvasGroup>());

            var container = root.AddComponent<UnitSlotsContainer>();
            SetObject(container, "_slotPrefab", slotTemplate);
            SetObject(container, "_inputHandlerPrefab", inputHandler);
            SetObject(container, "_slotsParent", row.GetComponent<RectTransform>());
            SetObject(container, "_canvas", canvas);
            SetObject(container, "_worldCamera", worldCamera);
            SetObject(container, "_errorView", placementErrorView);

            return new SummonUiContext(container);
        }

        private static WaveUiContext CreateWaveUi(Transform parent)
        {
            var root = CreateGameObject("WaveUi", parent, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var topBar = CreateGameObject("TopBar", root.transform, typeof(RectTransform));
            Stretch(topBar.GetComponent<RectTransform>(), new Vector2(0.02f, 0.9f), new Vector2(0.42f, 0.985f), Vector2.zero, Vector2.zero);

            var waveText = CreateLegacyText("WaveText", topBar.transform, "Wave 1/5", 24, FontStyle.Bold, TextAnchor.UpperLeft, new Color32(236, 243, 255, 255));
            Stretch(waveText.rectTransform, new Vector2(0f, 0.48f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);

            var countdownText = CreateLegacyText("CountdownText", topBar.transform, "3", 30, FontStyle.Bold, TextAnchor.MiddleLeft, new Color32(255, 208, 87, 255));
            Stretch(countdownText.rectTransform, new Vector2(0.56f, 0.48f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            var statusText = CreateLegacyText("StatusText", topBar.transform, "Wave Cleared!", 20, FontStyle.Bold, TextAnchor.LowerLeft, new Color32(116, 214, 167, 255));
            Stretch(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 0.48f), Vector2.zero, Vector2.zero);

            var hintText = CreateLegacyText("HintText", topBar.transform, "Drag a unit into the placement lane.", 16, FontStyle.Normal, TextAnchor.LowerLeft, new Color32(147, 162, 196, 255));
            Stretch(hintText.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);

            var waveHudView = topBar.AddComponent<WaveHudView>();
            SetObject(waveHudView, "waveText", waveText);
            SetObject(waveHudView, "countdownText", countdownText);
            SetObject(waveHudView, "statusText", statusText);
            SetObject(waveHudView, "firstWaveDeckHintText", hintText);

            var endView = CreateWaveEndView(root.transform);
            var flowController = CreateGameObject("WaveFlowController", root.transform).AddComponent<WaveFlowController>();
            return new WaveUiContext(waveHudView, endView, flowController);
        }

        private static CoreObjectiveContext CreateCoreObjective(Transform parent)
        {
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "ObjectiveCore";
            core.transform.SetParent(parent, false);
            core.transform.position = new Vector3(0f, 0.75f, 5.8f);
            core.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);
            core.GetComponent<Renderer>().sharedMaterial = CreateSurfaceMaterial(new Color(0.28f, 0.72f, 0.94f, 1f));

            var placementViewGo = CreateGameObject("PlacementAreaView", core.transform);
            placementViewGo.transform.localPosition = new Vector3(0f, -0.72f, 0f);
            placementViewGo.AddComponent<MeshFilter>();
            placementViewGo.AddComponent<MeshRenderer>();
            var placementView = placementViewGo.AddComponent<PlacementAreaView>();

            var entityIdHolder = core.AddComponent<EntityIdHolder>();
            var targetView = core.AddComponent<CombatTargetView>();
            SetObject(targetView, "_entityIdHolder", entityIdHolder);
            SetObject(targetView, "_renderer", core.GetComponent<Renderer>());

            var setup = core.AddComponent<CoreObjectiveSetup>();
            SetObject(setup, "_coreAnchor", core.transform);
            SetObject(setup, "_entityIdHolder", entityIdHolder);
            SetObject(setup, "_placementAreaView", placementView);
            SetFloat(setup, "_placementForwardOffset", -1.8f);
            SetFloat(setup, "_placementWidth", 11f);
            SetFloat(setup, "_placementDepth", 5.8f);

            return new CoreObjectiveContext(setup, targetView);
        }

        private static CombatSetupContext CreateCombatSetup(Transform parent, CombatTargetView coreTargetView)
        {
            var go = CreateGameObject("CombatSetup", parent);
            var targetAdapter = go.AddComponent<CombatTargetAdapter>();
            var setup = go.AddComponent<CombatSetup>();
            SetObject(setup, "_targetAdapter", targetAdapter);
            SetObjectArray(setup, "_targetViews", coreTargetView);
            return new CombatSetupContext(setup);
        }

        private static ZoneSetup CreateZoneSetup(Transform parent, Transform zoneRoot)
        {
            var go = CreateGameObject("ZoneSetup", parent);
            var adapter = go.AddComponent<ZoneEffectAdapter>();
            var setup = go.AddComponent<ZoneSetup>();

            SetObject(adapter, "_zonePrefab", LoadPrefabComponent<Features.Zone.Presentation.ZoneView>(ZoneEffectPrefabPath));
            SetObject(adapter, "_spawnRoot", zoneRoot);
            SetObject(setup, "_zoneEffectAdapter", adapter);
            return setup;
        }

        private static StatusSetup CreateStatusSetup(Transform parent)
        {
            var go = CreateGameObject("StatusSetup", parent);
            var tickController = go.AddComponent<StatusTickController>();
            var setup = go.AddComponent<StatusSetup>();
            SetObject(setup, "_tickController", tickController);
            return setup;
        }

        private static ProjectileSpawner CreateProjectileSpawner(Transform parent, Transform projectileRoot)
        {
            var go = CreateGameObject("ProjectileSpawner", parent);
            var spawner = go.AddComponent<ProjectileSpawner>();
            SetObject(spawner, "_projectilePrefab", LoadPrefabComponent<ProjectilePhysicsAdapter>(ProjectilePrefabPath));
            SetObject(spawner, "_spawnRoot", projectileRoot);
            return spawner;
        }

        private static DamageNumberSpawner CreateDamageNumberSpawner(Transform parent)
        {
            var go = CreateGameObject("DamageNumberSpawner", parent);
            var spawner = go.AddComponent<DamageNumberSpawner>();
            SetObject(spawner, "damageNumberPrefab", LoadAsset<GameObject>(DamageNumberPrefabPath));
            return spawner;
        }

        private static UnitSetup CreateUnitSetup(Transform parent, Transform unitsRoot)
        {
            var go = CreateGameObject("UnitSetup", parent);
            var summonAdapter = go.AddComponent<SummonPhotonAdapter>();
            var setup = go.AddComponent<UnitSetup>();

            SetObject(summonAdapter, "_battleEntityPrefab", LoadAsset<GameObject>(BattleEntityPrefabPath));
            SetObject(summonAdapter, "_spawnParent", unitsRoot);
            SetObject(setup, "_moduleCatalog", LoadAsset<ModuleCatalog>(ModuleCatalogPath));
            SetObject(setup, "_summonAdapter", summonAdapter);
            return setup;
        }

        private static GarageSetup CreateGarageSetup(Transform parent)
        {
            var go = CreateGameObject("GarageSetup", parent);
            var networkAdapter = go.AddComponent<GarageNetworkAdapter>();
            var setup = go.AddComponent<GarageSetup>();
            SetObject(setup, "_networkAdapter", networkAdapter);
            return setup;
        }

        private static WaveSetupContext CreateWaveSetup(Transform parent, WaveUiContext ui)
        {
            var go = CreateGameObject("WaveSetup", parent);
            var spawnAdapter = go.AddComponent<EnemySpawnAdapter>();
            var playerQuery = go.AddComponent<PlayerPositionQueryAdapter>();
            var unitQuery = go.AddComponent<UnitPositionQueryAdapter>();
            var networkAdapter = go.AddComponent<WaveNetworkAdapter>();
            var setup = go.AddComponent<WaveSetup>();

            SetObject(setup, "_waveTable", LoadAsset<WaveTableData>(WaveTablePath));
            SetObject(setup, "_spawnAdapter", spawnAdapter);
            SetObject(setup, "_playerPositionQuery", playerQuery);
            SetObject(setup, "_unitPositionQuery", unitQuery);
            SetObject(setup, "_hudView", ui.HudView);
            SetObject(setup, "_endView", ui.EndView);
            SetObject(setup, "_flowController", ui.FlowController);
            SetObject(setup, "_networkAdapter", networkAdapter);
            SetObject(setup, "_coreHealthView", CreateCoreHealthView(ui.HudView.transform.parent));
            return new WaveSetupContext(setup);
        }

        private static UnitSlotView BuildUnitSlotTemplate(Transform parent)
        {
            var root = CreatePanel("UnitSlotTemplate", parent, new Color32(31, 39, 58, 242));
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.preferredHeight = 92f;
            root.rectTransform.sizeDelta = new Vector2(150f, 92f);

            var iconGo = CreateGameObject("Icon", root.transform, typeof(RectTransform), typeof(Image));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            iconRt.sizeDelta = new Vector2(52f, -18f);
            var iconImage = iconGo.GetComponent<Image>();
            ApplyDefaultSprite(iconImage);
            iconImage.color = new Color32(97, 169, 244, 255);

            var nameText = CreateLegacyText("NameText", root.transform, "Unit", 16, FontStyle.Bold, TextAnchor.UpperLeft, new Color32(235, 241, 255, 255));
            Stretch(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(74f, -8f), new Vector2(-10f, -10f));

            var costText = CreateLegacyText("CostText", root.transform, "3", 24, FontStyle.Bold, TextAnchor.LowerRight, new Color32(255, 213, 97, 255));
            Stretch(costText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.54f), new Vector2(74f, 8f), new Vector2(-12f, 6f));

            var overlay = CreatePanel("CannotAffordOverlay", root.transform, new Color(0.05f, 0.05f, 0.05f, 0.62f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayText = CreateLegacyText("OverlayText", overlay.transform, "Need Energy", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(overlayText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var slotView = root.gameObject.AddComponent<UnitSlotView>();
            SetObject(slotView, "_iconImage", iconImage);
            SetObject(slotView, "_nameText", nameText);
            SetObject(slotView, "_costText", costText);
            SetObject(slotView, "_cannotAffordOverlay", overlay.gameObject);
            return slotView;
        }

        private static CoreHealthHudView CreateCoreHealthView(Transform parent)
        {
            var root = CreateGameObject("CoreHealthHud", parent, typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.73f, 0.91f);
            rt.anchorMax = new Vector2(0.97f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var slider = CreateSlider(root.transform, new Color(0.08f, 0.11f, 0.17f, 0.92f), new Color(0.23f, 0.69f, 0.95f, 1f));
            Stretch(slider.GetComponent<RectTransform>(), new Vector2(0f, 0.32f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var hpText = CreateLegacyText("HpText", root.transform, "1500 / 1500", 15, FontStyle.Bold, TextAnchor.LowerCenter, new Color32(235, 241, 255, 255));
            Stretch(hpText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.3f), Vector2.zero, Vector2.zero);

            var view = root.AddComponent<CoreHealthHudView>();
            SetObject(view, "healthSlider", slider);
            SetObject(view, "fillImage", slider.fillRect.GetComponent<Image>());
            SetObject(view, "hpText", hpText);
            return view;
        }

        private static WaveEndView CreateWaveEndView(Transform parent)
        {
            var overlay = CreateGameObject("WaveEndOverlay", parent, typeof(RectTransform));
            Stretch(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreatePanel("Panel", overlay.transform, new Color(0.04f, 0.08f, 0.14f, 0.84f));
            Stretch(panel.rectTransform, new Vector2(0.33f, 0.28f), new Vector2(0.67f, 0.66f), Vector2.zero, Vector2.zero);

            var resultText = CreateLegacyText("ResultText", panel.transform, "Victory!", 34, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            Stretch(resultText.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            var statsText = CreateLegacyText("StatsText", panel.transform, "결과: 승리", 18, FontStyle.Normal, TextAnchor.UpperLeft, new Color32(220, 229, 248, 255));
            Stretch(statsText.rectTransform, new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.62f), Vector2.zero, Vector2.zero);

            var returnButton = CreateButton("ReturnToLobbyButton", panel.transform, "Return To Lobby", new Color32(74, 122, 255, 255), Color.white);
            Stretch(returnButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.08f), new Vector2(0.76f, 0.2f), Vector2.zero, Vector2.zero);

            var endView = overlay.AddComponent<WaveEndView>();
            SetObject(endView, "panel", panel.gameObject);
            SetObject(endView, "resultText", resultText);
            SetObject(endView, "statsText", statsText);
            SetObject(endView, "returnToLobbyButton", returnButton);
            return endView;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateTmpText(string name, Transform parent, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform));
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Text CreateLegacyText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform));
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Material CreateSurfaceMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Standard");
            var material = new Material(shader);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            return material;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color fill, Color textColor)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var image = go.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = fill;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateLegacyText("Label", go.transform, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            return button;
        }

        private static Slider CreateSlider(Transform parent, Color backgroundColor, Color fillColor)
        {
            var sliderGo = CreateGameObject("Slider", parent, typeof(RectTransform), typeof(Slider));
            Stretch(sliderGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var background = CreateGameObject("Background", sliderGo.transform, typeof(RectTransform), typeof(Image));
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backgroundImage = background.GetComponent<Image>();
            ApplyDefaultSprite(backgroundImage);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = backgroundColor;

            var fillArea = CreateGameObject("Fill Area", sliderGo.transform, typeof(RectTransform));
            Stretch(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

            var fill = CreateGameObject("Fill", fillArea.transform, typeof(RectTransform), typeof(Image));
            Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImage = fill.GetComponent<Image>();
            ApplyDefaultSprite(fillImage);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;
            return slider;
        }

        private static GameObject CreateGameObject(string name, Transform parent = null, params Type[] componentTypes)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);

            foreach (var componentType in componentTypes)
            {
                if (componentType == typeof(RectTransform))
                {
                    if (go.GetComponent<RectTransform>() == null)
                        go.AddComponent<RectTransform>();
                    continue;
                }

                if (go.GetComponent(componentType) == null)
                    go.AddComponent(componentType);
            }

            return go;
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
        }

        private static void ApplyDefaultSprite(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.material = null;
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset not found at {path}");
            return asset;
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            var prefab = LoadAsset<GameObject>(path);
            var component = prefab.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Required component {typeof(T).Name} missing on prefab {path}");
            return component;
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"Array property {propertyName} was not found on {target.GetType().Name}");

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct SummonUiContext
        {
            public SummonUiContext(UnitSlotsContainer container)
            {
                Container = container;
            }

            public UnitSlotsContainer Container { get; }
        }

        private readonly struct WaveUiContext
        {
            public WaveUiContext(
                WaveHudView hudView,
                WaveEndView endView,
                WaveFlowController flowController)
            {
                HudView = hudView;
                EndView = endView;
                FlowController = flowController;
            }

            public WaveHudView HudView { get; }
            public WaveEndView EndView { get; }
            public WaveFlowController FlowController { get; }
        }

        private readonly struct CoreObjectiveContext
        {
            public CoreObjectiveContext(CoreObjectiveSetup setup, CombatTargetView targetView)
            {
                Setup = setup;
                TargetView = targetView;
            }

            public CoreObjectiveSetup Setup { get; }
            public CombatTargetView TargetView { get; }
        }

        private readonly struct CombatSetupContext
        {
            public CombatSetupContext(CombatSetup setup)
            {
                Setup = setup;
            }

            public CombatSetup Setup { get; }
        }

        private readonly struct WaveSetupContext
        {
            public WaveSetupContext(WaveSetup setup)
            {
                Setup = setup;
            }

            public WaveSetup Setup { get; }
        }
    }
}
#endif
