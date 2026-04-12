#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class ConsoleHandlers
    {
        static ConsoleHandlers()
        {
            "GET".Register("/console/errors", "Recent error/exception/assert logs", async (req, res) => await HandleConsoleErrorsAsync(req, res, UnityMcpBridge.ConsoleLogs, UnityMcpBridge.LogLock));
            "GET".Register("/console/logs", "All recent console logs", async (req, res) => await HandleConsoleLogsAsync(req, res, UnityMcpBridge.ConsoleLogs, UnityMcpBridge.LogLock));
        }

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
    }
}
#endif
