using System.Collections.Generic;
using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// Photon CustomProperties를 통한 편성 데이터 네트워크 동기화.
    /// Player CustomProperties["garageRoster"] (JSON 직렬화)
    /// Player CustomProperties["garageReady"] (bool)
    /// </summary>
    public sealed class GarageNetworkAdapter : MonoBehaviourPunCallbacks, IGarageNetworkPort
    {
        private const string KeyGarageRoster = "garageRoster";
        private const string KeyGarageReady = "garageReady";

        private readonly Dictionary<int, GarageRoster> _cachedRosters = new Dictionary<int, GarageRoster>();
        private readonly Dictionary<int, bool> _cachedReady = new Dictionary<int, bool>();

        public void SyncRoster(GarageRoster roster)
        {
            if (PhotonNetwork.LocalPlayer == null) return;

            string json = JsonUtility.ToJson(new RosterWrapper { roster = roster });
            var props = new ExitGames.Client.Photon.Hashtable { { KeyGarageRoster, json } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public void SyncReady(bool isReady)
        {
            if (PhotonNetwork.LocalPlayer == null) return;

            var props = new ExitGames.Client.Photon.Hashtable { { KeyGarageReady, isReady } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public GarageRoster GetPlayerRoster(object playerId)
        {
            if (playerId is int actorNumber && _cachedRosters.TryGetValue(actorNumber, out var roster))
                return roster;
            return new GarageRoster();
        }

        public bool IsPlayerReady(object playerId)
        {
            if (playerId is int actorNumber && _cachedReady.TryGetValue(actorNumber, out var ready))
                return ready;
            return false;
        }

        public Dictionary<object, GarageRoster> GetAllPlayersRosters()
        {
            var result = new Dictionary<object, GarageRoster>();
            foreach (var kvp in _cachedRosters)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        // Photon 콜백: MonoBehaviourPunCallbacks에서 public override로 구현해야 인터페이스 메서드가 정확히 일치함.
        // 이 패턴은 PlayerNetworkAdapter, LobbyPhotonAdapter, WaveBootstrap에서 동일하게 사용됨.
        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer == null) return;
            int actorNumber = targetPlayer.ActorNumber;

            if (changedProps.ContainsKey(KeyGarageRoster) && changedProps[KeyGarageRoster] is string json)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<RosterWrapper>(json);
                    _cachedRosters[actorNumber] = wrapper?.roster ?? new GarageRoster();
                }
                catch
                {
                    _cachedRosters[actorNumber] = new GarageRoster();
                }
            }

            if (changedProps.ContainsKey(KeyGarageReady) && changedProps[KeyGarageReady] is bool ready)
            {
                _cachedReady[actorNumber] = ready;
            }
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (otherPlayer != null)
            {
                _cachedRosters.Remove(otherPlayer.ActorNumber);
                _cachedReady.Remove(otherPlayer.ActorNumber);
            }
        }

        [System.Serializable]
        private class RosterWrapper
        {
            public GarageRoster roster;
        }
    }
}
