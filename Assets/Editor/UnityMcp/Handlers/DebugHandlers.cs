#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class DebugHandlers
    {
        static DebugHandlers()
        {
            "GET".Register("/unity/compile-errors", "Check Unity Editor log for compilation errors and warnings", async (req, res) => await HandleCompileErrorsAsync(res));
            "GET".Register("/unity/log-path", "Get Unity Editor log file path", async (req, res) => await HandleLogPathAsync(res));
        }

        public static async Task HandleCompileErrorsAsync(HttpListenerResponse response)
        {
            var logPath = GetEditorLogPath();
            var result = new CompileErrorsResult
            {
                logPath = logPath,
                logExists = File.Exists(logPath),
                errors = new List<LogEntry>(),
                warnings = new List<LogEntry>()
            };

            if (result.logExists)
            {
                var logContent = File.ReadAllText(logPath);

                // 에러 패턴: Assets\Path\ToFile.cs(line,col): error CSxxxx: message
                var errorPattern = @"^(Assets[^\(]+\(\d+,\d+\)): error (CS\d+): (.+)$";
                var warningPattern = @"^(Assets[^\(]+\(\d+,\d+\)): warning (CS\d+): (.+)$";

                var errors = Regex.Matches(logContent, errorPattern, RegexOptions.Multiline)
                    .Cast<Match>()
                    .Select(m => new LogEntry
                    {
                        location = m.Groups[1].Value,
                        code = m.Groups[2].Value,
                        message = m.Groups[3].Value
                    })
                    .ToList();

                var warnings = Regex.Matches(logContent, warningPattern, RegexOptions.Multiline)
                    .Cast<Match>()
                    .Select(m => new LogEntry
                    {
                        location = m.Groups[1].Value,
                        code = m.Groups[2].Value,
                        message = m.Groups[3].Value
                    })
                    .ToList();

                // 중복 제거 (최근 100개)
                result.errors = errors.Take(100).ToList();
                result.warnings = warnings.Take(100).ToList();
                result.errorCount = errors.Count;
                result.warningCount = warnings.Count;

                // 전체 로그에서 최근 에러/경고 라인 찾기
                var lines = logContent.Split('\n');
                var recentErrorLines = new List<string>();
                var recentWarningLines = new List<string>();

                for (int i = lines.Length - 1; i >= 0 && (recentErrorLines.Count < 20 || recentWarningLines.Count < 20); i--)
                {
                    var line = lines[i];
                    if (recentErrorLines.Count < 20 && line.Contains("error CS"))
                    {
                        recentErrorLines.Add(line.Trim());
                    }
                    if (recentWarningLines.Count < 20 && line.Contains("warning CS"))
                    {
                        recentWarningLines.Add(line.Trim());
                    }
                }

                result.recentErrors = recentErrorLines.ToArray();
                result.recentWarnings = recentWarningLines.ToArray();
            }

            result.success = result.errorCount == 0;
            result.timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            await UnityMcpBridge.WriteJsonAsync(response, 200, result);
        }

        public static async Task HandleLogPathAsync(HttpListenerResponse response)
        {
            var logPath = GetEditorLogPath();
            var exists = File.Exists(logPath);
            var lastModified = exists ? File.GetLastWriteTimeUtc(logPath).ToString("yyyy-MM-dd HH:mm:ss.fff") : "N/A";
            var sizeKb = exists ? (File.ReadAllBytes(logPath).Length / 1024.0).ToString("F2") : "0";

            await UnityMcpBridge.WriteJsonAsync(response, 200, new
            {
                logPath = logPath,
                exists = exists,
                lastModifiedUtc = lastModified,
                sizeKb = sizeKb
            });
        }

        private static string GetEditorLogPath()
        {
            // Windows: C:\Users\<username>\AppData\Local\Unity\Editor\Editor.log
            // macOS: ~/Library/Logs/Unity/Editor.log
            // Linux: ~/.config/unity3d/Editor.log

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Unity",
                    "Editor",
                    "Editor.log");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library",
                    "Logs",
                    "Unity",
                    "Editor.log");
            }
            else // Linux
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".config",
                    "unity3d",
                    "Editor.log");
            }
        }
    }

    internal class CompileErrorsResult
    {
        public bool success;
        public string logPath;
        public bool logExists;
        public int errorCount;
        public int warningCount;
        public List<LogEntry> errors;
        public List<LogEntry> warnings;
        public string[] recentErrors;
        public string[] recentWarnings;
        public string timestampUtc;
    }

    internal class LogEntry
    {
        public string location;
        public string code;
        public string message;
    }
}
#endif
