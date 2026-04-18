#if UNITY_EDITOR
using System;
using Features.Account.Presentation;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Lobby.Presentation;
using Features.Unit;
using Features.Unit.Infrastructure;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class CodexLobbyGarageAugmenter
    {
        private const string ScenePath = "Assets/Scenes/CodexLobbyScene.unity";

        [MenuItem("Tools/Codex/Augment Current Codex Lobby With Garage")]
        private static void AugmentCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Open {ScenePath} before running the augmenter.");

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var lobbyView = UnityEngine.Object.FindFirstObjectByType<LobbyView>();
            var lobbySetup = UnityEngine.Object.FindFirstObjectByType<LobbySetup>();

            if (canvas == null || lobbyView == null || lobbySetup == null)
                throw new InvalidOperationException("Canvas, LobbyView, and LobbySetup must exist before Garage augmentation.");

            Augment(canvas, lobbyView, lobbySetup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[CodexLobbyGarageAugmenter] Garage dashboard layout applied to CodexLobbyScene.");
        }

        internal static void Augment(Canvas canvas, LobbyView lobbyView, LobbySetup lobbySetup)
        {
            var catalog = CodexLobbyGarageDataBuilder.EnsureModuleCatalog();
            var lobbyPageRoot = EnsureLobbyPageRoot(canvas.transform);
            var garageButton = EnsureLobbyNavigationButton(lobbyPageRoot);
            ConfigureLobbyPanels(lobbyPageRoot);

            DestroyIfExists("GaragePageRoot");
            DestroyIfExists("UnitSetup");
            DestroyIfExists("GarageNetworkAdapter");
            DestroyIfExists("GarageSetup");
            DestroyIfExists("PreviewCamera");
            DestroyIfExists("FrameCube");
            DestroyIfExists("WeaponCylinder");
            DestroyIfExists("ThrusterCapsule");

            var garage = BuildGaragePage(canvas.transform);

            var unitSetup = CreateGameObject("UnitSetup").AddComponent<UnitSetup>();
            CodexLobbyGarageDataBuilder.SetObject(unitSetup, "_moduleCatalog", catalog);

            var garageNetwork = CreateGameObject("GarageNetworkAdapter").AddComponent<GarageNetworkAdapter>();

            var garageSetup = CreateGameObject("GarageSetup").AddComponent<GarageSetup>();
            CodexLobbyGarageDataBuilder.SetObject(garageSetup, "_networkAdapter", garageNetwork);
            CodexLobbyGarageDataBuilder.SetObject(garageSetup, "_pageController", garage.PageController);

            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyPageRoot", lobbyPageRoot.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garagePageRoot", garage.Root.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyTabButton", garage.LobbyNavigation.Button);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garageTabButton", garageButton.Button);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyTabText", garage.LobbyNavigation.Label);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garageTabText", garageButton.Label);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyTabBorder", garage.LobbyNavigation.Border);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garageTabBorder", garageButton.Border);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyPageCanvasGroup", EnsureCanvasGroup(lobbyPageRoot.gameObject));
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garagePageCanvasGroup", EnsureCanvasGroup(garage.Root.gameObject));

            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_unitSetup", unitSetup);
            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_garageSetup", garageSetup);
            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_accountSettingsView", garage.AccountSettingsView);
        }

        private static RectTransform EnsureLobbyPageRoot(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("LobbyPageRoot") as RectTransform;
            if (existing == null)
            {
                var root = CreateGameObject("LobbyPageRoot", canvasTransform, typeof(RectTransform));
                existing = root.GetComponent<RectTransform>();
            }

            Stretch(existing, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            EnsureCanvasGroup(existing.gameObject);

            var roomListPanel = UnityEngine.Object.FindFirstObjectByType<RoomListView>();
            if (roomListPanel != null)
                roomListPanel.transform.SetParent(existing, false);

            var roomDetailPanel = UnityEngine.Object.FindFirstObjectByType<RoomDetailView>();
            if (roomDetailPanel != null)
                roomDetailPanel.transform.SetParent(existing, false);

            return existing;
        }

        private static ButtonWidgets EnsureLobbyNavigationButton(RectTransform lobbyPageRoot)
        {
            var existing = lobbyPageRoot.Find("GarageTabButton");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var garageButton = CreateTabButton("GarageTabButton", lobbyPageRoot, "Garage", new Color32(73, 118, 255, 255));
            Stretch(garageButton.Button.GetComponent<RectTransform>(), new Vector2(0.82f, 0.90f), new Vector2(0.98f, 0.985f), Vector2.zero, Vector2.zero);
            var layout = garageButton.Button.GetComponent<LayoutElement>();
            if (layout != null)
                UnityEngine.Object.DestroyImmediate(layout);
            return garageButton;
        }

        private static void ConfigureLobbyPanels(RectTransform lobbyPageRoot)
        {
            if (lobbyPageRoot == null)
                return;

            var roomListPanel = UnityEngine.Object.FindFirstObjectByType<RoomListView>();
            if (roomListPanel != null)
                Stretch((RectTransform)roomListPanel.transform, Vector2.zero, new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero);

            var roomDetailPanel = UnityEngine.Object.FindFirstObjectByType<RoomDetailView>();
            if (roomDetailPanel != null)
                Stretch((RectTransform)roomDetailPanel.transform, Vector2.zero, new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero);
        }

        private static GarageBuildResult BuildGaragePage(Transform parent)
        {
            var rootImage = CreatePanel("GaragePageRoot", parent, new Color32(18, 25, 40, 247));
            var root = rootImage.rectTransform;
            Stretch(root, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

            EnsureCanvasGroup(root.gameObject);

            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(20, 20, 20, 20);
            rootLayout.spacing = 16f;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            var headerRow = CreateGameObject("GarageHeaderRow", root, typeof(RectTransform));
            var headerRowLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerRowLayout.spacing = 16f;
            headerRowLayout.childAlignment = TextAnchor.UpperLeft;
            headerRowLayout.childControlWidth = true;
            headerRowLayout.childControlHeight = true;
            headerRowLayout.childForceExpandWidth = false;
            headerRowLayout.childForceExpandHeight = false;
            headerRow.AddComponent<LayoutElement>().preferredHeight = 78f;

            var titleStack = CreateGameObject("GarageTitleStack", headerRow.transform, typeof(RectTransform));
            var titleStackLayout = titleStack.AddComponent<VerticalLayoutGroup>();
            titleStackLayout.spacing = 6f;
            titleStackLayout.childAlignment = TextAnchor.UpperLeft;
            titleStackLayout.childControlWidth = true;
            titleStackLayout.childControlHeight = true;
            titleStackLayout.childForceExpandWidth = true;
            titleStackLayout.childForceExpandHeight = false;
            titleStack.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var title = CreateText("GarageTitle", titleStack.transform, "GARAGE", 28, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            var subtitle = CreateText(
                "GarageSubtitle",
                titleStack.transform,
                "Edit your roster here, then return to the lobby when the loadout is ready.",
                13,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                new Color32(143, 164, 201, 255));
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var lobbyButton = CreateTabButton("LobbyTabButton", headerRow.transform, "Back To Lobby", new Color32(22, 164, 143, 255));
            var lobbyButtonLayout = lobbyButton.Button.gameObject.GetComponent<LayoutElement>();
            if (lobbyButtonLayout != null)
            {
                lobbyButtonLayout.preferredWidth = 176f;
                lobbyButtonLayout.minWidth = 176f;
            }

            var contentRow = CreateGameObject("GarageContentRow", root, typeof(RectTransform));
            var contentRowLayout = contentRow.AddComponent<HorizontalLayoutGroup>();
            contentRowLayout.spacing = 16f;
            contentRowLayout.childAlignment = TextAnchor.UpperLeft;
            contentRowLayout.childControlWidth = true;
            contentRowLayout.childControlHeight = true;
            contentRowLayout.childForceExpandWidth = false;
            contentRowLayout.childForceExpandHeight = true;
            var contentRowElement = contentRow.AddComponent<LayoutElement>();
            contentRowElement.flexibleHeight = 1f;
            contentRowElement.minHeight = 420f;

            var rosterPane = CreatePanel("RosterListPane", contentRow.transform, new Color32(23, 33, 52, 252));
            var rosterElement = rosterPane.gameObject.AddComponent<LayoutElement>();
            rosterElement.preferredWidth = 284f;
            rosterElement.minWidth = 256f;
            rosterElement.flexibleHeight = 1f;
            var rosterLayout = rosterPane.gameObject.AddComponent<VerticalLayoutGroup>();
            rosterLayout.padding = new RectOffset(16, 16, 18, 18);
            rosterLayout.spacing = 10f;
            rosterLayout.childAlignment = TextAnchor.UpperCenter;
            rosterLayout.childControlHeight = true;
            rosterLayout.childControlWidth = true;
            rosterLayout.childForceExpandHeight = false;
            rosterLayout.childForceExpandWidth = true;

            var slotViews = new GarageSlotItemView[Features.Garage.Domain.GarageRoster.MaxSlots];
            for (int i = 0; i < slotViews.Length; i++)
                slotViews[i] = BuildGarageSlotItem(rosterPane.transform, i);

            var editorPane = CreatePanel("UnitEditorPane", contentRow.transform, new Color32(24, 31, 50, 242));
            var editorElement = editorPane.gameObject.AddComponent<LayoutElement>();
            editorElement.flexibleWidth = 1.35f;
            editorElement.minWidth = 456f;
            editorElement.flexibleHeight = 1f;
            var editorLayout = editorPane.gameObject.AddComponent<VerticalLayoutGroup>();
            editorLayout.padding = new RectOffset(20, 20, 20, 20);
            editorLayout.spacing = 16f;
            editorLayout.childAlignment = TextAnchor.UpperLeft;
            editorLayout.childControlHeight = true;
            editorLayout.childControlWidth = true;
            editorLayout.childForceExpandHeight = false;
            editorLayout.childForceExpandWidth = true;

            var selectionTitle = CreateText("SelectionTitle", editorPane.transform, "Slot 1 Empty", 22, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            selectionTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var selectionSubtitle = CreateText("SelectionSubtitle", editorPane.transform, "Build a loadout and save when the roster is ready.", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            selectionSubtitle.textWrappingMode = TextWrappingModes.Normal;
            selectionSubtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            var frameSelector = BuildSelectorCard(editorPane.transform, "FRAME", new Color32(77, 127, 255, 255));
            var firepowerSelector = BuildSelectorCard(editorPane.transform, "FIREPOWER", new Color32(255, 134, 82, 255));
            var mobilitySelector = BuildSelectorCard(editorPane.transform, "MOBILITY", new Color32(31, 183, 150, 255));

            var clearButton = CreateButton("ClearButton", editorPane.transform, "Clear Draft", new Color32(193, 80, 86, 255), Color.white);
            clearButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            var rightRail = CreateGameObject("RightRail", contentRow.transform, typeof(RectTransform));
            var rightRailElement = rightRail.AddComponent<LayoutElement>();
            rightRailElement.preferredWidth = 320f;
            rightRailElement.minWidth = 292f;
            rightRailElement.flexibleHeight = 1f;
            var rightRailLayout = rightRail.AddComponent<VerticalLayoutGroup>();
            rightRailLayout.spacing = 16f;
            rightRailLayout.childAlignment = TextAnchor.UpperLeft;
            rightRailLayout.childControlWidth = true;
            rightRailLayout.childControlHeight = true;
            rightRailLayout.childForceExpandWidth = true;
            rightRailLayout.childForceExpandHeight = false;

            var account = BuildAccountCard(rightRail.transform);
            var preview = BuildPreviewCard(rightRail.transform);
            var result = BuildResultPane(rightRail.transform);

            var previewViewport = CreateGameObject("UnitPreviewViewport", preview.Card.transform);
            var previewLightGo = CreateGameObject("PreviewKeyLight", previewViewport.transform, typeof(Light));
            previewLightGo.transform.localPosition = new Vector3(1.2f, 1.6f, -1.5f);
            previewLightGo.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);
            var previewLight = previewLightGo.GetComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.color = new Color(0.86f, 0.91f, 1f, 1f);
            previewLight.intensity = 1.25f;

            var previewCameraGo = CreateGameObject("PreviewCamera", previewViewport.transform, typeof(Camera));
            previewCameraGo.transform.localPosition = new Vector3(0f, 0.05f, -2.35f);
            var previewCamera = previewCameraGo.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 20f;
            previewCamera.fieldOfView = 24f;

            var framePrototype = CreatePrimitivePrototype("FrameCube", PrimitiveType.Cube, previewViewport.transform);
            var weaponPrototype = CreatePrimitivePrototype("WeaponCylinder", PrimitiveType.Cylinder, previewViewport.transform);
            var thrusterPrototype = CreatePrimitivePrototype("ThrusterCapsule", PrimitiveType.Capsule, previewViewport.transform);

            var rosterListView = rosterPane.gameObject.AddComponent<GarageRosterListView>();
            CodexLobbyGarageDataBuilder.SetObjectArray(rosterListView, "_slotViews", slotViews);

            var frameSelectorView = frameSelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_prevButton", frameSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_nextButton", frameSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_titleText", frameSelector.TitleText);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_valueText", frameSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_hintText", frameSelector.HintText);

            var firepowerSelectorView = firepowerSelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_prevButton", firepowerSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_nextButton", firepowerSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_titleText", firepowerSelector.TitleText);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_valueText", firepowerSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_hintText", firepowerSelector.HintText);

            var mobilitySelectorView = mobilitySelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_prevButton", mobilitySelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_nextButton", mobilitySelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_titleText", mobilitySelector.TitleText);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_valueText", mobilitySelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_hintText", mobilitySelector.HintText);

            var unitEditorView = editorPane.gameObject.AddComponent<GarageUnitEditorView>();
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_selectionTitleText", selectionTitle);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_selectionSubtitleText", selectionSubtitle);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_frameSelectorView", frameSelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_firepowerSelectorView", firepowerSelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_mobilitySelectorView", mobilitySelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_clearButton", clearButton);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_clearButtonText", GetButtonLabel(clearButton));

            var resultPanelView = result.Root.gameObject.AddComponent<GarageResultPanelView>();
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_rosterStatusText", result.RosterStatusText);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_validationText", result.ValidationText);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_statsText", result.StatsText);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_saveButton", result.SaveButton);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_saveButtonText", result.SaveButtonLabel);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_saveButtonImage", result.SaveButtonImage);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_toastPanel", result.ToastPanel);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_toastCanvasGroup", result.ToastCanvasGroup);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_toastText", result.ToastText);

            var previewView = previewViewport.AddComponent<GarageUnitPreviewView>();
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_previewCamera", previewCamera);
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_rawImage", preview.RawImage);
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_emptyStateText", preview.EmptyStateText);
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_framePrefab", framePrototype);
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_weaponPrefab", weaponPrototype);
            CodexLobbyGarageDataBuilder.SetObject(previewView, "_thrusterPrefab", thrusterPrototype);

            var pageController = root.gameObject.AddComponent<GaragePageController>();
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_rosterListView", rosterListView);
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_unitEditorView", unitEditorView);
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_resultPanelView", resultPanelView);
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_unitPreviewView", previewView);

            return new GarageBuildResult(root, pageController, account.View, lobbyButton);
        }

        private static GarageSlotItemView BuildGarageSlotItem(Transform parent, int index)
        {
            var slot = CreateGameObject($"GarageSlot{index + 1}", parent, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            var layoutElement = slot.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 104f;
            layoutElement.minHeight = 104f;

            var image = slot.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = new Color32(25, 32, 49, 255);

            var button = slot.GetComponent<Button>();
            button.targetGraphic = image;

            var slotNumber = CreateText("SlotNumber", slot.transform, $"SLOT {index + 1}", 12, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(slotNumber.rectTransform, new Vector2(0.11f, 0.73f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            var title = CreateText("Title", slot.transform, "Empty Slot", 17, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(title.rectTransform, new Vector2(0.11f, 0.42f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            var summary = CreateText("Summary", slot.transform, "Select frame and modules", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(170, 188, 224, 255));
            Stretch(summary.rectTransform, new Vector2(0.11f, 0.11f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);

            var arrowIndicator = CreateText("ArrowIndicator", slot.transform, ">", 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(arrowIndicator.rectTransform, new Vector2(0.02f, 0.32f), new Vector2(0.09f, 0.64f), Vector2.zero, Vector2.zero);
            arrowIndicator.gameObject.SetActive(false);

            var border = CreateGameObject("Border", slot.transform, typeof(RectTransform), typeof(Image));
            Stretch(border.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var borderImage = border.GetComponent<Image>();
            ApplyDefaultSprite(borderImage);
            borderImage.type = Image.Type.Sliced;
            borderImage.color = Color.clear;
            borderImage.raycastTarget = false;
            border.SetActive(false);

            var slotView = slot.AddComponent<GarageSlotItemView>();
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_button", button);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_background", image);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_slotNumberText", slotNumber);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_titleText", title);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_summaryText", summary);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_arrowIndicator", arrowIndicator.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_borderImage", borderImage);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_canvasGroup", slot.GetComponent<CanvasGroup>());
            return slotView;
        }

        private static SelectorWidgets BuildSelectorCard(Transform parent, string title, Color accent)
        {
            var card = CreatePanel(title + "Card", parent, new Color32(31, 39, 61, 255));
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 132f;

            var titleText = CreateText(title + "Title", card.transform, title, 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, accent);
            Stretch(titleText.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.9f, 0.93f), Vector2.zero, Vector2.zero);

            var valuePanel = CreatePanel(title + "ValuePanel", card.transform, new Color32(18, 24, 39, 255));
            Stretch(valuePanel.rectTransform, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.68f), Vector2.zero, Vector2.zero);

            var prevButton = CreateButton(title + "PrevButton", valuePanel.transform, "<", accent, Color.white);
            Stretch(prevButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.14f), new Vector2(0.15f, 0.86f), Vector2.zero, Vector2.zero);

            var nextButton = CreateButton(title + "NextButton", valuePanel.transform, ">", accent, Color.white);
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.85f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);

            var valueText = CreateText(title + "ValueText", valuePanel.transform, "< Select >", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color32(244, 247, 255, 255));
            Stretch(valueText.rectTransform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);

            var hintText = CreateText(title + "HintText", card.transform, "Choose a part to update this slot.", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(170, 188, 224, 255));
            Stretch(hintText.rectTransform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.29f), Vector2.zero, Vector2.zero);

            return new SelectorWidgets(card.rectTransform, prevButton, nextButton, titleText, valueText, hintText);
        }

        private static AccountCardWidgets BuildAccountCard(Transform parent)
        {
            var card = CreatePanel("AccountCard", parent, new Color32(21, 30, 47, 252));
            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(16, 16, 16, 16);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = CreateText("AccountHeader", card.transform, "ACCOUNT", 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var authType = CreateText("AccountAuthTypeText", card.transform, "Auth: anonymous", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            authType.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var displayName = CreateText("AccountDisplayNameText", card.transform, "Guest Pilot", 18, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            displayName.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var googleButton = CreateButton("AccountGoogleSignInButton", card.transform, "Link Google", new Color32(73, 118, 255, 255), Color.white);
            googleButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

            var logoutButton = CreateButton("AccountLogoutButton", card.transform, "Logout", new Color32(87, 93, 112, 255), Color.white);
            logoutButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            var deleteButton = CreateButton("AccountDeleteButton", card.transform, "Delete Account", new Color32(193, 80, 86, 255), Color.white);
            deleteButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            var statusText = CreateText("AccountStatusText", card.transform, "Account status will appear here.", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(170, 188, 224, 255));
            statusText.textWrappingMode = TextWrappingModes.Normal;
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var view = card.gameObject.AddComponent<AccountSettingsView>();
            CodexLobbyGarageDataBuilder.SetObject(view, "_authTypeText", authType);
            CodexLobbyGarageDataBuilder.SetObject(view, "_displayNameText", displayName);
            CodexLobbyGarageDataBuilder.SetObject(view, "_googleSignInButton", googleButton);
            CodexLobbyGarageDataBuilder.SetObject(view, "_logoutButton", logoutButton);
            CodexLobbyGarageDataBuilder.SetObject(view, "_deleteAccountButton", deleteButton);
            CodexLobbyGarageDataBuilder.SetObject(view, "_deleteAccountButtonText", GetButtonLabel(deleteButton));
            CodexLobbyGarageDataBuilder.SetObject(view, "_statusMessageText", statusText);

            return new AccountCardWidgets(card.rectTransform, view);
        }

        private static PreviewWidgets BuildPreviewCard(Transform parent)
        {
            var card = CreatePanel("PreviewCard", parent, new Color32(20, 28, 45, 242));
            var cardElement = card.gameObject.AddComponent<LayoutElement>();
            cardElement.preferredHeight = 268f;

            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(16, 16, 16, 16);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            var header = CreateText("PreviewHeader", card.transform, "PREVIEW", 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var rawImageGo = CreateGameObject("PreviewRawImage", card.transform, typeof(RectTransform), typeof(RawImage));
            var rawImage = rawImageGo.GetComponent<RawImage>();
            rawImage.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            rawImage.raycastTarget = true;
            var rawElement = rawImageGo.AddComponent<LayoutElement>();
            rawElement.preferredHeight = 180f;
            rawElement.flexibleHeight = 1f;

            var emptyState = CreateText(
                "PreviewEmptyState",
                rawImageGo.transform,
                "No preview yet\nSelect a saved unit to inspect the loadout silhouette.",
                12,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Color32(143, 164, 201, 255));
            Stretch(emptyState.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            emptyState.textWrappingMode = TextWrappingModes.Normal;

            var hint = CreateText("PreviewHint", card.transform, "Selected saved unit appears here.", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            return new PreviewWidgets(card.rectTransform, rawImage, emptyState);
        }

        private static ResultWidgets BuildResultPane(Transform parent)
        {
            var rootImage = CreatePanel("ResultPane", parent, new Color32(20, 28, 45, 242));
            var root = rootImage.rectTransform;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1f;
            layoutElement.minHeight = 240f;

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = CreateText("ResultHeader", root, "RESULT", 18, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var rosterStatus = CreateText("RosterStatus", root, "Roster incomplete: 0/6 saved units. Add 3 more for Ready.", 15, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(232, 238, 252, 255));
            rosterStatus.textWrappingMode = TextWrappingModes.Normal;
            rosterStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

            var validationText = CreateText("ValidationText", root, "Select frame, firepower, and mobility to save this slot.", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(115, 222, 196, 255));
            validationText.textWrappingMode = TextWrappingModes.Normal;
            validationText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var statsText = CreateText("StatsText", root, "Pick all three parts to see composed HP, damage, and summon cost.", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(214, 223, 241, 255));
            statsText.textWrappingMode = TextWrappingModes.Normal;
            var statsElement = statsText.gameObject.AddComponent<LayoutElement>();
            statsElement.preferredHeight = 78f;
            statsElement.flexibleHeight = 1f;

            var saveButton = CreateButton("SaveButton", root, "Save Roster", new Color32(51, 102, 230, 255), Color.white);
            saveButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

            var toastPanel = CreatePanel("ToastPanel", root, new Color32(30, 41, 69, 235));
            var toastElement = toastPanel.gameObject.AddComponent<LayoutElement>();
            toastElement.preferredHeight = 34f;
            var toastCanvasGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            var toastText = CreateText("ToastText", toastPanel.transform, "Saved.", 12, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            Stretch(toastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-10f, -6f));

            return new ResultWidgets(
                root,
                rosterStatus,
                validationText,
                statsText,
                saveButton,
                GetButtonLabel(saveButton),
                saveButton.GetComponent<Image>(),
                toastPanel.gameObject,
                toastCanvasGroup,
                toastText);
        }

        private static GameObject CreatePrimitivePrototype(string name, PrimitiveType primitiveType, Transform parent)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                    ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var material = new Material(shader);
                    material.color = new Color(0.82f, 0.88f, 1f, 1f);
                    renderer.sharedMaterial = material;
                }
            }
            go.SetActive(false);
            return go;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            return group;
        }

        private static ButtonWidgets CreateTabButton(string name, Transform parent, string label, Color fill)
        {
            var button = CreateButton(name, parent, label, fill, Color.white);
            button.gameObject.AddComponent<LayoutElement>().preferredWidth = 148f;

            var borderGo = CreateGameObject("Border", button.transform, typeof(RectTransform), typeof(Image));
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(3f, 0f);
            var border = borderGo.GetComponent<Image>();
            ApplyDefaultSprite(border);
            border.type = Image.Type.Sliced;
            border.color = Color.clear;
            border.enabled = false;
            border.raycastTarget = false;

            return new ButtonWidgets(button, GetButtonLabel(button), border);
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            return button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private static void DestroyIfExists(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
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

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
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

        private readonly struct GarageBuildResult
        {
            public GarageBuildResult(RectTransform root, GaragePageController pageController, AccountSettingsView accountSettingsView, ButtonWidgets lobbyNavigation)
            {
                Root = root;
                PageController = pageController;
                AccountSettingsView = accountSettingsView;
                LobbyNavigation = lobbyNavigation;
            }

            public RectTransform Root { get; }
            public GaragePageController PageController { get; }
            public AccountSettingsView AccountSettingsView { get; }
            public ButtonWidgets LobbyNavigation { get; }
        }

        private readonly struct ButtonWidgets
        {
            public ButtonWidgets(Button button, TMP_Text label, Image border)
            {
                Button = button;
                Label = label;
                Border = border;
            }

            public Button Button { get; }
            public TMP_Text Label { get; }
            public Image Border { get; }
        }

        private readonly struct SelectorWidgets
        {
            public SelectorWidgets(RectTransform root, Button prevButton, Button nextButton, TMP_Text titleText, TMP_Text valueText, TMP_Text hintText)
            {
                Root = root;
                PrevButton = prevButton;
                NextButton = nextButton;
                TitleText = titleText;
                ValueText = valueText;
                HintText = hintText;
            }

            public RectTransform Root { get; }
            public Button PrevButton { get; }
            public Button NextButton { get; }
            public TMP_Text TitleText { get; }
            public TMP_Text ValueText { get; }
            public TMP_Text HintText { get; }
        }

        private readonly struct AccountCardWidgets
        {
            public AccountCardWidgets(RectTransform root, AccountSettingsView view)
            {
                Root = root;
                View = view;
            }

            public RectTransform Root { get; }
            public AccountSettingsView View { get; }
        }

        private readonly struct PreviewWidgets
        {
            public PreviewWidgets(RectTransform card, RawImage rawImage, TMP_Text emptyStateText)
            {
                Card = card;
                RawImage = rawImage;
                EmptyStateText = emptyStateText;
            }

            public RectTransform Card { get; }
            public RawImage RawImage { get; }
            public TMP_Text EmptyStateText { get; }
        }

        private readonly struct ResultWidgets
        {
            public ResultWidgets(
                RectTransform root,
                TMP_Text rosterStatusText,
                TMP_Text validationText,
                TMP_Text statsText,
                Button saveButton,
                TMP_Text saveButtonLabel,
                Image saveButtonImage,
                GameObject toastPanel,
                CanvasGroup toastCanvasGroup,
                TMP_Text toastText)
            {
                Root = root;
                RosterStatusText = rosterStatusText;
                ValidationText = validationText;
                StatsText = statsText;
                SaveButton = saveButton;
                SaveButtonLabel = saveButtonLabel;
                SaveButtonImage = saveButtonImage;
                ToastPanel = toastPanel;
                ToastCanvasGroup = toastCanvasGroup;
                ToastText = toastText;
            }

            public RectTransform Root { get; }
            public TMP_Text RosterStatusText { get; }
            public TMP_Text ValidationText { get; }
            public TMP_Text StatsText { get; }
            public Button SaveButton { get; }
            public TMP_Text SaveButtonLabel { get; }
            public Image SaveButtonImage { get; }
            public GameObject ToastPanel { get; }
            public CanvasGroup ToastCanvasGroup { get; }
            public TMP_Text ToastText { get; }
        }
    }
}
#endif
