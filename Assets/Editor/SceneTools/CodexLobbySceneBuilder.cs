#if UNITY_EDITOR
using System;
using Features.Lobby.Infrastructure.Photon;
using Features.Lobby.Presentation;
using Shared.Network;
using Shared.Runtime.Sound;
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
    public static class CodexLobbySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CodexLobbyScene.unity";
        private const string SoundCatalogPath = "Assets/Data/Sound/SoundCatalog.asset";
        private const string PooledAudioSourcePrefabPath = "Assets/Prefabs/Sound/PooledAudioSource.prefab";

        [MenuItem("Tools/Codex/Build Codex Lobby Scene")]
        private static void BuildCodexLobbyScene()
        {
            BuildCodexLobbySceneCore();
        }

        public static void BuildCodexLobbySceneForAutomation()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildCodexLobbySceneCore();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }

        private static void BuildCodexLobbySceneCore()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Open {ScenePath} before running the builder.");

            ClearScene(scene);

            CreateCamera();
            CreateDirectionalLight();
            CreateEventSystem();

            var canvas = CreateCanvas();
            CreateBackdrop(canvas.transform);
            var lobbyPageRoot = CreatePageRoot("LobbyPageRoot", canvas.transform);
            Stretch(lobbyPageRoot.GetComponent<RectTransform>(), new Vector2(0.03f, 0.12f), new Vector2(0.38f, 0.84f), Vector2.zero, Vector2.zero);
            lobbyPageRoot.AddComponent<CanvasGroup>();

            var title = CreateText(
                "Title",
                canvas.transform,
                "CODEX LOBBY",
                38,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                new Color32(242, 245, 255, 255));
            Stretch(title.rectTransform, new Vector2(0.05f, 0.905f), new Vector2(0.95f, 0.985f), Vector2.zero, Vector2.zero);

            var subtitle = CreateText(
                "Subtitle",
                canvas.transform,
                "Photon room flow for room creation, ready checks, and scene handoff.",
                18,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                new Color32(138, 156, 196, 255));
            Stretch(subtitle.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.925f), Vector2.zero, Vector2.zero);

            var roomListView = BuildRoomListPanel(lobbyPageRoot.transform);
            var roomDetailView = BuildRoomDetailPanel(lobbyPageRoot.transform);
            var errorPresenter = BuildSceneErrorPresenter(canvas.transform);

            var photonConnectionGo = CreateGameObject("PhotonConnection");
            photonConnectionGo.AddComponent<PhotonConnectionAdapter>();

            var photonAdapterGo = CreateGameObject("LobbyPhotonAdapter");
            var photonAdapter = photonAdapterGo.AddComponent<LobbyPhotonAdapter>();
            SetString(photonAdapter, "DefaultGameSceneName", "GameScene");

            var soundPlayerGo = CreateGameObject("SoundPlayer");
            var soundPlayer = soundPlayerGo.AddComponent<SoundPlayer>();
            SetObject(soundPlayer, "audioSourcePrefab", LoadAsset<GameObject>(PooledAudioSourcePrefabPath));
            SetObject(soundPlayer, "catalog", LoadAsset<ScriptableObject>(SoundCatalogPath));
            SetInt(soundPlayer, "initialPoolSize", 8);

            var lobbyViewGo = CreateGameObject("LobbyView");
            var lobbyView = lobbyViewGo.AddComponent<LobbyView>();
            SetObject(lobbyView, "_lobbyPageRoot", lobbyPageRoot);
            SetObject(lobbyView, "_roomListPanel", roomListView.gameObject);
            SetObject(lobbyView, "_roomDetailPanel", roomDetailView.gameObject);
            SetObject(lobbyView, "_roomListView", roomListView);
            SetObject(lobbyView, "_roomDetailView", roomDetailView);
            SetString(lobbyView, "_gameSceneName", "GameScene");

            var lobbySetupGo = CreateGameObject("LobbySetup");
            var lobbySetup = lobbySetupGo.AddComponent<LobbySetup>();
            SetObject(lobbySetup, "_view", lobbyView);
            SetObject(lobbySetup, "_photonAdapter", photonAdapter);
            SetObject(lobbySetup, "_sceneErrorPresenter", errorPresenter);
            SetObject(lobbySetup, "_soundPlayer", soundPlayer);

            CodexLobbyGarageAugmenter.Augment(canvas, lobbyView, lobbySetup);
            CodexLobbyAccountAugmenter.Augment(canvas, lobbySetup);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[CodexLobbySceneBuilder] CodexLobbyScene rebuilt.");
        }

        private static void CreateBackdrop(Transform parent)
        {
            var backdrop = CreatePanel("Backdrop", parent, new Color32(11, 16, 30, 255));
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var glow = CreatePanel("TopGlow", backdrop.transform, new Color32(33, 72, 139, 96));
            Stretch(glow.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.62f, 1.04f), Vector2.zero, Vector2.zero);
        }

        private static GameObject CreatePageRoot(string name, Transform parent)
        {
            var root = CreateGameObject(name, parent, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return root;
        }

        private static RoomListView BuildRoomListPanel(Transform parent)
        {
            var panel = CreatePanel("RoomListPanel", parent, new Color32(24, 31, 50, 242));
            Stretch(panel.rectTransform, Vector2.zero, new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateHeaderText(panel.transform, "PanelTitle", "Rooms", 28, new Color32(244, 247, 255, 255), 36f);
            CreateHeaderText(
                panel.transform,
                "Hint",
                "Create Room uses fallback defaults while dedicated input fields are still being wired.",
                16,
                new Color32(143, 164, 201, 255),
                48f);

            var roomNameInput = CreateInputField("RoomNameInput", panel.transform, "Room name", "Codex Room");
            var capacityInput = CreateInputField("CapacityInput", panel.transform, "Capacity", "4");
            var displayNameInput = CreateInputField("DisplayNameInput", panel.transform, "Display name", "Player");

            var createButton = CreateButton(
                "CreateRoomButton",
                panel.transform,
                "Create Default Room",
                new Color32(71, 125, 255, 255),
                new Color32(248, 251, 255, 255));
            createButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            CreateHeaderText(panel.transform, "ListHeader", "Open rooms", 17, new Color32(230, 237, 252, 255), 26f);

            var contentGo = CreateGameObject("RoomListContent", panel.transform, typeof(RectTransform));
            var contentRect = contentGo.GetComponent<RectTransform>();
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentGo.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var template = BuildRoomItemTemplate(contentGo.transform);
            template.gameObject.SetActive(false);

            var roomListView = panel.gameObject.AddComponent<RoomListView>();
            SetObject(roomListView, "_roomNameInput", roomNameInput);
            SetObject(roomListView, "_capacityInput", capacityInput);
            SetObject(roomListView, "_displayNameInput", displayNameInput);
            SetObject(roomListView, "_createRoomButton", createButton);
            SetObject(roomListView, "_roomListContent", contentRect);
            SetObject(roomListView, "_roomItemPrefab", template);
            return roomListView;
        }

        private static RoomDetailView BuildRoomDetailPanel(Transform parent)
        {
            var panel = CreatePanel("RoomDetailPanel", parent, new Color32(22, 29, 48, 242));
            Stretch(panel.rectTransform, Vector2.zero, new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var roomNameText = CreateHeaderText(panel.transform, "RoomNameText", "Select a room", 28, new Color32(244, 247, 255, 255), 38f);
            var memberCountText = CreateHeaderText(panel.transform, "MemberCountText", "0/4", 16, new Color32(143, 164, 201, 255), 24f);
            var difficultyText = CreateHeaderText(panel.transform, "DifficultyText", "Difficulty: Normal", 16, new Color32(143, 164, 201, 255), 24f);

            var buttonRow = CreateGameObject("ActionButtons", panel.transform, typeof(RectTransform));
            var buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 10f;
            buttonRowLayout.childAlignment = TextAnchor.MiddleLeft;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childControlWidth = true;
            buttonRowLayout.childForceExpandHeight = false;
            buttonRowLayout.childForceExpandWidth = false;
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 52f;

            var leaveButton = CreateButton("LeaveButton", buttonRow.transform, "Leave", new Color32(87, 93, 112, 255), Color.white);
            var redButton = CreateButton("TeamRedButton", buttonRow.transform, "Red", new Color32(176, 68, 77, 255), Color.white);
            var blueButton = CreateButton("TeamBlueButton", buttonRow.transform, "Blue", new Color32(59, 96, 186, 255), Color.white);
            var readyButton = CreateButton("ReadyButton", buttonRow.transform, "Ready", new Color32(49, 151, 111, 255), Color.white);
            var startButton = CreateButton("StartGameButton", buttonRow.transform, "Start", new Color32(255, 183, 64, 255), new Color32(24, 24, 24, 255));
            SetPreferredWidth(leaveButton, 90f);
            SetPreferredWidth(redButton, 82f);
            SetPreferredWidth(blueButton, 82f);
            SetPreferredWidth(readyButton, 90f);
            SetPreferredWidth(startButton, 104f);

            CreateHeaderText(panel.transform, "MembersHeader", "Members", 18, new Color32(230, 237, 252, 255), 28f);

            var contentGo = CreateGameObject("MemberListContent", panel.transform, typeof(RectTransform));
            var contentRect = contentGo.GetComponent<RectTransform>();
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentGo.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var memberTemplate = BuildMemberItemTemplate(contentGo.transform);
            memberTemplate.gameObject.SetActive(false);

            var roomDetailView = panel.gameObject.AddComponent<RoomDetailView>();
            SetObject(roomDetailView, "_roomNameText", roomNameText);
            SetObject(roomDetailView, "_memberCountText", memberCountText);
            SetObject(roomDetailView, "_difficultyText", difficultyText);
            SetObject(roomDetailView, "_memberListContent", contentRect);
            SetObject(roomDetailView, "_memberItemPrefab", memberTemplate);
            SetObject(roomDetailView, "_leaveButton", leaveButton);
            SetObject(roomDetailView, "_teamRedButton", redButton);
            SetObject(roomDetailView, "_teamBlueButton", blueButton);
            SetObject(roomDetailView, "_readyButton", readyButton);
            SetObject(roomDetailView, "_readyButtonText", readyButton.GetComponentInChildren<TextMeshProUGUI>());
            SetObject(roomDetailView, "_startGameButton", startButton);

            panel.gameObject.SetActive(false);
            return roomDetailView;
        }

        private static SceneErrorPresenter BuildSceneErrorPresenter(Transform parent)
        {
            var root = CreateGameObject("SceneErrorPresenter", parent, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var banner = CreatePanel("Banner", root.transform, new Color32(193, 80, 86, 235));
            Stretch(banner.rectTransform, new Vector2(0.32f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            var bannerMessage = CreateText(
                "BannerMessage",
                banner.transform,
                "Scene errors will appear here.",
                16,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                Color.white);
            Stretch(bannerMessage.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

            var modalOverlay = CreatePanel("Modal", root.transform, new Color32(4, 8, 15, 210));
            Stretch(modalOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var modalGroup = modalOverlay.gameObject.AddComponent<CanvasGroup>();

            var card = CreatePanel("ModalCard", modalOverlay.transform, new Color32(28, 35, 58, 255));
            Stretch(card.rectTransform, new Vector2(0.29f, 0.33f), new Vector2(0.71f, 0.63f), Vector2.zero, Vector2.zero);

            var modalMessage = CreateText(
                "ModalMessage",
                card.transform,
                "Modal errors will appear here.",
                20,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color32(244, 247, 255, 255));
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

        private static RoomItemView BuildRoomItemTemplate(Transform parent)
        {
            var item = CreatePanel("RoomItemTemplate", parent, new Color32(36, 44, 66, 255));
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            var layout = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var roomNameText = CreateText("RoomNameText", item.transform, "Room Name", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color32(244, 247, 255, 255));
            roomNameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 2.4f;

            var memberCountText = CreateText("MemberCountText", item.transform, "1/4", 15, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(143, 164, 201, 255));
            memberCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 60f;

            var difficultyText = CreateText("DifficultyText", item.transform, "Normal", 15, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(255, 198, 87, 255));
            difficultyText.gameObject.AddComponent<LayoutElement>().preferredWidth = 78f;

            var joinButton = CreateButton("JoinButton", item.transform, "Join", new Color32(71, 125, 255, 255), Color.white);
            SetPreferredWidth(joinButton, 92f);

            var roomItemView = item.gameObject.AddComponent<RoomItemView>();
            SetObject(roomItemView, "_roomNameText", roomNameText);
            SetObject(roomItemView, "_memberCountText", memberCountText);
            SetObject(roomItemView, "_difficultyText", difficultyText);
            SetObject(roomItemView, "_joinButton", joinButton);
            return roomItemView;
        }

        private static MemberItemView BuildMemberItemTemplate(Transform parent)
        {
            var item = CreatePanel("MemberItemTemplate", parent, new Color32(36, 44, 66, 255));
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;

            var layout = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var nameText = CreateText("NameText", item.transform, "Player", 17, FontStyles.Bold, TextAlignmentOptions.Left, new Color32(244, 247, 255, 255));
            nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 2f;

            var teamText = CreateText("TeamText", item.transform, "-", 15, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(143, 164, 201, 255));
            teamText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

            var readyIconGo = CreateGameObject("ReadyIcon", item.transform, typeof(RectTransform), typeof(Image));
            var readyLayout = readyIconGo.AddComponent<LayoutElement>();
            readyLayout.preferredWidth = 18f;
            readyLayout.minWidth = 18f;
            var readyImage = readyIconGo.GetComponent<Image>();
            ApplyDefaultSprite(readyImage);
            readyImage.color = Color.gray;

            var memberItemView = item.gameObject.AddComponent<MemberItemView>();
            SetObject(memberItemView, "_nameText", nameText);
            SetObject(memberItemView, "_teamText", teamText);
            SetObject(memberItemView, "_readyIcon", readyImage);
            SetColor(memberItemView, "_readyColor", "#48D597");
            SetColor(memberItemView, "_notReadyColor", "#77829A");
            return memberItemView;
        }

        private static TextMeshProUGUI CreateHeaderText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Color color,
            float preferredHeight)
        {
            var text = CreateText(name, parent, value, fontSize, FontStyles.Bold, TextAlignmentOptions.Left, color);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return text;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string label, string placeholder)
        {
            var root = CreateGameObject(name, parent, typeof(RectTransform));
            root.AddComponent<LayoutElement>().preferredHeight = 72f;

            var labelText = CreateText("Label", root.transform, label, 13, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(labelText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var fieldGo = CreateGameObject("Field", root.transform, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            Stretch(fieldGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.6f), Vector2.zero, Vector2.zero);

            var fieldImage = fieldGo.GetComponent<Image>();
            ApplyDefaultSprite(fieldImage);
            fieldImage.type = Image.Type.Sliced;
            fieldImage.color = new Color32(18, 24, 39, 255);

            var textViewport = CreateGameObject("Text Area", fieldGo.transform, typeof(RectTransform), typeof(RectMask2D));
            Stretch(textViewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));

            var text = CreateText("Text", textViewport.transform, string.Empty, 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color32(244, 247, 255, 255));
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var placeholderText = CreateText("Placeholder", textViewport.transform, placeholder, 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color32(109, 124, 154, 255));
            Stretch(placeholderText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var input = fieldGo.GetComponent<TMP_InputField>();
            input.textViewport = textViewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            return input;
        }

        private static Canvas CreateCanvas()
        {
            var go = CreateGameObject("Canvas", null, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return canvas;
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

        private static void CreateCamera()
        {
            var cameraGo = CreateGameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 12, 22, 255);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateDirectionalLight()
        {
            var lightGo = CreateGameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color32(255, 244, 224, 255);
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
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

        private static Button CreateButton(string name, Transform parent, string label, Color fill, Color textColor)
        {
            var go = CreateGameObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var image = go.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = fill;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = fill * 1.08f;
            colors.pressedColor = fill * 0.92f;
            colors.selectedColor = fill * 1.02f;
            colors.disabledColor = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 0.65f);
            button.colors = colors;

            var labelText = CreateText("Label", go.transform, label, 16, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            return button;
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

        private static void Stretch(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
        }

        private static void SetPreferredWidth(Button button, float width)
        {
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset not found at {path}");
            return asset;
        }

        private static void ApplyDefaultSprite(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.material = null;
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

        private static void SetColor(UnityEngine.Object target, string propertyName, string htmlColor)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            if (!ColorUtility.TryParseHtmlString(htmlColor, out var color))
                throw new InvalidOperationException($"Invalid color value {htmlColor}");

            property.colorValue = color;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
