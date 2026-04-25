#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class PlayHandlers
    {
        private static readonly object PlayModeLock = new object();
        private static bool _isWaitingForPlayMode = false;
        private static DateTime _playModeChangeStartTime = DateTime.MinValue;
        private const int PlayModeTimeoutMs = 30000; // 30초 타임아웃
        private const int PlayModePollIntervalMs = 100;

        static PlayHandlers()
        {
            "GET".Register(
                "/health",
                "Bridge health and editor state",
                async (req, res) => await HandleHealthAsync(res)
            );
            "GET".Register(
                "/scene/current",
                "Active scene info",
                async (req, res) => await HandleCurrentSceneAsync(res)
            );
            "POST".Register(
                "/play/start",
                "Enter play mode",
                async (req, res) => await HandlePlayStartAsync(res)
            );
            "POST".Register(
                "/play/stop",
                "Exit play mode",
                async (req, res) => await HandlePlayStopAsync(res)
            );
            "POST".Register(
                "/play/wait-for-play",
                "Wait until play mode is fully entered",
                async (req, res) => await HandleWaitForPlayAsync(res)
            );
            "POST".Register(
                "/play/wait-for-stop",
                "Wait until play mode is fully exited",
                async (req, res) => await HandleWaitForStopAsync(res)
            );
            "POST".Register(
                "/screenshot/capture",
                "Capture game screenshot",
                async (req, res) => await HandleScreenshotCaptureAsync(req, res)
            );
            "POST".Register(
                "/sceneview/capture",
                "Capture current SceneView screenshot, including Prefab Mode",
                async (req, res) => await HandleSceneViewCaptureAsync(req, res)
            );
        }

        public static async Task HandleHealthAsync(HttpListenerResponse response)
        {
            var health = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var state = McpEditorState.Capture();
                return BuildHealthResponse(state);
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, health);
        }

        public static async Task HandleCurrentSceneAsync(HttpListenerResponse response)
        {
            var sceneInfo = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var state = McpEditorState.Capture();
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                return new SceneResponse
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isLoaded = scene.isLoaded,
                    isDirty = scene.isDirty,
                    isPlaying = state.isPlaying,
                    isPlayingOrWillChange = state.isPlayingOrWillChange,
                    isPlayModeChanging = state.isPlayModeChanging,
                    rawIsPlayingOrWillChange = state.rawIsPlayingOrWillChange,
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, sceneInfo);
        }

        public static async Task HandlePlayStartAsync(HttpListenerResponse response)
        {
            try
            {
                var currentState = await UnityMcpBridge.RunOnMainThreadAsync(McpEditorState.Capture);
                if (HasReachedPlayModeState(currentState, targetIsPlaying: true))
                {
                    await UnityMcpBridge.WriteJsonAsync(
                        response,
                        200,
                        BuildPlayModeResponse(
                            "start",
                            new PlayModeWaitResult { state = currentState, waitedMs = 0 }
                        )
                    );
                    return;
                }

                BeginPlayModeTransition();
                await PlayModeChangeQueue.EnqueuePlayAsync();
                var waitResult = await WaitForPlayModeStateAsync(targetIsPlaying: true);
                await UnityMcpBridge.WriteJsonAsync(response, 200, BuildPlayModeResponse("start", waitResult));
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse
                    {
                        error = "Play mode change timeout",
                        detail = ex.Message,
                        hint = "Check /health for isCompiling or isPlayModeChanging before retrying.",
                    }
                );
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already pending"))
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse
                    {
                        error = "Play start pending",
                        detail = ex.Message,
                        hint =
                            "Pending action="
                            + PlayModeChangeQueue.PendingActionName
                            + ", ageMs="
                            + PlayModeChangeQueue.PendingActionAgeMs
                            + ". Wait for the current transition to settle before retrying.",
                    }
                );
            }
            catch (Exception ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    500,
                    new ErrorResponse
                    {
                        error = "Play start failed",
                        detail = ex.Message,
                        stackTrace = ex.ToString(),
                        hint = "Inspect /health and /console/errors to find the blocking editor state.",
                    }
                );
            }
            finally
            {
                lock (PlayModeLock)
                    _isWaitingForPlayMode = false;
            }
        }

        public static async Task HandlePlayStopAsync(HttpListenerResponse response)
        {
            try
            {
                var currentState = await UnityMcpBridge.RunOnMainThreadAsync(McpEditorState.Capture);
                if (HasReachedPlayModeState(currentState, targetIsPlaying: false))
                {
                    await UnityMcpBridge.WriteJsonAsync(
                        response,
                        200,
                        BuildPlayModeResponse(
                            "stop",
                            new PlayModeWaitResult { state = currentState, waitedMs = 0 }
                        )
                    );
                    return;
                }

                BeginPlayModeTransition();

                if (McpConfig.AutoSaveSceneOnPlayStop)
                {
                    McpConfig.TryAutoSaveScene(out _);
                }

                await PlayModeChangeQueue.EnqueueStopAsync();
                var waitResult = await WaitForPlayModeStateAsync(targetIsPlaying: false);
                await UnityMcpBridge.WriteJsonAsync(response, 200, BuildPlayModeResponse("stop", waitResult));
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse
                    {
                        error = "Play mode change timeout",
                        detail = ex.Message,
                        hint = "Play mode exit is still changing. Check /health and Unity console before retrying.",
                    }
                );
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already pending"))
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse
                    {
                        error = "Play stop pending",
                        detail = ex.Message,
                        hint =
                            "Pending action="
                            + PlayModeChangeQueue.PendingActionName
                            + ", ageMs="
                            + PlayModeChangeQueue.PendingActionAgeMs
                            + ". Wait for the current transition to settle before retrying.",
                    }
                );
            }
            catch (Exception ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    500,
                    new ErrorResponse
                    {
                        error = "Play stop failed",
                        detail = ex.Message,
                        stackTrace = ex.ToString(),
                        hint = "Inspect /health and /console/errors. Do not fall back to menu execution unless you are debugging the bridge itself.",
                    }
                );
            }
            finally
            {
                lock (PlayModeLock)
                    _isWaitingForPlayMode = false;
            }
        }

        public static async Task HandleWaitForPlayAsync(HttpListenerResponse response)
        {
            try
            {
                var waitResult = await WaitForPlayModeStateAsync(targetIsPlaying: true);
                await UnityMcpBridge.WriteJsonAsync(response, 200, BuildPlayModeResponse("wait-for-play", waitResult));
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse { error = "Play mode wait timeout", detail = ex.Message }
                );
            }
        }

        public static async Task HandleWaitForStopAsync(HttpListenerResponse response)
        {
            try
            {
                var waitResult = await WaitForPlayModeStateAsync(targetIsPlaying: false);
                await UnityMcpBridge.WriteJsonAsync(response, 200, BuildPlayModeResponse("wait-for-stop", waitResult));
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse { error = "Play mode wait timeout", detail = ex.Message }
                );
            }
        }

        private static void BeginPlayModeTransition()
        {
            lock (PlayModeLock)
            {
                _isWaitingForPlayMode = true;
                _playModeChangeStartTime = DateTime.UtcNow;
            }
        }

        private static HealthResponse BuildHealthResponse(McpEditorStateSnapshot state)
        {
            var response = new HealthResponse
            {
                ok = true,
                bridgeRunning = UnityMcpBridge.IsRunning,
                port = UnityMcpBridge.ActivePort > 0 ? UnityMcpBridge.ActivePort : UnityMcpBridge.ResolvePort(),
                isPlaying = state.isPlaying,
                isPlayingOrWillChange = state.isPlayingOrWillChange,
                isPlayModeChanging = state.isPlayModeChanging,
                rawIsPlayingOrWillChange = state.rawIsPlayingOrWillChange,
                isCompiling = state.isCompiling,
                projectKey = UnityMcpBridge.ProjectKey,
                projectRootPath = UnityMcpBridge.ProjectRootPath,
                activeScene = state.activeScene,
                activeScenePath = state.activeScenePath,
                isWaitingForPlayMode = _isWaitingForPlayMode,
                pendingPlayAction = PlayModeChangeQueue.PendingActionName,
                pendingPlayAgeMs = PlayModeChangeQueue.PendingActionAgeMs,
            };
            return response;
        }

        private static object BuildPlayModeResponse(string action, PlayModeWaitResult waitResult)
        {
            return new
            {
                success = true,
                action = action,
                isPlaying = waitResult.state.isPlaying,
                isPlayingOrWillChange = waitResult.state.isPlayingOrWillChange,
                isPlayModeChanging = waitResult.state.isPlayModeChanging,
                rawIsPlayingOrWillChange = waitResult.state.rawIsPlayingOrWillChange,
                isCompiling = waitResult.state.isCompiling,
                activeScene = waitResult.state.activeScene,
                activeScenePath = waitResult.state.activeScenePath,
                waitedMs = waitResult.waitedMs,
            };
        }

        private static async Task<PlayModeWaitResult> WaitForPlayModeStateAsync(bool targetIsPlaying)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(PlayModeTimeoutMs);
            var startedAt = _isWaitingForPlayMode && _playModeChangeStartTime != DateTime.MinValue
                ? _playModeChangeStartTime
                : DateTime.UtcNow;
            McpEditorStateSnapshot latestState = null;

            while (DateTime.UtcNow < deadline)
            {
                latestState = await UnityMcpBridge.RunOnMainThreadAsync(McpEditorState.Capture);
                if (HasReachedPlayModeState(latestState, targetIsPlaying))
                {
                    await Task.Delay(300);
                    latestState = await UnityMcpBridge.RunOnMainThreadAsync(McpEditorState.Capture);
                    if (HasReachedPlayModeState(latestState, targetIsPlaying))
                    {
                        return new PlayModeWaitResult
                        {
                            state = latestState,
                            waitedMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds,
                        };
                    }
                }

                await Task.Delay(PlayModePollIntervalMs);
            }

            latestState = latestState ?? await UnityMcpBridge.RunOnMainThreadAsync(McpEditorState.Capture);
            var targetText = targetIsPlaying ? "start" : "stop";
            throw new TimeoutException(
                "Play mode did not "
                    + targetText
                    + " within "
                    + PlayModeTimeoutMs
                    + "ms. "
                    + "isPlaying="
                    + latestState.isPlaying
                    + ", isPlayModeChanging="
                    + latestState.isPlayModeChanging
                    + ", isCompiling="
                    + latestState.isCompiling
                    + "."
            );
        }

        private static bool HasReachedPlayModeState(McpEditorStateSnapshot state, bool targetIsPlaying)
        {
            return state != null
                && state.isPlaying == targetIsPlaying
                && !state.isPlayModeChanging
                && !state.isCompiling;
        }

        public static async Task HandleScreenshotCaptureAsync(
            HttpListenerRequest request,
            HttpListenerResponse response
        )
        {
            ScreenshotCaptureRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<ScreenshotCaptureRequest>(body);
                }
            }

            ScreenshotCapturePlan capturePlan;
            try
            {
                capturePlan = BuildScreenshotCapturePlan(req);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    400,
                    new ErrorResponse { error = "Invalid screenshot request", detail = ex.Message }
                );
                return;
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse { error = "Screenshot unavailable", detail = ex.Message }
                );
                return;
            }

            try
            {
                if (File.Exists(capturePlan.absolutePath))
                {
                    if (!(req?.overwrite ?? false))
                    {
                        throw new InvalidOperationException(
                            "Screenshot target already exists. Set overwrite=true or choose a different outputPath: "
                                + capturePlan.relativePath
                        );
                    }

                    File.Delete(capturePlan.absolutePath);
                }

                await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Screenshot capture requires play mode so the Game view is rendering."
                        );
                    }

                    ScreenCapture.CaptureScreenshot(
                        capturePlan.absolutePath,
                        capturePlan.superSize
                    );
                    return true;
                });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse { error = "Screenshot unavailable", detail = ex.Message }
                );
                return;
            }

            try
            {
                var fileSizeBytes = await WaitForScreenshotFileAsync(capturePlan.absolutePath);
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    200,
                    new ScreenshotCaptureResponse
                    {
                        success = true,
                        message = "Screenshot captured.",
                        relativePath = capturePlan.relativePath,
                        absolutePath = capturePlan.absolutePath,
                        fileSizeBytes = fileSizeBytes,
                        sourceView = "GameView",
                    }
                );
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse { error = "Screenshot timeout", detail = ex.Message }
                );
            }
        }

        public static async Task HandleSceneViewCaptureAsync(
            HttpListenerRequest request,
            HttpListenerResponse response
        )
        {
            ScreenshotCaptureRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<ScreenshotCaptureRequest>(body);
                }
            }

            ScreenshotCapturePlan capturePlan;
            try
            {
                capturePlan = BuildScreenshotCapturePlan(req);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    400,
                    new ErrorResponse { error = "Invalid scene view capture request", detail = ex.Message }
                );
                return;
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse { error = "Scene view capture unavailable", detail = ex.Message }
                );
                return;
            }

            try
            {
                if (File.Exists(capturePlan.absolutePath))
                {
                    if (!(req?.overwrite ?? false))
                    {
                        throw new InvalidOperationException(
                            "SceneView capture target already exists. Set overwrite=true or choose a different outputPath: "
                                + capturePlan.relativePath
                        );
                    }

                    File.Delete(capturePlan.absolutePath);
                }

                var captureResult = await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    var sceneView = ResolveSceneViewWindow();
                    if (sceneView == null)
                    {
                        throw new InvalidOperationException("No SceneView window is available to capture.");
                    }

                    sceneView.Show();
                    sceneView.Focus();
                    sceneView.Repaint();

                    var superSize = capturePlan.superSize > 0 ? capturePlan.superSize : 1;
                    var width = capturePlan.width > 0
                        ? capturePlan.width
                        : Mathf.Max(1, Mathf.RoundToInt(sceneView.position.width * superSize));
                    var height = capturePlan.height > 0
                        ? capturePlan.height
                        : Mathf.Max(1, Mathf.RoundToInt(sceneView.position.height * superSize));

                    var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                    var previousActive = RenderTexture.active;
                    var reviewCamera = GameObject.Find("StitchRuntimeReviewCamera")?.GetComponent<Camera>();
                    var sceneCamera = reviewCamera != null ? reviewCamera : sceneView.camera;
                    var previousTarget = sceneCamera != null ? sceneCamera.targetTexture : null;
                    var previousAspect = sceneCamera != null ? sceneCamera.aspect : 1f;
                    Texture2D texture = null;

                    try
                    {
                        if (sceneCamera == null)
                        {
                            throw new InvalidOperationException("SceneView camera is unavailable.");
                        }

                        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        sceneCamera.targetTexture = renderTexture;
                        if (reviewCamera != null)
                        {
                            sceneCamera.aspect = width / (float)height;
                        }

                        RenderTexture.active = renderTexture;
                        GL.Clear(true, true, sceneCamera.backgroundColor);
                        sceneCamera.Render();
                        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        texture.Apply(false, false);

                        var pngBytes = texture.EncodeToPNG();
                        File.WriteAllBytes(capturePlan.absolutePath, pngBytes);

                        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        return new ScreenshotCaptureResponse
                        {
                            success = true,
                            message = "SceneView screenshot captured.",
                            relativePath = capturePlan.relativePath,
                            absolutePath = capturePlan.absolutePath,
                            fileSizeBytes = pngBytes.LongLength,
                            sourceView = reviewCamera != null ? "SceneCamera" : "SceneView",
                            width = width,
                            height = height,
                            prefabStageAssetPath = prefabStage != null ? prefabStage.assetPath : null,
                        };
                    }
                    finally
                    {
                        if (sceneCamera != null)
                        {
                            sceneCamera.targetTexture = previousTarget;
                            if (reviewCamera != null)
                            {
                                sceneCamera.aspect = previousAspect;
                            }
                        }

                        RenderTexture.active = previousActive;
                        RenderTexture.ReleaseTemporary(renderTexture);

                        if (texture != null)
                        {
                            UnityEngine.Object.DestroyImmediate(texture);
                        }
                    }
                });

                await UnityMcpBridge.WriteJsonAsync(response, 200, captureResult);
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse { error = "Scene view capture unavailable", detail = ex.Message }
                );
            }
            catch (Exception ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    500,
                    new ErrorResponse
                    {
                        error = "Scene view capture failed",
                        detail = ex.Message,
                        stackTrace = ex.ToString(),
                    }
                );
            }
        }

        private static ScreenshotCapturePlan BuildScreenshotCapturePlan(
            ScreenshotCaptureRequest req
        )
        {
            if (req == null)
            {
                req = new ScreenshotCaptureRequest();
            }

            var relativePath = !string.IsNullOrWhiteSpace(req.outputPath)
                ? req.outputPath.Replace('\\', '/')
                : BuildDefaultScreenshotRelativePath();

            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("outputPath must be project-relative, not an absolute path.");
            }

            if (!relativePath.EndsWith(".png", StringComparison.Ordinal))
            {
                relativePath += ".png";
            }

            var absolutePath = Path.Combine(
                UnityMcpBridge.ProjectRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar)
            );
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var superSize = req.superSize > 0 ? req.superSize : 1;

            return new ScreenshotCapturePlan
            {
                relativePath = relativePath,
                absolutePath = absolutePath,
                superSize = superSize,
                width = req.width > 0 ? req.width : 0,
                height = req.height > 0 ? req.height : 0,
            };
        }

        private static async Task<long> WaitForScreenshotFileAsync(string absolutePath)
        {
            var deadline =
                DateTime.UtcNow
                + TimeSpan.FromMilliseconds(UnityMcpBridge.ScreenshotWriteTimeoutMs);
            var pollDelay = UnityMcpBridge.ScreenshotPollDelayMs;

            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(absolutePath))
                {
                    var info = new FileInfo(absolutePath);
                    if (info.Length > 0)
                    {
                        return info.Length;
                    }
                }

                await Task.Delay(pollDelay);
            }

            throw new TimeoutException(
                "Screenshot file was not written within "
                    + UnityMcpBridge.ScreenshotWriteTimeoutMs
                    + "ms: "
                    + absolutePath
            );
        }

        private static string BuildDefaultScreenshotRelativePath()
        {
            return UnityMcpBridge.DefaultScreenshotDirectoryRelativePath
                + "/"
                + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff")
                + ".png";
        }

        private static SceneView ResolveSceneViewWindow()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                return SceneView.lastActiveSceneView;
            }

            if (SceneView.sceneViews != null && SceneView.sceneViews.Count > 0)
            {
                return SceneView.sceneViews[0] as SceneView;
            }

            return EditorWindow.GetWindow<SceneView>();
        }

        private sealed class PlayModeWaitResult
        {
            public McpEditorStateSnapshot state;
            public int waitedMs;
        }
    }
}
#endif
