using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectSD.EditorTools.SceneTools
{
    public static class AccountConnectionUitkPreviewTool
    {
        private const string AccountUxmlPath = "Assets/UI/UIToolkit/AccountSync/AccountSyncConsole.uxml";
        private const string ConnectionUxmlPath = "Assets/UI/UIToolkit/ConnectionReconnect/ConnectionReconnectControl.uxml";
        private const string AccountPanelSettingsPath = "Assets/Settings/UI/AccountSyncPanelSettings.asset";
        private const string ConnectionPanelSettingsPath = "Assets/Settings/UI/ConnectionReconnectPanelSettings.asset";
        private const string AccountScenePath = "Assets/Scenes/AccountSyncUitkPreview.unity";
        private const string ConnectionScenePath = "Assets/Scenes/ConnectionReconnectUitkPreview.unity";

        [MenuItem("Tools/UIToolkit/Create Account Sync Preview Scene")]
        public static void CreateAccountSyncPreviewScene()
        {
            CreatePreviewScene(
                AccountUxmlPath,
                AccountPanelSettingsPath,
                AccountScenePath,
                "AccountSyncUitkDocument",
                "Account Sync UI Toolkit preview scene created");
        }

        [MenuItem("Tools/UIToolkit/Create Connection Reconnect Preview Scene")]
        public static void CreateConnectionReconnectPreviewScene()
        {
            CreatePreviewScene(
                ConnectionUxmlPath,
                ConnectionPanelSettingsPath,
                ConnectionScenePath,
                "ConnectionReconnectUitkDocument",
                "Connection Reconnect UI Toolkit preview scene created");
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
