using System.Collections.Generic;

namespace Shared.Analytics
{
    /// <summary>
    /// 플레이 데이터 수집 포트. Firebase 등 외부 분석 서비스의 구현체로 교체 가능.
    /// </summary>
    public interface IAnalyticsPort
    {
        // ── 세션 ──
        void LogSessionStart();
        void LogSessionEnd(float sessionDurationSeconds);

        // ── 플레이 루프 ──
        void LogGameStart(string matchId);
        void LogGameEnd(string matchId, float playTimeSeconds, int roundIndex);

        // ── 게임 결과 ──
        void LogGameResult(bool isVictory, int reachedWave, float playTimeSeconds, int summonCount, int unitKillCount);

        // ── 이탈 지점 ──
        void LogDropOff(string context, float elapsedSeconds);

        // ── 핵심 행동 ──
        void LogAction(string actionName, IReadOnlyDictionary<string, object> parameters);
    }
}
