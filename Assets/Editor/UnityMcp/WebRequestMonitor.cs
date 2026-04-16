#if UNITY_EDITOR
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// UnityWebRequest 감지 및 타임아웃 개선
    /// </summary>
    internal static class WebRequestMonitor
    {
        private const int DefaultTimeoutMs = 30000; // 30초
        private const int PollIntervalMs = 500;

        /// <summary>
        /// UnityWebRequest를 실행하고 타임아웃을 감지
        /// </summary>
        public static async Task<T> ExecuteWithTimeoutAsync<T>(string url, string method, string body, int timeoutMs = DefaultTimeoutMs)
            where T : class, new()
        {
            var operationId = AsyncMonitorHandlers.RegisterOperation("UnityWebRequest", $"{method} {url}");

            try
            {
                var request = new UnityWebRequest(url, method)
                {
                    timeout = timeoutMs / 1000
                };

                request.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(body))
                {
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                }
                request.downloadHandler = new DownloadHandlerBuffer();

                var operation = request.SendWebRequest();

                // 폴링으로 완료 감지
                var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs + 5000);

                while (!operation.isDone)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        request.Abort();
                        AsyncMonitorHandlers.FailOperation(operationId, $"Request timed out after {timeoutMs}ms");
                        throw new TimeoutException($"UnityWebRequest timeout: {method} {url}");
                    }

                    await Task.Delay(PollIntervalMs);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var error = request.error ?? "Unknown error";
                    var responseCode = (int)request.responseCode;
                    AsyncMonitorHandlers.FailOperation(operationId, $"Request failed: {error} (HTTP {responseCode})");
                    throw new Exception($"UnityWebRequest failed: {method} {url} - {error} (HTTP {responseCode})");
                }

                // JSON 파싱
                var json = request.downloadHandler.text;
                var result = JsonUtility.FromJson<T>(json);

                AsyncMonitorHandlers.CompleteOperation(operationId, json.Substring(0, Math.Min(100, json.Length)) + "...");

                return result;
            }
            catch (Exception ex)
            {
                if (!(ex is TimeoutException))
                {
                    AsyncMonitorHandlers.FailOperation(operationId, ex.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// UnityWebRequest를 실행하고 프로퍼티 모니터링 (지연 토큰 감지)
        /// </summary>
        public static async Task ExecuteWithProgressAsync(UnityWebRequest request, int timeoutMs = DefaultTimeoutMs, System.Action<float, long, long> onProgress = null)
        {
            var operationId = AsyncMonitorHandlers.RegisterOperation("UnityWebRequest", request.url);

            try
            {
                var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
                var startTime = DateTime.UtcNow;

                _ = request.SendWebRequest();

                while (!request.isDone)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        request.Abort();
                        AsyncMonitorHandlers.FailOperation(operationId, $"Request timed out after {timeoutMs}ms");
                        throw new TimeoutException($"UnityWebRequest timeout: {request.url}");
                    }

                    // 진행률 계산
                    if (request.downloadHandler is DownloadHandlerBuffer downloadHandler && downloadHandler.data != null)
                    {
                        var contentLength = request.GetResponseHeader("Content-Length");
                        if (long.TryParse(contentLength, out var totalBytes) && totalBytes > 0)
                        {
                            var progress = (float)downloadHandler.data.Length / totalBytes;
                            var elapsed = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                            onProgress?.Invoke(progress, downloadHandler.data.Length, totalBytes);
                            AsyncMonitorHandlers.UpdateOperationProgress(operationId, $"{(int)(progress * 100)}%");
                        }
                    }

                    await Task.Delay(PollIntervalMs);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var error = request.error ?? "Unknown error";
                    AsyncMonitorHandlers.FailOperation(operationId, $"Request failed: {error}");
                    throw new Exception($"UnityWebRequest failed: {request.url} - {error}");
                }

                AsyncMonitorHandlers.CompleteOperation(operationId);
            }
            catch (Exception ex)
            {
                if (!(ex is TimeoutException))
                {
                    AsyncMonitorHandlers.FailOperation(operationId, ex.Message);
                }
                throw;
            }
        }
    }
}
#endif
