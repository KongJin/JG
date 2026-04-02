using Features.Wave.Domain;

namespace Features.Wave.Application.Events
{
    public readonly struct WaveHydratedEvent
    {
        public int WaveIndex { get; }
        public int TotalWaves { get; }
        public WaveState State { get; }
        public float CountdownRemaining { get; }

        public WaveHydratedEvent(int waveIndex, int totalWaves, WaveState state, float countdownRemaining)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
            State = state;
            CountdownRemaining = countdownRemaining;
        }
    }
}
