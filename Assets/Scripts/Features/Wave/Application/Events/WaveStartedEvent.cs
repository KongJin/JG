namespace Features.Wave.Application.Events
{
    public readonly struct WaveStartedEvent
    {
        public int WaveIndex { get; }
        public int TotalWaves { get; }
        public int EnemyCount { get; }

        public WaveStartedEvent(int waveIndex, int totalWaves, int enemyCount)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
            EnemyCount = enemyCount;
        }
    }
}
