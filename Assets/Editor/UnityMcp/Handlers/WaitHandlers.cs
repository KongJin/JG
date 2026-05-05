#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// Phase 2: Auto-wait — 조건 충족 시 자동 대기.
    /// 모든 Unity API 호출은 RunOnMainThreadAsync 안에서만 실행.
    /// [Editor-only] 엔드포인트.
    /// </summary>
    internal static class WaitHandlers
    {
        private const int DefaultTimeoutMs = 10000;
        private const int DefaultPollIntervalMs = 100;

        static WaitHandlers()
        {
            "POST".Register("/wait/for-locator", "Wait until selector finds a GameObject", async (req, res) => await HandleWaitForLocatorAsync(req, res));
            "POST".Register("/wait/for-active", "Wait until GameObject becomes active", async (req, res) => await HandleWaitForActiveAsync(req, res));
            "POST".Register("/wait/for-component", "Wait until GameObject has a specific component", async (req, res) => await HandleWaitForComponentAsync(req, res));
            "POST".Register("/wait/for-scene", "Wait until a scene is loaded", async (req, res) => await HandleWaitForSceneAsync(req, res));
            "POST".Register("/wait/for-inactive", "Wait until GameObject becomes inactive", async (req, res) => await HandleWaitForInactiveAsync(req, res));
        }

        // =====================================================================
        // POST /wait/for-locator
        // =====================================================================

        public static async Task HandleWaitForLocatorAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            WaitRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                    req = JsonUtility.FromJson<WaitRequest>(body);
            }

            if (req == null || string.IsNullOrWhiteSpace(req.selector))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "INVALID_SELECTOR",
                    message = "selector is required",
                    detail = "Expected: { selector: \"component:Button\", timeoutMs: 10000 }"
                });
                return;
            }

            var timeoutMs = req.timeoutMs > 0 ? req.timeoutMs : DefaultTimeoutMs;
            var pollIntervalMs = req.pollIntervalMs > 0 ? req.pollIntervalMs : DefaultPollIntervalMs;
            var scope = req.scope ?? "";

            var result = await WaitForConditionAsync(
                () =>
                {
                    var matches = McpSharedHelpers.FindBySelector(req.selector, scope, false);
                    return matches.Count > 0 ? matches[0] : null;
                },
                timeoutMs, pollIntervalMs, "locator-found");

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 504, result);
        }

        // =====================================================================
        // POST /wait/for-active
        // =====================================================================

        public static async Task HandleWaitForActiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            WaitRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                    req = JsonUtility.FromJson<WaitRequest>(body);
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path is required",
                    detail = "Expected: { path: \"/GarageSetBUitkDocument\", timeoutMs: 10000 }"
                });
                return;
            }

            var timeoutMs = req.timeoutMs > 0 ? req.timeoutMs : DefaultTimeoutMs;
            var pollIntervalMs = req.pollIntervalMs > 0 ? req.pollIntervalMs : DefaultPollIntervalMs;

            var result = await WaitForConditionAsync(
                () =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    return (go != null && go.activeInHierarchy) ? go : null;
                },
                timeoutMs, pollIntervalMs, "active");

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 504, result);
        }

        // =====================================================================
        // POST /wait/for-component
        // =====================================================================

        public static async Task HandleWaitForComponentAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            WaitRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                    req = JsonUtility.FromJson<WaitRequest>(body);
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path) || string.IsNullOrWhiteSpace(req.componentType))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path and componentType are required",
                    detail = "Expected: { path: \"/GarageSetBUitkDocument\", componentType: \"UIDocument\" }"
                });
                return;
            }

            var timeoutMs = req.timeoutMs > 0 ? req.timeoutMs : DefaultTimeoutMs;
            var pollIntervalMs = req.pollIntervalMs > 0 ? req.pollIntervalMs : DefaultPollIntervalMs;
            var componentType = req.componentType;

            var result = await WaitForConditionAsync(
                () =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    if (go == null) return null;
                    var comps = go.GetComponents<Component>();
                    foreach (var c in comps)
                    {
                        if (c != null && c.GetType().Name.Equals(componentType, StringComparison.OrdinalIgnoreCase))
                            return go;
                    }
                    return null;
                },
                timeoutMs, pollIntervalMs, "component-attached");

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 504, result);
        }

        // =====================================================================
        // POST /wait/for-scene
        // =====================================================================

        public static async Task HandleWaitForSceneAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            WaitRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                    req = JsonUtility.FromJson<WaitRequest>(body);
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path is required (use path as scene name)",
                    detail = "Expected: { path: \"BattleScene\", timeoutMs: 10000 }"
                });
                return;
            }

            var timeoutMs = req.timeoutMs > 0 ? req.timeoutMs : DefaultTimeoutMs;
            var pollIntervalMs = req.pollIntervalMs > 0 ? req.pollIntervalMs : DefaultPollIntervalMs;
            var sceneName = req.path;

            var result = await WaitForConditionAsync(
                () =>
                {
                    var sceneCount = SceneManager.sceneCount;
                    for (int i = 0; i < sceneCount; i++)
                    {
                        var s = SceneManager.GetSceneAt(i);
                        if (s.isLoaded && s.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                            return new GameObject("__scene_loaded__");
                    }
                    return null;
                },
                timeoutMs, pollIntervalMs, "scene-loaded");

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 504, result);
        }

        // =====================================================================
        // POST /wait/for-inactive
        // =====================================================================

        public static async Task HandleWaitForInactiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            WaitRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                    req = JsonUtility.FromJson<WaitRequest>(body);
            }

            if (req == null || string.IsNullOrWhiteSpace(req.path))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponseEnvelope
                {
                    code = "NOT_FOUND",
                    message = "path is required",
                    detail = "Expected: { path: \"/GarageSetBUitkDocument\", timeoutMs: 10000 }"
                });
                return;
            }

            var timeoutMs = req.timeoutMs > 0 ? req.timeoutMs : DefaultTimeoutMs;
            var pollIntervalMs = req.pollIntervalMs > 0 ? req.pollIntervalMs : DefaultPollIntervalMs;

            var result = await WaitForConditionAsync(
                () =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(req.path);
                    return (go != null && !go.activeInHierarchy) ? go : null;
                },
                timeoutMs, pollIntervalMs, "inactive");

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 504, result);
        }

        // =====================================================================
        // Core Polling Loop — 모든 Unity API는 반드시 main thread에서 호출
        // =====================================================================

        private static async Task<WaitResponse> WaitForConditionAsync(Func<GameObject> condition, int timeoutMs, int pollIntervalMs, string conditionName)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var startTime = DateTime.UtcNow;

            // condition은 반드시 UnityMcpBridge.RunOnMainThreadAsync를 통해 호출된 결과여야 함
            // 이 함수 자체는 condition이 이미 main thread를 거친 GameObject를 반환한다고 가정
            GameObject found = null;

            while (DateTime.UtcNow < deadline)
            {
                found = condition();
                if (found != null) break;
                await Task.Delay(pollIntervalMs);
            }

            var waitedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (found != null)
            {
                // GameObject의 정보를 추출하는 것도 main thread에서
                var itemResult = await ExtractLocatorItemSafe(found);
                return new WaitResponse
                {
                    success = true,
                    timedOut = false,
                    waitedMs = waitedMs,
                    condition = conditionName,
                    result = itemResult,
                    hint = null
                };
            }

            return new WaitResponse
            {
                success = false,
                timedOut = true,
                waitedMs = waitedMs,
                condition = conditionName,
                result = null,
                hint = "Condition not met within " + timeoutMs + "ms. Use GET /scene/hierarchy to inspect current state."
            };
        }

        /// <summary>
        /// LocatorItem 생성을 main thread에서만 실행.
        /// </summary>
        private static async Task<LocatorItem> ExtractLocatorItemSafe(GameObject go)
        {
            return await UnityMcpBridge.RunOnMainThreadAsync(() =>
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
            });
        }
    }
}
#endif
