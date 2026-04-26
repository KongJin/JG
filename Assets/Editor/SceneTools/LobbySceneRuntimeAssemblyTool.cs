#if UNITY_EDITOR
using System.IO;
using Features.Account;
using Features.Account.Presentation;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Lobby.Infrastructure.Photon;
using Features.Lobby.Presentation;
using Features.Unit;
using Features.Unit.Infrastructure;
using Shared.Runtime.Sound;
using Shared.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    // Destructive scene rebuild helper.
    //
    // This is a bounded fallback for recreating the LobbyScene runtime contract from
    // known prefab surfaces. It is not a general repair command: running it deletes
    // the existing LobbyRuntime, LobbyCanvas, EventSystem, and LobbyPreviewCamera
    // roots before rebuilding them.
    internal static class LobbySceneRuntimeAssemblyTool
    {
        private const string MenuPath = "Tools/Scene/Rebuild Lobby Scene Runtime (Destructive)";
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string LobbyPrefabPath = "Assets/Prefabs/Features/Lobby/Root/LobbyPageRoot.prefab";
        private const string GaragePrefabPath = "Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab";
        private const string CreateRoomModalPrefabPath = "Assets/Prefabs/Features/Lobby/Root/SetACreateRoomModalRoot.prefab";
        private const string RoomDetailPanelPrefabPath = "Assets/Prefabs/Features/Lobby/Independent/SetCRoomDetailPanelRoot.prefab";
        private const string CommonErrorDialogPrefabPath = "Assets/Prefabs/Features/Common/Independent/SetCCommonErrorDialogRoot.prefab";
        private const string LoginLoadingOverlayPrefabPath = "Assets/Prefabs/Features/Common/Independent/SetCLoginLoadingOverlayRoot.prefab";
        private const string AccountConfigPath = "Assets/Settings/AccountConfig.asset";
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string SoundCatalogPath = "Assets/Data/Sound/SoundCatalog.asset";
        private const float GarageMobileViewportLeftInset = 16f;
        private const float GarageMobileViewportBottomInset = 78f;
        private const float GarageMobileViewportWidthInset = -32f;
        private const float GarageMobileViewportHeightInset = -218f;
        private const float GarageMobileSlotHostHeight = 208f;
        private const float GarageMobileFocusBarHeight = 52f;
        private const float GarageMobileEditorHeight = 188f;
        private const float GarageMobilePreviewCardHeight = 220f;
        private const float GarageMobileResultPaneHeight = 118f;
        private const float GarageMobileSaveDockBottomOffset = 88f;
        private const float GarageMobileSaveDockHeight = 72f;

        [MenuItem(MenuPath)]
        private static void Assemble()
        {
            if (!ConfirmDestructiveRebuild())
                return;

            Debug.Log("[LobbySceneRuntimeAssemblyTool] Starting destructive LobbyScene runtime rebuild. Existing runtime/canvas/input roots will be replaced.");

            EnsureSceneOpen();

            DestroyRootIfExists("LobbyRuntime");
            DestroyRootIfExists("LobbyCanvas");
            DestroyRootIfExists("EventSystem");
            DestroyRootIfExists("LobbyPreviewCamera");

            var runtimeRoot = new GameObject("LobbyRuntime").transform;
            var canvasRoot = CreateCanvasRoot();
            EnsureEventSystem();

            var lobbyPage = InstantiateUiSurface(LobbyPrefabPath, "LobbyPageRoot", canvasRoot.transform);
            var garagePage = InstantiateUiSurface(GaragePrefabPath, "GaragePageRoot", canvasRoot.transform);
            HideChildIfExists(lobbyPage.transform, "HeaderChrome");
            HideChildIfExists(lobbyPage.transform, "MainScroll");
            HideChildIfExists(lobbyPage.transform, "SaveDock");
            HideChildIfExists(garagePage.transform, "HeaderChrome");
            HideChildIfExists(garagePage.transform, "MainScroll");
            HideChildIfExists(garagePage.transform, "SaveDock");
            HideChildIfExists(garagePage.transform, "RightRailRoot");
            var overlays = CreateRect(canvasRoot.transform, "Overlays", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var createRoomModal = InstantiateUiSurface(CreateRoomModalPrefabPath, "SetACreateRoomModalRoot", overlays);
            var roomDetailSurface = InstantiateUiSurface(RoomDetailPanelPrefabPath, "SetCRoomDetailPanelRoot", overlays);
            var commonErrorSurface = InstantiateUiSurface(CommonErrorDialogPrefabPath, "SetCCommonErrorDialogRoot", overlays);
            var loginLoadingSurface = InstantiateUiSurface(LoginLoadingOverlayPrefabPath, "SetCLoginLoadingOverlayRoot", overlays);

            createRoomModal.SetActive(false);
            roomDetailSurface.SetActive(false);
            commonErrorSurface.SetActive(false);
            loginLoadingSurface.SetActive(false);

            var lobbyGroup = EnsureComponent<CanvasGroup>(lobbyPage);
            var garageGroup = EnsureComponent<CanvasGroup>(garagePage);
            var lobbyRuntime = CreateRuntimeObject("LobbyView", runtimeRoot);
            var lobbyView = lobbyRuntime.AddComponent<LobbyView>();

            var navBar = BuildNavBar(canvasRoot.transform);
            var roomListView = BuildRoomList(lobbyPage.transform);
            var roomDetailView = BuildRoomDetail(roomDetailSurface.transform);
            var summaryView = BuildGarageSummary(lobbyPage.transform);
            var garagePageController = BuildGaragePage(garagePage.transform);
            var loginLoadingView = BuildLoginLoading(loginLoadingSurface.transform);
            var accountSettingsView = BuildAccountSettings(overlays);
            var sceneErrorPresenter = BuildSceneErrorPresenter(commonErrorSurface.transform);

            var lobbySetup = CreateRuntimeObject("LobbySetup", runtimeRoot).AddComponent<LobbySetup>();
            var photonAdapter = CreateRuntimeObject("LobbyPhotonAdapter", runtimeRoot).AddComponent<LobbyPhotonAdapter>();
            var accountSetup = CreateRuntimeObject("AccountSetup", runtimeRoot).AddComponent<AccountSetup>();
            var unitSetup = CreateRuntimeObject("UnitSetup", runtimeRoot).AddComponent<UnitSetup>();
            var garageSetup = CreateRuntimeObject("GarageSetup", runtimeRoot).AddComponent<GarageSetup>();
            var garageNetworkAdapter = CreateRuntimeObject("GarageNetworkAdapter", runtimeRoot).AddComponent<GarageNetworkAdapter>();
            var soundPlayer = CreateRuntimeObject("SoundPlayer", runtimeRoot).AddComponent<SoundPlayer>();

            SetRef(accountSetup, "_config", LoadRequiredAsset<Object>(AccountConfigPath));
            SetRef(unitSetup, "_moduleCatalog", LoadRequiredAsset<Object>(ModuleCatalogPath));
            SetRef(soundPlayer, "catalog", LoadRequiredAsset<Object>(SoundCatalogPath));
            SetRef(garageSetup, "_networkAdapter", garageNetworkAdapter);
            SetRef(garageSetup, "_pageController", garagePageController);

            SetRef(lobbyView, "_lobbyPageRoot", lobbyPage);
            SetRef(lobbyView, "_garagePageRoot", garagePage);
            SetRef(lobbyView, "_navigationBar", navBar);
            SetRef(lobbyView, "_roomListPanel", roomListView.gameObject);
            SetRef(lobbyView, "_roomDetailPanel", roomDetailView.gameObject);
            SetRef(lobbyView, "_lobbyPageCanvasGroup", lobbyGroup);
            SetRef(lobbyView, "_garagePageCanvasGroup", garageGroup);
            SetRef(lobbyView, "_roomListView", roomListView);
            SetRef(lobbyView, "_roomDetailView", roomDetailView);
            SetRef(lobbyView, "_garageSummaryView", summaryView);

            SetRef(lobbySetup, "_view", lobbyView);
            SetRef(lobbySetup, "_photonAdapter", photonAdapter);
            SetRef(lobbySetup, "_sceneErrorPresenter", sceneErrorPresenter);
            SetRef(lobbySetup, "_soundPlayer", soundPlayer);
            SetRef(lobbySetup, "_accountSetup", accountSetup);
            SetRef(lobbySetup, "_loginLoadingView", loginLoadingView);
            SetRef(lobbySetup, "_accountSettingsView", accountSettingsView);
            SetRef(lobbySetup, "_unitSetup", unitSetup);
            SetRef(lobbySetup, "_garageSetup", garageSetup);
            SetString(photonAdapter, "DefaultGameSceneName", "BattleScene");

            garagePage.SetActive(false);
            RegisterBuildSettings();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[LobbySceneRuntimeAssemblyTool] Destructive LobbyScene runtime rebuild completed.");
        }

        private static bool ConfirmDestructiveRebuild()
        {
            return EditorUtility.DisplayDialog(
                "Rebuild LobbyScene Runtime",
                "This command deletes and recreates LobbyRuntime, LobbyCanvas, EventSystem, and LobbyPreviewCamera in Assets/Scenes/LobbyScene.unity. Use it only as a bounded rebuild fallback, not as a routine scene repair.",
                "Rebuild",
                "Cancel");
        }

        private static void EnsureSceneOpen()
        {
            if (!File.Exists(ScenePath))
            {
                var created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(created, ScenePath, true);
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject CreateRuntimeObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateCanvasRoot()
        {
            var canvasGo = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(canvasGo.GetComponent<RectTransform>());
            return canvasGo;
        }

        private static void EnsureEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<EventSystem>();
        }

        private static GameObject InstantiateUiSurface(string prefabPath, string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance;
            if (prefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
                if (instance == null)
                    throw new System.InvalidOperationException("Failed to instantiate prefab: " + prefabPath);
            }
            else
            {
                instance = new GameObject(name, typeof(RectTransform), typeof(Image));
                instance.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 1f);
                Debug.LogWarning("[LobbySceneRuntimeAssemblyTool] Missing prefab, created fallback: " + prefabPath);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            if (instance.transform is RectTransform rect)
                Stretch(rect);
            return instance;
        }

        private static LobbyGarageNavBarView BuildNavBar(Transform parent)
        {
            var root = CreatePanel(parent, "LobbyGarageNavBar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 72f), new Color(0.03f, 0.05f, 0.09f, 0.96f));
            var view = root.gameObject.AddComponent<LobbyGarageNavBarView>();
            var lobby = CreateButton(root, "LobbyTabButton", "Lobby", new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(150f, 48f));
            var garage = CreateButton(root, "GarageTabButton", "Garage", new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(150f, 48f));
            var lobbyBorder = CreateImage((RectTransform)lobby.transform, "LobbyTabBorder", new Color(0.28f, 0.46f, 1f, 1f));
            var garageBorder = CreateImage((RectTransform)garage.transform, "GarageTabBorder", new Color(0.28f, 0.46f, 1f, 1f));
            SetBottomBorder(lobbyBorder.rectTransform);
            SetBottomBorder(garageBorder.rectTransform);
            SetRef(view, "_lobbyTabButton", lobby);
            SetRef(view, "_garageTabButton", garage);
            SetRef(view, "_lobbyTabText", lobby.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_garageTabText", garage.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_lobbyTabBorder", lobbyBorder);
            SetRef(view, "_garageTabBorder", garageBorder);
            return view;
        }

        private static RoomListView BuildRoomList(Transform parent)
        {
            var root = CreatePanel(parent, "RoomListPanel", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(-24f, -168f), new Color(0.06f, 0.08f, 0.13f, 0.86f));
            var view = root.gameObject.AddComponent<RoomListView>();
            var title = CreateText(root, "Title", "Open Rooms", 28f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -20f), new Vector2(-32f, 42f));
            title.color = Color.white;
            var roomName = CreateInput(root, "RoomNameInput", "Room", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -74f), new Vector2(-32f, 40f));
            var capacity = CreateInput(root, "CapacityInput", "4", new Vector2(0f, 1f), new Vector2(0.48f, 1f), new Vector2(0f, 1f), new Vector2(16f, -122f), new Vector2(-20f, 40f));
            var display = CreateInput(root, "DisplayNameInput", "Pilot", new Vector2(0.52f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -122f), new Vector2(-20f, 40f));
            var create = CreateButton(root, "CreateRoomButton", "Create", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -170f), new Vector2(-32f, 44f));
            var count = CreateText(root, "RoomListCountText", "0 open rooms", 16f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -226f), new Vector2(-32f, 28f));
            var empty = CreateText(root, "RoomListEmptyStateText", "No open rooms yet.", 18f, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32f, 80f));
            var content = CreateRect(root, "RoomListContent", new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), new Vector2(-32f, -32f));
            content.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 8f;
            var itemPrefab = BuildRoomItemTemplate(root);
            SetRef(view, "_roomNameInput", roomName);
            SetRef(view, "_capacityInput", capacity);
            SetRef(view, "_displayNameInput", display);
            SetRef(view, "_createRoomButton", create);
            SetRef(view, "_roomListContent", content);
            SetRef(view, "_roomItemPrefab", itemPrefab);
            SetRef(view, "_roomListCountText", count);
            SetRef(view, "_roomListEmptyStateText", empty);
            return view;
        }

        private static RoomItemView BuildRoomItemTemplate(RectTransform parent)
        {
            var root = CreatePanel(parent, "RoomItemTemplate", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(-32f, 64f), new Color(0.09f, 0.12f, 0.19f, 1f));
            var view = root.gameObject.AddComponent<RoomItemView>();
            var name = CreateText(root, "RoomNameText", "Room", 18f, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0.45f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 10f), new Vector2(-12f, 24f));
            var count = CreateText(root, "MemberCountText", "0/4", 14f, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0.45f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -14f), new Vector2(-12f, 20f));
            var difficulty = CreateText(root, "DifficultyText", "Normal", 14f, TextAlignmentOptions.Left, new Vector2(0.45f, 0.5f), new Vector2(0.70f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(-8f, 24f));
            var meta = CreateText(root, "RoomMetaText", "Normal", 12f, TextAlignmentOptions.Left, new Vector2(0.45f, 0.5f), new Vector2(0.70f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -22f), new Vector2(-8f, 18f));
            var join = CreateButton(root, "JoinButton", "Join", new Vector2(0.74f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(-8f, 40f));
            SetRef(view, "_roomNameText", name);
            SetRef(view, "_memberCountText", count);
            SetRef(view, "_difficultyText", difficulty);
            SetRef(view, "_roomMetaText", meta);
            SetRef(view, "_joinButton", join);
            root.gameObject.SetActive(false);
            return view;
        }

        private static RoomDetailView BuildRoomDetail(Transform parent)
        {
            var root = CreatePanel(parent, "RoomDetailPanel", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(16f, 96f), new Vector2(-32f, -192f), new Color(0.06f, 0.08f, 0.13f, 0.94f));
            var view = root.gameObject.AddComponent<RoomDetailView>();
            var roomName = CreateText(root, "RoomNameText", "Room", 26f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -20f), new Vector2(-32f, 38f));
            var count = CreateText(root, "MemberCountText", "0/4", 16f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(16f, -62f), new Vector2(-24f, 24f));
            var difficulty = CreateText(root, "DifficultyText", "Difficulty: Normal", 16f, TextAlignmentOptions.Right, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -62f), new Vector2(-24f, 24f));
            var content = CreateRect(root, "MemberListContent", new Vector2(0f, 0.35f), new Vector2(1f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-32f, 0f));
            content.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 8f;
            var memberPrefab = BuildMemberItemTemplate(root);
            var leave = CreateButton(root, "LeaveButton", "Leave", new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 0f), new Vector2(16f, 116f), new Vector2(-20f, 42f));
            var red = CreateButton(root, "TeamRedButton", "Red", new Vector2(0.52f, 0f), new Vector2(0.76f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 116f), new Vector2(-12f, 42f));
            var blue = CreateButton(root, "TeamBlueButton", "Blue", new Vector2(0.76f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 116f), new Vector2(-12f, 42f));
            var ready = CreateButton(root, "ReadyButton", "Ready", new Vector2(0f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 0f), new Vector2(16f, 64f), new Vector2(-20f, 42f));
            var start = CreateButton(root, "StartGameButton", "Start", new Vector2(0.52f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 64f), new Vector2(-20f, 42f));
            SetRef(view, "_roomNameText", roomName);
            SetRef(view, "_memberCountText", count);
            SetRef(view, "_difficultyText", difficulty);
            SetRef(view, "_memberListContent", content);
            SetRef(view, "_memberItemPrefab", memberPrefab);
            SetRef(view, "_leaveButton", leave);
            SetRef(view, "_teamRedButton", red);
            SetRef(view, "_teamBlueButton", blue);
            SetRef(view, "_readyButton", ready);
            SetRef(view, "_readyButtonText", ready.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_startGameButton", start);
            return view;
        }

        private static MemberItemView BuildMemberItemTemplate(RectTransform parent)
        {
            var root = CreatePanel(parent, "MemberItemTemplate", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(-32f, 48f), new Color(0.09f, 0.12f, 0.19f, 1f));
            var view = root.gameObject.AddComponent<MemberItemView>();
            var name = CreateText(root, "NameText", "Pilot", 16f, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0.55f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(-12f, 24f));
            var team = CreateText(root, "TeamText", "-", 16f, TextAlignmentOptions.Center, new Vector2(0.55f, 0.5f), new Vector2(0.82f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, 24f));
            var icon = CreateImage(root, "ReadyIcon", Color.gray);
            icon.rectTransform.anchorMin = new Vector2(0.88f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0.88f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.rectTransform.sizeDelta = new Vector2(16f, 16f);
            SetRef(view, "_nameText", name);
            SetRef(view, "_teamText", team);
            SetRef(view, "_readyIcon", icon);
            root.gameObject.SetActive(false);
            return view;
        }

        private static LobbyGarageSummaryView BuildGarageSummary(Transform parent)
        {
            var root = CreatePanel(parent, "LobbyGarageSummary", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(16f, 132f), new Vector2(-32f, 96f), new Color(0.08f, 0.11f, 0.18f, 0.92f));
            var view = root.gameObject.AddComponent<LobbyGarageSummaryView>();
            var status = CreateText(root, "StatusPillText", "LOCKED", 14f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -12f), new Vector2(-24f, 20f));
            var headline = CreateText(root, "HeadlineText", "No saved roster yet", 18f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -36f), new Vector2(-24f, 24f));
            var body = CreateText(root, "BodyText", "Save at least 3 units to unlock Ready.", 14f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 12f), new Vector2(-24f, 28f));
            body.gameObject.SetActive(false);
            SetRef(view, "_statusPillText", status);
            SetRef(view, "_headlineText", headline);
            SetRef(view, "_bodyText", body);
            root.gameObject.SetActive(false);
            return view;
        }

        private static GaragePageController BuildGaragePage(Transform parent)
        {
            var controller = parent.gameObject.AddComponent<GaragePageController>();
            var chromeBindings = parent.gameObject.AddComponent<GaragePageChromeBindings>();
            var header = CreateText((RectTransform)parent, "GarageHeaderSummaryText", "Garage", 24f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -18f), new Vector2(-96f, 32f));
            header.gameObject.SetActive(false);
            var settingsOpen = CreateButton((RectTransform)parent, "SettingsOpenButton", "Settings", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -18f), new Vector2(80f, 32f));
            var contentRoot = CreatePanel(parent, "MobileContentRoot", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(GarageMobileViewportLeftInset, GarageMobileViewportBottomInset), new Vector2(GarageMobileViewportWidthInset, GarageMobileViewportHeightInset), new Color(0.06f, 0.08f, 0.13f, 0.82f));
            var bodyHost = CreatePanel(contentRoot, "MobileBodyHost", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 0f), Color.clear);
            ConfigureMobileScroll(contentRoot, bodyHost);
            var slotHost = CreatePanel(bodyHost, "MobileSlotHost", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, GarageMobileSlotHostHeight), new Color(0.04f, 0.05f, 0.09f, 0.4f));
            SetPreferredHeight(slotHost, GarageMobileSlotHostHeight);
            var roster = BuildRosterList(slotHost);
            var tabBar = CreatePanel(bodyHost, "MobileTabBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, GarageMobileFocusBarHeight), new Color(0.03f, 0.05f, 0.09f, 0.95f));
            SetPreferredHeight(tabBar, GarageMobileFocusBarHeight);
            var editTab = CreateButton(tabBar, "MobileEditTabButton", "Edit", new Vector2(0f, 0.5f), new Vector2(0.33f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(-8f, 36f));
            var firepowerTab = CreateButton(tabBar, "MobileFirepowerTabButton", "Firepower", new Vector2(0.33f, 0.5f), new Vector2(0.66f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, 36f));
            var summaryTab = CreateButton(tabBar, "MobileSummaryTabButton", "Summary", new Vector2(0.66f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(-8f, 36f));
            var editor = BuildUnitEditor(bodyHost);
            SetPreferredHeight((RectTransform)editor.transform, GarageMobileEditorHeight);
            var previewCard = CreatePanel(bodyHost, "PreviewCard", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, GarageMobilePreviewCardHeight), new Color(0.08f, 0.11f, 0.18f, 1f));
            SetPreferredHeight(previewCard, GarageMobilePreviewCardHeight);
            var preview = BuildPreview(previewCard);
            var resultPane = CreatePanel(bodyHost, "ResultPane", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, GarageMobileResultPaneHeight), new Color(0.08f, 0.11f, 0.18f, 1f));
            SetPreferredHeight(resultPane, GarageMobileResultPaneHeight);
            var result = BuildResultPanel(resultPane);
            var saveDock = CreatePanel(parent, "MobileSaveDockRoot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(GarageMobileViewportLeftInset, GarageMobileSaveDockBottomOffset), new Vector2(GarageMobileViewportWidthInset, GarageMobileSaveDockHeight), new Color(0.04f, 0.06f, 0.10f, 0.98f));
            var saveButton = CreateButton(saveDock, "MobileSaveButton", "Save", new Vector2(0f, 0.5f), new Vector2(0.45f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(-16f, 48f));
            var saveState = CreateText(saveDock, "MobileSaveStateText", "Draft", 14f, TextAlignmentOptions.Left, new Vector2(0.48f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(4f, 0f), new Vector2(-12f, -16f));
            var settingsOverlay = CreatePanel(parent, "SettingsOverlayRoot", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
            var settingsClose = CreateButton(settingsOverlay, "SettingsCloseButton", "Close", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 48f));
            settingsOverlay.gameObject.SetActive(false);
            var rightRail = CreatePanel(parent, "RightRailRoot", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 0f), Color.clear);
            rightRail.gameObject.SetActive(false);

            SetRef(controller, "_rosterListView", roster);
            SetRef(controller, "_unitEditorView", editor);
            SetRef(controller, "_resultPanelView", result);
            SetRef(controller, "_unitPreviewView", preview);
            SetRef(controller, "_chromeBindings", chromeBindings);

            SetRef(chromeBindings, "_mobileContentRoot", contentRoot.gameObject);
            SetRef(chromeBindings, "_mobileBodyHost", bodyHost);
            SetRef(chromeBindings, "_mobileSlotHost", slotHost);
            SetRef(chromeBindings, "_rightRailRoot", rightRail.gameObject);
            SetRef(chromeBindings, "_previewCard", previewCard.gameObject);
            SetRef(chromeBindings, "_resultPane", resultPane.gameObject);
            SetRef(chromeBindings, "_mobileTabBar", tabBar.gameObject);
            SetRef(chromeBindings, "_mobileEditTabButton", editTab);
            SetRef(chromeBindings, "_mobileEditTabLabel", editTab.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_mobileFirepowerTabButton", firepowerTab);
            SetRef(chromeBindings, "_mobileFirepowerTabLabel", firepowerTab.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_mobileSummaryTabButton", summaryTab);
            SetRef(chromeBindings, "_mobileSummaryTabLabel", summaryTab.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_garageHeaderSummaryText", header);
            SetRef(chromeBindings, "_settingsOpenButton", settingsOpen);
            SetRef(chromeBindings, "_settingsOpenButtonLabel", settingsOpen.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_settingsOverlayRoot", settingsOverlay.gameObject);
            SetRef(chromeBindings, "_settingsCloseButton", settingsClose);
            SetRef(chromeBindings, "_settingsCloseButtonLabel", settingsClose.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_mobileSaveDockRoot", saveDock.gameObject);
            SetRef(chromeBindings, "_mobileSaveButton", saveButton);
            SetRef(chromeBindings, "_mobileSaveButtonLabel", saveButton.GetComponentInChildren<TMP_Text>());
            SetRef(chromeBindings, "_mobileSaveStateText", saveState);
            return controller;
        }

        private static void ConfigureMobileScroll(RectTransform viewport, RectTransform content)
        {
            var mask = EnsureComponent<RectMask2D>(viewport.gameObject);
            mask.padding = Vector4.zero;
            mask.softness = Vector2Int.zero;

            var scrollRect = EnsureComponent<ScrollRect>(viewport.gameObject);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            var layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            layout.padding = new RectOffset(8, 8, 8, 24);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void SetPreferredHeight(RectTransform rect, float preferredHeight)
        {
            var element = EnsureComponent<LayoutElement>(rect.gameObject);
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
        }

        private static GarageRosterListView BuildRosterList(RectTransform parent)
        {
            var root = CreatePanel(parent, "GarageRosterListView", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-16f, -16f), Color.clear);
            var view = root.gameObject.AddComponent<GarageRosterListView>();
            var layout = root.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.cellSize = new Vector2(160f, 56f);
            layout.spacing = new Vector2(8f, 8f);
            var slots = new GarageSlotItemView[6];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildGarageSlot(root, i + 1);
            SetRefArray(view, "_slotViews", slots);
            return view;
        }

        private static GarageSlotItemView BuildGarageSlot(RectTransform parent, int number)
        {
            var root = CreatePanel(parent, "GarageSlot" + number, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.13f, 0.20f, 1f));
            var button = root.gameObject.AddComponent<Button>();
            var view = root.gameObject.AddComponent<GarageSlotItemView>();
            var bg = root.GetComponent<Image>();
            SetButtonGraphic(button, bg);
            var slot = CreateText(root, "SlotNumberText", number.ToString(), 10f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -6f), new Vector2(-24f, 14f));
            var title = CreateText(root, "TitleText", "+", 17f, TextAlignmentOptions.Left, new Vector2(0f, 0.40f), new Vector2(1f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(-44f, 22f));
            var summary = CreateText(root, "SummaryText", "Empty", 11f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0.34f), new Vector2(0.5f, 0.5f), new Vector2(16f, 4f), new Vector2(-32f, 16f));
            var arrow = CreateText(root, "ArrowIndicator", ">", 18f, TextAlignmentOptions.Right, new Vector2(0.85f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(-8f, 24f)).gameObject;
            var border = CreateImage(root, "BorderImage", new Color(0.28f, 0.46f, 1f, 1f));
            SetLeftBorder(border.rectTransform);
            border.raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            SetRef(view, "_button", button);
            SetRef(view, "_background", bg);
            SetRef(view, "_slotNumberText", slot);
            SetRef(view, "_titleText", title);
            SetRef(view, "_summaryText", summary);
            SetRef(view, "_arrowIndicator", arrow);
            SetRef(view, "_borderImage", border);
            SetRef(view, "_canvasGroup", group);
            return view;
        }

        private static GarageUnitEditorView BuildUnitEditor(RectTransform parent)
        {
            var root = CreatePanel(parent, "GarageUnitEditorView", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-16f, -16f), new Color(0.07f, 0.10f, 0.16f, 1f));
            var view = root.gameObject.AddComponent<GarageUnitEditorView>();
            var title = CreateText(root, "SelectionTitleText", "Select unit", 20f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -10f), new Vector2(-24f, 28f));
            var subtitle = CreateText(root, "SelectionSubtitleText", "Pick frame, firepower, mobility.", 14f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -42f), new Vector2(-24f, 24f));
            var frame = BuildPartSelector(root, "FrameSelectorView", "Frame", new Vector2(0f, 0.48f), new Vector2(1f, 0.73f));
            var firepower = BuildPartSelector(root, "FirepowerSelectorView", "Firepower", new Vector2(0f, 0.48f), new Vector2(1f, 0.73f));
            var mobility = BuildPartSelector(root, "MobilitySelectorView", "Mobility", new Vector2(0f, 0.48f), new Vector2(1f, 0.73f));
            var clear = CreateButton(root, "ClearButton", "Clear", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 8f), new Vector2(-24f, 36f));
            SetRef(view, "_selectionTitleText", title);
            SetRef(view, "_selectionSubtitleText", subtitle);
            SetRef(view, "_frameSelectorView", frame);
            SetRef(view, "_firepowerSelectorView", firepower);
            SetRef(view, "_mobilitySelectorView", mobility);
            SetRef(view, "_clearButton", clear);
            SetRef(view, "_clearButtonText", clear.GetComponentInChildren<TMP_Text>());
            return view;
        }

        private static GaragePartSelectorView BuildPartSelector(RectTransform parent, string name, string titleText, Vector2 min, Vector2 max)
        {
            var root = CreatePanel(parent, name, min, max, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(-24f, -8f), new Color(0.10f, 0.13f, 0.20f, 1f));
            var view = root.gameObject.AddComponent<GaragePartSelectorView>();
            var prev = CreateButton(root, "PrevButton", "<", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(36f, 34f));
            var next = CreateButton(root, "NextButton", ">", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(36f, 34f));
            var title = CreateText(root, "TitleText", titleText, 10f, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, -7f), new Vector2(-96f, 14f));
            var value = CreateText(root, "ValueText", "< empty >", 17f, TextAlignmentOptions.Center, new Vector2(0f, 0.42f), new Vector2(1f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(48f, -2f), new Vector2(-96f, 24f));
            var hint = CreateText(root, "HintText", "Tap arrows", 10f, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 0.36f), new Vector2(0.5f, 0.5f), new Vector2(48f, 4f), new Vector2(-96f, 18f));
            SetRef(view, "_prevButton", prev);
            SetRef(view, "_nextButton", next);
            SetRef(view, "_titleText", title);
            SetRef(view, "_valueText", value);
            SetRef(view, "_hintText", hint);
            return view;
        }

        private static GarageUnitPreviewView BuildPreview(RectTransform parent)
        {
            var root = CreatePanel(parent, "GarageUnitPreviewView", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-16f, -16f), new Color(0.04f, 0.05f, 0.09f, 1f));
            var view = root.gameObject.AddComponent<GarageUnitPreviewView>();
            var raw = CreateRect(root, "RawImage", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            raw.color = new Color(0.07f, 0.09f, 0.13f, 1f);
            var empty = CreateText(root, "EmptyStateText", "Saved unit silhouette", 16f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, 60f));
            var camera = new GameObject("LobbyPreviewCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.position = new Vector3(100f, 100f, -6f);
            camera.orthographic = true;
            camera.orthographicSize = 2.2f;
            var framePrefab = CreatePreviewPrimitive("PreviewFrameTemplate", PrimitiveType.Cube);
            var weaponPrefab = CreatePreviewPrimitive("PreviewWeaponTemplate", PrimitiveType.Cylinder);
            var thrusterPrefab = CreatePreviewPrimitive("PreviewThrusterTemplate", PrimitiveType.Capsule);
            SetRef(view, "_previewCamera", camera);
            SetRef(view, "_rawImage", raw);
            SetRef(view, "_emptyStateText", empty);
            SetRef(view, "_framePrefab", framePrefab);
            SetRef(view, "_weaponPrefab", weaponPrefab);
            SetRef(view, "_thrusterPrefab", thrusterPrefab);
            return view;
        }

        private static GameObject CreatePreviewPrimitive(string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = new Vector3(100f, 100f, 100f);
            go.SetActive(false);
            return go;
        }

        private static GarageResultPanelView BuildResultPanel(RectTransform parent)
        {
            var root = CreatePanel(parent, "GarageResultPanelView", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-16f, -16f), Color.clear);
            var view = root.gameObject.AddComponent<GarageResultPanelView>();
            var status = CreateText(root, "RosterStatusText", "Roster draft", 15f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(8f, -8f), new Vector2(-16f, 22f));
            var validation = CreateText(root, "ValidationText", "Save at least 3 units.", 13f, TextAlignmentOptions.Left, new Vector2(0f, 0.55f), new Vector2(1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-16f, 30f));
            var stats = CreateText(root, "StatsText", "0/6 units", 13f, TextAlignmentOptions.Left, new Vector2(0f, 0.28f), new Vector2(1f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-16f, 24f));
            var save = CreateButton(root, "SaveButton", "Save", new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(0f, 0f), new Vector2(8f, 8f), new Vector2(-16f, 36f));
            var toast = CreatePanel(root, "ToastPanel", new Vector2(0.48f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(-16f, 36f), new Color(0.04f, 0.06f, 0.10f, 1f));
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            var toastText = CreateText(toast, "ToastText", "Saved", 12f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetRef(view, "_rosterStatusText", status);
            SetRef(view, "_validationText", validation);
            SetRef(view, "_statsText", stats);
            SetRef(view, "_saveButton", save);
            SetRef(view, "_saveButtonText", save.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_saveButtonImage", save.GetComponent<Image>());
            SetRef(view, "_toastPanel", toast.gameObject);
            SetRef(view, "_toastCanvasGroup", toastGroup);
            SetRef(view, "_toastText", toastText);
            return view;
        }

        private static LoginLoadingView BuildLoginLoading(Transform parent)
        {
            var view = parent.gameObject.AddComponent<LoginLoadingView>();
            var loading = CreatePanel(parent, "LoadingPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
            var status = CreateText(loading, "StatusText", "Signing in...", 20f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, 80f));
            var error = CreatePanel(parent, "ErrorPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));
            var errorText = CreateText(error, "ErrorText", "Network error.", 18f, TextAlignmentOptions.Center, new Vector2(0f, 0.45f), new Vector2(1f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-48f, 96f));
            var retry = CreateButton(error, "RetryButton", "Retry", new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 44f));
            error.gameObject.SetActive(false);
            SetRef(view, "_loadingPanel", loading.gameObject);
            SetRef(view, "_statusText", status);
            SetRef(view, "_errorPanel", error.gameObject);
            SetRef(view, "_errorText", errorText);
            SetRef(view, "_retryButton", retry);
            return view;
        }

        private static AccountSettingsView BuildAccountSettings(RectTransform parent)
        {
            var root = CreatePanel(parent, "AccountSettingsView", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -18f), new Vector2(-32f, 128f), new Color(0.06f, 0.08f, 0.13f, 0.96f));
            var view = root.gameObject.AddComponent<AccountSettingsView>();
            var auth = CreateText(root, "AuthTypeText", "Anonymous account", 14f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -10f), new Vector2(-24f, 22f));
            var display = CreateText(root, "DisplayNameText", "Pilot profile", 18f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -36f), new Vector2(-24f, 24f));
            var google = CreateButton(root, "GoogleSignInButton", "Google", new Vector2(0f, 0f), new Vector2(0.32f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(-8f, 36f));
            var logout = CreateButton(root, "LogoutButton", "Logout", new Vector2(0.34f, 0f), new Vector2(0.66f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-8f, 36f));
            var delete = CreateButton(root, "DeleteAccountButton", "Delete", new Vector2(0.68f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(-8f, 36f));
            var status = CreateText(root, "StatusMessageText", "Link Google to keep this roster across devices.", 12f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 56f), new Vector2(-24f, 24f));
            SetRef(view, "_authTypeText", auth);
            SetRef(view, "_displayNameText", display);
            SetRef(view, "_googleSignInButton", google);
            SetRef(view, "_logoutButton", logout);
            SetRef(view, "_deleteAccountButton", delete);
            SetRef(view, "_deleteAccountButtonText", delete.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_statusMessageText", status);
            root.gameObject.SetActive(false);
            return view;
        }

        private static SceneErrorPresenter BuildSceneErrorPresenter(Transform parent)
        {
            var view = parent.gameObject.AddComponent<SceneErrorPresenter>();
            var banner = CreatePanel(parent, "Banner", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -16f), new Vector2(-32f, 56f), new Color(0.22f, 0.10f, 0.08f, 0.96f));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            var bannerText = CreateText(banner, "BannerMessageText", "Error", 15f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var modal = CreatePanel(parent, "Modal", new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.10f, 0.16f, 0.98f));
            var modalGroup = modal.gameObject.AddComponent<CanvasGroup>();
            var modalText = CreateText(modal, "ModalMessageText", "Error", 17f, TextAlignmentOptions.Center, new Vector2(0f, 0.35f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32f, -20f));
            var dismiss = CreateButton(modal, "ModalDismissButton", "OK", new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 40f));
            SetRef(view, "_bannerGroup", bannerGroup);
            SetRef(view, "_bannerMessageText", bannerText);
            SetRef(view, "_modalGroup", modalGroup);
            SetRef(view, "_modalMessageText", modalText);
            SetRef(view, "_modalDismissButton", dismiss);
            return view;
        }

        private static TMP_InputField CreateInput(RectTransform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, new Color(0.10f, 0.13f, 0.20f, 1f));
            var input = root.gameObject.AddComponent<TMP_InputField>();
            var viewport = CreateRect(root, "TextViewport", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-16f, -8f));
            var text = CreateText(viewport, "Text", value, 16f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var placeholder = CreateText(viewport, "Placeholder", value, 16f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            placeholder.color = new Color(0.6f, 0.64f, 0.72f, 0.55f);
            SetRef(input, "m_TextViewport", viewport);
            SetRef(input, "m_TextComponent", text);
            SetRef(input, "m_Placeholder", placeholder);
            input.text = value;
            SetButtonGraphic(input, root.GetComponent<Image>());
            return input;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, new Color(0.18f, 0.29f, 0.62f, 1f));
            var button = root.gameObject.AddComponent<Button>();
            SetButtonGraphic(button, root.GetComponent<Image>());
            var label = CreateText(root, "Label", text, 15f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            label.color = Color.white;
            return button;
        }

        private static TMP_Text CreateText(RectTransform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.88f, 0.91f, 0.96f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var image = CreateRect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetBottomBorder(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 4f);
        }

        private static void SetLeftBorder(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(4f, 0f);
        }

        private static void SetButtonGraphic(Selectable selectable, Graphic graphic)
        {
            var so = new SerializedObject(selectable);
            var property = so.FindProperty("m_TargetGraphic");
            if (property != null)
            {
                property.objectReferenceValue = graphic;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
                throw new System.InvalidOperationException(target.GetType().Name + "." + fieldName + " was not found.");

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray<T>(Object target, string fieldName, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null || !property.isArray)
                throw new System.InvalidOperationException(target.GetType().Name + "." + fieldName + " array was not found.");

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
                throw new System.InvalidOperationException(target.GetType().Name + "." + fieldName + " was not found.");

            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException("Required asset missing: " + path);
            return asset;
        }

        private static void DestroyRootIfExists(string name)
        {
            var existing = GameObject.Find("/" + name);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        private static void HideChildIfExists(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                child.gameObject.SetActive(false);
        }

        private static void RegisterBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            var hasLobby = false;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path != ScenePath)
                    continue;

                scenes[i].enabled = true;
                hasLobby = true;
            }

            if (!hasLobby)
            {
                var updated = new EditorBuildSettingsScene[scenes.Length + 1];
                updated[0] = new EditorBuildSettingsScene(ScenePath, true);
                for (var i = 0; i < scenes.Length; i++)
                    updated[i + 1] = scenes[i];
                scenes = updated;
            }

            EditorBuildSettings.scenes = scenes;
        }
    }
}
#endif
