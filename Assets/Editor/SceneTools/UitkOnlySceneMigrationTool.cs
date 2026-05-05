#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Features.Account.Presentation;
using Features.Lobby.Presentation;
using Features.Player;
using Features.Player.Presentation;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Wave.Presentation;
using Shared.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class UitkOnlySceneMigrationTool
    {
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
        private const string TempScenePath = "Assets/Scenes/TempScene.unity";
        private const string SharedPanelSettingsPath = "Assets/Settings/UI/SharedShellPanelSettings.asset";
        private const string BattlePanelSettingsPath = "Assets/Settings/UI/BattleHudPanelSettings.asset";
        private const string LobbyShellUxmlPath = "Assets/UI/UIToolkit/Lobby/LobbyShell.uxml";
        private const string GarageSetBUxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";
        private const string OperationMemoryUxmlPath = "Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uxml";
        private const string AccountSyncUxmlPath = "Assets/UI/UIToolkit/AccountSync/AccountSyncConsole.uxml";
        private const string ConnectionReconnectUxmlPath = "Assets/UI/UIToolkit/ConnectionReconnect/ConnectionReconnectControl.uxml";
        private const string BattleHudUxmlPath = "Assets/UI/UIToolkit/BattleHud/BattleHud.uxml";
        private const string BattleHudUssPath = "Assets/UI/UIToolkit/BattleHud/BattleHud.uss";

        [MenuItem("Tools/UIToolkit/Migrate Production Scenes To UITK Only")]
        public static void MigrateProductionScenes()
        {
            EnsurePanelSettings(SharedPanelSettingsPath);
            EnsurePanelSettings(BattlePanelSettingsPath);

            MigrateLobbyScene();
            MigrateBattleScene();
            MigrateTempSceneIfPresent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UITK Only] Production scene migration completed.");
        }

        private static void MigrateLobbyScene()
        {
            var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var lobbyDocument = EnsureUidocument(
                "LobbyUitkDocument",
                SharedPanelSettingsPath,
                LobbyShellUxmlPath,
                sortingOrder: 20);
            var lobbyView = EnsureComponent<LobbyPageController>(lobbyDocument);
            var lobbyUidocument = lobbyDocument.GetComponent<UIDocument>();
            AssignObjectReference(lobbyView, "_document", lobbyUidocument);
            AssignObjectReference(lobbyView, "_lobbyShellTree", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LobbyShellUxmlPath));
            AssignObjectReference(lobbyView, "_operationMemoryTree", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OperationMemoryUxmlPath));
            AssignObjectReference(lobbyView, "_accountSyncTree", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AccountSyncUxmlPath));
            AssignObjectReference(lobbyView, "_connectionReconnectTree", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ConnectionReconnectUxmlPath));
            var garageHost = EnsureUidocument(
                "GarageSetBUitkDocument",
                SharedPanelSettingsPath,
                GarageSetBUxmlPath,
                sortingOrder: 10);
            var garageDocument = garageHost.GetComponent<UIDocument>();
            AssignObjectReference(garageDocument, "m_ParentUI", lobbyUidocument);
            AssignObjectReference(lobbyView, "_garageDocument", garageDocument);

            var stateHost = EnsureGameObject("LobbyRuntimeUiState");
            var loadingView = EnsureComponent<LoginLoadingView>(stateHost);
            var accountSettingsView = EnsureComponent<AccountSettingsView>(stateHost);
            var sceneErrorPresenter = EnsureComponent<SceneErrorPresenter>(stateHost);

            var setup = UnityEngine.Object.FindFirstObjectByType<LobbySetup>(FindObjectsInactive.Include);
            if (setup != null)
            {
                AssignObjectReference(setup, "_view", lobbyView);
                AssignObjectReference(setup, "_sceneErrorPresenter", sceneErrorPresenter);
                AssignObjectReference(setup, "_loginLoadingView", loadingView);
                AssignObjectReference(setup, "_accountSettingsView", accountSettingsView);
            }

            RemoveLegacyUiObjects(lobbyDocument, stateHost);
            RemoveExtraComponents(lobbyView);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void MigrateBattleScene()
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            var battleDocument = EnsureUidocument(
                "BattleHudUitkDocument",
                BattlePanelSettingsPath,
                BattleHudUxmlPath,
                sortingOrder: 30);
            var battleSurface = EnsureComponent<BattleHudUitkSurface>(battleDocument);
            AssignObjectReference(battleSurface, "_document", battleDocument.GetComponent<UIDocument>());
            AssignObjectReference(battleSurface, "_visualTree", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BattleHudUxmlPath));
            AssignObjectReference(battleSurface, "_styleSheet", AssetDatabase.LoadAssetAtPath<StyleSheet>(BattleHudUssPath));

            var stateHost = EnsureGameObject("BattleRuntimeHudState");
            var energy = EnsureComponent<EnergyBarView>(stateHost);
            var waveHud = EnsureComponent<WaveHudView>(stateHost);
            var waveEnd = EnsureComponent<WaveEndView>(stateHost);
            var coreHealth = EnsureComponent<CoreHealthHudView>(stateHost);
            var placementError = EnsureComponent<PlacementErrorView>(stateHost);
            var summonCommand = EnsureComponent<SummonCommandController>(stateHost);
            var slotsContainer = EnsureComponent<UnitSlotsContainer>(stateHost);
            var sceneErrorPresenter = EnsureComponent<SceneErrorPresenter>(stateHost);

            var slotParent = EnsureChild(stateHost.transform, "UnitSlotStateParent");
            var slotPrototype = EnsureComponent<UnitSlotView>(EnsureChild(stateHost.transform, "UnitSlotPrototype").gameObject);
            AssignObjectReference(slotsContainer, "_slotPrefab", slotPrototype);
            AssignObjectReference(slotsContainer, "_slotsParent", slotParent);
            AssignObjectReference(slotsContainer, "_errorView", placementError);
            AssignObjectReference(slotsContainer, "_commandController", summonCommand);

            var root = UnityEngine.Object.FindFirstObjectByType<GameSceneRoot>(FindObjectsInactive.Include);
            if (root != null)
            {
                AssignObjectReference(root, "_sceneErrorPresenter", sceneErrorPresenter);
                AssignObjectReference(root, "_energyBarView", energy);
                AssignObjectReference(root, "_unitSlotsContainer", slotsContainer);
            }

            var waveSetup = UnityEngine.Object.FindFirstObjectByType<WaveSetup>(FindObjectsInactive.Include);
            if (waveSetup != null)
            {
                AssignObjectReference(waveSetup, "_hudView", waveHud);
                AssignObjectReference(waveSetup, "_endView", waveEnd);
                AssignObjectReference(waveSetup, "_coreHealthView", coreHealth);
            }

            RemoveLegacyUiObjects(battleDocument, stateHost);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void MigrateTempSceneIfPresent()
        {
            if (!File.Exists(TempScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Single);
            RemoveLegacyUiObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveExtraComponents<T>(T keep) where T : Component
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component != null && component != keep)
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
            }
        }

        private static GameObject EnsureUidocument(
            string name,
            string panelSettingsPath,
            string visualTreePath,
            int sortingOrder)
        {
            var go = EnsureGameObject(name);
            var document = EnsureComponent<UIDocument>(go);
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            document.visualTreeAsset = string.IsNullOrWhiteSpace(visualTreePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreePath);
            document.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(document);
            return go;
        }

        private static PanelSettings EnsurePanelSettings(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, path);
            }

            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(390, 844);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 20;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }

        private static GameObject EnsureGameObject(string name)
        {
            var existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing;

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
                return;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void RemoveLegacyUiObjects(params GameObject[] protectedObjects)
        {
            var protectedSet = new HashSet<GameObject>(protectedObjects);
            foreach (var go in protectedObjects)
            {
                if (go == null)
                    continue;

                var current = go.transform.parent;
                while (current != null)
                {
                    protectedSet.Add(current.gameObject);
                    current = current.parent;
                }
            }

            var destroyRoots = new HashSet<GameObject>();
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null || protectedSet.Contains(go) || !IsLegacyUiObject(go))
                    continue;

                destroyRoots.Add(FindLegacyUiDestroyRoot(go, protectedSet));
            }

            foreach (var root in destroyRoots)
            {
                if (root != null && !protectedSet.Contains(root))
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject FindLegacyUiDestroyRoot(GameObject go, HashSet<GameObject> protectedSet)
        {
            var current = go.transform;
            var result = go.transform;
            while (current.parent != null &&
                   !protectedSet.Contains(current.parent.gameObject) &&
                   IsLegacyUiObject(current.parent.gameObject))
            {
                result = current.parent;
                current = current.parent;
            }

            return result.gameObject;
        }

        private static bool IsLegacyUiObject(GameObject go)
        {
            if (go == null)
                return false;

            if (go.GetComponent<UIDocument>() != null)
                return false;

            if (go.transform is RectTransform)
                return true;

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                    continue;

                var type = component.GetType();
                var fullName = type.FullName ?? string.Empty;
                if (fullName == "UnityEngine.Canvas" ||
                    fullName == "UnityEngine.CanvasGroup" ||
                    fullName == "UnityEngine.CanvasRenderer" ||
                    fullName.StartsWith("UnityEngine.UI.", StringComparison.Ordinal) ||
                    fullName.StartsWith("UnityEngine.EventSystems.", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(string.IsNullOrWhiteSpace(parent) ? "Assets" : parent, folder);
        }
    }
}
#endif
