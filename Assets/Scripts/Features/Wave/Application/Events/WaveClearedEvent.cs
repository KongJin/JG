namespace Features.Wave.Application.Events
{
    public readonly struct WaveClearedEvent
    {
        public int WaveIndex { get; }

        public WaveClearedEvent(int waveIndex)
        {
            WaveIndex = waveIndex;
        }
    }
}
