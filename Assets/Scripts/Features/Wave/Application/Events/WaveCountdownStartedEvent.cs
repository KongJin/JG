namespace Features.Wave.Application.Events
{
    public readonly struct WaveCountdownStartedEvent
    {
        public int WaveIndex { get; }
        public int TotalWaves { get; }
        public float Duration { get; }

        public WaveCountdownStartedEvent(int waveIndex, int totalWaves, float duration)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
            Duration = duration;
        }
    }
}
