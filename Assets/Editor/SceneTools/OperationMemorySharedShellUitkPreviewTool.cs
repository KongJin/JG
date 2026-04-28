using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectSD.EditorTools.SceneTools
{
    public static class OperationMemorySharedShellUitkPreviewTool
    {
        private const string OperationMemoryUxmlPath = "Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uxml";
        private const string SharedShellUxmlPath = "Assets/UI/UIToolkit/Shared/SharedShellNavigation.uxml";
        private const string OperationMemoryPanelSettingsPath = "Assets/Settings/UI/OperationMemoryPanelSettings.asset";
        private const string SharedShellPanelSettingsPath = "Assets/Settings/UI/SharedShellPanelSettings.asset";
        private const string OperationMemoryScenePath = "Assets/Scenes/OperationMemoryUitkPreview.unity";
        private const string SharedShellScenePath = "Assets/Scenes/SharedShellUitkPreview.unity";

        [MenuItem("Tools/UIToolkit/Create Operation Memory Preview Scene")]
        public static void CreateOperationMemoryPreviewScene()
        {
            CreatePreviewScene(
                OperationMemoryUxmlPath,
                OperationMemoryPanelSettingsPath,
                OperationMemoryScenePath,
                "OperationMemoryUitkDocument",
                "Operation Memory UI Toolkit preview scene created");
        }

        [MenuItem("Tools/UIToolkit/Create Shared Shell Preview Scene")]
        public static void CreateSharedShellPreviewScene()
        {
            CreatePreviewScene(
                SharedShellUxmlPath,
                SharedShellPanelSettingsPath,
                SharedShellScenePath,
                "SharedShellUitkDocument",
                "Shared Shell UI Toolkit preview scene created");
        }

        private static void CreatePreviewScene(
            string uxmlPath,
            string panelSettingsPath,
            string scenePath,
            string documentName,
            string logMessage)
        {
            AssetDatabase.ImportAsset(uxmlPath, ImportAssetOptions.ForceSynchronousImport);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                throw new InvalidOperationException($"Missing UI Toolkit source: {uxmlPath}");
            }

            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/UI");

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
            }

            ConfigurePanelSettings(panelSettings);
            EditorUtility.SetDirty(panelSettings);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.027f, 0.043f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = -10f;
            camera.farClipPlane = 10f;

            var documentObject = new GameObject(documentName);
            var document = documentObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 10;

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{logMessage}: {scenePath}");
        }

        private static void ConfigurePanelSettings(PanelSettings panelSettings)
        {
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(390, 844);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 10;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", folder);
        }
    }
}
