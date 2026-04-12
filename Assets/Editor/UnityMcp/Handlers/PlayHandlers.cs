#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class PlayHandlers
    {
        public static async Task HandleHealthAsync(HttpListenerResponse response)
        {
            var sceneInfo = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var isPlayModeChanging = UnityMcpBridge.IsPlayModeChanging();
                return new HealthResponse
                {
                    ok = true,
                    bridgeRunning = UnityMcpBridge.IsRunning,
                    port = UnityMcpBridge.ActivePort > 0 ? UnityMcpBridge.ActivePort : UnityMcpBridge.ResolvePort(),
                    isPlaying = EditorApplication.isPlaying,
                    isPlayingOrWillChange = isPlayModeChanging,
                    isPlayModechanging = isPlayModeChanging,
                    rawIsPlayingOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode,
                    isCompiling = EditorApplication.isCompiling,
                    projectKey = UnityMcpBridge.ProjectKey,
                    projectRootPath = UnityMcpBridge.ProjectRootPath,
                    activeScene = scene.name,
                    activeScenePath = scene.path
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, sceneInfo);
        }

        public static async Task HandleCurrentSceneAsync(HttpListenerResponse response)
        {
            var sceneInfo = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var isPlayModeChanging = UnityMcpBridge.IsPlayModeChanging();
                return new SceneResponse
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isLoaded = scene.isLoaded,
                    isDirty = scene.isDirty,
                    isPlaying = EditorApplication.isPlaying,
                    isPlayingOrWillChange = isPlayModeChanging,
                    isPlayModeChanging = isPlayModeChanging,
                    rawIsPlayingOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, sceneInfo);
        }

        public static async Task HandlePlayStartAsync(HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (!EditorApplication.isPlaying)
                {
                    UnityMcpBridge.IsPlayModeChanging = true;
                    EditorApplication.isPlaying = true;
                }

                var isPlayModeChanging = UnityMcpBridge.IsPlayModeChanging();

                return new PlayResponse
                {
                    action = "start",
                    isPlaying = EditorApplication.isPlaying,
                    isPlayingOrWillChange = isPlayModeChanging,
                    isPlayModeChanging = isPlayModeChanging,
                    rawIsPlayingOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandlePlayStopAsync(HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (EditorApplication.isPlaying)
                {
                    UnityMcpBridge.IsPlayModeChanging = true;
                    EditorApplication.isPlaying = false;
                }

                var isPlayModeChanging = UnityMcpBridge.IsPlayModeChanging();

                return new PlayResponse
                {
                    action = "stop",
                    isPlaying = EditorApplication.isPlaying,
                    isPlayingOrWillChange = isPlayModeChanging,
                    isPlayModeChanging = isPlayModeChanging,
                    rawIsPlayingOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleScreenshotCaptureAsync(HttpListenerRequest request, HttpListenerResponse response)
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
                    new ErrorResponse
                    {
                        error = "Invalid screenshot request",
                        detail = ex.Message
                    });
                return;
            }

            try
            {
                await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Screenshot capture requires play mode so the Game view is rendering."
                        );
                    }

                    ScreenCapture.CaptureScreenshot(capturePlan.absolutePath, capturePlan.superSize);
                    return true;
                });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    409,
                    new ErrorResponse
                    {
                        error = "Screenshot unavailable",
                        detail = ex.Message
                    });
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
                        fileSizeBytes = fileSizeBytes
                    });
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(
                    response,
                    504,
                    new ErrorResponse
                    {
                        error = "Screenshot timeout",
                        detail = ex.Message
                    });
            }
        }

        private static ScreenshotCapturePlan BuildScreenshotCapturePlan(ScreenshotCaptureRequest req)
        {
            if (req == null)
            {
                req = new ScreenshotCaptureRequest();
            }

            var relativePath = !string.IsNullOrWhiteSpace(req.outputPath)
                ? req.outputPath.Replace('\\', '/')
                : BuildDefaultScreenshotRelativePath();

            if (!relativePath.EndsWith(".png", StringComparison.Ordinal))
            {
                relativePath += ".png";
            }

            var absolutePath = Path.Combine(UnityMcpBridge.ProjectRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var superSize = req.superSize > 0 ? req.superSize : 1;
            var overwrite = req.overwrite;

            return new ScreenshotCapturePlan
            {
                relativePath = relativePath,
                absolutePath = absolutePath,
                superSize = superSize,
                overwrite = overwrite
            };
        }

        private static async Task<long> WaitForScreenshotFileAsync(string absolutePath)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(UnityMcpBridge.ScreenshotWriteTimeoutMs);
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
                "Screenshot file was not written within " + UnityMcpBridge.ScreenshotWriteTimeoutMs + "ms: " + absolutePath
            );
        }

        private static string BuildDefaultScreenshotRelativePath()
        {
            return UnityMcpBridge.DefaultScreenshotDirectoryRelativePath + "/"
                + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".png";
        }
    }
}
#endif
