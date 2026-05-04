#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class LobbySceneHierarchyOrganizer
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";

        [MenuItem("Tools/Scene/Organize Lobby Scene Hierarchy")]
        private static void Organize()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var runtime = Require(scene, "LobbyRuntime");
            var previewRig = EnsureRoot(scene, "GaragePreviewRig");
            var previewTemplates = EnsureRoot(scene, "GaragePreviewTemplates");

            Parent(scene, "LobbyRuntimeUiState", runtime);

            Parent(scene, "LobbyPreviewCamera", previewRig);
            Parent(scene, "GarageSetBPreviewCamera", previewRig);
            Parent(scene, "GarageSetBPartPreviewCamera", previewRig);
            Parent(scene, "GaragePreviewKeyLight", previewRig);

            Parent(scene, "PreviewFrameTemplate", previewTemplates);
            Parent(scene, "PreviewWeaponTemplate", previewTemplates);
            Parent(scene, "PreviewThrusterTemplate", previewTemplates);

            SetRootOrder(scene, "LobbyRuntime", 0);
            SetRootOrder(scene, "LobbyUitkDocument", 1);
            SetRootOrder(scene, "GarageSetBUitkDocument", 2);
            SetRootOrder(scene, "GaragePreviewRig", 3);
            SetRootOrder(scene, "GaragePreviewTemplates", 4);
            SetRootOrder(scene, "SoundPlayer", 5);

            SetChildOrder(runtime, "LobbySetup", 0);
            SetChildOrder(runtime, "AccountSetup", 1);
            SetChildOrder(runtime, "UnitSetup", 2);
            SetChildOrder(runtime, "GarageSetup", 3);
            SetChildOrder(runtime, "LobbyPhotonAdapter", 4);
            SetChildOrder(runtime, "GarageNetworkAdapter", 5);
            SetChildOrder(runtime, "LobbyRuntimeUiState", 6);

            SetChildOrder(previewRig, "LobbyPreviewCamera", 0);
            SetChildOrder(previewRig, "GarageSetBPreviewCamera", 1);
            SetChildOrder(previewRig, "GarageSetBPartPreviewCamera", 2);
            SetChildOrder(previewRig, "GaragePreviewKeyLight", 3);

            SetChildOrder(previewTemplates, "PreviewFrameTemplate", 0);
            SetChildOrder(previewTemplates, "PreviewWeaponTemplate", 1);
            SetChildOrder(previewTemplates, "PreviewThrusterTemplate", 2);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save LobbyScene after hierarchy organization.");

            Debug.Log("[LobbySceneHierarchyOrganizer] LobbyScene hierarchy organized.");
        }

        private static Transform EnsureRoot(Scene scene, string name)
        {
            var existing = FindUnique(scene, name);
            if (existing != null)
            {
                existing.SetParent(null, true);
                return existing;
            }

            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject.transform;
        }

        private static Transform Require(Scene scene, string name)
        {
            var transform = FindUnique(scene, name);
            if (transform == null)
                throw new InvalidOperationException($"LobbyScene object not found: {name}");

            return transform;
        }

        private static void Parent(Scene scene, string childName, Transform parent)
        {
            var child = Require(scene, childName);
            child.SetParent(parent, true);
        }

        private static Transform FindUnique(Scene scene, string name)
        {
            var matches = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
                CollectByName(root.transform, name, matches);

            if (matches.Count > 1)
                throw new InvalidOperationException($"LobbyScene has duplicate objects named {name}.");

            return matches.Count == 0 ? null : matches[0];
        }

        private static void CollectByName(Transform current, string name, List<Transform> matches)
        {
            if (current.name == name)
                matches.Add(current);

            for (int i = 0; i < current.childCount; i++)
                CollectByName(current.GetChild(i), name, matches);
        }

        private static void SetRootOrder(Scene scene, string name, int index)
        {
            var transform = Require(scene, name);
            if (transform.parent != null)
                throw new InvalidOperationException($"{name} is expected to be a root object.");

            transform.SetSiblingIndex(index);
        }

        private static void SetChildOrder(Transform parent, string childName, int index)
        {
            var child = parent.Find(childName);
            if (child == null)
                throw new InvalidOperationException($"{childName} is expected under {parent.name}.");

            child.SetSiblingIndex(index);
        }
    }
}
#endif
