#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class ComponentHandlers
    {
        static ComponentHandlers()
        {
            "POST".Register("/component/add", "Add component to GameObject", async (req, res) => await HandleComponentAddAsync(req, res));
            "POST".Register("/component/set", "Set component property value", async (req, res) => await HandleComponentSetAsync(req, res));
            "POST".Register("/component/get", "Get component serialized properties", async (req, res) => await HandleComponentGetAsync(req, res));
            "POST".Register("/component/set-serialized-field", "Set a serialized field directly", async (req, res) => await HandleComponentSetSerializedFieldAsync(req, res));
            "POST".Register("/component/auto-connect-fields", "Auto-wire null object references by field name", async (req, res) => await HandleComponentAutoConnectFieldsAsync(req, res));
        }

        public static async Task HandleComponentAddAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentAddRequest>(body);

            string addedMessage = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = GameObject.Find(req.gameObjectPath);
                    if (go == null) throw new Exception("GameObject not found: " + req.gameObjectPath);
                    var comp = McpSharedHelpers.AddComponentByName(go, req.componentType);
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    addedMessage = "Added " + comp.GetType().Name + " to " + req.gameObjectPath;
                    return true;
                }, "Add Component: " + req.componentType + " to " + req.gameObjectPath, out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new GenericResponse
            {
                success = true,
                message = addedMessage,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentSetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentSetRequest>(body);

            string setMessage = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
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
                    setMessage = "Set " + req.componentType + "." + req.propertyName + " = " + req.value;
                    return true;
                }, "Set Component Property: " + req.componentType + "." + req.propertyName, out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new GenericResponse
            {
                success = true,
                message = setMessage,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
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

            string setFieldMessage = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
                {
                    var go = GameObject.Find(req.componentPath);
                    if (go == null) throw new Exception("GameObject not found: " + req.componentPath);
                    var comp = go.GetComponent(req.componentTypeName);
                    if (comp == null)
                    {
                        var components = go.GetComponents<Component>();
                        var shortName = ExtractShortTypeName(req.componentTypeName);
                        comp = components.FirstOrDefault(c => c != null && c.GetType().Name.Contains(shortName));
                    }
                    if (comp == null) throw new Exception("Component " + req.componentTypeName + " not found on " + req.componentPath);
                    var so = new SerializedObject(comp);
                    var sp = so.FindProperty(req.fieldName);
                    if (sp == null) throw new Exception("Field " + req.fieldName + " not found on " + req.componentTypeName);
                    var targetGo = McpSharedHelpers.FindGameObjectByPath(req.targetPath);
                    if (targetGo == null) throw new Exception("Target GameObject not found: " + req.targetPath);
                    Undo.RecordObject(comp, "MCP SetSerializedField " + req.fieldName);
                    sp.objectReferenceValue = targetGo;
                    so.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    setFieldMessage = "Set " + req.fieldName + " on " + req.componentTypeName;
                    return true;
                }, "Set Serialized Field: " + req.fieldName + " on " + req.componentTypeName, out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new ComponentSetSerializedFieldResponse
            {
                success = true,
                message = setFieldMessage,
                componentPath = req.componentPath,
                fieldName = req.fieldName,
                targetPath = req.targetPath,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && req.autoSave && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleComponentAutoConnectFieldsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<ComponentAutoConnectFieldsRequest>(body);

            string connectMessage = null;
            int connectedCount = 0;
            string[] connectedFields = null;
            string componentPath = null;
            int enqueueResult = 0;
            string enqueueError = null;

            await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                enqueueResult = SceneChangeQueue.EnqueueOrExecute(() =>
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
                        var shortName = ExtractShortTypeName(req.componentTypeName);
                        foreach (var comp in components)
                        {
                            if (comp != null && comp.GetType().Name.Contains(shortName)) { component = comp; break; }
                        }
                    }
                    if (component == null) throw new Exception("Component not found on " + req.componentPath);
                    var serializedObject = new SerializedObject(component);
                    var property = serializedObject.GetIterator();
                    var connectedList = new List<string>();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.name.StartsWith("m_"))
                        {
                            var targetGo = McpSharedHelpers.FindGameObjectByFieldName(go, property.name, req.searchScope);
                            if (targetGo != null) { property.objectReferenceValue = targetGo; connectedList.Add(property.name); }
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                    if (connectedList.Count > 0) { Undo.RecordObject(component, "MCP Auto Connect Fields"); EditorSceneManager.MarkSceneDirty(go.scene); }
                    connectedCount = connectedList.Count;
                    connectedFields = connectedList.ToArray();
                    componentPath = McpSharedHelpers.GetTransformPath(go.transform);
                    connectMessage = "Auto-connected " + connectedCount + " fields on " + component.GetType().Name;
                    return true;
                }, "Auto Connect Fields: " + req.componentPath, out enqueueError);
            });

            if (enqueueResult == -1)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Scene change failed",
                    detail = enqueueError,
                    hint = UnityMcpBridge.GetErrorHintFromMessage(enqueueError)
                });
                return;
            }

            var wasQueued = enqueueResult == 0;
            var result = new ComponentAutoConnectFieldsResponse
            {
                success = true,
                message = connectMessage,
                componentPath = componentPath,
                connectedCount = connectedCount,
                connectedFields = connectedFields,
                queued = wasQueued,
                pendingCount = wasQueued ? SceneChangeQueue.PendingCount : 0,
                autoSaved = !wasQueued && McpSharedHelpers.TryAutoSave()
            };
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        private static string ExtractShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return string.Empty;
            var lastDot = fullTypeName.LastIndexOf('.');
            return lastDot >= 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;
        }
    }
}
#endif
