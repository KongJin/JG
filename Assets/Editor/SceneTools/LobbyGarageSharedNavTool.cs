#if UNITY_EDITOR
using System.IO;
using Features.Lobby.Presentation;
using TMPro;
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
    internal static class LobbyGarageSharedNavTool
    {
        private const string MenuPath = "Tools/Scene/Generate Lobby Garage Shared Nav";
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string NavPrefabPath = "Assets/Prefabs/Shared/Ui/Navigation/LobbyGarageNavBar.prefab";
        private const string LobbyRootPrefabPath = "Assets/Prefabs/Features/Lobby/Root/LobbyPageRoot.prefab";
        private const string LobbyRoomDetailPrefabPath = "Assets/Prefabs/Features/Lobby/Independent/LobbyRoomDetailPanel.prefab";
        private const string GarageRootPrefabPath = "Assets/Prefabs/Features/Garage/Root/GaragePageRoot.prefab";

        [MenuItem(MenuPath)]
        private static void GenerateSharedNav()
        {
            var fontAsset = ResolveFontAsset();
            if (fontAsset == null)
            {
                Debug.LogError("[LobbyGarageSharedNavTool] TMP font asset not found. Import TMP essentials or refresh Noto Sans KR fallback first.");
                return;
            }

            EnsureFolders();
            SaveNavPrefab(BuildNavPrefab(fontAsset), NavPrefabPath);
            UpdateLobbyPageRootPrefab();
            AssembleTempSceneReviewShell();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LobbyGarageSharedNavTool] Generated shared nav prefab and assembled the TempScene review shell.");
        }

        private static TMP_FontAsset ResolveFontAsset()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            var noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansKR Dynamic.asset");
            if (noto != null)
                return noto;

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Shared");
            EnsureFolder("Assets/Prefabs/Shared/Ui");
            EnsureFolder("Assets/Prefabs/Shared/Ui/Navigation");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static void SaveNavPrefab(GameObject instanceRoot, string assetPath)
        {
            PrefabUtility.SaveAsPrefabAsset(instanceRoot, assetPath);
            Object.DestroyImmediate(instanceRoot);
        }

        private static GameObject BuildNavPrefab(TMP_FontAsset fontAsset)
        {
            var root = new GameObject("LobbyGarageNavBar", typeof(RectTransform), typeof(LobbyGarageNavBarView));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -18f);
            rootRect.sizeDelta = new Vector2(342f, 56f);

            var lobbyButton = CreateTabButton(root.transform, fontAsset, "LobbyTabButton", "LOBBY", new Vector2(0f, 0.5f), new Vector2(0f, 0f));
            var garageButton = CreateTabButton(root.transform, fontAsset, "GarageTabButton", "GARAGE", new Vector2(1f, 0.5f), new Vector2(0f, 0f));

            ConfigureButtonRect((RectTransform)lobbyButton.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f));
            ConfigureButtonRect((RectTransform)garageButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f));

            var lobbyBorder = CreateBorder(lobbyButton.transform, "LobbyTabBorder");
            var garageBorder = CreateBorder(garageButton.transform, "GarageTabBorder");
            var lobbyLabel = CreateLabel(lobbyButton.transform, fontAsset, "Text (TMP)", "LOBBY");
            var garageLabel = CreateLabel(garageButton.transform, fontAsset, "Text (TMP)", "GARAGE");

            var navView = root.GetComponent<LobbyGarageNavBarView>();
            SetObjectReference(navView, "_lobbyTabButton", lobbyButton.GetComponent<Button>());
            SetObjectReference(navView, "_garageTabButton", garageButton.GetComponent<Button>());
            SetObjectReference(navView, "_lobbyTabText", lobbyLabel);
            SetObjectReference(navView, "_garageTabText", garageLabel);
            SetObjectReference(navView, "_lobbyTabBorder", lobbyBorder);
            SetObjectReference(navView, "_garageTabBorder", garageBorder);
            EditorUtility.SetDirty(navView);
            navView.SetState(true);
            return root;
        }

        private static GameObject CreateTabButton(Transform parent, TMP_FontAsset fontAsset, string name, string label, Vector2 pivot, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(164f, 48f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.086f, 0.157f, 0.196f, 1f);
            image.raycastTarget = true;
            image.type = Image.Type.Simple;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            return go;
        }

        private static void ConfigureButtonRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(164f, 48f);
        }

        private static Image CreateBorder(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(4f, 28f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.404f, 0.722f, 1f, 1f);
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateLabel(Transform parent, TMP_FontAsset fontAsset, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(128f, 24f);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = label;
            text.fontSize = 20f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private static void UpdateLobbyPageRootPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyRootPrefabPath) == null)
                return;

            var prefabRoot = PrefabUtility.LoadPrefabContents(LobbyRootPrefabPath);
            try
            {
                var garageTabButton = prefabRoot.transform.Find("RoomListPanel/GarageSummaryCard/GarageTabButton");
                if (garageTabButton != null)
                    garageTabButton.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, LobbyRootPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void AssembleTempSceneReviewShell()
        {
            EnsureTempSceneOpen();

            RemoveIfExists("/LobbyView");
            RemoveIfExists("/Canvas");
            RemoveIfExists("/EventSystem");

            var canvasRoot = EnsureCanvas();
            EnsureEventSystem();

            var lobbyRoot = TryInstantiatePrefabUnderCanvas(LobbyRootPrefabPath, canvasRoot.transform);
            var roomDetailRoot = TryInstantiatePrefabUnderCanvas(LobbyRoomDetailPrefabPath, canvasRoot.transform);
            var garageRoot = InstantiatePrefabUnderCanvas(GarageRootPrefabPath, canvasRoot.transform);
            var navRoot = TryInstantiatePrefabUnderCanvas(NavPrefabPath, canvasRoot.transform);

            var siblingIndex = 0;
            if (lobbyRoot != null)
                ConfigurePageRoot((RectTransform)lobbyRoot.transform, siblingIndex++);
            if (roomDetailRoot != null)
                ConfigurePageRoot((RectTransform)roomDetailRoot.transform, siblingIndex++);
            ConfigurePageRoot((RectTransform)garageRoot.transform, siblingIndex++);
            if (navRoot != null)
                ConfigureNavRoot((RectTransform)navRoot.transform, siblingIndex++);

            if (lobbyRoot != null)
                lobbyRoot.SetActive(true);
            if (roomDetailRoot != null)
                roomDetailRoot.SetActive(false);
            garageRoot.SetActive(lobbyRoot == null);

            if (lobbyRoot != null && roomDetailRoot != null && navRoot != null)
                CreateLobbyView(lobbyRoot, roomDetailRoot, garageRoot, navRoot);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = navRoot != null ? navRoot : garageRoot;
        }

        private static void EnsureTempSceneOpen()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                StageUtility.GoToMainStage();

            if (!File.Exists(TempScenePath))
            {
                var createdScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(createdScene, TempScenePath, true);
            }

            if (SceneManager.GetActiveScene().path != TempScenePath)
                EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Single);
        }

        private static GameObject EnsureCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = scaler.referenceResolution;
            return canvasGo;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void RemoveIfExists(string path)
        {
            var target = GameObject.Find(path);
            if (target != null)
                Object.DestroyImmediate(target);
        }

        private static GameObject InstantiatePrefabUnderCanvas(string assetPath, Transform parent)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset == null)
                throw new UnityException($"Missing prefab asset: {assetPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
                throw new UnityException($"Failed to instantiate prefab asset: {assetPath}");

            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static GameObject TryInstantiatePrefabUnderCanvas(string assetPath, Transform parent)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[LobbyGarageSharedNavTool] Optional prefab missing: {assetPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefabAsset, SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
                throw new UnityException($"Failed to instantiate prefab asset: {assetPath}");

            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void ConfigurePageRoot(RectTransform rect, int siblingIndex)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.SetSiblingIndex(siblingIndex);
        }

        private static void ConfigureNavRoot(RectTransform rect, int siblingIndex)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.SetSiblingIndex(siblingIndex);
        }

        private static void CreateLobbyView(GameObject lobbyRoot, GameObject roomDetailRoot, GameObject garageRoot, GameObject navRoot)
        {
            var lobbyViewGo = new GameObject("LobbyView", typeof(LobbyView));
            var lobbyView = lobbyViewGo.GetComponent<LobbyView>();

            var roomListPanel = FindRequiredChild(lobbyRoot.transform, "RoomListPanel").gameObject;
            var roomDetailPanel = roomDetailRoot;
            var roomListView = roomListPanel.GetComponentInChildren<RoomListView>(true);
            var roomDetailView = roomDetailPanel.GetComponentInChildren<RoomDetailView>(true);
            var garageSummaryCard = FindRequiredChild(lobbyRoot.transform, "RoomListPanel/GarageSummaryCard").gameObject;
            var garageSummaryView = garageSummaryCard.GetComponentInChildren<LobbyGarageSummaryView>(true);
            var navView = navRoot.GetComponent<LobbyGarageNavBarView>();

            SetObjectReference(lobbyView, "_lobbyPageRoot", lobbyRoot);
            SetObjectReference(lobbyView, "_garagePageRoot", garageRoot);
            SetObjectReference(lobbyView, "_navigationBar", navView);
            SetObjectReference(lobbyView, "_roomListPanel", roomListPanel);
            SetObjectReference(lobbyView, "_roomDetailPanel", roomDetailPanel);
            SetObjectReference(lobbyView, "_lobbyPageCanvasGroup", lobbyRoot.GetComponent<CanvasGroup>());
            SetObjectReference(lobbyView, "_garagePageCanvasGroup", garageRoot.GetComponent<CanvasGroup>());
            SetObjectReference(lobbyView, "_roomListView", roomListView);
            SetObjectReference(lobbyView, "_roomDetailView", roomDetailView);
            SetObjectReference(lobbyView, "_garageSummaryView", garageSummaryView);
        }

        private static Transform FindRequiredChild(Transform root, string relativePath)
        {
            var child = root.Find(relativePath);
            if (child == null)
                throw new UnityException($"Missing child path '{relativePath}' under '{root.name}'.");

            return child;
        }

        private static void SetObjectReference(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
                throw new UnityException($"Missing serialized field '{fieldName}' on {target.GetType().Name}.");

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
