namespace Features.Wave.Application.Events
{
    public readonly struct UpgradeSelectionRequestedEvent
    {
        public UpgradeSelectionRequestedEvent(int waveIndex)
        {
            WaveIndex = waveIndex;
        }

        public int WaveIndex { get; }
    }
}
