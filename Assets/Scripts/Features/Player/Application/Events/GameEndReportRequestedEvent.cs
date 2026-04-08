namespace Features.Player.Application.Events
{
    /// <summary>
    /// 게임 종료 시 통계 리포트 요청 이벤트.
    /// Application 계층에서 발행하고 Bootstrap이 Debug.Log로 출력한다.
    /// </summary>
    public readonly struct GameEndReportRequestedEvent
    {
        public bool IsVictory { get; }
        public int ReachedWave { get; }
        public float PlayTimeSeconds { get; }
        public int SummonCount { get; }
        public int UnitKillCount { get; }

        public GameEndReportRequestedEvent(bool isVictory, int reachedWave, float playTimeSeconds, int summonCount, int unitKillCount)
        {
            IsVictory = isVictory;
            ReachedWave = reachedWave;
            PlayTimeSeconds = playTimeSeconds;
            SummonCount = summonCount;
            UnitKillCount = unitKillCount;
        }
    }
}
