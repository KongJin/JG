using System;

namespace Features.Wave.Application.Ports
{
    public interface IWaveNetworkPort
    {
        int ServerTimestampMs { get; }
        void SyncWaveState(int waveIndex, int waveStateInt, int countdownEndMs);

        Action<int, int, int> OnWaveStateSynced { get; set; }
    }
}
