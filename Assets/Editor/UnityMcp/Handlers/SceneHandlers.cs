#if UNITY_EDITOR
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class SceneHandlers
    {
        static SceneHandlers()
        {
            "GET".Register("/scene/hierarchy", "Full scene hierarchy tree", async (req, res) => await HandleSceneHierarchyAsync(req, res));
            "POST".Register("/scene/open", "Open a scene by path", async (req, res) => await HandleSceneOpenAsync(req, res));
            "POST".Register("/scene/save", "Save current scene", async (req, res) => await HandleSceneSaveAsync(res));
        }

        public static async Task HandleSceneHierarchyAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var maxDepthRaw = request.QueryString["depth"];
            var maxDepth = 10;
            if (!string.IsNullOrEmpty(maxDepthRaw) && int.TryParse(maxDepthRaw, out var d))
                maxDepth = Mathf.Clamp(d, 1, 50);

            var pathFilter = request.QueryString["path"];
            var includeComponentsRaw = request.QueryString["includeComponents"];
            var includeComponents = string.IsNullOrEmpty(includeComponentsRaw)
                || (!string.Equals(includeComponentsRaw, "false", StringComparison.OrdinalIgnoreCase)
                    && includeComponentsRaw != "0");

            var json = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();
                var sb = new StringBuilder();
                sb.Append("{\"sceneName\":\"").Append(EscapeJsonString(scene.name)).Append("\",\"nodes\":[");

                if (!string.IsNullOrEmpty(pathFilter))
                {
                    var target = GameObject.Find(pathFilter);
                    if (target != null)
                        AppendHierarchyNodeJson(sb, target.transform, maxDepth, 0, "", includeComponents);
                }
                else
                {
                    for (var i = 0; i < roots.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        AppendHierarchyNodeJson(sb, roots[i].transform, maxDepth, 0, "", includeComponents);
                    }
                }

                sb.Append("]}");
                return sb.ToString();
            });

            await UnityMcpBridge.WriteRawJsonAsync(response, 200, json);
        }

        public static async Task HandleSceneOpenAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            string scenePath = null;
            var saveCurrentSceneIfDirty = false;
            try
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                var json = JsonUtility.FromJson<SceneOpenRequest>(body);
                scenePath = json?.scenePath;
                saveCurrentSceneIfDirty = json != null && json.saveCurrentSceneIfDirty;
            }
            catch { /* fall through to validation */ }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Missing scenePath" });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (saveCurrentSceneIfDirty)
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (activeScene.IsValid() && activeScene.isDirty)
                    {
                        if (string.IsNullOrWhiteSpace(activeScene.path))
                        {
                            return new GenericResponse { success = false, message = "Current scene has unsaved changes but no path. Save it manually before opening another scene." };
                        }
                        if (!EditorSceneManager.SaveScene(activeScene))
                        {
                            return new GenericResponse { success = false, message = "Failed to save current scene before opening: " + activeScene.path };
                        }
                    }
                    var scene = EditorSceneManager.OpenScene(scenePath);
                    return new GenericResponse { success = true, message = "Scene opened (with auto-save): " + scenePath };
                }
                else
                {
                    var scene = EditorSceneManager.OpenScene(scenePath);
                    return new GenericResponse { success = true, message = "Scene opened: " + scenePath };
                }
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleSceneSaveAsync(HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.path))
                {
                    return new GenericResponse { success = false, message = "No valid scene open to save." };
                }
                if (EditorSceneManager.SaveScene(activeScene))
                {
                    return new GenericResponse { success = true, message = "Scene saved: " + activeScene.path };
                }
                return new GenericResponse { success = false, message = "Failed to save scene: " + activeScene.path };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        private static void AppendHierarchyNodeJson(StringBuilder sb, Transform t, int maxDepth, int currentDepth, string parentPath, bool includeComponents)
        {
            var nodePath = parentPath.Length == 0 ? "/" + t.name : parentPath + "/" + t.name;
            sb.Append("{\"name\":\"").Append(EscapeJsonString(t.name))
              .Append("\",\"path\":\"").Append(EscapeJsonString(nodePath))
              .Append("\",\"activeSelf\":").Append(t.gameObject.activeSelf ? "true" : "false")
              .Append(",\"components\":[");

            if (includeComponents)
            {
                var comps = t.GetComponents<Component>();
                var first = true;
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    if (!first) sb.Append(',');
                    sb.Append('"').Append(EscapeJsonString(c.GetType().Name)).Append('"');
                    first = false;
                }
            }

            sb.Append("],\"childCount\":").Append(t.childCount).Append(",\"children\":[");
            if (currentDepth < maxDepth)
            {
                for (var i = 0; i < t.childCount; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendHierarchyNodeJson(sb, t.GetChild(i), maxDepth, currentDepth + 1, nodePath, includeComponents);
                }
            }
            sb.Append("]}");
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                        {
                            // 제어 문자(0x00~0x1F)는 \uXXXX 형식으로 이스케이프
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
#endif
