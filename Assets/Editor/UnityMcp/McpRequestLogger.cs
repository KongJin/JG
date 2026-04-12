#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// MCP 요청/응답 내역을 기록하는 circular buffer 로거.
    /// 디버깅 목적이며 릴리즈 빌드에서는 자동으로 비활성화된다.
    /// </summary>
    internal static class McpRequestLogger
    {
        internal sealed class RequestRecord
        {
            public int id;
            public string method;
            public string path;
            public int statusCode;
            public long elapsedMs;
            public string error;
            public string timestamp;
        }

        private const int MaxRecords = 100;
        private static readonly List<RequestRecord> Records = new List<RequestRecord>(MaxRecords);
        private static readonly object Lock = new object();
        private static int _nextId = 1;

        /// <summary>
        /// 요청 처리 전후로 호출하여 기록을 남긴다.
        /// </summary>
        public static async Task RecordAsync(string method, string path, Func<Task<int>> handler)
        {
            var sw = Stopwatch.StartNew();
            int statusCode = 500;
            string error = null;

            try
            {
                statusCode = await handler();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                statusCode = 500;
            }
            finally
            {
                sw.Stop();
            }

            lock (Lock)
            {
                var record = new RequestRecord
                {
                    id = _nextId++,
                    method = method,
                    path = path,
                    statusCode = statusCode,
                    elapsedMs = sw.ElapsedMilliseconds,
                    error = error,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };

                Records.Add(record);
                while (Records.Count > MaxRecords) Records.RemoveAt(0);
            }
        }

        /// <summary>
        /// 최근 요청 내역을 조회한다.
        /// </summary>
        public static RequestRecord[] GetRecent(int limit = 20)
        {
            lock (Lock)
            {
                var take = Math.Min(limit, Records.Count);
                return Records.GetRange(Math.Max(0, Records.Count - take), take).ToArray();
            }
        }

        /// <summary>
        /// 지정 시간(ms) 이상 걸린 느린 요청만 조회한다.
        /// </summary>
        public static RequestRecord[] GetSlow(long thresholdMs)
        {
            lock (Lock)
            {
                return Records.Where(r => r.elapsedMs >= thresholdMs).OrderByDescending(r => r.elapsedMs).ToArray();
            }
        }

        /// <summary>
        /// 기록을 모두 지운다.
        /// </summary>
        public static void Clear()
        {
            lock (Lock) { Records.Clear(); }
        }
    }

    /// <summary>
    /// 디버깅 전용 엔드포인트.
    /// </summary>
    internal static class DebugHandlers
    {
        static DebugHandlers()
        {
            "GET".Register("/debug/requests", "Recent request log (query: limit=N)", async (req, res) => await HandleRequestsAsync(req, res));
            "GET".Register("/debug/slow", "Slow requests (query: threshold=N ms)", async (req, res) => await HandleSlowAsync(req, res));
            "POST".Register("/debug/clear", "Clear request log", async (req, res) => await HandleClearAsync(res));
        }

        private static async Task HandleRequestsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var limitRaw = request.QueryString["limit"];
            var limit = 20;
            if (!string.IsNullOrEmpty(limitRaw) && int.TryParse(limitRaw, out var parsed)) limit = Math.Max(1, Math.Min(parsed, 100));
            var recent = McpRequestLogger.GetRecent(limit);
            await UnityMcpBridge.WriteJsonAsync(response, 200, new { count = recent.Length, items = recent });
        }

        private static async Task HandleSlowAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var thresholdRaw = request.QueryString["threshold"];
            var threshold = 500L;
            if (!string.IsNullOrEmpty(thresholdRaw) && long.TryParse(thresholdRaw, out var parsed)) threshold = Math.Max(1, parsed);
            var slow = McpRequestLogger.GetSlow(threshold);
            await UnityMcpBridge.WriteJsonAsync(response, 200, new { thresholdMs = threshold, count = slow.Length, items = slow });
        }

        private static async Task HandleClearAsync(HttpListenerResponse response)
        {
            McpRequestLogger.Clear();
            await UnityMcpBridge.WriteJsonAsync(response, 200, new { success = true, message = "Request log cleared" });
        }
    }
}
#endif
