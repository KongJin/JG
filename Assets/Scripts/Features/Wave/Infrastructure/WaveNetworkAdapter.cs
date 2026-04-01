using System;
using ExitGames.Client.Photon;
using Features.Wave.Application.Ports;
using Photon.Pun;

namespace Features.Wave.Infrastructure
{
    public sealed class WaveNetworkAdapter : MonoBehaviourPunCallbacks,
        IWaveNetworkCommandPort, IWaveNetworkCallbackPort
    {
        private const string WaveIndexKey = "waveIndex";
        private const string WaveStateKey = "waveState";

        public Action<int, int> OnWaveStateSynced { get; set; }

        public void SyncWaveState(int waveIndex, int waveStateInt)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return;

            var props = new Hashtable
            {
                { WaveIndexKey, waveIndex },
                { WaveStateKey, waveStateInt }
            };
            room.SetCustomProperties(props);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (PhotonNetwork.IsMasterClient) return;

            if (!propertiesThatChanged.ContainsKey(WaveIndexKey) &&
                !propertiesThatChanged.ContainsKey(WaveStateKey))
                return;

            var (waveIndex, waveState) = ReadRoomProperties();
            OnWaveStateSynced?.Invoke(waveIndex, waveState);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            var (waveIndex, waveState) = ReadRoomProperties();
            OnWaveStateSynced?.Invoke(waveIndex, waveState);
        }

        public void HydrateFromRoomProperties()
        {
            var (waveIndex, waveState) = ReadRoomProperties();

            if (waveIndex == 0 && waveState == 0)
                return;

            OnWaveStateSynced?.Invoke(waveIndex, waveState);
        }

        private (int waveIndex, int waveState) ReadRoomProperties()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return (0, 0);

            var props = room.CustomProperties;
            var waveIndex = props.ContainsKey(WaveIndexKey) ? (int)props[WaveIndexKey] : 0;
            var waveState = props.ContainsKey(WaveStateKey) ? (int)props[WaveStateKey] : 0;
            return (waveIndex, waveState);
        }
    }
}
