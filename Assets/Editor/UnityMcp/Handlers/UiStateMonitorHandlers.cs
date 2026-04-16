#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// UI 상태 모니터링 핸들러 - View 활성/비활성 대기, 화면 비교
    /// </summary>
    internal static class UiStateMonitorHandlers
    {
        static UiStateMonitorHandlers()
        {
            "POST".Register("/ui/wait-for-active", "Wait for UI element to become active", async (req, res) => await HandleWaitForActiveAsync(req, res));
            "POST".Register("/ui/wait-for-inactive", "Wait for UI element to become inactive", async (req, res) => await HandleWaitForInactiveAsync(req, res));
            "POST".Register("/ui/wait-for-text", "Wait for UI text content to match", async (req, res) => await HandleWaitForTextAsync(req, res));
            "POST".Register("/ui/wait-for-component", "Wait for component to exist on GameObject", async (req, res) => await HandleWaitForComponentAsync(req, res));
            "POST".Register("/ui/compare-screenshots", "Compare two screenshots and return difference", async (req, res) => await HandleCompareScreenshotsAsync(req, res));
            "GET".Register("/ui/state", "Get current UI state snapshot", async (req, res) => await HandleGetUiStateAsync(res));
        }

        public static async Task HandleWaitForActiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<WaitForUiStateRequest>(body);

            try
            {
                var waitedMs = await WaitForStateAsync(
                    req.path,
                    req.name,
                    "active",
                    state => state.activeInHierarchy,
                    req.timeoutMs,
                    req.pollIntervalMs);

                await UnityMcpBridge.WriteJsonAsync(response, 200, new
                {
                    success = true,
                    path = req.path,
                    name = req.name,
                    activeInHierarchy = true,
                    waitedMs = waitedMs
                });
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 504, new ErrorResponse
                {
                    error = "UI wait timeout",
                    detail = ex.Message
                });
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse
                {
                    error = "UI element not found",
                    detail = ex.Message
                });
            }
        }

        public static async Task HandleWaitForInactiveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<WaitForUiStateRequest>(body);

            try
            {
                var waitedMs = await WaitForStateAsync(
                    req.path,
                    req.name,
                    "inactive",
                    state => !state.activeInHierarchy,
                    req.timeoutMs,
                    req.pollIntervalMs);

                await UnityMcpBridge.WriteJsonAsync(response, 200, new
                {
                    success = true,
                    path = req.path,
                    name = req.name,
                    activeInHierarchy = false,
                    waitedMs = waitedMs
                });
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 504, new ErrorResponse
                {
                    error = "UI wait timeout",
                    detail = ex.Message
                });
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse
                {
                    error = "UI element not found",
                    detail = ex.Message
                });
            }
        }

        public static async Task HandleWaitForTextAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<WaitForTextRequest>(body);

            try
            {
                var waitedMs = await WaitForTextAsync(req.path, req.name, req.expectedText, req.exact, req.timeoutMs, req.pollIntervalMs);

                await UnityMcpBridge.WriteJsonAsync(response, 200, new
                {
                    success = true,
                    path = req.path,
                    name = req.name,
                    expectedText = req.expectedText,
                    waitedMs = waitedMs
                });
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 504, new ErrorResponse
                {
                    error = "Text wait timeout",
                    detail = ex.Message
                });
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse
                {
                    error = "UI element not found",
                    detail = ex.Message
                });
            }
        }

        public static async Task HandleWaitForComponentAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<WaitForComponentRequest>(body);

            try
            {
                var waitedMs = await WaitForComponentAsync(req.path, req.componentType, req.timeoutMs, req.pollIntervalMs);

                await UnityMcpBridge.WriteJsonAsync(response, 200, new
                {
                    success = true,
                    path = req.path,
                    componentType = req.componentType,
                    waitedMs = waitedMs
                });
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 504, new ErrorResponse
                {
                    error = "Component wait timeout",
                    detail = ex.Message
                });
            }
            catch (MissingMemberException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse
                {
                    error = "GameObject not found",
                    detail = ex.Message
                });
            }
        }

        public static async Task HandleCompareScreenshotsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var req = JsonUtility.FromJson<CompareScreenshotsRequest>(body);

            try
            {
                var result = await CompareScreenshotsAsync(req.beforePath, req.afterPath, req.tolerance);

                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (Exception ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 500, new ErrorResponse
                {
                    error = "Screenshot comparison failed",
                    detail = ex.Message,
                    stackTrace = ex.ToString()
                });
            }
        }

        public static async Task HandleGetUiStateAsync(HttpListenerResponse response)
        {
            var state = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                // 모든 Canvas 하위 UI 요소 수집
                var canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var canvasStates = new List<object>();

                foreach (var canvas in canvases)
                {
                    var canvasPath = McpSharedHelpers.GetTransformPath(canvas.transform);
                    var children = new List<object>();

                    CollectUiState(canvas.transform, children, 0, 3); // 최대 3단계 깊이

                    canvasStates.Add(new
                    {
                        path = canvasPath,
                        name = canvas.name,
                        activeInHierarchy = canvas.gameObject.activeInHierarchy,
                        childCount = children.Count,
                        children = children
                    });
                }

                var uiState = new
                {
                    sceneName = SceneManager.GetActiveScene().name,
                    isPlaying = EditorApplication.isPlaying,
                    timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    canvases = canvasStates
                };

                return uiState;
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, state);
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static async Task<int> WaitForStateAsync(string path, string name, string stateType, Func<UiElementState, bool> checkState, int timeoutMs, int pollIntervalMs)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                var elementState = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var go = string.IsNullOrEmpty(path)
                        ? GameObject.Find(name)
                        : McpSharedHelpers.FindGameObjectByPath(path);

                    if (go == null) return null;

                    return new UiElementState
                    {
                        path = McpSharedHelpers.GetTransformPath(go.transform),
                        name = go.name,
                        activeSelf = go.activeSelf,
                        activeInHierarchy = go.activeInHierarchy
                    };
                });

                if (elementState != null && checkState(elementState))
                {
                    return (int)(DateTime.UtcNow - deadline.Add(TimeSpan.FromMilliseconds(-timeoutMs))).TotalMilliseconds;
                }

                await Task.Delay(pollIntervalMs);
            }

            throw new TimeoutException($"UI element did not become {stateType} within {timeoutMs}ms: {path ?? name}");
        }

        private static async Task<int> WaitForTextAsync(string path, string name, string expectedText, bool exact, int timeoutMs, int pollIntervalMs)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                var hasText = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var go = string.IsNullOrEmpty(path)
                        ? GameObject.Find(name)
                        : McpSharedHelpers.FindGameObjectByPath(path);

                    if (go == null) return false;

                    string currentText = null;

                    var tmpText = go.GetComponent<TMPro.TMP_Text>();
                    if (tmpText != null)
                    {
                        currentText = tmpText.text;
                    }
                    else
                    {
                        var legacyText = go.GetComponent<UnityEngine.UI.Text>();
                        if (legacyText != null)
                        {
                            currentText = legacyText.text;
                        }
                    }

                    if (currentText == null) return false;

                    var checkText = exact ? currentText : currentText.Trim();
                    return exact
                        ? checkText == expectedText
                        : checkText.Contains(expectedText);
                });

                if (hasText)
                {
                    return (int)(DateTime.UtcNow - deadline.Add(TimeSpan.FromMilliseconds(-timeoutMs))).TotalMilliseconds;
                }

                await Task.Delay(pollIntervalMs);
            }

            throw new TimeoutException($"UI text did not match '{expectedText}' within {timeoutMs}ms: {path ?? name}");
        }

        private static async Task<int> WaitForComponentAsync(string path, string componentType, int timeoutMs, int pollIntervalMs)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                var hasComponent = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var go = McpSharedHelpers.FindGameObjectByPath(path);
                    if (go == null) return false;

                    var type = UnityMcpBridge.ResolveTypeByFullName(componentType);
                    if (type == null) type = UnityMcpBridge.ResolveTypeByFullName("UnityEngine.UI." + componentType);
                    if (type == null) return false;

                    return go.GetComponent(type) != null;
                });

                if (hasComponent)
                {
                    return (int)(DateTime.UtcNow - deadline.Add(TimeSpan.FromMilliseconds(-timeoutMs))).TotalMilliseconds;
                }

                await Task.Delay(pollIntervalMs);
            }

            throw new TimeoutException($"Component {componentType} did not appear on {path} within {timeoutMs}ms");
        }

        private static async Task<CompareScreenshotsResponse> CompareScreenshotsAsync(string beforePath, string afterPath, int tolerance)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                // 스크린샷 로드
                var beforeTexture = LoadTexture(beforePath);
                var afterTexture = LoadTexture(afterPath);

                if (beforeTexture == null || afterTexture == null)
                {
                    throw new Exception("Failed to load one or both screenshots");
                }

                // 크기 확인
                if (beforeTexture.width != afterTexture.width || beforeTexture.height != afterTexture.height)
                {
                    return new CompareScreenshotsResponse
                    {
                        sameSize = false,
                        differentPixels = -1,
                        similarity = 0f,
                        error = "Screenshot sizes do not match"
                    };
                }

                // 픽셀 비교
                int differentPixels = 0;
                var colorsBefore = beforeTexture.GetPixels32();
                var colorsAfter = afterTexture.GetPixels32();

                for (int i = 0; i < colorsBefore.Length; i++)
                {
                    var c1 = colorsBefore[i];
                    var c2 = colorsAfter[i];

                    if (Math.Abs(c1.r - c2.r) > tolerance ||
                        Math.Abs(c1.g - c2.g) > tolerance ||
                        Math.Abs(c1.b - c2.b) > tolerance ||
                        Math.Abs(c1.a - c2.a) > tolerance)
                    {
                        differentPixels++;
                    }
                }

                var similarity = 1f - (float)differentPixels / colorsBefore.Length;

                UnityEngine.Object.DestroyImmediate(beforeTexture);
                UnityEngine.Object.DestroyImmediate(afterTexture);

                return new CompareScreenshotsResponse
                {
                    sameSize = true,
                    differentPixels = differentPixels,
                    totalPixels = colorsBefore.Length,
                    similarity = similarity
                };
            });

            return result;
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                return texture;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }

        private static void CollectUiState(Transform transform, List<object> result, int depth, int maxDepth)
        {
            if (depth >= maxDepth) return;

            foreach (Transform child in transform)
            {
                var path = McpSharedHelpers.GetTransformPath(child);
                var hasUiComponent = child.GetComponent<UnityEngine.UI.Graphic>() != null ||
                                       child.GetComponent<UnityEngine.UI.Image>() != null ||
                                       child.GetComponent<UnityEngine.UI.Button>() != null ||
                                       child.GetComponent<TMPro.TMP_Text>() != null ||
                                       child.GetComponent<TMPro.TextMeshProUGUI>() != null;

                if (hasUiComponent)
                {
                    result.Add(new
                    {
                        path = path,
                        name = child.name,
                        activeInHierarchy = child.gameObject.activeInHierarchy,
                        components = GetUiComponentTypes(child.gameObject)
                    });
                }

                if (child.childCount > 0)
                {
                    CollectUiState(child, result, depth + 1, maxDepth);
                }
            }
        }

        private static string[] GetUiComponentTypes(GameObject go)
        {
            var types = new List<string>();

            if (go.GetComponent<Graphic>() != null) types.Add("Graphic");
            if (go.GetComponent<Button>() != null) types.Add("Button");
            if (go.GetComponent<TMPro.TMP_Text>() != null) types.Add("TMP_Text");
            if (go.GetComponent<TMPro.TextMeshProUGUI>() != null) types.Add("TMP_Text");
            if (go.GetComponent<TMPro.TextMeshPro>() != null) types.Add("TMP");
            if (go.GetComponent<InputField>() != null) types.Add("InputField");
            if (go.GetComponent<TMPro.TMP_InputField>() != null) types.Add("InputField");
            if (go.GetComponent<Toggle>() != null) types.Add("Toggle");
            if (go.GetComponent<Slider>() != null) types.Add("Slider");
            if (go.GetComponent<ScrollRect>() != null) types.Add("ScrollRect");

            return types.ToArray();
        }
    }

    // =====================================================================
    // Data Models
    // =====================================================================

    [Serializable]
    internal class WaitForUiStateRequest
    {
        public string path;
        public string name;
        public int timeoutMs = 10000;
        public int pollIntervalMs = 100;
    }

    [Serializable]
    internal class WaitForTextRequest
    {
        public string path;
        public string name;
        public string expectedText;
        public bool exact = false;
        public int timeoutMs = 10000;
        public int pollIntervalMs = 100;
    }

    [Serializable]
    internal class WaitForComponentRequest
    {
        public string path;
        public string componentType;
        public int timeoutMs = 10000;
        public int pollIntervalMs = 100;
    }

    [Serializable]
    internal class CompareScreenshotsRequest
    {
        public string beforePath;
        public string afterPath;
        public int tolerance = 10;
    }

    [Serializable]
    internal class CompareScreenshotsResponse
    {
        public bool sameSize;
        public int differentPixels;
        public int totalPixels;
        public float similarity;
        public string error;
    }

    internal class UiElementState
    {
        public string path;
        public string name;
        public bool activeSelf;
        public bool activeInHierarchy;
    }
}
#endif
