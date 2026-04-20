#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class TempScenePrefabImportTool
    {
        private const string MenuPath = "Tools/Scene/Import Root Repeat Independent Prefabs";
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string CanvasRootName = "PrefabImportCanvas";
        private const string ReviewRootName = "PrefabImportReviewRoot";

        private static readonly PrefabSpec[] RootPrefabs =
        {
            new("LobbyPageRoot", "Assets/Prefabs/Features/Lobby/LobbyPageRoot.prefab", new Vector2(-430f, 0f), Vector3.one * 0.42f),
            new("GaragePageRoot", "Assets/Prefabs/Features/Garage/GaragePageRoot.prefab", new Vector2(0f, 0f), Vector3.one * 0.36f),
        };

        private static readonly PrefabSpec[] RepeatPrefabs =
        {
            new("LobbyRoomItem", "Assets/Prefabs/Features/Lobby/LobbyRoomItem.prefab", new Vector2(470f, 260f), Vector3.one * 0.85f),
            new("LobbyMemberItem", "Assets/Prefabs/Features/Lobby/LobbyMemberItem.prefab", new Vector2(470f, 70f), Vector3.one * 0.85f),
            new("GarageSlotItem", "Assets/Prefabs/Features/Garage/GarageSlotItem.prefab", new Vector2(470f, -140f), Vector3.one * 0.85f),
        };

        private static readonly PrefabSpec[] IndependentPrefabs =
        {
            new("LobbyRoomDetailPanel", "Assets/Prefabs/Features/Lobby/LobbyRoomDetailPanel.prefab", new Vector2(930f, 260f), Vector3.one * 0.58f),
            new("LoginLoadingOverlay", "Assets/Prefabs/Shared/Ui/LoginLoadingOverlay.prefab", new Vector2(930f, 20f), Vector3.one * 0.58f),
            new("LobbySceneErrorPresenter", "Assets/Prefabs/Shared/Ui/LobbySceneErrorPresenter.prefab", new Vector2(930f, -250f), Vector3.one * 0.58f),
            new("GarageSettingsOverlay", "Assets/Prefabs/Features/Garage/GarageSettingsOverlay.prefab", new Vector2(1350f, 170f), Vector3.one * 0.75f),
            new("GarageSaveDock", "Assets/Prefabs/Features/Garage/GarageSaveDock.prefab", new Vector2(1350f, -70f), Vector3.one),
            new("GaragePartSelector", "Assets/Prefabs/Features/Garage/GaragePartSelector.prefab", new Vector2(1350f, -300f), Vector3.one * 0.82f),
        };

        [MenuItem(MenuPath)]
        private static void ImportPrefabs()
        {
            EnsureTempSceneOpen();

            var canvasRoot = ResetCanvasRoot();
            var reviewRoot = CreateChildRect(canvasRoot.transform, ReviewRootName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f, Vector2.zero, new Vector2(1800f, 1200f));

            CreateSectionHeader(reviewRoot, "Root Prefabs", new Vector2(-430f, 510f));
            CreateSectionHeader(reviewRoot, "Repeat Prefabs", new Vector2(470f, 510f));
            CreateSectionHeader(reviewRoot, "Independent Prefabs", new Vector2(1130f, 510f));

            ImportSection(reviewRoot, RootPrefabs);
            ImportSection(reviewRoot, RepeatPrefabs);
            ImportSection(reviewRoot, IndependentPrefabs);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = reviewRoot.gameObject;
            Debug.Log("[TempScenePrefabImportTool] Imported root/repeat/independent prefab review board into TempScene.");
        }

        private static void EnsureTempSceneOpen()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                StageUtility.GoToMainStage();
            }

            if (!System.IO.File.Exists(TempScenePath))
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

        private static GameObject ResetCanvasRoot()
        {
            var existing = GameObject.Find("/" + CanvasRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var canvasGo = new GameObject(CanvasRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create " + CanvasRootName);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            return canvasGo;
        }

        private static void ImportSection(Transform parent, IEnumerable<PrefabSpec> specs)
        {
            foreach (var spec in specs)
            {
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.AssetPath);
                if (prefabAsset == null)
                {
                    Debug.LogWarning($"[TempScenePrefabImportTool] Prefab missing: {spec.AssetPath}");
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(prefabAsset, SceneManager.GetActiveScene()) as GameObject;
                if (instance == null)
                {
                    Debug.LogWarning($"[TempScenePrefabImportTool] Failed to instantiate prefab: {spec.AssetPath}");
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + prefabAsset.name);
                instance.name = spec.DisplayName;
                instance.transform.SetParent(parent, false);

                if (instance.transform is RectTransform rect)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = spec.Position;
                    rect.localScale = spec.Scale;
                }
                else
                {
                    instance.transform.localPosition = spec.Position;
                    instance.transform.localScale = spec.Scale;
                }
            }
        }

        private static RectTransform CreateChildRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static void CreateSectionHeader(Transform parent, string text, Vector2 anchoredPosition)
        {
            var header = CreateChildRect(parent, text.Replace(" ", string.Empty) + "Header", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(320f, 40f));
            var label = header.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 28f;
            label.color = new Color(0.93f, 0.95f, 1f, 1f);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private readonly struct PrefabSpec
        {
            public PrefabSpec(string displayName, string assetPath, Vector2 position, Vector3 scale)
            {
                DisplayName = displayName;
                AssetPath = assetPath;
                Position = position;
                Scale = scale;
            }

            public string DisplayName { get; }
            public string AssetPath { get; }
            public Vector2 Position { get; }
            public Vector3 Scale { get; }
        }
    }
}
#endif
