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
        private const string CountdownEndKey = "countdownEnd";

        public int ServerTimestampMs => PhotonNetwork.ServerTimestamp;

        public Action<int, int, int> OnWaveStateSynced { get; set; }

        public void SyncWaveState(int waveIndex, int waveStateInt, int countdownEndMs)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return;

            var props = new Hashtable
            {
                { WaveIndexKey, waveIndex },
                { WaveStateKey, waveStateInt },
                { CountdownEndKey, countdownEndMs }
            };
            room.SetCustomProperties(props);
        }

        public void ResetRoomPropertiesForNewMatch()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
                return;

            var props = new Hashtable
            {
                { WaveIndexKey, 0 },
                { WaveStateKey, 0 },
                { CountdownEndKey, 0 }
            };
            room.SetCustomProperties(props);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (PhotonNetwork.IsMasterClient) return;

            if (!propertiesThatChanged.ContainsKey(WaveIndexKey) &&
                !propertiesThatChanged.ContainsKey(WaveStateKey))
                return;

            var (waveIndex, waveState, countdownEndMs) = ReadRoomProperties();
            OnWaveStateSynced?.Invoke(waveIndex, waveState, countdownEndMs);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            var (waveIndex, waveState, countdownEndMs) = ReadRoomProperties();
            OnWaveStateSynced?.Invoke(waveIndex, waveState, countdownEndMs);
        }

        public void HydrateFromRoomProperties()
        {
            var (waveIndex, waveState, countdownEndMs) = ReadRoomProperties();

            if (waveIndex == 0 && waveState == 0)
                return;

            OnWaveStateSynced?.Invoke(waveIndex, waveState, countdownEndMs);
        }

        private (int waveIndex, int waveState, int countdownEndMs) ReadRoomProperties()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return (0, 0, 0);

            var props = room.CustomProperties;
            var waveIndex = props.ContainsKey(WaveIndexKey) ? (int)props[WaveIndexKey] : 0;
            var waveState = props.ContainsKey(WaveStateKey) ? (int)props[WaveStateKey] : 0;
            var countdownEndMs = props.ContainsKey(CountdownEndKey) ? (int)props[CountdownEndKey] : 0;
            return (waveIndex, waveState, countdownEndMs);
        }
    }
}
