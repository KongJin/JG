#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Phase 1: Locator 시스템 — selector 기반 GameObject 탐색.
    /// [Editor-only] 엔드포인트.
    /// </summary>
    internal static class LocatorHandlers
    {
        static LocatorHandlers()
        {
            "POST".Register("/locator/find", "Find GameObject by selector (component:/name:/path:)", async (req, res) => await HandleLocatorFindAsync(req, res));
            "POST".Register("/locator/find-all", "Find all matching GameObjects by selector", async (req, res) => await HandleLocatorFindAllAsync(req, res));
            "GET".Register("/locator/count", "Count matching GameObjects", async (req, res) => await HandleLocatorCountAsync(req, res));
        }

        // =====================================================================
        // POST /locator/find
        // =====================================================================

        public static async Task HandleLocatorFindAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            LocatorRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<LocatorRequest>(body);
                }
            }

            if (req == null || string.IsNullOrWhiteSpace(req.selector))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "INVALID_SELECTOR",
                    message = "selector is required",
                    detail = "Expected format: component:Button, name:SaveButton, or path:/Canvas/SaveButton"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var matches = McpSharedHelpers.FindBySelector(req.selector, req.scope, req.activeOnly);
                var first = matches.Count > 0 ? matches[0] : null;
                return new LocatorResponse
                {
                    found = first != null,
                    count = matches.Count,
                    items = first != null ? new[] { BuildLocatorItem(first) } : new LocatorItem[0]
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // POST /locator/find-all
        // =====================================================================

        public static async Task HandleLocatorFindAllAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            LocatorRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<LocatorRequest>(body);
                }
            }

            if (req == null || string.IsNullOrWhiteSpace(req.selector))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "INVALID_SELECTOR",
                    message = "selector is required",
                    detail = "Expected format: component:Button, name:SaveButton, or path:/Canvas/SaveButton"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var matches = McpSharedHelpers.FindBySelector(req.selector, req.scope, req.activeOnly);
                var items = matches.Select(BuildLocatorItem).ToArray();
                return new LocatorResponse
                {
                    found = items.Length > 0,
                    count = items.Length,
                    items = items
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // GET /locator/count
        // =====================================================================

        public static async Task HandleLocatorCountAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var selector = request.QueryString["selector"];
            var scope = request.QueryString["scope"];
            var activeOnlyStr = request.QueryString["activeOnly"];
            bool activeOnly = string.IsNullOrEmpty(activeOnlyStr) || activeOnlyStr != "false";

            if (string.IsNullOrWhiteSpace(selector))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "INVALID_SELECTOR",
                    message = "selector query parameter is required",
                    detail = "Example: ?selector=component:Button"
                });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var matches = McpSharedHelpers.FindBySelector(selector, scope, activeOnly);
                return new LocatorCountResponse { count = matches.Count };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static LocatorItem BuildLocatorItem(GameObject go)
        {
            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
            {
                if (c != null) compNames.Add(c.GetType().Name);
            }

            return new LocatorItem
            {
                path = McpSharedHelpers.GetTransformPath(go.transform),
                name = go.name,
                activeSelf = go.activeSelf,
                activeInHierarchy = go.activeInHierarchy,
                components = compNames.ToArray()
            };
        }
    }
}
#endif
