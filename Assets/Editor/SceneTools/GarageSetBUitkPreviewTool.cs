using System;
using System.IO;
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
                    "- Iconography uses text placeholders instead of Material Symbols or project icon assets.",
                    "- Runtime replacement needs a separate binding pass and acceptance capture against the active Lobby/Garage flow.",
                    ""));
        }
    }
}
