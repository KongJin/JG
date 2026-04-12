#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class GameObjectHandlers
    {
        static GameObjectHandlers()
        {
            "POST".Register("/gameobject/find", "Find GameObject by path or name", async (req, res) => await HandleGameObjectFindAsync(req, res));
            "POST".Register("/gameobject/create", "Create new GameObject", async (req, res) => await HandleGameObjectCreateAsync(req, res));
            "POST".Register("/gameobject/create-primitive", "Create primitive (cube/sphere/etc)", async (req, res) => await HandleGameObjectCreatePrimitiveAsync(req, res));
            "POST".Register("/gameobject/destroy", "Destroy GameObject", async (req, res) => await HandleGameObjectDestroyAsync(req, res));
            "POST".Register("/gameobject/set-active", "Set GameObject active state", async (req, res) => await HandleGameObjectSetActiveAsync(req, res));
            "POST".Register("/gameobject/set-sibling", "Set sibling order", async (req, res) => await HandleGameObjectSetSiblingAsync(req, res));
        }

        public static async Task HandleGameObjectFindAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                GameObject go = null;
                if (!string.IsNullOrEmpty(req.path))
                {
                    go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) go = GameObject.Find(req.path);
                }
                else if (!string.IsNullOrEmpty(req.name))
                {
                    go = McpSharedHelpers.FindGameObjectByNameInActiveScene(req.name);
                }

                if (go == null) return new GameObjectResponse { found = false };
                return BuildGameObjectResponse(go, req.lightweight, req.componentFilter);
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectCreateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CreateRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                Transform parent = null;
                if (!string.IsNullOrEmpty(req.parent))
                {
                    var parentGo = McpSharedHelpers.FindGameObjectByPath(req.parent);
                    if (parentGo == null) parentGo = GameObject.Find(req.parent);
                    if (parentGo == null) throw new Exception("Parent not found: " + req.parent);
                    parent = parentGo.transform;
                }

                var go = new GameObject(string.IsNullOrEmpty(req.name) ? "New GameObject" : req.name);
                if (parent != null) go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "MCP Create " + go.name);

                if (req.components != null)
                    foreach (var compName in req.components) McpSharedHelpers.AddComponentByName(go, compName);

                if (!string.IsNullOrEmpty(req.uiPreset)) ApplyUiPreset(go, req.uiPreset, req.width, req.height);
                EditorSceneManager.MarkSceneDirty(go.scene);

                return new CreateResponse { name = go.name, path = McpSharedHelpers.GetTransformPath(go.transform), instanceId = go.GetInstanceID() };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectCreatePrimitiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CreatePrimitiveRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                PrimitiveType primitiveType;
                switch ((req.primitiveType ?? "").ToLowerInvariant())
                {
                    case "sphere": primitiveType = PrimitiveType.Sphere; break;
                    case "capsule": primitiveType = PrimitiveType.Capsule; break;
                    case "cylinder": primitiveType = PrimitiveType.Cylinder; break;
                    case "cube": primitiveType = PrimitiveType.Cube; break;
                    case "plane": primitiveType = PrimitiveType.Plane; break;
                    case "quad": primitiveType = PrimitiveType.Quad; break;
                    default: throw new Exception("Unknown primitive type: " + req.primitiveType);
                }

                var go = GameObject.CreatePrimitive(primitiveType);
                if (!string.IsNullOrEmpty(req.name)) go.name = req.name;
                Undo.RegisterCreatedObjectUndo(go, "MCP CreatePrimitive " + go.name);

                if (req.components != null)
                    foreach (var compName in req.components) McpSharedHelpers.AddComponentByName(go, compName);

                EditorSceneManager.MarkSceneDirty(go.scene);
                return new CreateResponse { name = go.name, path = McpSharedHelpers.GetTransformPath(go.transform), instanceId = go.GetInstanceID() };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectDestroyAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<FindRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var path = !string.IsNullOrEmpty(req.path) ? req.path : req.name;
                var go = GameObject.Find(path);
                if (go == null) throw new Exception("GameObject not found: " + path);
                Undo.DestroyObjectImmediate(go);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                return new GenericResponse { success = true, message = "Destroyed: " + path };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetActiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetActiveRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.path);
                if (go == null) throw new Exception("GameObject not found: " + req.path);
                Undo.RecordObject(go, "MCP SetActive " + req.path);
                go.SetActive(req.active);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new GenericResponse { success = true, message = "SetActive(" + req.active + "): " + req.path };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleGameObjectSetSiblingAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<GameObjectSetSiblingRequest>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var go = GameObject.Find(req.path);
                if (go == null) throw new Exception("GameObject not found: " + req.path);

                if (req.siblingIndex >= 0) go.transform.SetSiblingIndex(req.siblingIndex);
                EditorSceneManager.MarkSceneDirty(go.scene);

                return new GameObjectSetSiblingResponse
                {
                    success = true,
                    message = "Set sibling: " + req.path,
                    path = McpSharedHelpers.GetTransformPath(go.transform),
                    siblingIndex = go.transform.GetSiblingIndex()
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static GameObjectResponse BuildGameObjectResponse(GameObject go, bool lightweight, string[] componentFilter)
        {
            var filterActive = componentFilter != null && componentFilter.Length > 0;
            var components = go.GetComponents<Component>();
            var compInfos = new List<ComponentInfo>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                var info = new ComponentInfo { typeName = type.Name, fullTypeName = type.FullName };
                if (!lightweight && !filterActive) info.properties = McpSharedHelpers.CollectSerializedPropertiesForComponent(comp);
                else info.properties = new PropertyInfo[0];
                if (filterActive && !McpSharedHelpers.MatchesComponentTypeFilter(type, componentFilter)) continue;
                compInfos.Add(info);
            }

            return new GameObjectResponse
            {
                found = true, name = go.name, path = McpSharedHelpers.GetTransformPath(go.transform),
                activeSelf = go.activeSelf, layer = LayerMask.LayerToName(go.layer),
                tag = go.tag, components = compInfos.ToArray()
            };
        }

        private static void ApplyUiPreset(GameObject go, string preset, float? width, float? height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;
            float w = width ?? 160f;
            float h = height ?? 30f;
            switch ((preset ?? "").ToLowerInvariant())
            {
                case "button-bottom-center": rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = new Vector2(0f, 30f); rect.sizeDelta = new Vector2(w, h); break;
                case "button-top-center": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -30f); rect.sizeDelta = new Vector2(w, h); break;
                case "panel-top-center": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -120f); rect.sizeDelta = new Vector2(w, h); break;
                case "panel-bottom-center": rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = new Vector2(0f, 80f); rect.sizeDelta = new Vector2(w, h); break;
                case "stretch-parent": rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero; break;
                case "center-fixed": rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h); break;
                case "raw-image-top": rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -200f); rect.sizeDelta = new Vector2(w, h); break;
                default: rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h); break;
            }
        }
    }
}
#endif
