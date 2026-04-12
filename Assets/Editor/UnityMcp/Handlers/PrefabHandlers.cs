#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class PrefabHandlers
    {
        public static async Task HandlePrefabSaveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<PrefabSaveRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (string.IsNullOrEmpty(req.gameObjectPath)) throw new Exception("gameObjectPath is required");
                if (string.IsNullOrEmpty(req.savePath)) throw new Exception("savePath is required");
                var go = GameObject.Find(req.gameObjectPath);
                if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                var directory = Path.GetDirectoryName(req.savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, req.savePath, out var success);
                if (!success || prefab == null) throw new Exception("Failed to save prefab at: " + req.savePath);
                if (req.destroySceneObject) Undo.DestroyObjectImmediate(go);
                return new GenericResponse { success = true, message = "Prefab saved: " + req.savePath };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandlePrefabGetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<PrefabGetRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var target = ResolvePrefabTarget(req.assetPath, req.childPath);
                return BuildGameObjectResponse(target, req.lightweight, req.componentFilter);
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandlePrefabSetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<PrefabSetRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var target = ResolvePrefabTarget(req.assetPath, req.childPath);
                Component comp = null;
                if (!string.IsNullOrEmpty(req.componentType))
                {
                    var components = target.GetComponents<Component>();
                    comp = components.FirstOrDefault(c => c != null && (c.GetType().Name.Equals(req.componentType, StringComparison.OrdinalIgnoreCase) || c.GetType().FullName.Equals(req.componentType, StringComparison.OrdinalIgnoreCase)));
                }
                if (comp == null) throw new Exception("Component not found: " + req.componentType + " on prefab " + req.assetPath);
                var so = new SerializedObject(comp);
                var sp = so.FindProperty(req.propertyName);
                if (sp == null) throw new Exception("Property not found: " + req.propertyName + " on " + req.componentType);
                if (!string.IsNullOrEmpty(req.autoWireType))
                {
                    if (sp.propertyType != SerializedPropertyType.ObjectReference) throw new Exception("autoWireType only works on ObjectReference properties, but " + req.propertyName + " is " + sp.propertyType);
                    var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(req.assetPath);
                    var wireType = UnityMcpBridge.ResolveTypeByFullName(req.autoWireType);
                    if (wireType == null) throw new Exception("Type not found for auto-wire: " + req.autoWireType);
                    var found = prefabRoot.GetComponentInChildren(wireType, true);
                    if (found == null) throw new Exception("No " + req.autoWireType + " found in prefab hierarchy for auto-wire");
                    sp.objectReferenceValue = found;
                }
                else
                {
                    ComponentHandlers.SetSerializedPropertyValue(sp, req.value, req.assetReferencePath);
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
                AssetDatabase.SaveAssets();
                return new GenericResponse { success = true, message = "Set " + req.componentType + "." + req.propertyName + " on prefab " + req.assetPath };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandlePrefabAddComponentAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<PrefabAddComponentRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var target = ResolvePrefabTarget(req.assetPath, req.childPath);
                var compType = UnityMcpBridge.ResolveTypeByFullName(req.componentType);
                if (compType == null || !typeof(Component).IsAssignableFrom(compType)) throw new Exception("Component type not found: " + req.componentType);
                var added = target.AddComponent(compType);
                if (added == null) throw new Exception("Failed to add " + req.componentType + " to prefab");
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                return new GenericResponse { success = true, message = "Added " + req.componentType + " to prefab " + req.assetPath };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static GameObject ResolvePrefabTarget(string assetPath, string childPath)
        {
            if (string.IsNullOrEmpty(assetPath)) throw new Exception("assetPath is required");
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabRoot == null) throw new Exception("Prefab not found at: " + assetPath);
            if (string.IsNullOrEmpty(childPath)) return prefabRoot;
            var childTransform = prefabRoot.transform.Find(childPath);
            if (childTransform == null) throw new Exception("Child not found in prefab: " + childPath + " (prefab: " + assetPath + ")");
            return childTransform.gameObject;
        }

        private static GameObjectResponse BuildGameObjectResponse(GameObject go, bool lightweight, string[] componentFilter)
        {
            var filterActive = componentFilter != null && componentFilter.Length > 0;
            var components = go.GetComponents<Component>();
            var compInfos = new System.Collections.Generic.List<ComponentInfo>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                var info = new ComponentInfo { typeName = type.Name, fullTypeName = type.FullName };
                if (!lightweight && !filterActive) info.properties = CollectSerializedPropertiesForComponent(comp);
                else info.properties = new PropertyInfo[0];
                if (filterActive && !MatchesComponentTypeFilter(type, componentFilter)) continue;
                compInfos.Add(info);
            }
            return new GameObjectResponse
            {
                found = true, name = go.name, path = GetTransformPath(go.transform),
                activeSelf = go.activeSelf, layer = LayerMask.LayerToName(go.layer),
                tag = go.tag, components = compInfos.ToArray()
            };
        }

        private static bool MatchesComponentTypeFilter(Type type, string[] filters)
        {
            if (filters == null || filters.Length == 0) return false;
            foreach (var f in filters)
            {
                if (string.IsNullOrEmpty(f)) continue;
                if (type.Name.Equals(f, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(type.FullName) && type.FullName.Equals(f, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string GetTransformPath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            var current = t;
            while (current != null) { parts.Insert(0, current.name); current = current.parent; }
            return "/" + string.Join("/", parts);
        }

        private static PropertyInfo[] CollectSerializedPropertiesForComponent(Component comp)
        {
            var so = new SerializedObject(comp);
            var props = new System.Collections.Generic.List<PropertyInfo>();
            var sp = so.GetIterator();
            if (sp.NextVisible(true))
            {
                do
                {
                    if (sp.name == "m_Script") continue;
                    props.Add(new PropertyInfo { name = sp.name, type = sp.propertyType.ToString(), value = ComponentHandlers.GetSerializedPropertyValue(sp) });
                } while (sp.NextVisible(false));
            }
            return props.ToArray();
        }
    }
}
#endif
