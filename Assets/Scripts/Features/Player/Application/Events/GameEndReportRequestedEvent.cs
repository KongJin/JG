using System;

namespace Features.Player.Application.Events
{
    /// <summary>
    /// 게임 종료 시 통계 리포트 요청 이벤트.
    /// Application 계층에서 발행하고 Bootstrap이 수신해서 로그로 출력한다.
    /// </summary>
    public readonly struct GameEndReportRequestedEvent
    {
        public bool IsVictory { get; }
        public int ReachedWave { get; }
        public float PlayTimeSeconds { get; }
        public int SummonCount { get; }
        public int UnitKillCount { get; }
        public ResultContributionCard[] ContributionCards { get; }
        public float CoreRemainingHealth { get; }
        public float CoreMaxHealth { get; }

        public GameEndReportRequestedEvent(
            bool isVictory,
            int reachedWave,
            float playTimeSeconds,
            int summonCount,
            int unitKillCount,
            ResultContributionCard[] contributionCards = null,
            float coreRemainingHealth = 0f,
            float coreMaxHealth = 0f)
        {
            IsVictory = isVictory;
            ReachedWave = reachedWave;
            PlayTimeSeconds = playTimeSeconds;
            SummonCount = summonCount;
            UnitKillCount = unitKillCount;
            ContributionCards = contributionCards ?? Array.Empty<ResultContributionCard>();
            CoreRemainingHealth = coreRemainingHealth;
            CoreMaxHealth = coreMaxHealth;
        }
    }
}
