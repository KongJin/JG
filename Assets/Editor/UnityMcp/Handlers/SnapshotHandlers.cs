#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Phase 4: 경량 스냅샷 — hierarchy의 경량 서브셋.
    /// [Editor-only] 엔드포인트.
    /// </summary>
    internal static class SnapshotHandlers
    {
        private const int DefaultMaxDepth = 8;

        static SnapshotHandlers()
        {
            "GET".Register("/snapshot/ui", "Lightweight UI hierarchy snapshot", async (req, res) => await HandleSnapshotUiAsync(req, res));
            "GET".Register("/snapshot/components", "Root-level custom component summary", async (req, res) => await HandleSnapshotComponentsAsync(req, res));
            "POST".Register("/snapshot/diff", "Compare two UI snapshots", async (req, res) => await HandleSnapshotDiffAsync(req, res));
        }

        // =====================================================================
        // GET /snapshot/ui
        // =====================================================================

        public static async Task HandleSnapshotUiAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var maxDepthStr = request.QueryString["maxDepth"];
            var canvasPath = request.QueryString["canvasPath"];
            int maxDepth = DefaultMaxDepth;
            if (!string.IsNullOrEmpty(maxDepthStr) && int.TryParse(maxDepthStr, out var d))
                maxDepth = Mathf.Clamp(d, 1, 50);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                Transform rootTransform = null;

                if (!string.IsNullOrEmpty(canvasPath))
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(canvasPath);
                    if (go != null) rootTransform = go.transform;
                }

                // canvasPath가 지정되지 않으면 Canvas 루트를 찾거나 active scene 루트 사용
                if (rootTransform == null)
                {
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null) rootTransform = canvas.transform;
                }

                var nodes = new List<SnapshotUiNode>();
                int interactiveCount = 0;

                if (rootTransform != null)
                {
                    CollectUiNodes(rootTransform, rootTransform, 0, maxDepth, nodes, ref interactiveCount);
                }

                return new SnapshotUiResponse
                {
                    scene = scene.name,
                    canvasPath = rootTransform != null ? McpSharedHelpers.GetTransformPath(rootTransform) : "(not found)",
                    uiNodes = nodes.ToArray(),
                    totalUiNodes = nodes.Count,
                    interactiveElements = interactiveCount
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // GET /snapshot/components
        // =====================================================================

        public static async Task HandleSnapshotComponentsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();
                var infos = new List<RootComponentInfo>();

                foreach (var root in roots)
                {
                    var comps = root.GetComponents<Component>();
                    var customComps = new List<string>();
                    foreach (var c in comps)
                    {
                        if (c != null && !IsUnityBuiltinType(c.GetType()))
                        {
                            customComps.Add(c.GetType().Name);
                        }
                    }

                    if (customComps.Count > 0)
                    {
                        infos.Add(new RootComponentInfo
                        {
                            path = "/" + root.name,
                            name = root.name,
                            customComponents = customComps.ToArray()
                        });
                    }
                }

                return new SnapshotComponentsResponse
                {
                    scene = scene.name,
                    rootComponents = infos.ToArray()
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // POST /snapshot/diff
        // =====================================================================

        public static async Task HandleSnapshotDiffAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            SnapshotDiffRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<SnapshotDiffRequest>(body);
                }
            }

            if (req == null || string.IsNullOrWhiteSpace(req.beforeJson) || string.IsNullOrWhiteSpace(req.afterJson))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "INVALID_SELECTOR",
                    message = "beforeJson and afterJson are required",
                    detail = "Pass the full JSON strings from two /snapshot/ui responses"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var beforePaths = ExtractPathsFromSnapshotJson(req.beforeJson);
                var afterPaths = ExtractPathsFromSnapshotJson(req.afterJson);

                var added = afterPaths.Except(beforePaths).ToList();
                var removed = beforePaths.Except(afterPaths).ToList();
                var changed = new List<string>(); // 단순 path 비교로는 변경 감지 불가, 상세 비교 필요 시 확장

                return new SnapshotDiffResponse
                {
                    added = added.ToArray(),
                    removed = removed.ToArray(),
                    changed = changed.ToArray()
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static void CollectUiNodes(Transform root, Transform current, int depth, int maxDepth, List<SnapshotUiNode> nodes, ref int interactiveCount)
        {
            if (depth > maxDepth) return;

            var go = current.gameObject;
            var comps = go.GetComponents<Component>();
            var views = new List<string>();
            bool isInteractive = false;

            foreach (var c in comps)
            {
                if (c == null) continue;
                var type = c.GetType();
                // Unity 내장 컴포넌트 제외 (Transform, RectTransform, CanvasRenderer 등)
                if (!IsUnityBuiltinType(type))
                {
                    views.Add(type.Name);
                }

                // interactive 체크
                var typeName = type.Name;
                if (typeName == "Button" || typeName == "InputField" || typeName == "Toggle" || typeName == "Slider" || typeName == "Dropdown")
                {
                    isInteractive = true;
                }
            }

            // 최소 1개 이상의 custom view가 있거나 leaf 노드만 추가
            if (views.Count > 0 || current.childCount == 0)
            {
                nodes.Add(new SnapshotUiNode
                {
                    path = GetRelativePath(root, current),
                    name = go.name,
                    activeSelf = go.activeSelf,
                    childCount = current.childCount,
                    views = views.ToArray()
                });
            }

            if (isInteractive) interactiveCount++;

            for (int i = 0; i < current.childCount; i++)
            {
                CollectUiNodes(root, current.GetChild(i), depth + 1, maxDepth, nodes, ref interactiveCount);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            var current = target;
            while (current != null && current != root.parent)
            {
                parts.Insert(0, current.name);
                if (current == root) break;
                current = current.parent;
            }
            return "/" + string.Join("/", parts);
        }

        private static bool IsUnityBuiltinType(Type type)
        {
            if (type == null) return false;
            var ns = type.Namespace ?? "";
            // UnityEngine and UnityEngine.UI built-in components are not project views.
            // 단, MonoBehaviour를 상속한 프로젝트 스크립트는 제외
            if (ns.StartsWith("UnityEngine"))
            {
                // MonoBehaviour를 상속한 커스텀 스크립트는 built-in 아님
                return !typeof(MonoBehaviour).IsAssignableFrom(type);
            }
            return false;
        }

        private static List<string> ExtractPathsFromSnapshotJson(string json)
        {
            var paths = new List<string>();
            // 간단한 파싱: "path":"..." 패턴 추출
            var idx = 0;
            var keyPattern = "\"path\":\"";
            while (idx < json.Length)
            {
                var pos = json.IndexOf(keyPattern, idx, StringComparison.Ordinal);
                if (pos < 0) break;
                var start = pos + keyPattern.Length;
                var end = json.IndexOf("\"", start, StringComparison.Ordinal);
                if (end < 0) break;
                var path = json.Substring(start, end - start);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
                idx = end + 1;
            }
            return paths;
        }
    }
}
#endif
