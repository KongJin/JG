#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class BuildHandlers
    {
        public static async Task HandleAssetRefreshAsync(HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                AssetDatabase.Refresh();
                return new GenericResponse { success = true, message = "Asset database refreshed." };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleCompileStatusAsync(HttpListenerResponse response)
        {
            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                return new CompileStatusResponse { isCompiling = EditorApplication.isCompiling };
            });
            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleCompileRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            CompileRequestBody parsed = null;
            if (!string.IsNullOrWhiteSpace(body)) parsed = JsonUtility.FromJson<CompileRequestBody>(body);

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (EditorApplication.isCompiling)
                {
                    return new CompileRequestResponse { ok = false, cleanBuildCacheRequested = false, isCompiling = true, message = "Already compiling." };
                }

                if (parsed != null && parsed.cleanBuildCache)
                {
                    EditorUtility.RequestScriptReload();
                    return new CompileRequestResponse { ok = true, cleanBuildCacheRequested = true, isCompiling = false, message = "Script reload requested." };
                }

                AssetDatabase.Refresh();
                return new CompileRequestResponse { ok = true, cleanBuildCacheRequested = false, isCompiling = EditorApplication.isCompiling, message = "Asset refresh triggered." };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleCompileWaitAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var parsed = string.IsNullOrWhiteSpace(body) ? null : JsonUtility.FromJson<CompileWaitBody>(body);

            var timeoutMs = 60000;
            var pollIntervalMs = 100;
            var requestFirst = true;
            var cleanBuildCache = false;

            if (parsed != null)
            {
                timeoutMs = parsed.timeoutMs > 0 ? parsed.timeoutMs : 60000;
                pollIntervalMs = parsed.pollIntervalMs > 0 ? parsed.pollIntervalMs : 100;
                requestFirst = parsed.requestFirst;
                cleanBuildCache = parsed.cleanBuildCache;
            }

            if (requestFirst)
            {
                await UnityMcpBridge.RunOnMainThreadAsync(() =>
                {
                    if (cleanBuildCache) EditorUtility.RequestScriptReload();
                    else AssetDatabase.Refresh();
                });
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var isCompiling = true;
            while (DateTime.UtcNow < deadline)
            {
                isCompiling = await UnityMcpBridge.RunOnMainThreadAsync(() => EditorApplication.isCompiling);
                if (!isCompiling) break;
                await Task.Delay(pollIntervalMs);
            }

            var responseObj = new CompileWaitResponse
            {
                ok = !isCompiling,
                timedOut = isCompiling,
                message = isCompiling ? "Compilation did not finish within " + timeoutMs + "ms." : "Compilation complete."
            };

            await UnityMcpBridge.WriteJsonAsync(response, isCompiling ? 504 : 200, responseObj);
        }

        public static async Task HandleBuildWebGLAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            string outputPath = "Build/WebGL";
            bool fastBuild = false;

            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var parsed = JsonUtility.FromJson<BuildWebGLRequest>(body);
                    if (!string.IsNullOrWhiteSpace(parsed?.outputPath)) outputPath = parsed.outputPath;
                    fastBuild = parsed != null && parsed.fastBuild;
                }
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) return new GenericResponse { success = false, message = "No scenes enabled in Build Settings." };

                var originalCompressionFormat = PlayerSettings.WebGL.compressionFormat;
                try
                {
                    var buildOptions = BuildOptions.None;
                    var buildLabel = "release";
                    if (fastBuild)
                    {
                        buildOptions |= BuildOptions.Development;
                        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                        buildLabel = "fast";
                    }
                    var report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.WebGL, buildOptions);
                    var succeeded = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                    return new GenericResponse
                    {
                        success = succeeded,
                        message = succeeded ? "WebGL " + buildLabel + " build completed: " + outputPath : "WebGL " + buildLabel + " build failed: " + report.summary.result
                    };
                }
                finally { PlayerSettings.WebGL.compressionFormat = originalCompressionFormat; }
            });

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 500, result);
        }

        public static async Task HandleMenuExecuteAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
            var parsed = string.IsNullOrWhiteSpace(body) ? null : JsonUtility.FromJson<MenuExecuteRequest>(body);
            var menuPath = parsed != null ? parsed.menuPath : null;

            if (string.IsNullOrWhiteSpace(menuPath))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "menuPath is required", detail = "Expected format: Tools/My Menu Item" });
                return;
            }

            var result = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var method = typeof(EditorApplication).GetMethod("ExecuteMenuItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] { menuPath });
                    return new GenericResponse { success = true, message = "Executed menu: " + menuPath };
                }
                return new GenericResponse { success = false, message = "Failed to execute menu: " + menuPath };
            });

            await UnityMcpBridge.WriteJsonAsync(response, result.success ? 200 : 500, result);
        }
    }
}
#endif
