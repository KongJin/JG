#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class GarageNovaPartsPanelInstallTool
    {
        private const string MenuPath = "Tools/Scene/Install Garage Nova Parts Panel";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";
        private const string ReportPath = "artifacts/unity/garage-nova-parts-panel-install-report.md";
        private const float PanelHeight = 430f;
        private const int RowCount = 8;

        [MenuItem(MenuPath)]
        private static void InstallFromMenu()
        {
            InstallIntoActiveScene();
        }

        public static void InstallIntoActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before installing Garage Nova Parts panel.");

            var scene = SceneManager.GetActiveScene();
            var garageRoot = FindSceneObjectByPath(scene, "/LobbyCanvas/GaragePageRoot")
                ?? FindSceneObjectByPath(scene, "/GaragePageRoot");
            if (garageRoot == null)
                throw new InvalidOperationException("GaragePageRoot was not found in the active scene.");

            var controller = garageRoot.GetComponent<GaragePageController>();
            var setup = FindSceneComponent<GarageSetup>(scene);
            var contentRoot = RequireRect(garageRoot.transform, "MobileContentRoot");
            var bodyHost = RequireRect(contentRoot, "MobileBodyHost");
            var editor = RequireRect(bodyHost, "GarageUnitEditorView");
            var previewCard = RequireRect(bodyHost, "PreviewCard");

            Undo.RegisterFullObjectHierarchyUndo(garageRoot, "Install Garage Nova Parts Panel");
            if (setup != null)
                Undo.RecordObject(setup, "Wire Nova Part Catalogs");

            var existing = bodyHost.Find("GarageNovaPartsPanelView");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var panel = BuildPanel(bodyHost);
            panel.transform.SetSiblingIndex(Mathf.Clamp(editor.GetSiblingIndex() + 1, 0, bodyHost.childCount - 1));
            previewCard.SetSiblingIndex(Mathf.Max(previewCard.GetSiblingIndex(), panel.transform.GetSiblingIndex() + 1));

            if (controller != null)
                SetRef(controller, "_novaPartsPanelView", panel);

            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            if (setup != null && visualCatalog != null)
                SetRef(setup, "_novaPartVisualCatalog", visualCatalog);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            if (setup != null && alignmentCatalog != null)
                SetRef(setup, "_novaPartAlignmentCatalog", alignmentCatalog);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            WriteReport(garageRoot, panel, visualCatalog != null, alignmentCatalog != null);
            Debug.Log("[GarageNovaPartsPanelInstallTool] Garage Nova Parts panel installed.");
        }

        private static GarageNovaPartsPanelView BuildPanel(RectTransform parent)
        {
            var root = CreatePanel(parent, "GarageNovaPartsPanelView", Vector2.zero, Vector2.one, new Color(0.06f, 0.09f, 0.14f, 1f));
            ConfigureLayoutElement(root, PanelHeight);
            var view = root.gameObject.AddComponent<GarageNovaPartsPanelView>();

            var title = CreateText(root, "TitleText", "Nova Parts", 18f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -8f), new Vector2(-18f, 24f));
            var count = CreateText(root, "CountText", "0 parts", 12f, TextAlignmentOptions.Right, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-12f, 20f));
            var search = CreateInput(root, "SearchInput", "search id / name / source", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -42f), new Vector2(-24f, 34f));

            var frame = CreateButton(root, "FrameFilterButton", "Frame", new Vector2(0f, 1f), new Vector2(0.333f, 1f), new Vector2(12f, -84f), new Vector2(-8f, 30f));
            var fire = CreateButton(root, "FirepowerFilterButton", "Fire", new Vector2(0.333f, 1f), new Vector2(0.666f, 1f), new Vector2(4f, -84f), new Vector2(-8f, 30f));
            var mob = CreateButton(root, "MobilityFilterButton", "Mob", new Vector2(0.666f, 1f), new Vector2(1f, 1f), new Vector2(4f, -84f), new Vector2(-12f, 30f));

            var rows = new GarageNovaPartsPanelRowView[RowCount];
            for (int i = 0; i < rows.Length; i++)
            {
                float top = -122f - (i * 29f);
                rows[i] = BuildRow(root, i, top);
            }

            var selectedName = CreateText(root, "SelectedNameText", "No selected part", 15f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 76f), new Vector2(-116f, 24f));
            var selectedDetail = CreateText(root, "SelectedDetailText", "Search and select a Nova part.", 11f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 20f), new Vector2(-116f, 56f));
            var apply = CreateButton(root, "ApplyButton", "Apply", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-100f, 20f), new Vector2(88f, 56f));

            SetRef(view, "_titleText", title);
            SetRef(view, "_countText", count);
            SetRef(view, "_searchInput", search);
            SetRef(view, "_frameFilterButton", frame);
            SetRef(view, "_frameFilterLabel", frame.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_firepowerFilterButton", fire);
            SetRef(view, "_firepowerFilterLabel", fire.GetComponentInChildren<TMP_Text>());
            SetRef(view, "_mobilityFilterButton", mob);
            SetRef(view, "_mobilityFilterLabel", mob.GetComponentInChildren<TMP_Text>());
            SetRefArray(view, "_rowViews", rows);
            SetRef(view, "_selectedNameText", selectedName);
            SetRef(view, "_selectedDetailText", selectedDetail);
            SetRef(view, "_applyButton", apply);
            SetRef(view, "_applyButtonLabel", apply.GetComponentInChildren<TMP_Text>());
            return view;
        }

        private static GarageNovaPartsPanelRowView BuildRow(RectTransform parent, int index, float top)
        {
            var root = CreatePanel(parent, "NovaPartRow" + (index + 1), new Vector2(0f, 1f), new Vector2(1f, 1f), new Color(0.10f, 0.13f, 0.20f, 1f));
            root.anchoredPosition = new Vector2(12f, top);
            root.sizeDelta = new Vector2(-24f, 25f);
            var button = root.gameObject.AddComponent<Button>();
            var background = root.GetComponent<Image>();
            SetButtonGraphic(button, background);
            var name = CreateText(root, "NameText", "Part", 12f, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(8f, 0f), new Vector2(-4f, 0f));
            var detail = CreateText(root, "DetailText", "Stats", 10f, TextAlignmentOptions.Left, new Vector2(0.52f, 0f), new Vector2(0.86f, 1f), Vector2.zero, new Vector2(-4f, 0f));
            var badge = CreateText(root, "BadgeText", "slot", 10f, TextAlignmentOptions.Right, new Vector2(0.86f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-8f, 0f));
            var view = root.gameObject.AddComponent<GarageNovaPartsPanelRowView>();
            SetRef(view, "_button", button);
            SetRef(view, "_background", background);
            SetRef(view, "_nameText", name);
            SetRef(view, "_detailText", detail);
            SetRef(view, "_badgeText", badge);
            return view;
        }

        private static TMP_InputField CreateInput(RectTransform parent, string name, string placeholderText, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, new Color(0.10f, 0.13f, 0.20f, 1f));
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = sizeDelta;
            var input = root.gameObject.AddComponent<TMP_InputField>();
            var viewport = CreateRect(root, "TextViewport", Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-16f, -6f));
            var text = CreateText(viewport, "Text", string.Empty, 13f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var placeholder = CreateText(viewport, "Placeholder", placeholderText, 13f, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(0.65f, 0.70f, 0.80f, 0.55f);
            SetRef(input, "m_TextViewport", viewport);
            SetRef(input, "m_TextComponent", text);
            SetRef(input, "m_Placeholder", placeholder);
            SetButtonGraphic(input, root.GetComponent<Image>());
            return input;
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, new Color(0.14f, 0.20f, 0.30f, 1f));
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = sizeDelta;
            var button = root.gameObject.AddComponent<Button>();
            SetButtonGraphic(button, root.GetComponent<Image>());
            var label = CreateText(root, "Label", text, 12f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.color = Color.white;
            return button;
        }

        private static TMP_Text CreateText(RectTransform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.88f, 0.91f, 0.96f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static void ConfigureLayoutElement(RectTransform rect, float preferredHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, preferredHeight);

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;
        }

        private static RectTransform RequireRect(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child is RectTransform rect)
                return rect;

            throw new InvalidOperationException(PathOf(parent) + "/" + childName + " was not found.");
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static GameObject FindSceneObjectByPath(Scene scene, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path[0] != '/')
                return null;

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == parts[0]);
            if (root == null)
                return null;

            var current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                    return null;
            }

            return current.gameObject;
        }

        private static string PathOf(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return "/" + path;
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

        private static void SetRef(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " was not found.");

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(UnityEngine.Object target, string fieldName, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null)
                throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " was not found.");

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteReport(
            GameObject garageRoot,
            GarageNovaPartsPanelView panel,
            bool visualCatalogWired,
            bool alignmentCatalogWired)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,
                "# Garage Nova Parts Panel Install Report\n\n" +
                $"- scene: `{SceneManager.GetActiveScene().path}`\n" +
                $"- garage root: `{PathOf(garageRoot.transform)}`\n" +
                $"- panel: `{PathOf(panel.transform)}`\n" +
                $"- row count: {RowCount}\n" +
                $"- visual catalog wired: {visualCatalogWired.ToString().ToLowerInvariant()}\n" +
                $"- alignment catalog wired: {alignmentCatalogWired.ToString().ToLowerInvariant()}\n");
        }
    }
}
#endif
