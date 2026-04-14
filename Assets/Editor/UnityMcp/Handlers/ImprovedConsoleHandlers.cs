#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// 개선된 Console 핸들러 - 로그 스트리밍, 필터링, 태그 추출 기능 추가
    /// </summary>
    internal static class ImprovedConsoleHandlers
    {
        private static readonly List<LogStreamEntry> StreamedLogs = new List<LogStreamEntry>();
        private static int _nextLogId = 1;
        private static readonly object StreamLock = new object();
        private static readonly HashSet<HttpListenerResponse> ActiveStreamClients = new HashSet<HttpListenerResponse>();
        private static bool _streamActive = false;

        // 태그 추출용 정규식: [태그] 형식
        private static readonly Regex TagRegex = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled);

        static ImprovedConsoleHandlers()
        {
            // 로그 스트리밍 엔드포인트
            "GET".Register("/console/stream", "Real-time log streaming (SSE)", async (req, res) => await HandleLogStreamAsync(req, res));
            "POST".Register("/console/stream/stop", "Stop log streaming client", async (req, res) => await HandleStopStreamAsync(req, res));

            // 로그 필터링된 조회
            "GET".Register("/console/logs/filter", "Filter logs by type/tag/keyword", async (req, res) => await HandleFilteredLogsAsync(req, res));

            // 로그 통계
            "GET".Register("/console/stats", "Log statistics and counts", async (req, res) => await HandleLogStatsAsync(req, res));

            // 기존 엔드포인트 유지 (호환성)
            "GET".Register("/console/errors", "Recent error/exception/assert logs", async (req, res) => await HandleConsoleErrorsAsync(req, res, UnityMcpBridge.ConsoleLogs, UnityMcpBridge.LogLock));
            "GET".Register("/console/logs", "All recent console logs", async (req, res) => await HandleConsoleLogsAsync(req, res, UnityMcpBridge.ConsoleLogs, UnityMcpBridge.LogLock));

            // Unity 로그 캡처 리스너
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        // =====================================================================
        // 로그 스트리밍 (SSE)
        // =====================================================================

        private static async Task HandleLogStreamAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                response.StatusCode = 200;
                response.ContentType = "text/event-stream";
                response.Headers["Cache-Control"] = "no-cache";
                response.Headers["Connection"] = "keep-alive";
                response.Headers["Access-Control-Allow-Origin"] = "*";

                await response.OutputStream.FlushAsync();

                lock (StreamLock)
                {
                    ActiveStreamClients.Add(response);
                    _streamActive = true;
                }

                // 현재 로그들을 모두 전송
                await SendExistingLogsAsync(response);

                // 연결 유지 (SSE ping)
                var pingTimer = new Timer(_ => SendPing(response), null, 30000, 30000);

                // 연결이 끊날 때까지 대기
                var timeoutCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), timeoutCts.Token);
                    lock (StreamLock) ActiveStreamClients.Remove(response);
                });

                while (!response.OutputStream.CanWrite)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                lock (StreamLock) ActiveStreamClients.Remove(response);
                Debug.LogError($"[Unity MCP] Log stream error: {ex.Message}");
            }
        }

        private static async Task HandleStopStreamAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            lock (StreamLock)
            {
                ActiveStreamClients.Clear();
            }
            await UnityMcpBridge.WriteJsonAsync(response, 200, new { success = true, message = "Stream clients cleared" });
        }

        private static async Task SendExistingLogsAsync(HttpListenerResponse response)
        {
            List<LogStreamEntry> logs;
            lock (StreamLock)
            {
                logs = StreamedLogs.ToList();
            }

            foreach (var log in logs)
            {
                await SendSseEventAsync(response, "log", JsonUtility.ToJson(log));
            }

            // 전송 완료 마커
            await SendSseEventAsync(response, "ready", new { initialLogsCount = logs.Count }.ToJson());
        }

        private static async Task SendPing(HttpListenerResponse response)
        {
            try
            {
                if (response.OutputStream.CanWrite)
                {
                    await SendSseEventAsync(response, "ping", "\"keepalive\"");
                }
            }
            catch
            {
                lock (StreamLock) ActiveStreamClients.Remove(response);
            }
        }

        private static async Task SendSseEventAsync(HttpListenerResponse response, string eventType, string data)
        {
            if (!response.OutputStream.CanWrite) return;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(eventType))
            {
                sb.Append("event: ").Append(eventType).Append("\n");
            }
            sb.Append("data: ").Append(data).Append("\n\n");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            await response.OutputStream.FlushAsync();
        }

        // =====================================================================
        // 필터링된 로그 조회
        // =====================================================================

        private static async Task HandleFilteredLogsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var limit = 100;
            var limitRaw = request.QueryString["limit"];
            if (!string.IsNullOrEmpty(limitRaw) && int.TryParse(limitRaw, out var parsed))
            {
                limit = Mathf.Clamp(parsed, 1, 200);
            }

            var types = request.QueryString["types"]?.Split(',') ?? new[] { "Log", "Warning", "Error", "Exception" };
            var tags = request.QueryString["tags"]?.Split(',') ?? Array.Empty<string>();
            var contains = request.QueryString["contains"];
            var sinceIdRaw = request.QueryString["sinceId"];
            var sinceId = 0;
            if (!string.IsNullOrEmpty(sinceIdRaw) && int.TryParse(sinceIdRaw, out var parsedSinceId))
            {
                sinceId = parsedSinceId;
            }

            var payload = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                List<LogStreamEntry> filtered;

                lock (StreamLock)
                {
                    filtered = StreamedLogs
                        .Where(l =>
                        {
                            // ID 필터
                            if (l.id <= sinceId) return false;

                            // 타입 필터
                            if (!types.Contains(l.type)) return false;

                            // 태그 필터
                            if (tags.Length > 0 && !tags.Any(t => l.tags.Contains(t))) return false;

                            // 키워드 필터
                            if (!string.IsNullOrEmpty(contains) &&
                                !l.message.Contains(contains, StringComparison.OrdinalIgnoreCase) &&
                                !l.stackTrace.Contains(contains, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }

                            return true;
                        })
                        .Take(limit)
                        .ToList();
                }

                return new
                {
                    count = filtered.Count,
                    items = filtered,
                    filters = new { types, tags, contains, sinceId }
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, payload);
        }

        // =====================================================================
        // 로그 통계
        // =====================================================================

        private static async Task HandleLogStatsAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var stats = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                int errorCount, warningCount, lastLogId;
                long oldestMs, newestMs;

                lock (StreamLock)
                {
                    errorCount = StreamedLogs.Count(l => l.type == "Error" || l.type == "Exception");
                    warningCount = StreamedLogs.Count(l => l.type == "Warning");
                    lastLogId = StreamedLogs.Count > 0 ? StreamedLogs.Last().id : 0;
                    oldestMs = StreamedLogs.Count > 0 ? StreamedLogs.First().timestampMs : 0;
                    newestMs = StreamedLogs.Count > 0 ? StreamedLogs.Last().timestampMs : 0;
                }

                return new LogStreamStatsResponse
                {
                    totalLogs = StreamedLogs.Count,
                    errorCount = errorCount,
                    warningCount = warningCount,
                    lastLogId = lastLogId,
                    oldestLogTimestampMs = oldestMs,
                    newestLogTimestampMs = newestMs
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, stats);
        }

        // =====================================================================
        // 기존 핸들러 (호환성)
        // =====================================================================

        public static async Task HandleConsoleErrorsAsync(
            HttpListenerRequest request,
            HttpListenerResponse response,
            List<ConsoleLogEntry> consoleLogs,
            object logLock)
        {
            var limit = 20;
            var limitRaw = request.QueryString["limit"];
            if (!string.IsNullOrEmpty(limitRaw) && int.TryParse(limitRaw, out var parsed))
            {
                limit = Mathf.Clamp(parsed, 1, 100);
            }

            var payload = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                ConsoleLogEntry[] latest;
                lock (logLock)
                {
                    var picked = new List<ConsoleLogEntry>(limit);
                    for (var i = consoleLogs.Count - 1; i >= 0 && picked.Count < limit; i--)
                    {
                        var e = consoleLogs[i];
                        if (IsConsoleErrorSeverity(e.type))
                        {
                            picked.Add(e);
                        }
                    }

                    picked.Reverse();
                    latest = picked.ToArray();
                }

                return new ConsoleLogsResponse
                {
                    count = latest.Length,
                    items = latest
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, payload);
        }

        public static async Task HandleConsoleLogsAsync(
            HttpListenerRequest request,
            HttpListenerResponse response,
            List<ConsoleLogEntry> consoleLogs,
            object logLock)
        {
            var limit = 100;
            var limitRaw = request.QueryString["limit"];
            if (!string.IsNullOrEmpty(limitRaw) && int.TryParse(limitRaw, out var parsed))
            {
                limit = Mathf.Clamp(parsed, 1, 200);
            }

            var payload = await UnityMcpBridge.RunOnMainThreadAsync(() =>
            {
                ConsoleLogEntry[] latest;
                lock (logLock)
                {
                    var take = Math.Min(limit, consoleLogs.Count);
                    latest = consoleLogs.GetRange(Math.Max(0, consoleLogs.Count - take), take).ToArray();
                }

                return new ConsoleLogsResponse
                {
                    count = latest.Length,
                    items = latest
                };
            });

            await UnityMcpBridge.WriteJsonAsync(response, 200, payload);
        }

        public static bool IsConsoleErrorSeverity(string type)
        {
            return string.Equals(type, LogType.Error.ToString(), StringComparison.Ordinal)
                || string.Equals(type, LogType.Exception.ToString(), StringComparison.Ordinal)
                || string.Equals(type, LogType.Assert.ToString(), StringComparison.Ordinal);
        }

        // =====================================================================
        // 로그 캡처 및 스트리밍
        // =====================================================================

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var truncatedMessage = UnityMcpBridge.Truncate(condition, UnityMcpBridge.MaxLogMessageLength);
            var truncatedStack = UnityMcpBridge.Truncate(stackTrace, UnityMcpBridge.MaxStackTraceLength);

            // 태그 추출
            var tags = ExtractTags(truncatedMessage).ToArray();

            // 스트리밍용 엔트리 생성
            var streamEntry = new LogStreamEntry
            {
                id = Interlocked.Increment(ref _nextLogId),
                timestampUtc = timestampUtc,
                timestampMs = timestampMs,
                type = type.ToString(),
                message = truncatedMessage,
                stackTrace = truncatedStack,
                tags = tags
            };

            lock (StreamLock)
            {
                StreamedLogs.Add(streamEntry);
                while (StreamedLogs.Count > 1000) // 스트림 로그는 더 많이 저장
                {
                    StreamedLogs.RemoveAt(0);
                }

                // SSE 클라이언트들에게 실시간 전송
                if (ActiveStreamClients.Count > 0)
                {
                    var json = JsonUtility.ToJson(streamEntry);
                    foreach (var client in ActiveStreamClients.ToArray())
                    {
                        if (client.OutputStream.CanWrite)
                        {
                            _ = SendSseEventAsync(client, "log", json);
                        }
                        else
                        {
                            ActiveStreamClients.Remove(client);
                        }
                    }
                }
            }

            // 기존 ConsoleLogs에도 추가 (호환성)
            lock (UnityMcpBridge.LogLock)
            {
                UnityMcpBridge.ConsoleLogs.Add(new ConsoleLogEntry
                {
                    type = type.ToString(),
                    message = truncatedMessage,
                    stackTrace = truncatedStack,
                    timestampUtc = timestampUtc
                });
                while (UnityMcpBridge.ConsoleLogs.Count > UnityMcpBridge.MaxStoredLogs)
                {
                    UnityMcpBridge.ConsoleLogs.RemoveAt(0);
                }
            }
        }

        private static List<string> ExtractTags(string message)
        {
            var tags = new List<string>();
            var matches = TagRegex.Matches(message);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    tags.Add(match.Groups[1].Value);
                }
            }
            return tags;
        }
    }

    // =====================================================================
    // 추가 DTO
    // =====================================================================

    [Serializable]
    internal class LogStreamEntry
    {
        public int id;
        public string timestampUtc;
        public long timestampMs;
        public string type;
        public string message;
        public string stackTrace;
        public string[] tags;
    }
}
#endif
