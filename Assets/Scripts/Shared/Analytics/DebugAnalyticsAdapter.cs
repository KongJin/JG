using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shared.Analytics
{
    /// <summary>
    /// IAnalyticsPort의 Debug.Log 스텁 구현. Firebase SDK 연동 전까지 사용.
    /// </summary>
    public sealed class DebugAnalyticsAdapter : IAnalyticsPort
    {
        const string Tag = "[Analytics]";

        public void LogSessionStart()
        {
            Debug.Log($"{Tag} SessionStart");
        }

        public void LogSessionEnd(float sessionDurationSeconds)
        {
            Debug.Log($"{Tag} SessionEnd | duration={sessionDurationSeconds:F1}s");
        }

        public void LogGameStart(string matchId)
        {
            Debug.Log($"{Tag} GameStart | matchId={matchId}");
        }

        public void LogGameEnd(string matchId, float playTimeSeconds, int roundIndex)
        {
            Debug.Log($"{Tag} GameEnd | matchId={matchId} playTime={playTimeSeconds:F1}s round={roundIndex}");
        }

        public void LogDropOff(string context, float elapsedSeconds)
        {
            Debug.Log($"{Tag} DropOff | context={context} elapsed={elapsedSeconds:F1}s");
        }

        public void LogAction(string actionName, IReadOnlyDictionary<string, object> parameters)
        {
            var sb = new StringBuilder();
            sb.Append($"{Tag} Action | name={actionName}");
            if (parameters != null)
            {
                foreach (var kv in parameters)
                    sb.Append($" {kv.Key}={kv.Value}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
