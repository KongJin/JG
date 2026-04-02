using System;

namespace Features.Wave.Application.Ports
{
    public interface IWaveNetworkCallbackPort
    {
        /// <param name="waveIndex">Current wave index</param>
        /// <param name="waveStateInt">WaveState as int</param>
        /// <param name="countdownEndMs">PhotonNetwork.ServerTimestamp (ms) when countdown ends; 0 if not in Countdown</param>
        Action<int, int, int> OnWaveStateSynced { get; set; }
    }
}
