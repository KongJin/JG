using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Shared.Analytics
{
    /// <summary>
    /// Firebase Analytics 어댑터.
    /// WebGL 빌드: 실제 Firebase JS SDK 호출 (__Internal DllImport).
    /// 에디터/기타 플랫폼: 디버깅용 콘솔 로그 (스텁).
    /// </summary>
    public sealed class FirebaseAnalyticsAdapter : IAnalyticsPort
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FirebaseAnalytics_Init();
        [DllImport("__Internal")] static extern void FirebaseAnalytics_LogEvent(string eventName, string jsonParams);
#else
        static void FirebaseAnalytics_Init() { }
        static void FirebaseAnalytics_LogEvent(string eventName, string jsonParams)
        {
            // Editor stub — logs to console. Real Firebase calls in WebGL build.
            Debug.Log($"[FirebaseStub] {eventName} {jsonParams}");
        }
#endif

        public FirebaseAnalyticsAdapter()
        {
            FirebaseAnalytics_Init();
        }

        public void LogSessionStart()
        {
            LogEvent("session_start");
        }

        public void LogSessionEnd(float sessionDurationSeconds)
        {
            LogEvent("session_end", new Dictionary<string, object>
            {
                { "duration_seconds", sessionDurationSeconds }
            });
        }

        public void LogGameStart(string matchId)
        {
            LogEvent("game_start", new Dictionary<string, object>
            {
                { "match_id", matchId }
            });
        }

        public void LogGameEnd(string matchId, float playTimeSeconds, int roundIndex)
        {
            LogEvent("game_end", new Dictionary<string, object>
            {
                { "match_id", matchId },
                { "play_time_seconds", playTimeSeconds },
                { "round_index", roundIndex }
            });
        }

        public void LogGameResult(bool isVictory, int reachedWave, float playTimeSeconds, int summonCount, int unitKillCount)
        {
            LogEvent("game_result", new Dictionary<string, object>
            {
                { "is_victory", isVictory },
                { "reached_wave", reachedWave },
                { "play_time_seconds", playTimeSeconds },
                { "summon_count", summonCount },
                { "unit_kill_count", unitKillCount },
                { "kd_ratio", summonCount > 0 ? (float)unitKillCount / summonCount : 0f }
            });
        }

        public void LogDropOff(string context, float elapsedSeconds)
        {
            LogEvent("drop_off", new Dictionary<string, object>
            {
                { "context", context },
                { "elapsed_seconds", elapsedSeconds }
            });
        }

        public void LogAction(string actionName, IReadOnlyDictionary<string, object> parameters)
        {
            LogEvent(actionName, parameters);
        }

        void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            var json = parameters != null ? ToJson(parameters) : "{}";
            FirebaseAnalytics_LogEvent(eventName, json);
        }

        static string ToJson(IReadOnlyDictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('{');
            var first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(EscapeJson(kv.Key)).Append("\":");
                if (kv.Value is string s)
                    sb.Append('"').Append(EscapeJson(s)).Append('"');
                else
                    sb.Append(kv.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
