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

            roster?.Normalize();
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
            if (playerId is int actorNumber)
            {
                HydratePlayerCache(actorNumber);

                if (_cachedRosters.TryGetValue(actorNumber, out var roster))
                {
                    roster.Normalize();
                    return roster;
                }
            }

            return new GarageRoster();
        }

        public bool IsPlayerReady(object playerId)
        {
            if (playerId is int actorNumber)
            {
                HydratePlayerCache(actorNumber);

                if (_cachedReady.TryGetValue(actorNumber, out var ready))
                    return ready;
            }

            return false;
        }

        public Dictionary<object, GarageRoster> GetAllPlayersRosters()
        {
            HydrateRoomCache();

            var result = new Dictionary<object, GarageRoster>();
            foreach (var kvp in _cachedRosters)
            {
                kvp.Value?.Normalize();
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        public GarageRoster GetLocalPlayerRoster()
        {
            if (PhotonNetwork.LocalPlayer == null)
                return new GarageRoster();

            var actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            return GetPlayerRoster(actorNumber);
        }

        // Photon 콜백: MonoBehaviourPunCallbacks에서 public override로 구현해야 인터페이스 메서드가 정확히 일치함.
        // 이 패턴은 PlayerNetworkAdapter, LobbyPhotonAdapter, WaveBootstrap에서 동일하게 사용됨.
        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer == null) return;
            CachePlayerProperties(targetPlayer);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (otherPlayer != null)
            {
                _cachedRosters.Remove(otherPlayer.ActorNumber);
                _cachedReady.Remove(otherPlayer.ActorNumber);
            }
        }

        private void HydrateRoomCache()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom?.Players == null)
                return;

            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                CachePlayerProperties(player);
            }
        }

        private void HydratePlayerCache(int actorNumber)
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom?.Players == null)
                return;

            if (PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out var player))
            {
                CachePlayerProperties(player);
            }
        }

        private void CachePlayerProperties(Photon.Realtime.Player player)
        {
            if (player == null)
                return;

            int actorNumber = player.ActorNumber;
            var props = player.CustomProperties;
            if (props == null)
                return;

            if (TryReadRoster(props, out var roster))
            {
                _cachedRosters[actorNumber] = roster;
            }

            if (TryReadReady(props, out var ready))
            {
                _cachedReady[actorNumber] = ready;
            }
        }

        private static bool TryReadRoster(ExitGames.Client.Photon.Hashtable props, out GarageRoster roster)
        {
            roster = new GarageRoster();

            if (props == null || !props.ContainsKey(KeyGarageRoster) || props[KeyGarageRoster] is not string json)
                return false;

            try
            {
                var wrapper = JsonUtility.FromJson<RosterWrapper>(json);
                roster = wrapper?.roster ?? new GarageRoster();
                roster.Normalize();
                return true;
            }
            catch
            {
                roster = new GarageRoster();
                return true;
            }
        }

        private static bool TryReadReady(ExitGames.Client.Photon.Hashtable props, out bool ready)
        {
            ready = false;

            if (props == null || !props.ContainsKey(KeyGarageReady) || props[KeyGarageReady] is not bool readyValue)
                return false;

            ready = readyValue;
            return true;
        }

        [System.Serializable]
        private class RosterWrapper
        {
            public GarageRoster roster;
        }
    }
}
