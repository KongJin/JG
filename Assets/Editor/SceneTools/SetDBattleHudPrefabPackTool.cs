#if UNITY_EDITOR
using System.IO;
using Features.Unit.Presentation;
using Features.Wave.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class SetDBattleHudPrefabPackTool
    {
        private const string MenuPath = "Tools/Scene/Generate Set D Battle HUD Prefabs";
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string RootFolder = "Assets/Prefabs/Features/BattleHud";
        private const string RootPrefabPath = RootFolder + "/Root/BattleHudCommandRoot.prefab";
        private const string SlotPrefabPath = RootFolder + "/Repeat/BattleUnitSlotCard.prefab";
        private const string WaveTopBarPrefabPath = RootFolder + "/Independent/BattleWaveTopBar.prefab";
        private const string CoreStatusPrefabPath = RootFolder + "/Independent/BattleCoreStatusCard.prefab";
        private const string PlacementFeedbackPrefabPath = RootFolder + "/Independent/BattlePlacementFeedback.prefab";
        private const string StatsPopupPrefabPath = RootFolder + "/Independent/BattleUnitStatsPopup.prefab";
        private const string CoreWarningBannerPrefabPath = RootFolder + "/Independent/BattleCoreWarningBanner.prefab";

        [MenuItem(MenuPath)]
        private static void GeneratePrefabPack()
        {
            EnsureMainStageAndTempScene();
            EnsureFolders();

            var legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            SavePrefab(BuildCommandRoot(legacyFont), RootPrefabPath);
            SavePrefab(BuildSlotCard(legacyFont, true), SlotPrefabPath);
            SavePrefab(BuildWaveTopBar(legacyFont), WaveTopBarPrefabPath);
            SavePrefab(BuildCoreStatusCard(legacyFont), CoreStatusPrefabPath);
            SavePrefab(BuildPlacementFeedback(legacyFont), PlacementFeedbackPrefabPath);
            SavePrefab(BuildUnitStatsPopup(legacyFont), StatsPopupPrefabPath);
            SavePrefab(BuildCoreWarningBanner(legacyFont), CoreWarningBannerPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetDBattleHudPrefabPackTool] Generated Set D prefab pack under Assets/Prefabs/Features/BattleHud.");
        }

        private static void EnsureMainStageAndTempScene()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                StageUtility.GoToMainStage();
            }

            if (!File.Exists(TempScenePath))
            {
                var createdScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(createdScene, TempScenePath, true);
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != TempScenePath)
            {
                EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Features");
            EnsureFolder(RootFolder);
            EnsureFolder(RootFolder + "/Root");
            EnsureFolder(RootFolder + "/Repeat");
            EnsureFolder(RootFolder + "/Independent");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static void SavePrefab(GameObject instanceRoot, string assetPath)
        {
            PrefabUtility.SaveAsPrefabAsset(instanceRoot, assetPath);
            Object.DestroyImmediate(instanceRoot);
        }

        private static GameObject BuildCommandRoot(Font font)
        {
            var root = CreateUiRoot("BattleHudCommandRoot", new Vector2(390f, 156f), new Color(0.04f, 0.07f, 0.11f, 0.96f));
            root.AddComponent<CanvasGroup>();
            root.AddComponent<SummonCommandController>();

            var slotRow = CreateUiChild(root.transform, "SlotRow", new Vector2(352f, 68f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), Color.clear);
            var slotLayout = slotRow.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 12f;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlWidth = false;
            slotLayout.childControlHeight = false;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;

            CreateSlotVisual(slotRow.transform, font, "VANG", "450 E", false);
            CreateSlotVisual(slotRow.transform, font, "어썰트 봇", "250 E", true);
            CreateSlotVisual(slotRow.transform, font, "방어 거너", "320 E", false);

            var energyBar = CreateUiChild(root.transform, "EnergyBar", new Vector2(146f, 44f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Color(0.13f, 0.24f, 0.38f, 0.98f));
            CreateText(energyBar.transform, "EnergyLabel", font, "현재 에너지", 12, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.73f, 0.88f, 1f, 0.95f), new Vector2(130f, 16f), new Vector2(0.5f, 1f), new Vector2(0f, -6f));
            CreateText(energyBar.transform, "EnergyValue", font, "320E", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(130f, 24f), new Vector2(0.5f, 0f), new Vector2(0f, 8f));
            CreateText(energyBar.transform, "EnergyIcon", font, "⚡", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.82f, 0.93f, 1f, 1f), new Vector2(24f, 24f), new Vector2(0f, 0.5f), new Vector2(10f, 0f));

            var feedback = CreateUiChild(root.transform, "PlacementErrorView", new Vector2(112f, 28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Color(0.12f, 0.24f, 0.39f, 0.92f));
            feedback.AddComponent<CanvasGroup>();
            CreateText(feedback.transform, "FeedbackText", font, "배치 가능", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.87f, 0.95f, 1f, 1f), new Vector2(96f, 20f), new Vector2(0.5f, 0.5f), Vector2.zero);

            return root;
        }

        private static GameObject BuildSlotCard(Font font, bool attachBehavior)
        {
            var root = CreateUiRoot("BattleUnitSlotCard", new Vector2(112f, 120f), new Color(0.07f, 0.10f, 0.15f, 0.98f));
            var layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 112f;
            layout.preferredHeight = 120f;

            var icon = CreateUiChild(root.transform, "Icon", new Vector2(32f, 32f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Color(0.18f, 0.54f, 0.94f, 1f));
            var nameText = CreateText(root.transform, "NameText", font, "어썰트 봇", 15, FontStyle.Bold, TextAnchor.LowerCenter, new Color(0.88f, 0.94f, 1f, 1f), new Vector2(92f, 18f), new Vector2(0.5f, 0f), new Vector2(0f, 12f));
            var costText = CreateText(root.transform, "CostText", font, "250 E", 12, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.67f, 0.26f, 1f), new Vector2(72f, 18f), new Vector2(1f, 1f), new Vector2(-10f, -8f));
            var cannotAfford = CreateUiChild(root.transform, "CannotAffordOverlay", new Vector2(112f, 120f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.04f, 0.05f, 0.08f, 0.72f));
            cannotAfford.SetActive(false);
            CreateText(cannotAfford.transform, "Label", font, "NEED\nENERGY", 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.45f, 0.45f, 1f), new Vector2(84f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero);

            if (attachBehavior)
            {
                var slotView = root.AddComponent<UnitSlotView>();
                SetObjectReference(slotView, "_iconImage", icon.GetComponent<Image>());
                SetObjectReference(slotView, "_nameText", nameText);
                SetObjectReference(slotView, "_costText", costText);
                SetObjectReference(slotView, "_cannotAffordOverlay", cannotAfford);
                SetObjectReference(slotView, "_backgroundImage", root.GetComponent<Image>());
                SetObjectReference(slotView, "_layoutElement", layout);
            }

            return root;
        }

        private static GameObject BuildWaveTopBar(Font font)
        {
            var root = CreateUiRoot("BattleWaveTopBar", new Vector2(160f, 92f), new Color(0.07f, 0.11f, 0.16f, 0.94f));
            var view = root.AddComponent<WaveHudView>();

            var waveText = CreateText(root.transform, "WaveText", font, "WAVE 09 / 15", 22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(140f, 26f), new Vector2(0f, 1f), new Vector2(12f, -12f));
            var countdownText = CreateText(root.transform, "CountdownText", font, "04:28", 24, FontStyle.Bold, TextAnchor.LowerLeft, new Color(1f, 0.58f, 0.26f, 1f), new Vector2(90f, 24f), new Vector2(0f, 0f), new Vector2(12f, 12f));
            var statusText = CreateText(root.transform, "StatusText", font, "웨이브 정보", 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.56f, 0.78f, 1f, 1f), new Vector2(90f, 16f), new Vector2(0f, 1f), new Vector2(12f, -34f));
            var hintText = CreateText(root.transform, "FirstWaveDeckHintText", font, "슬롯 선택 후 전장을 탭해 배치하세요.", 12, FontStyle.Normal, TextAnchor.LowerLeft, new Color(0.74f, 0.87f, 1f, 0.95f), new Vector2(144f, 16f), new Vector2(0f, 0f), new Vector2(12f, 8f));
            hintText.gameObject.SetActive(false);

            SetObjectReference(view, "waveText", waveText);
            SetObjectReference(view, "countdownText", countdownText);
            SetObjectReference(view, "statusText", statusText);
            SetObjectReference(view, "backgroundImage", root.GetComponent<Image>());
            SetObjectReference(view, "firstWaveDeckHintText", hintText);
            return root;
        }

        private static GameObject BuildCoreStatusCard(Font font)
        {
            var root = CreateUiRoot("BattleCoreStatusCard", new Vector2(150f, 92f), new Color(0.07f, 0.11f, 0.16f, 0.94f));
            var view = root.AddComponent<CoreHealthHudView>();

            var title = CreateText(root.transform, "HpText", font, "코어 무결성\n72%", 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.97f, 0.97f, 1f, 1f), new Vector2(124f, 34f), new Vector2(0f, 1f), new Vector2(12f, -12f));

            var sliderRoot = CreateUiChild(root.transform, "HealthSlider", new Vector2(122f, 18f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), Color.clear);
            var slider = sliderRoot.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 72f;

            var background = CreateUiChild(sliderRoot.transform, "Background", new Vector2(122f, 10f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.24f, 0.11f, 0.12f, 0.95f));
            var fillArea = CreateUiChild(sliderRoot.transform, "Fill Area", new Vector2(116f, 10f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Color.clear);
            var fill = CreateUiChild(fillArea.transform, "Fill", new Vector2(84f, 10f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Color(1f, 0.35f, 0.32f, 1f));

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();

            SetObjectReference(view, "healthSlider", slider);
            SetObjectReference(view, "fillImage", fill.GetComponent<Image>());
            SetObjectReference(view, "hpText", title);
            SetObjectReference(view, "panelImage", root.GetComponent<Image>());
            SetObjectReference(view, "sliderBackgroundImage", background.GetComponent<Image>());
            return root;
        }

        private static GameObject BuildPlacementFeedback(Font font)
        {
            var root = CreateUiRoot("BattlePlacementFeedback", new Vector2(112f, 28f), new Color(0.12f, 0.24f, 0.39f, 0.92f));
            var canvasGroup = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<PlacementErrorView>();

            var text = CreateText(root.transform, "ErrorText", font, "배치 가능", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.87f, 0.95f, 1f, 1f), new Vector2(96f, 20f), new Vector2(0.5f, 0.5f), Vector2.zero);

            SetObjectReference(view, "_errorText", text);
            SetObjectReference(view, "_canvasGroup", canvasGroup);
            SetObjectReference(view, "_backgroundImage", root.GetComponent<Image>());
            return root;
        }

        private static GameObject BuildUnitStatsPopup(Font font)
        {
            var root = CreateUiRoot("BattleUnitStatsPopup", new Vector2(232f, 116f), new Color(0.07f, 0.10f, 0.15f, 0.97f));
            CreateText(root.transform, "Title", font, "어썰트 봇", 22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(120f, 24f), new Vector2(0f, 1f), new Vector2(14f, -10f));
            CreateText(root.transform, "Tag", font, "[ASSAULT]", 12, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.55f, 0.79f, 1f, 1f), new Vector2(80f, 14f), new Vector2(0f, 1f), new Vector2(14f, -34f));

            var costChip = CreateUiChild(root.transform, "CostChip", new Vector2(42f, 18f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Color(0.31f, 0.12f, 0.05f, 0.98f));
            CreateText(costChip.transform, "Value", font, "250 E", 10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.62f, 0.22f, 1f), new Vector2(36f, 12f), new Vector2(0.5f, 0.5f), Vector2.zero);

            CreateSeparator(root.transform, "RuleA", new Vector2(204f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f));
            CreateText(root.transform, "StatLeft", font, "전력 생산\n+ 강력", 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.80f, 0.89f, 1f, 1f), new Vector2(88f, 38f), new Vector2(0f, 1f), new Vector2(18f, -58f));
            CreateText(root.transform, "StatRight", font, "전선 배치\n+ 자동 기동", 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.80f, 0.89f, 1f, 1f), new Vector2(92f, 38f), new Vector2(1f, 1f), new Vector2(-96f, -58f));
            CreateSeparator(root.transform, "RuleB", new Vector2(204f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 26f));
            CreateText(root.transform, "Footer", font, "현재 배치 가능", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.72f, 0.88f, 1f, 1f), new Vector2(180f, 18f), new Vector2(0.5f, 0f), new Vector2(0f, 12f));
            return root;
        }

        private static GameObject BuildCoreWarningBanner(Font font)
        {
            var root = CreateUiRoot("BattleCoreWarningBanner", new Vector2(286f, 24f), new Color(0.27f, 0.07f, 0.08f, 0.96f));
            CreateText(root.transform, "WarningText", font, "경고: 코어 무결성 임계치 도달", 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.55f, 1f), new Vector2(250f, 18f), new Vector2(0.5f, 0.5f), Vector2.zero);
            return root;
        }

        private static GameObject CreateUiRoot(string name, Vector2 size, Color backgroundColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = false;
            return go;
        }

        private static GameObject CreateUiChild(Transform parent, string name, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Color backgroundColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = false;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Font font, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 size, Vector2 anchor, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void CreateSeparator(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 anchoredPosition)
        {
            CreateUiChild(parent, name, size, anchor, anchor, anchoredPosition, new Color(0.24f, 0.31f, 0.40f, 0.9f));
        }

        private static void CreateSlotVisual(Transform parent, Font font, string unitName, string cost, bool selected)
        {
            var slot = BuildSlotCard(font, false);
            slot.name = unitName.Replace(" ", string.Empty) + "Preview";
            slot.transform.SetParent(parent, false);
            slot.GetComponent<Image>().color = selected
                ? new Color(0.18f, 0.25f, 0.41f, 1f)
                : new Color(0.07f, 0.10f, 0.15f, 0.98f);

            var nameText = slot.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = unitName;
                nameText.color = selected ? new Color(1f, 0.56f, 0.16f, 1f) : new Color(0.72f, 0.80f, 0.92f, 1f);
            }

            var costText = slot.transform.Find("CostText")?.GetComponent<Text>();
            if (costText != null)
            {
                costText.text = cost;
            }
        }

        private static void SetObjectReference(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new UnityException($"Missing serialized field '{fieldName}' on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
