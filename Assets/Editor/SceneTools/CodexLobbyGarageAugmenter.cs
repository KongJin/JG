#if UNITY_EDITOR
using System;
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
            Debug.Log("[CodexLobbyGarageAugmenter] Garage page applied to CodexLobbyScene.");
        }

        internal static void Augment(Canvas canvas, LobbyView lobbyView, LobbySetup lobbySetup)
        {
            var catalog = CodexLobbyGarageDataBuilder.EnsureModuleCatalog();
            var lobbyPageRoot = EnsureLobbyPageRoot(canvas.transform);

            DestroyIfExists("TopTabs");
            DestroyIfExists("GaragePageRoot");
            DestroyIfExists("UnitSetup");
            DestroyIfExists("GarageNetworkAdapter");
            DestroyIfExists("GarageSetup");

            CreateTopTabs(canvas.transform, out var lobbyTabButton, out var garageTabButton);
            var garagePageController = BuildGaragePage(canvas.transform);
            garagePageController.gameObject.SetActive(false);

            var unitSetup = CreateGameObject("UnitSetup").AddComponent<UnitSetup>();
            CodexLobbyGarageDataBuilder.SetObject(unitSetup, "_moduleCatalog", catalog);

            var garageNetwork = CreateGameObject("GarageNetworkAdapter").AddComponent<GarageNetworkAdapter>();

            var garageSetup = CreateGameObject("GarageSetup").AddComponent<GarageSetup>();
            CodexLobbyGarageDataBuilder.SetObject(garageSetup, "_networkAdapter", garageNetwork);
            CodexLobbyGarageDataBuilder.SetObject(garageSetup, "_pageController", garagePageController);

            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyPageRoot", lobbyPageRoot.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garagePageRoot", garagePageController.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyTabButton", lobbyTabButton);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garageTabButton", garageTabButton);

            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_unitSetup", unitSetup);
            CodexLobbyGarageDataBuilder.SetObject(lobbySetup, "_garageSetup", garageSetup);
        }

        private static RectTransform EnsureLobbyPageRoot(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("LobbyPageRoot") as RectTransform;
            if (existing == null)
            {
                var root = CreateGameObject("LobbyPageRoot", canvasTransform, typeof(RectTransform));
                existing = root.GetComponent<RectTransform>();
            }

            Stretch(existing, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var roomListPanel = UnityEngine.Object.FindFirstObjectByType<RoomListView>();
            if (roomListPanel != null)
                roomListPanel.transform.SetParent(existing, false);

            var roomDetailPanel = UnityEngine.Object.FindFirstObjectByType<RoomDetailView>();
            if (roomDetailPanel != null)
                roomDetailPanel.transform.SetParent(existing, false);

            return existing;
        }

        private static void CreateTopTabs(Transform parent, out Button lobbyTabButton, out Button garageTabButton)
        {
            var bar = CreateGameObject("TopTabs", parent, typeof(RectTransform));
            Stretch(bar.GetComponent<RectTransform>(), new Vector2(0.71f, 0.875f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            lobbyTabButton = CreateButton("LobbyTabButton", bar.transform, "Lobby", new Color32(73, 118, 255, 255), Color.white);
            garageTabButton = CreateButton("GarageTabButton", bar.transform, "Garage", new Color32(22, 164, 143, 255), Color.white);
            lobbyTabButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 148f;
            garageTabButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 148f;
        }

        private static GaragePageController BuildGaragePage(Transform parent)
        {
            var panel = CreatePanel("GaragePageRoot", parent, new Color32(18, 25, 40, 247));
            Stretch(panel.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);

            var title = CreateText("GarageTitle", panel.transform, "GARAGE", 28, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(title.rectTransform, new Vector2(0.03f, 0.91f), new Vector2(0.35f, 0.98f), Vector2.zero, Vector2.zero);

            var subtitle = CreateText("GarageSubtitle", panel.transform, "Saved roster stays on the left. Draft editing happens in the center. The right panel reflects current results.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(subtitle.rectTransform, new Vector2(0.03f, 0.865f), new Vector2(0.87f, 0.93f), Vector2.zero, Vector2.zero);

            var rosterPane = CreatePanel("RosterListPane", panel.transform, new Color32(23, 33, 52, 252));
            Stretch(rosterPane.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.3f, 0.84f), Vector2.zero, Vector2.zero);
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

            var editorPane = CreatePanel("UnitEditorPane", panel.transform, new Color32(24, 31, 50, 242));
            Stretch(editorPane.rectTransform, new Vector2(0.33f, 0.08f), new Vector2(0.68f, 0.84f), Vector2.zero, Vector2.zero);
            var editorLayout = editorPane.gameObject.AddComponent<VerticalLayoutGroup>();
            editorLayout.padding = new RectOffset(20, 20, 20, 20);
            editorLayout.spacing = 16f;
            editorLayout.childAlignment = TextAnchor.UpperLeft;
            editorLayout.childControlHeight = true;
            editorLayout.childControlWidth = true;
            editorLayout.childForceExpandHeight = false;
            editorLayout.childForceExpandWidth = true;

            var selectionTitle = CreateText("SelectionTitle", editorPane.transform, "Slot 1 Empty", 24, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            selectionTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var selectionSubtitle = CreateText("SelectionSubtitle", editorPane.transform, "Build a loadout. Valid combinations save immediately.", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            selectionSubtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            var frameSelector = BuildSelectorCard(editorPane.transform, "FRAME", new Color32(77, 127, 255, 255));
            var firepowerSelector = BuildSelectorCard(editorPane.transform, "FIREPOWER", new Color32(255, 134, 82, 255));
            var mobilitySelector = BuildSelectorCard(editorPane.transform, "MOBILITY", new Color32(31, 183, 150, 255));

            var clearButton = CreateButton("ClearButton", editorPane.transform, "Clear Slot", new Color32(193, 80, 86, 255), Color.white);
            clearButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            var resultPane = CreatePanel("ResultPane", panel.transform, new Color32(20, 28, 45, 242));
            Stretch(resultPane.rectTransform, new Vector2(0.71f, 0.08f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero);

            var resultHeader = CreateText("ResultHeader", resultPane.transform, "RESULT", 22, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(resultHeader.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            var rosterStatus = CreateText("RosterStatus", resultPane.transform, "Roster incomplete: 0/6 saved units. Add 3 more for Ready.", 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(232, 238, 252, 255));
            Stretch(rosterStatus.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);

            var validationText = CreateText("ValidationText", resultPane.transform, "Select frame, firepower, and mobility to save this slot.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(115, 222, 196, 255));
            Stretch(validationText.rectTransform, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);

            var statsText = CreateText("StatsText", resultPane.transform, "Pick all three parts to see composed HP, damage, and summon cost.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(214, 223, 241, 255));
            Stretch(statsText.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.38f), Vector2.zero, Vector2.zero);

            var rosterListView = rosterPane.gameObject.AddComponent<GarageRosterListView>();
            CodexLobbyGarageDataBuilder.SetObjectArray(rosterListView, "_slotViews", slotViews);

            var frameSelectorView = frameSelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_prevButton", frameSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_nextButton", frameSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_valueText", frameSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(frameSelectorView, "_hintText", frameSelector.HintText);

            var firepowerSelectorView = firepowerSelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_prevButton", firepowerSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_nextButton", firepowerSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_valueText", firepowerSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(firepowerSelectorView, "_hintText", firepowerSelector.HintText);

            var mobilitySelectorView = mobilitySelector.Root.gameObject.AddComponent<GaragePartSelectorView>();
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_prevButton", mobilitySelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_nextButton", mobilitySelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_valueText", mobilitySelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(mobilitySelectorView, "_hintText", mobilitySelector.HintText);

            var unitEditorView = editorPane.gameObject.AddComponent<GarageUnitEditorView>();
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_selectionTitleText", selectionTitle);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_selectionSubtitleText", selectionSubtitle);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_frameSelectorView", frameSelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_firepowerSelectorView", firepowerSelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_mobilitySelectorView", mobilitySelectorView);
            CodexLobbyGarageDataBuilder.SetObject(unitEditorView, "_clearButton", clearButton);

            var resultPanelView = resultPane.gameObject.AddComponent<GarageResultPanelView>();
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_rosterStatusText", rosterStatus);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_validationText", validationText);
            CodexLobbyGarageDataBuilder.SetObject(resultPanelView, "_statsText", statsText);

            var pageController = panel.gameObject.AddComponent<GaragePageController>();
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_rosterListView", rosterListView);
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_unitEditorView", unitEditorView);
            CodexLobbyGarageDataBuilder.SetObject(pageController, "_resultPanelView", resultPanelView);
            return pageController;
        }

        private static GarageSlotItemView BuildGarageSlotItem(Transform parent, int index)
        {
            var slot = CreateGameObject($"GarageSlot{index + 1}", parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var layoutElement = slot.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 88f;
            layoutElement.minHeight = 88f;

            var image = slot.GetComponent<Image>();
            ApplyDefaultSprite(image);
            image.type = Image.Type.Sliced;
            image.color = new Color32(25, 32, 49, 255);

            var button = slot.GetComponent<Button>();
            button.targetGraphic = image;

            var slotNumber = CreateText("SlotNumber", slot.transform, $"SLOT {index + 1}", 12, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(slotNumber.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            var title = CreateText("Title", slot.transform, "Empty Slot", 17, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(title.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);

            var summary = CreateText("Summary", slot.transform, "Select frame and modules", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(170, 188, 224, 255));
            Stretch(summary.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.38f), Vector2.zero, Vector2.zero);

            var slotView = slot.AddComponent<GarageSlotItemView>();
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_button", button);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_background", image);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_slotNumberText", slotNumber);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_titleText", title);
            CodexLobbyGarageDataBuilder.SetObject(slotView, "_summaryText", summary);
            return slotView;
        }

        private static SelectorWidgets BuildSelectorCard(Transform parent, string title, Color accent)
        {
            var card = CreatePanel(title + "Card", parent, new Color32(31, 39, 61, 255));
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 126f;

            var titleText = CreateText(title + "Title", card.transform, title, 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, accent);
            Stretch(titleText.rectTransform, new Vector2(0.05f, 0.76f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero);

            var valuePanel = CreatePanel(title + "ValuePanel", card.transform, new Color32(18, 24, 39, 255));
            Stretch(valuePanel.rectTransform, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.7f), Vector2.zero, Vector2.zero);

            var prevButton = CreateButton(title + "PrevButton", valuePanel.transform, "<", accent, Color.white);
            Stretch(prevButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.14f), new Vector2(0.15f, 0.86f), Vector2.zero, Vector2.zero);

            var nextButton = CreateButton(title + "NextButton", valuePanel.transform, ">", accent, Color.white);
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.85f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);

            var valueText = CreateText(title + "ValueText", valuePanel.transform, "< Select >", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color32(244, 247, 255, 255));
            Stretch(valueText.rectTransform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);

            var hintText = CreateText(title + "HintText", card.transform, "Choose a part to update this slot.", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(170, 188, 224, 255));
            Stretch(hintText.rectTransform, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero);

            return new SelectorWidgets(card.rectTransform, prevButton, nextButton, valueText, hintText);
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

        private readonly struct SelectorWidgets
        {
            public SelectorWidgets(RectTransform root, Button prevButton, Button nextButton, TMP_Text valueText, TMP_Text hintText)
            {
                Root = root;
                PrevButton = prevButton;
                NextButton = nextButton;
                ValueText = valueText;
                HintText = hintText;
            }

            public RectTransform Root { get; }
            public Button PrevButton { get; }
            public Button NextButton { get; }
            public TMP_Text ValueText { get; }
            public TMP_Text HintText { get; }
        }
    }
}
#endif
