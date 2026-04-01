using System;

namespace Features.Wave.Application.Ports
{
    public interface IWaveNetworkCallbackPort
    {
        Action<int, int> OnWaveStateSynced { get; set; }
    }
}
