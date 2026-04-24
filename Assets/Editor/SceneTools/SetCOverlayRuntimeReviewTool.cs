#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class SetCOverlayRuntimeReviewTool
    {
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string CameraRootName = "SetCOverlayReviewCamera";
        private const string CanvasRootName = "SetCOverlayReviewCanvas";
        private const string FrameRootName = "MobileReviewFrame";
        private const string OverlayRootName = "OverlaySurface";

        private const string MenuRoot = "Tools/Scene/Prepare Set C Overlay Runtime Review/";
        private const string AccountDeleteConfirmPrefabPath = "Assets/Prefabs/Features/Account/Independent/AccountDeleteConfirmDialogRoot.prefab";
        private const string CommonErrorDialogPrefabPath = "Assets/Prefabs/Features/Common/Independent/CommonErrorDialogRoot.prefab";

        [MenuItem(MenuRoot + "Account Delete Confirm")]
        private static void PrepareAccountDeleteConfirm()
        {
            PrepareSurface(
                displayName: "AccountDeleteConfirm",
                prefabPath: AccountDeleteConfirmPrefabPath,
                cameraColor: new Color(0.02f, 0.03f, 0.05f, 1f),
                frameColor: new Color(0.03f, 0.04f, 0.07f, 1f)
            );
        }

        [MenuItem(MenuRoot + "Common Error Dialog")]
        private static void PrepareCommonErrorDialog()
        {
            PrepareSurface(
                displayName: "CommonErrorDialog",
                prefabPath: CommonErrorDialogPrefabPath,
                cameraColor: new Color(0.08f, 0.04f, 0.04f, 1f),
                frameColor: new Color(0.10f, 0.06f, 0.06f, 1f)
            );
        }

        private static void PrepareSurface(string displayName, string prefabPath, Color cameraColor, Color frameColor)
        {
            EnsureMainStageAndTempScene();

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[SetCOverlayRuntimeReviewTool] Prefab missing: {prefabPath}");
                return;
            }

            var reviewCamera = ResetSceneRoots(cameraColor);
            var canvasRoot = CreateCanvasRoot(reviewCamera);
            var frameRoot = CreateFrameRoot(canvasRoot.transform, frameColor);
            EnsureEventSystem();

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[SetCOverlayRuntimeReviewTool] Failed to instantiate prefab: {prefabPath}");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + prefabAsset.name);
            instance.name = OverlayRootName;
            instance.transform.SetParent(frameRoot, false);

            if (instance.transform is RectTransform overlayRect)
            {
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.anchoredPosition = Vector2.zero;
                overlayRect.sizeDelta = Vector2.zero;
                overlayRect.localScale = Vector3.one;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            FocusSceneView();
            Selection.activeGameObject = instance;
            Debug.Log($"[SetCOverlayRuntimeReviewTool] Prepared {displayName} runtime review surface in TempScene.");
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
            rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

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
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
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
                Object.DestroyImmediate(existing);
            }
        }

        private static void FocusSceneView()
        {
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            if (sceneView == null)
            {
                return;
            }

            sceneView.orthographic = true;
            sceneView.in2DMode = true;
            sceneView.LookAt(Vector3.zero, Quaternion.identity, 4.5f, true);
            sceneView.Repaint();
        }
    }
}
#endif
