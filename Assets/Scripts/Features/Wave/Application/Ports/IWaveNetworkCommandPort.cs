namespace Features.Wave.Application.Ports
{
    public interface IWaveNetworkCommandPort
    {
        void SyncWaveState(int waveIndex, int waveStateInt);
    }
}
