#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class ComponentHandlers
    {
        public static async Task HandleComponentAddAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentAddRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.gameObjectPath);
                if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                var comp = McpSharedHelpers.AddComponentByName(go, req.componentType);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new GenericResponse { success = true, message = "Added " + comp.GetType().Name + " to " + req.gameObjectPath };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentSetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentSetRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.gameObjectPath);
                if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                Component comp = null;
                if (!string.IsNullOrEmpty(req.componentType))
                {
                    var components = go.GetComponents<Component>();
                    comp = components.FirstOrDefault(c => c != null && (c.GetType().Name.Equals(req.componentType, StringComparison.OrdinalIgnoreCase) || c.GetType().FullName.Equals(req.componentType, StringComparison.OrdinalIgnoreCase)));
                }
                if (comp == null) throw new Exception("Component not found: " + req.componentType + " on " + req.gameObjectPath);
                var so = new SerializedObject(comp);
                var sp = so.FindProperty(req.propertyName);
                if (sp == null) throw new Exception("Property not found: " + req.propertyName + " on " + req.componentType);
                Undo.RecordObject(comp, "MCP Set " + req.propertyName);
                McpSharedHelpers.SetSerializedPropertyValue(sp, req.value, req.assetPath);
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new GenericResponse { success = true, message = "Set " + req.componentType + "." + req.propertyName + " = " + req.value };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentGetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentGetRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.gameObjectPath);
                if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                Component comp = null;
                var components = go.GetComponents<Component>();
                comp = components.FirstOrDefault(c => c != null && (c.GetType().Name.Equals(req.componentType, StringComparison.OrdinalIgnoreCase) || c.GetType().FullName.Equals(req.componentType, StringComparison.OrdinalIgnoreCase)));
                if (comp == null) throw new Exception("Component not found: " + req.componentType + " on " + req.gameObjectPath);
                HashSet<string> propertyNameFilter = null;
                if (req.propertyNames != null && req.propertyNames.Length > 0) propertyNameFilter = new HashSet<string>(req.propertyNames, StringComparer.Ordinal);
                var props = McpSharedHelpers.CollectSerializedPropertiesForComponent(comp, propertyNameFilter);
                return new ComponentGetResponse { gameObjectPath = req.gameObjectPath, componentType = comp.GetType().Name, properties = props };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentSetSerializedFieldAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentSetSerializedFieldRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.gameObjectPath);
                if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                var comp = go.GetComponent(req.componentType);
                if (comp == null)
                {
                    var components = go.GetComponents<Component>();
                    comp = components.FirstOrDefault(c => c != null && c.GetType().Name.Contains(req.componentType.Split('.')[^1]));
                }
                if (comp == null) throw new Exception("Component " + req.componentType + " not found on " + req.gameObjectPath);
                var so = new SerializedObject(comp);
                var sp = so.FindProperty(req.fieldName);
                if (sp == null) throw new Exception("Field " + req.fieldName + " not found on " + req.componentType);
                Undo.RecordObject(comp, "MCP SetSerializedField " + req.fieldName);
                McpSharedHelpers.SetSerializedPropertyValue(sp, req.value, req.assetReferencePath);
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new ComponentSetSerializedFieldResponse { success = true, message = "Set " + req.fieldName + " on " + req.componentType };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentAutoConnectFieldsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentAutoConnectFieldsRequest>(body);
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.componentPath);
                if (go == null) throw new Exception("Component GameObject not found: " + req.componentPath);
                Component component = null;
                if (!string.IsNullOrEmpty(req.componentTypeName))
                {
                    var type = Type.GetType(req.componentTypeName);
                    if (type != null) component = go.GetComponent(type);
                }
                if (component == null)
                {
                    var components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp != null && comp.GetType().Name.Contains(req.componentTypeName.Split('.')[^1])) { component = comp; break; }
                    }
                }
                if (component == null) throw new Exception("Component not found on " + req.componentPath);
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                var connectedFields = new List<string>();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.name.StartsWith("m_"))
                    {
                        var targetGo = McpSharedHelpers.FindGameObjectByFieldName(go, property.name, req.searchScope);
                        if (targetGo != null) { property.objectReferenceValue = targetGo; connectedFields.Add(property.name); }
                    }
                }
                serializedObject.ApplyModifiedProperties();
                if (connectedFields.Count > 0) { Undo.RecordObject(component, "MCP Auto Connect Fields"); EditorSceneManager.MarkSceneDirty(go.scene); }
                return new ComponentAutoConnectFieldsResponse { success = true, message = "Auto-connected " + connectedFields.Count + " fields on " + component.GetType().Name, componentPath = McpSharedHelpers.GetTransformPath(go.transform), connectedCount = connectedFields.Count, connectedFields = connectedFields.ToArray() };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }
    }
}
#endif
