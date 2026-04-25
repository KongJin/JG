#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class StitchRuntimeReviewTool
    {
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string MenuRoot = "Tools/Scene/Prepare Stitch Runtime Review/";
        private const string RequestPath = "Temp/StitchRuntimeReview/request.json";
        private const string CameraRootName = "StitchRuntimeReviewCamera";
        private const string CanvasRootName = "StitchRuntimeReviewCanvas";
        private const string FrameRootName = "MobileReviewFrame";
        private const float ReviewCanvasScale = 0.01f;
        private const float ReviewFramePaddingFactor = 0.62f;

        [Serializable]
        private sealed class ReviewRequest
        {
            public string surfaceId = string.Empty;
            public string displayName = string.Empty;
            public string prefabPath = string.Empty;
        }

        private sealed class ReviewVisualSpec
        {
            public string DefaultRootName { get; set; }
            public Color CameraColor { get; set; }
            public Color FrameColor { get; set; }
        }

        [MenuItem(MenuRoot + "Surface")]
        private static void PrepareSurface()
        {
            PrepareRequestedSurface();
        }

        private static void PrepareRequestedSurface()
        {
            var request = LoadRequest();
            if (request == null)
            {
                Debug.LogError($"[StitchRuntimeReviewTool] Missing or invalid request file: {RequestPath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(request.prefabPath))
            {
                Debug.LogError("[StitchRuntimeReviewTool] Request does not contain prefabPath.");
                return;
            }

            EnsureMainStageAndTempScene();

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(request.prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[StitchRuntimeReviewTool] Prefab missing: {request.prefabPath}");
                return;
            }

            var spec = GetReviewVisualSpec();
            var reviewCamera = ResetSceneRoots(spec.CameraColor);
            var canvasRoot = CreateCanvasRoot(reviewCamera);
            var frameRoot = CreateFrameRoot(canvasRoot.transform, spec.FrameColor);
            EnsureEventSystem();

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[StitchRuntimeReviewTool] Failed to instantiate prefab: {request.prefabPath}");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + prefabAsset.name);
            instance.name = spec.DefaultRootName;
            instance.transform.SetParent(frameRoot, false);

            if (instance.transform is RectTransform surfaceRect)
            {
                surfaceRect.anchorMin = Vector2.zero;
                surfaceRect.anchorMax = Vector2.one;
                surfaceRect.pivot = new Vector2(0.5f, 0.5f);
                surfaceRect.anchoredPosition = Vector2.zero;
                surfaceRect.sizeDelta = Vector2.zero;
                surfaceRect.localScale = Vector3.one;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = frameRoot.gameObject;
            FocusSceneView(frameRoot);

            var displayName = string.IsNullOrWhiteSpace(request.displayName) ? prefabAsset.name : request.displayName;
            Debug.Log($"[StitchRuntimeReviewTool] Prepared {displayName} review surface in TempScene.");
        }

        private static ReviewRequest LoadRequest()
        {
            if (!File.Exists(RequestPath))
            {
                return null;
            }

            var json = File.ReadAllText(RequestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<ReviewRequest>(json);
        }

        private static ReviewVisualSpec GetReviewVisualSpec()
        {
            return new ReviewVisualSpec
            {
                DefaultRootName = "ReviewSurface",
                CameraColor = new Color(0.04f, 0.05f, 0.08f, 1f),
                FrameColor = new Color(0.07f, 0.08f, 0.11f, 1f)
            };
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

            if (SceneManager.GetActiveScene().path != TempScenePath)
            {
                EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Single);
            }
        }

        private static Camera ResetSceneRoots(Color cameraColor)
        {
            DestroyRootIfExists(CameraRootName);
            DestroyRootIfExists(CanvasRootName);

            var cameraGo = new GameObject(CameraRootName, typeof(Camera));
            Undo.RegisterCreatedObjectUndo(cameraGo, "Create " + CameraRootName);

            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraColor;
            camera.orthographic = true;
            camera.orthographicSize = 4.22f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.transform.position = new Vector3(0f, 0f, -5f);
            return camera;
        }

        private static GameObject CreateCanvasRoot(Camera reviewCamera)
        {
            var canvasGo = new GameObject(CanvasRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create " + CanvasRootName);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = reviewCamera;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1f;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(390f, 844f);
            rect.localScale = new Vector3(ReviewCanvasScale, ReviewCanvasScale, ReviewCanvasScale);

            return canvasGo;
        }

        private static RectTransform CreateFrameRoot(Transform parent, Color frameColor)
        {
            var frameGo = new GameObject(FrameRootName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(frameGo, "Create " + FrameRootName);

            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.SetParent(parent, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = Vector2.zero;
            frameRect.sizeDelta = Vector2.zero;

            var image = frameGo.GetComponent<Image>();
            image.color = frameColor;
            image.raycastTarget = false;

            return frameRect;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
                eventSystem = eventSystemGo.GetComponent<EventSystem>();
            }

            foreach (var standaloneModule in eventSystem.GetComponents<StandaloneInputModule>())
            {
                Undo.DestroyObjectImmediate(standaloneModule);
            }

            foreach (var inputModule in eventSystem.GetComponents<BaseInputModule>())
            {
                if (inputModule is InputSystemUIInputModule)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(inputModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            }
        }

        private static void DestroyRootIfExists(string rootName)
        {
            var existing = GameObject.Find("/" + rootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void FocusSceneView(RectTransform targetRect)
        {
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            if (sceneView == null || targetRect == null)
            {
                return;
            }

            var bounds = GetWorldBounds(targetRect);
            var size = Mathf.Max(bounds.size.y * 0.5f, bounds.size.x * 0.5f) * ReviewFramePaddingFactor;
            sceneView.orthographic = true;
            sceneView.in2DMode = true;
            sceneView.LookAtDirect(bounds.center, Quaternion.identity, Mathf.Max(0.1f, size));
            sceneView.Repaint();
        }

        private static Bounds GetWorldBounds(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var bounds = new Bounds(corners[0], Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(corners[i]);
            }

            return bounds;
        }
    }
}
#endif
