using System.Reflection;
using Shared.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class RequiredFieldValidator
    {
        static RequiredFieldValidator()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            var roots = scene.GetRootGameObjects();
            var errorCount = 0;

            foreach (var root in roots)
            {
                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null)
                        continue;
                    errorCount += ValidateObject(behaviour);
                }
            }

            if (errorCount > 0)
                Debug.LogError($"[RequiredFieldValidator] Scene '{scene.name}': {errorCount} required field(s) are missing. Check errors above.");
        }

        private static void OnPrefabSaving(GameObject prefab)
        {
            var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            var errorCount = 0;

            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                errorCount += ValidateObject(behaviour);
            }

            if (errorCount > 0)
                Debug.LogError($"[RequiredFieldValidator] Prefab '{prefab.name}': {errorCount} required field(s) are missing. Check errors above.");
        }

        private static int ValidateObject(MonoBehaviour behaviour)
        {
            var errorCount = 0;
            var type = behaviour.GetType();

            while (type != null && type != typeof(MonoBehaviour))
            {
                var fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly
                );

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<RequiredAttribute>() == null)
                        continue;

                    var value = field.GetValue(behaviour);
                    if (value == null || value.Equals(null))
                    {
                        Debug.LogError(
                            $"[Required] '{type.Name}.{field.Name}' is not assigned.",
                            behaviour
                        );
                        errorCount++;
                    }
                }

                type = type.BaseType;
            }

            return errorCount;
        }

        [MenuItem("Tools/Validate Required Fields")]
        public static void ValidateFromMenu()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var errorCount = 0;

            foreach (var root in roots)
            {
                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null)
                        continue;
                    errorCount += ValidateObject(behaviour);
                }
            }

            if (errorCount == 0)
                Debug.Log("[RequiredFieldValidator] All required fields are assigned.");
            else
                Debug.LogError($"[RequiredFieldValidator] {errorCount} required field(s) are missing.");
        }
    }

    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var isMissing = property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue == null;

            if (isMissing)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                EditorGUI.PropertyField(position, property, label);
                GUI.backgroundColor = prev;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
