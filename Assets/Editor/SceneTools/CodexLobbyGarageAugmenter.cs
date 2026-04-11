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
            var lobbyBootstrap = UnityEngine.Object.FindFirstObjectByType<LobbyBootstrap>();

            if (canvas == null || lobbyView == null || lobbyBootstrap == null)
                throw new InvalidOperationException("Canvas, LobbyView, and LobbyBootstrap must exist before Garage augmentation.");

            Augment(canvas, lobbyView, lobbyBootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[CodexLobbyGarageAugmenter] Garage page applied to CodexLobbyScene.");
        }

        internal static void Augment(Canvas canvas, LobbyView lobbyView, LobbyBootstrap lobbyBootstrap)
        {
            var catalog = CodexLobbyGarageDataBuilder.EnsureModuleCatalog();

            DestroyIfExists("TopTabs");
            DestroyIfExists("GaragePageRoot");
            DestroyIfExists("UnitBootstrap");
            DestroyIfExists("GarageNetworkAdapter");
            DestroyIfExists("GarageBootstrap");

            CreateTopTabs(canvas.transform, out var lobbyTabButton, out var garageTabButton);
            var garagePanelView = BuildGaragePage(canvas.transform);
            garagePanelView.gameObject.SetActive(false);

            var unitBootstrap = CreateGameObject("UnitBootstrap").AddComponent<UnitBootstrap>();
            CodexLobbyGarageDataBuilder.SetObject(unitBootstrap, "_moduleCatalog", catalog);

            var garageNetwork = CreateGameObject("GarageNetworkAdapter").AddComponent<GarageNetworkAdapter>();

            var garageBootstrap = CreateGameObject("GarageBootstrap").AddComponent<GarageBootstrap>();
            CodexLobbyGarageDataBuilder.SetObject(garageBootstrap, "_networkAdapter", garageNetwork);
            CodexLobbyGarageDataBuilder.SetObject(garageBootstrap, "_panelView", garagePanelView);

            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyPageRoot", canvas.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garagePageRoot", garagePanelView.gameObject);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_lobbyTabButton", lobbyTabButton);
            CodexLobbyGarageDataBuilder.SetObject(lobbyView, "_garageTabButton", garageTabButton);

            CodexLobbyGarageDataBuilder.SetObject(lobbyBootstrap, "_unitBootstrap", unitBootstrap);
            CodexLobbyGarageDataBuilder.SetObject(lobbyBootstrap, "_garageBootstrap", garageBootstrap);
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

        private static GaragePanelView BuildGaragePage(Transform parent)
        {
            var panel = CreatePanel("GaragePageRoot", parent, new Color32(18, 25, 40, 247));
            Stretch(panel.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);

            var title = CreateText("GarageTitle", panel.transform, "GARAGE", 28, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(title.rectTransform, new Vector2(0.03f, 0.91f), new Vector2(0.35f, 0.98f), Vector2.zero, Vector2.zero);

            var subtitle = CreateText("GarageSubtitle", panel.transform, "Top slots are saved roster entries. The editor below writes valid loadouts immediately.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(subtitle.rectTransform, new Vector2(0.03f, 0.865f), new Vector2(0.77f, 0.93f), Vector2.zero, Vector2.zero);

            var slotStrip = CreatePanel("SlotStrip", panel.transform, new Color32(24, 35, 55, 255));
            Stretch(slotStrip.rectTransform, new Vector2(0.03f, 0.73f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            var slotLayout = slotStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.padding = new RectOffset(18, 18, 14, 14);
            slotLayout.spacing = 12f;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlHeight = true;
            slotLayout.childControlWidth = true;
            slotLayout.childForceExpandHeight = true;
            slotLayout.childForceExpandWidth = true;

            var slotViews = new GarageSlotItemView[Features.Garage.Domain.GarageRoster.MaxSlots];
            for (int i = 0; i < slotViews.Length; i++)
                slotViews[i] = BuildGarageSlotItem(slotStrip.transform, i);

            var editorPanel = CreatePanel("GarageEditorPanel", panel.transform, new Color32(24, 31, 50, 242));
            Stretch(editorPanel.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.63f, 0.68f), Vector2.zero, Vector2.zero);
            var editorLayout = editorPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            editorLayout.padding = new RectOffset(20, 20, 20, 20);
            editorLayout.spacing = 16f;
            editorLayout.childAlignment = TextAnchor.UpperLeft;
            editorLayout.childControlHeight = true;
            editorLayout.childControlWidth = true;
            editorLayout.childForceExpandHeight = false;
            editorLayout.childForceExpandWidth = true;

            var frameSelector = BuildSelectorCard(editorPanel.transform, "FRAME", new Color32(77, 127, 255, 255));
            var firepowerSelector = BuildSelectorCard(editorPanel.transform, "FIREPOWER", new Color32(255, 134, 82, 255));
            var mobilitySelector = BuildSelectorCard(editorPanel.transform, "MOBILITY", new Color32(31, 183, 150, 255));

            var summaryPanel = CreatePanel("GarageSummaryPanel", panel.transform, new Color32(20, 28, 45, 242));
            Stretch(summaryPanel.rectTransform, new Vector2(0.66f, 0.08f), new Vector2(0.97f, 0.68f), Vector2.zero, Vector2.zero);

            var selectionTitle = CreateText("SelectionTitle", summaryPanel.transform, "Slot 1 Empty", 24, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(244, 247, 255, 255));
            Stretch(selectionTitle.rectTransform, new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            var selectionSubtitle = CreateText("SelectionSubtitle", summaryPanel.transform, "Build a loadout. Valid combinations save immediately.", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(143, 164, 201, 255));
            Stretch(selectionSubtitle.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);

            var rosterStatus = CreateText("RosterStatus", summaryPanel.transform, "Roster incomplete: 0/6 saved units. Add 3 more for Ready.", 16, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color32(232, 238, 252, 255));
            Stretch(rosterStatus.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.7f), Vector2.zero, Vector2.zero);

            var validationText = CreateText("ValidationText", summaryPanel.transform, "Select frame, firepower, and mobility to save this slot.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(115, 222, 196, 255));
            Stretch(validationText.rectTransform, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);

            var statsText = CreateText("StatsText", summaryPanel.transform, "Pick all three parts to see composed HP, damage, and summon cost.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color32(214, 223, 241, 255));
            Stretch(statsText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.44f), Vector2.zero, Vector2.zero);

            var clearButton = CreateButton("ClearButton", summaryPanel.transform, "Clear Slot", new Color32(193, 80, 86, 255), Color.white);
            Stretch(clearButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.48f, 0.14f), Vector2.zero, Vector2.zero);

            var panelView = panel.gameObject.AddComponent<GaragePanelView>();
            CodexLobbyGarageDataBuilder.SetObjectArray(panelView, "_slotViews", slotViews);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_framePrevButton", frameSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_frameNextButton", frameSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_frameValueText", frameSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_frameHintText", frameSelector.HintText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_firepowerPrevButton", firepowerSelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_firepowerNextButton", firepowerSelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_firepowerValueText", firepowerSelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_firepowerHintText", firepowerSelector.HintText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_mobilityPrevButton", mobilitySelector.PrevButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_mobilityNextButton", mobilitySelector.NextButton);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_mobilityValueText", mobilitySelector.ValueText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_mobilityHintText", mobilitySelector.HintText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_selectionTitleText", selectionTitle);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_selectionSubtitleText", selectionSubtitle);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_rosterStatusText", rosterStatus);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_validationText", validationText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_statsText", statsText);
            CodexLobbyGarageDataBuilder.SetObject(panelView, "_clearButton", clearButton);
            return panelView;
        }

        private static GarageSlotItemView BuildGarageSlotItem(Transform parent, int index)
        {
            var slot = CreateGameObject($"GarageSlot{index + 1}", parent, typeof(RectTransform), typeof(Image), typeof(Button));
            slot.AddComponent<LayoutElement>().preferredWidth = 150f;

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

            return new SelectorWidgets(prevButton, nextButton, valueText, hintText);
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
            text.enableWordWrapping = true;
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
            public SelectorWidgets(Button prevButton, Button nextButton, TMP_Text valueText, TMP_Text hintText)
            {
                PrevButton = prevButton;
                NextButton = nextButton;
                ValueText = valueText;
                HintText = hintText;
            }

            public Button PrevButton { get; }
            public Button NextButton { get; }
            public TMP_Text ValueText { get; }
            public TMP_Text HintText { get; }
        }
    }
}
#endif
