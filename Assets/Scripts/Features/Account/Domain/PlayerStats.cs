using System;

namespace Features.Account.Domain
{
    /// <summary>
    /// 플레이어 전적 데이터 (ValueObject).
    /// </summary>
    [Serializable]
    public sealed class PlayerStats
    {
        public float totalPlayTimeSeconds;
        public int totalGames;
        public int totalVictories;
        public int totalDefeats;
        public int highestWave;
        public int totalSummons;
        public int totalUnitKills;

        public PlayerStats() { }

        public PlayerStats Clone() => (PlayerStats)MemberwiseClone();

        public float VictoryRate => totalGames > 0 ? (float)totalVictories / totalGames : 0f;
    }
}
