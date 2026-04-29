using System;
using System.IO;
using Features.Garage.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectSD.EditorTools.SceneTools
{
    public static class GarageSetBUitkPreviewTool
    {
        private const string UxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";
        private const string PanelSettingsPath = "Assets/Settings/UI/GarageSetBPanelSettings.asset";
        private const string ScenePath = "Assets/Scenes/GarageSetBUitkPreview.unity";
        private const string ReportPath = "artifacts/unity/garage-setb-uitoolkit-port-report.md";

        [MenuItem("Tools/UIToolkit/Create Garage SetB Preview Scene")]
        public static void CreatePreviewScene()
        {
            AssetDatabase.ImportAsset(UxmlPath, ImportAssetOptions.ForceSynchronousImport);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                throw new InvalidOperationException($"Missing UI Toolkit source: {UxmlPath}");
            }

            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/UI");

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            ConfigurePanelSettings(panelSettings);
            EditorUtility.SetDirty(panelSettings);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.035f, 0.043f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.depth = 100f;
            camera.nearClipPlane = -10f;
            camera.farClipPlane = 10f;

            var documentObject = new GameObject("GarageSetBUitkDocument");
            var document = documentObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 10;

            var previewCameraObject = new GameObject("GarageSetBPreviewCamera");
            var previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.035f, 0.035f, 0.043f, 1f);
            previewCamera.enabled = false;
            previewCamera.fieldOfView = 32f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 50f;
            previewCamera.depth = -10f;
            var previewRenderer = previewCameraObject.AddComponent<GarageSetBUitkPreviewRenderer>();

            var partPreviewCameraObject = new GameObject("GarageSetBPartPreviewCamera");
            var partPreviewCamera = partPreviewCameraObject.AddComponent<Camera>();
            partPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            partPreviewCamera.backgroundColor = new Color(0.035f, 0.035f, 0.043f, 1f);
            partPreviewCamera.enabled = false;
            partPreviewCamera.fieldOfView = 32f;
            partPreviewCamera.nearClipPlane = 0.01f;
            partPreviewCamera.farClipPlane = 50f;
            partPreviewCamera.depth = -11f;
            var partPreviewRenderer = partPreviewCameraObject.AddComponent<GarageSetBUitkPreviewRenderer>();

            var runtimeAdapter = documentObject.AddComponent<GarageSetBUitkRuntimeAdapter>();
            WireRuntimeAdapter(runtimeAdapter, document, previewRenderer, partPreviewRenderer);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport();

            Debug.Log($"Garage SetB UI Toolkit preview scene created: {ScenePath}");
        }

        private static void ConfigurePanelSettings(PanelSettings panelSettings)
        {
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(390, 844);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 10;
        }

        private static void WireRuntimeAdapter(
            GarageSetBUitkRuntimeAdapter runtimeAdapter,
            UIDocument document,
            GarageSetBUitkPreviewRenderer previewRenderer,
            GarageSetBUitkPreviewRenderer partPreviewRenderer)
        {
            var serializedObject = new SerializedObject(runtimeAdapter);
            serializedObject.FindProperty("_document").objectReferenceValue = document;
            serializedObject.FindProperty("_previewRenderer").objectReferenceValue = previewRenderer;
            serializedObject.FindProperty("_partPreviewRenderer").objectReferenceValue = partPreviewRenderer;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private static void WriteReport()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "artifacts/unity");
            File.WriteAllText(
                ReportPath,
                string.Join(
                    Environment.NewLine,
                    "# Garage SetB UI Toolkit Port Report",
                    "",
                    "- Status: pilot surface created",
                    "- Source: `.stitch/designs/set-b-garage-main-workspace.{html,png}`",
                    "- UXML: `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml`",
                    "- USS: `Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss`",
                    "- PanelSettings: `Assets/Settings/UI/GarageSetBPanelSettings.asset`",
                    "- Preview scene: `Assets/Scenes/GarageSetBUitkPreview.unity`",
                    "",
                    "## Scope",
                    "",
                    "- This pass does not replace `GaragePageRoot.prefab`.",
                    "- This pass does not change Garage presenter/runtime binding.",
                    "- The screen is a static UI Toolkit translation used for visual comparison and implementation sizing.",
                    "",
                    "## Preserved SetB Reading Order",
                    "",
                    "1. Current slot summary + slot selector",
                    "2. Part focus bar",
                    "3. Focused editor",
                    "4. Preview + summary",
                    "5. Persistent save dock",
                    "",
                    "## Known Gaps",
                    "",
                    "- Static sample data only; no Garage state binding yet.",
                    "- Blueprint preview is still a UITK placeholder, not the assembled 3D unit preview.",
                    "- Iconography uses project-local UITK icon assets mapped from the Stitch Material Symbol names.",
                    "- Runtime replacement needs a separate binding pass and acceptance capture against the active Lobby/Garage flow.",
                    ""));
        }
    }
}
