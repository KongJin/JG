#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class GarageMobileScrollMigrationTool
    {
        private const string MenuPath = "Tools/Scene/Migrate Garage Mobile Scroll Layout";
        private const string ReportPath = "artifacts/unity/garage-mobile-scroll-migration-report.md";
        private const float ViewportLeftInset = 16f;
        private const float ViewportBottomInset = 78f;
        private const float ViewportWidthInset = -32f;
        private const float ViewportHeightInset = -218f;
        private const float SlotHostHeight = 208f;
        private const float FocusBarHeight = 52f;
        private const float EditorHeight = 188f;
        private const float PreviewCardHeight = 220f;
        private const float ResultPaneHeight = 118f;
        private const float SaveDockBottomOffset = 88f;
        private const float SaveDockHeight = 72f;

        [MenuItem(MenuPath)]
        private static void MigrateFromMenu()
        {
            MigrateActiveScene();
        }

        public static void MigrateActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before migrating Garage mobile scroll layout.");

            var scene = SceneManager.GetActiveScene();
            var garageRoot = FindSceneObjectByPath(scene, "/LobbyCanvas/GaragePageRoot")
                ?? FindSceneObjectByPath(scene, "/GaragePageRoot");
            if (garageRoot == null)
                throw new InvalidOperationException("GaragePageRoot was not found in the active scene.");

            Undo.RegisterFullObjectHierarchyUndo(garageRoot, "Migrate Garage Mobile Scroll Layout");

            var contentRoot = RequireRect(garageRoot.transform, "MobileContentRoot");
            var bodyHost = RequireRect(contentRoot, "MobileBodyHost");
            var slotHost = FindRect(contentRoot, "MobileSlotHost") ?? RequireRect(bodyHost, "MobileSlotHost");
            var tabBar = FindRect(garageRoot.transform, "MobileTabBar") ?? RequireRect(bodyHost, "MobileTabBar");
            var editor = RequireRect(bodyHost, "GarageUnitEditorView");
            var previewCard = RequireRect(bodyHost, "PreviewCard");
            var resultPane = RequireRect(bodyHost, "ResultPane");
            var saveDock = RequireRect(garageRoot.transform, "MobileSaveDockRoot");
            RenameChildIfExists(tabBar, "MobilePreviewTabButton", "MobileFirepowerTabButton");

            ConfigureViewport(contentRoot, bodyHost);
            ConfigureContent(bodyHost);

            MoveIntoContent(slotHost, bodyHost, 0);
            MoveIntoContent(tabBar, bodyHost, 1);
            MoveIntoContent(editor, bodyHost, 2);
            MoveIntoContent(previewCard, bodyHost, 3);
            MoveIntoContent(resultPane, bodyHost, 4);

            ConfigureChild(slotHost, SlotHostHeight);
            ConfigureChild(tabBar, FocusBarHeight);
            ConfigureChild(editor, EditorHeight);
            ConfigureChild(previewCard, PreviewCardHeight);
            ConfigureChild(resultPane, ResultPaneHeight);
            ConfigureSaveDock(saveDock);

            contentRoot.gameObject.SetActive(true);
            bodyHost.gameObject.SetActive(true);
            slotHost.gameObject.SetActive(true);
            tabBar.gameObject.SetActive(true);
            previewCard.gameObject.SetActive(true);
            resultPane.gameObject.SetActive(true);
            saveDock.gameObject.SetActive(true);

            HideIfExists(garageRoot.transform, "MainScroll");
            HideIfExists(garageRoot.transform, "RightRailRoot");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            WriteReport(garageRoot, contentRoot, bodyHost, slotHost, tabBar, previewCard, resultPane, saveDock);
            Debug.Log("[GarageMobileScrollMigrationTool] Garage mobile scroll layout migrated.");
        }

        private static void ConfigureViewport(RectTransform viewport, RectTransform content)
        {
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.anchoredPosition = new Vector2(ViewportLeftInset, ViewportBottomInset);
            viewport.sizeDelta = new Vector2(ViewportWidthInset, ViewportHeightInset);

            if (viewport.TryGetComponent<Image>(out var image))
            {
                image.raycastTarget = false;
            }

            var mask = Ensure<RectMask2D>(viewport.gameObject);
            mask.padding = Vector4.zero;
            mask.softness = Vector2Int.zero;

            var scrollRect = Ensure<ScrollRect>(viewport.gameObject);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private static void ConfigureContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            if (content.TryGetComponent<Image>(out var image))
            {
                image.color = Color.clear;
                image.raycastTarget = false;
            }

            var layout = Ensure<VerticalLayoutGroup>(content.gameObject);
            layout.padding = new RectOffset(8, 8, 8, 24);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = Ensure<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void MoveIntoContent(RectTransform child, RectTransform content, int siblingIndex)
        {
            if (child.parent != content)
                child.SetParent(content, false);

            child.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, content.childCount - 1));
        }

        private static void ConfigureChild(RectTransform rect, float preferredHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, preferredHeight);

            var element = Ensure<LayoutElement>(rect.gameObject);
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;
        }

        private static void ConfigureSaveDock(RectTransform saveDock)
        {
            saveDock.anchorMin = new Vector2(0f, 0f);
            saveDock.anchorMax = new Vector2(1f, 0f);
            saveDock.pivot = new Vector2(0.5f, 0f);
            saveDock.anchoredPosition = new Vector2(ViewportLeftInset, SaveDockBottomOffset);
            saveDock.sizeDelta = new Vector2(ViewportWidthInset, SaveDockHeight);
        }

        private static RectTransform FindRect(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
                return null;

            if (child is RectTransform rect)
                return rect;

            throw new InvalidOperationException(PathOf(child) + " exists but is not a RectTransform.");
        }

        private static RectTransform RequireRect(Transform parent, string childName)
        {
            var rect = FindRect(parent, childName);
            if (rect != null)
                return rect;

            throw new InvalidOperationException(PathOf(parent) + "/" + childName + " was not found.");
        }

        private static void RenameChildIfExists(RectTransform parent, string oldName, string newName)
        {
            var child = parent.Find(oldName);
            if (child != null)
                child.name = newName;
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();
        }

        private static void HideIfExists(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                child.gameObject.SetActive(false);
        }

        private static GameObject FindSceneObjectByPath(Scene scene, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path[0] != '/')
                return null;

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == parts[0]);
            if (root == null)
                return null;

            var current = root.transform;
            for (var i = 1; i < parts.Length; i++)
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

        private static void WriteReport(
            GameObject garageRoot,
            RectTransform contentRoot,
            RectTransform bodyHost,
            RectTransform slotHost,
            RectTransform tabBar,
            RectTransform previewCard,
            RectTransform resultPane,
            RectTransform saveDock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(
                ReportPath,
                "# Garage Mobile Scroll Migration Report\n\n"
                    + "- scene: " + SceneManager.GetActiveScene().path + "\n"
                    + "- garageRoot: " + PathOf(garageRoot.transform) + "\n"
                    + "- scrollRect: " + PathOf(contentRoot) + "\n"
                    + "- content: " + PathOf(bodyHost) + "\n"
                    + "- slotHost: " + PathOf(slotHost) + "\n"
                    + "- focusBar: " + PathOf(tabBar) + "\n"
                    + "- previewCard: " + PathOf(previewCard) + "\n"
                    + "- resultPane: " + PathOf(resultPane) + "\n"
                    + "- saveDock: " + PathOf(saveDock) + "\n"
                    + "- result: migrated to single vertical ScrollRect with fixed save dock\n",
                System.Text.Encoding.UTF8);
        }
    }
}
#endif
