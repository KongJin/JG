#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// 비동기 작업 감지 핸들러 - UnityWebRequest 타임아웃, Task 상태 모니터링
    /// </summary>
    internal static class AsyncMonitorHandlers
    {
        private static readonly ConcurrentDictionary<int, AsyncOperationInfo> ActiveOperations =
            new ConcurrentDictionary<int, AsyncOperationInfo>();
        private static int _nextOperationId = 1;

        static AsyncMonitorHandlers()
        {
            "GET".Register("/async/list", "List all active async operations", async (req, res) => await HandleListOperationsAsync(res));
            "GET".Register("/async/status/{id}", "Get async operation status", async (req, res) => await HandleGetOperationStatusAsync(req, res));
            "POST".Register("/async/wait/{id}", "Wait for async operation completion", async (req, res) => await HandleWaitForOperationAsync(req, res));
            "POST".Register("/async/cancel/{id}", "Cancel async operation", async (req, res) => await HandleCancelOperationAsync(req, res));
        }

        // =====================================================================
        // Async Operation 등록/관리
        // =====================================================================

        public static int RegisterOperation(string type, string description)
        {
            var id = Interlocked.Increment(ref _nextOperationId);
            var info = new AsyncOperationInfo
            {
                id = id,
                type = type,
                description = description,
                status = "running",
                startTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            ActiveOperations.TryAdd(id, info);

            // 자동 정리: 5분 후 완료되지 않은 작업 타임아웃 처리
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                if (ActiveOperations.TryGetValue(id, out var op) && op.status == "running")
                {
                    op.status = "timeout";
                    op.error = $"Operation timed out after 5 minutes";
                }
            });

            return id;
        }

        public static void CompleteOperation(int id, string result = null)
        {
            if (ActiveOperations.TryGetValue(id, out var info))
            {
                info.status = "completed";
                info.endTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                info.endTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                info.durationMs = info.endTimeMs - info.startTimeMs;
                info.result = result;
            }
        }

        public static void FailOperation(int id, string error)
        {
            if (ActiveOperations.TryGetValue(id, out var info))
            {
                info.status = "failed";
                info.error = error;
                info.endTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                info.endTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                info.durationMs = info.endTimeMs - info.startTimeMs;
            }
        }

        public static void UpdateOperationProgress(int id, string progress)
        {
            if (ActiveOperations.TryGetValue(id, out var info))
            {
                info.progress = progress;
            }
        }

        // =====================================================================
        // 엔드포인트 핸들러
        // =====================================================================

        public static async Task HandleListOperationsAsync(HttpListenerResponse response)
        {
            var operations = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                var ops = ActiveOperations.Values.ToList();
                var responseObj = new
                {
                    count = ops.Count,
                    running = ops.Count(o => o.status == "running"),
                    completed = ops.Count(o => o.status == "completed"),
                    failed = ops.Count(o => o.status == "failed"),
                    timeout = ops.Count(o => o.status == "timeout"),
                    operations = ops
                };
                return responseObj;
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, operations);
        }

        public static async Task HandleGetOperationStatusAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            if (pathParts.Length < 5)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID" });
                return;
            }

            if (!int.TryParse(pathParts[4], out var operationId))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID format" });
                return;
            }

            var operation = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (ActiveOperations.TryGetValue(operationId, out var info))
                {
                    return info;
                }
                return null;
            });

            if (operation == null)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 404, new ErrorResponse { error = "Operation not found", detail = $"ID: {operationId}" });
                return;
            }

            await UnityMcpBridge.WriteJsonAsync(response, 200, operation);
        }

        public static async Task HandleWaitForOperationAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            if (pathParts.Length < 5)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID" });
                return;
            }

            if (!int.TryParse(pathParts[4], out var operationId))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID format" });
                return;
            }

            var timeoutMs = 30000; // 기본 30초
            var timeoutRaw = request.QueryString["timeout"];
            if (!string.IsNullOrEmpty(timeoutRaw) && int.TryParse(timeoutRaw, out var parsedTimeout))
            {
                timeoutMs = Math.Min(parsedTimeout, 300000); // 최대 5분
            }

            var pollIntervalMs = 100;
            var pollRaw = request.QueryString["poll"];
            if (!string.IsNullOrEmpty(pollRaw) && int.TryParse(pollRaw, out var parsedPoll))
            {
                pollIntervalMs = Math.Max(parsedPoll, 50);
            }

            try
            {
                var result = await WaitForOperationCompletionAsync(operationId, timeoutMs, pollIntervalMs);
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (TimeoutException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 504, new ErrorResponse
                {
                    error = "Operation wait timeout",
                    detail = ex.Message
                });
            }
        }

        public static async Task HandleCancelOperationAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            if (pathParts.Length < 5)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID" });
                return;
            }

            if (!int.TryParse(pathParts[4], out var operationId))
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid operation ID format" });
                return;
            }

            var cancelled = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                if (ActiveOperations.TryGetValue(operationId, out var info))
                {
                    if (info.status == "running")
                    {
                        info.status = "cancelled";
                        info.error = "Operation cancelled by user";
                        return true;
                    }
                    return false;
                }
                return false;
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, new
            {
                success = cancelled,
                message = cancelled ? $"Operation {operationId} cancelled" : $"Operation {operationId} already completed or not found"
            });
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static async Task<AsyncOperationInfo> WaitForOperationCompletionAsync(int operationId, int timeoutMs, int pollIntervalMs)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                if (ActiveOperations.TryGetValue(operationId, out var info))
                {
                    if (info.status != "running")
                    {
                        return info;
                    }
                }
                else
                {
                    throw new TimeoutException($"Operation {operationId} not found");
                }

                await Task.Delay(pollIntervalMs);
            }

            throw new TimeoutException($"Operation {operationId} did not complete within {timeoutMs}ms");
        }
    }

    // =====================================================================
    // Data Models
    // =====================================================================

    [Serializable]
    internal class AsyncOperationInfo
    {
        public int id;
        public string type;
        public string description;
        public string status; // running, completed, failed, timeout, cancelled
        public string progress;
        public string result;
        public string error;
        public string startTimeUtc;
        public long startTimeMs;
        public string endTimeUtc;
        public long endTimeMs;
        public long durationMs;
    }
}
#endif
