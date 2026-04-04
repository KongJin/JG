using System.Reflection;
using Shared.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    public static class RequiredFieldAudit
    {
        [MenuItem("Tools/Audit Required Fields In Project")]
        public static void AuditProject()
        {
            var originalScenePath = EditorSceneManager.GetActiveScene().path;
            var totalMissingCount = 0;

            try
            {
                totalMissingCount += AuditScenes();
                totalMissingCount += AuditPrefabs();
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(originalScenePath))
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            if (totalMissingCount == 0)
                Debug.Log("[RequiredAudit] All required fields are assigned across scenes and prefabs.");
            else
                Debug.LogError($"[RequiredAudit] {totalMissingCount} required field(s) are missing across scenes and prefabs.");
        }

        private static int AuditScenes()
        {
            var missingCount = 0;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldAuditScene(scenePath))
                    continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour == null)
                            continue;

                        missingCount += AuditObject(behaviour, scenePath, GetHierarchyPath(behaviour.transform));
                    }
                }
            }

            return missingCount;
        }

        private static int AuditPrefabs()
        {
            var missingCount = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldAuditPrefab(prefabPath))
                    continue;

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour == null)
                            continue;

                        missingCount += AuditObject(behaviour, prefabPath, GetHierarchyPath(behaviour.transform));
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return missingCount;
        }

        private static int AuditObject(MonoBehaviour behaviour, string assetPath, string hierarchyPath)
        {
            var missingCount = 0;
            var type = behaviour.GetType();

            while (type != null && type != typeof(MonoBehaviour))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<RequiredAttribute>() == null)
                        continue;

                    var value = field.GetValue(behaviour);
                    if (value != null && !value.Equals(null))
                        continue;

                    Debug.LogError($"[RequiredAudit] {assetPath}::{hierarchyPath} '{type.Name}.{field.Name}' is not assigned.", behaviour);
                    missingCount++;
                }

                type = type.BaseType;
            }

            return missingCount;
        }

        private static bool ShouldAuditScene(string scenePath)
        {
            return !string.IsNullOrWhiteSpace(scenePath)
                   && scenePath.StartsWith("Assets/Scenes/");
        }

        private static bool ShouldAuditPrefab(string prefabPath)
        {
            return !string.IsNullOrWhiteSpace(prefabPath)
                   && (prefabPath.StartsWith("Assets/Resources/")
                       || prefabPath.StartsWith("Assets/Prefabs/"));
        }

        private static string GetHierarchyPath(Transform current)
        {
            if (current == null)
                return "(null)";

            var path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }

            return path;
        }
    }
}
