namespace Features.Wave.Application.Ports
{
    public interface IWaveNetworkCommandPort
    {
        int ServerTimestampMs { get; }
        void SyncWaveState(int waveIndex, int waveStateInt, int countdownEndMs);
    }
}
