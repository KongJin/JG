using System.Collections.Generic;
using ExitGames.Client.Photon;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Photon.Pun;
using Photon.Realtime;
using Shared.Kernel;
using UnityEngine;
using DomainRoom = Features.Lobby.Domain.Room;
using PhotonPlayer = Photon.Realtime.Player;

namespace Features.Lobby.Infrastructure.Photon
{
    internal sealed class LobbyPhotonCallbackTranslator
    {
        public void HandleCreatedRoom(LobbyPhotonPendingState pendingState, System.Action<DomainRoom> onCreateRoomSucceeded)
        {
            var room = pendingState.TakePendingCreateRoom();
// csharp-guardrails: allow-null-defense
            if (room == null)
            {
                Debug.LogWarning("[LobbyPhotonAdapter] Unexpected OnCreatedRoom: no pending create.");
                return;
            }

            onCreateRoomSucceeded?.Invoke(room);
        }

        public void HandleJoinedRoom(LobbyPhotonPendingState pendingState, System.Action<JoinRoomData> onJoinRoomSucceeded)
        {
            if (!pendingState.TryConsumeJoin())
                return;

            var photonRoom = PhotonNetwork.CurrentRoom;
            var roomId = new DomainEntityId(photonRoom.Name);
            var roomName =
                photonRoom.CustomProperties.TryGetValue(
                    LobbyPhotonConstants.RoomDisplayNameKey,
                    out var nameRaw
                ) && nameRaw is string nameStr
                    ? nameStr
                    : photonRoom.Name;

            var members = LobbyPhotonRoomMapper.BuildMembersFromPlayers(photonRoom);
            if (members.Count == 0)
            {
                Debug.LogError("[LobbyPhotonAdapter] OnJoinedRoom: no members could be built from players.");
                return;
            }

            DomainEntityId masterMemberId = default;
            if (photonRoom.Players.TryGetValue(photonRoom.MasterClientId, out var masterPlayer))
            {
                var member = LobbyPhotonRoomMapper.BuildMemberFromPlayer(masterPlayer);
// csharp-guardrails: allow-null-defense
                if (member != null)
                    masterMemberId = member.Id;
            }

            var localMember = LobbyPhotonRoomMapper.BuildMemberFromPlayer(PhotonNetwork.LocalPlayer);
// csharp-guardrails: allow-null-defense
            var localMemberId = localMember != null ? localMember.Id : default;
            var difficultyPreset = LobbyPhotonRoomMapper.ReadDifficultyPresetFromProps(photonRoom.CustomProperties);

            onJoinRoomSucceeded?.Invoke(
                new JoinRoomData(
                    roomId,
                    roomName,
                    photonRoom.MaxPlayers,
                    members,
                    masterMemberId,
                    localMemberId,
                    difficultyPreset));
        }

        public void HandlePlayerEnteredRoom(PhotonPlayer newPlayer, System.Action<DomainEntityId, RoomMember> onRemotePlayerEntered)
        {
            if (!PhotonNetwork.InRoom || newPlayer == PhotonNetwork.LocalPlayer)
                return;

            var member = LobbyPhotonRoomMapper.BuildMemberFromPlayer(newPlayer);
// csharp-guardrails: allow-null-defense
            if (member == null)
            {
                Debug.LogWarning("[LobbyPhotonAdapter] Remote player entered but has no memberId property.");
                return;
            }

            var roomId = new DomainEntityId(PhotonNetwork.CurrentRoom.Name);
            onRemotePlayerEntered?.Invoke(roomId, member);
        }

        public void HandlePlayerLeftRoom(PhotonPlayer otherPlayer, System.Action<DomainEntityId, DomainEntityId> onRemotePlayerLeft)
        {
            if (!PhotonNetwork.InRoom || otherPlayer == PhotonNetwork.LocalPlayer)
                return;

            if (!otherPlayer.CustomProperties.TryGetValue(LobbyPhotonConstants.MemberIdKey, out var memberIdRaw) ||
                memberIdRaw is not string memberIdStr)
            {
                Debug.LogWarning("[LobbyPhotonAdapter] Remote player left but has no memberId property.");
                return;
            }

            var roomId = new DomainEntityId(PhotonNetwork.CurrentRoom.Name);
            var memberId = new DomainEntityId(memberIdStr);
            onRemotePlayerLeft?.Invoke(roomId, memberId);
        }

        public void HandlePlayerPropertiesUpdate(
            PhotonPlayer targetPlayer,
            Hashtable changedProps,
            System.Action<PlayerPropertiesData> onPlayerPropertiesChanged)
        {
            if (!PhotonNetwork.InRoom)
                return;

            if (!targetPlayer.CustomProperties.TryGetValue(LobbyPhotonConstants.MemberIdKey, out var midRaw) ||
                midRaw is not string memberIdText)
                return;

            TeamType? team =
                changedProps.TryGetValue(LobbyPhotonConstants.TeamKey, out var teamRaw)
                && teamRaw is int teamInt
                    ? (TeamType)teamInt
                    : null;
            bool? isReady =
                changedProps.TryGetValue(LobbyPhotonConstants.IsReadyKey, out var readyRaw)
                && readyRaw is bool readyBool
                    ? readyBool
                    : null;

            if (!team.HasValue && !isReady.HasValue)
                return;

            var roomId = new DomainEntityId(PhotonNetwork.CurrentRoom.Name);
            var memberId = new DomainEntityId(memberIdText);
            onPlayerPropertiesChanged?.Invoke(new PlayerPropertiesData(roomId, memberId, team, isReady));
        }

        public void HandleEvent(EventData photonEvent, System.Action<DomainEntityId> onGameStarted)
        {
            if (photonEvent.Code != LobbyPhotonConstants.GameStartedEventCode)
                return;

            if (photonEvent.CustomData is not string roomIdValue || string.IsNullOrWhiteSpace(roomIdValue))
            {
                Debug.LogWarning("[LobbyPhotonAdapter] Received GameStarted event with invalid payload.");
                return;
            }

            onGameStarted?.Invoke(new DomainEntityId(roomIdValue));
        }

        public List<RoomListItem> BuildRoomListItems(List<RoomInfo> roomList)
        {
            var items = new List<RoomListItem>();
            foreach (var info in roomList)
            {
                if (info.RemovedFromList || !info.IsVisible)
                    continue;

                var roomId = new DomainEntityId(info.Name);
                string displayName = info.Name;
                if (info.CustomProperties.TryGetValue(LobbyPhotonConstants.RoomDisplayNameKey, out var nameRaw) &&
                    nameRaw is string nameStr)
                {
                    displayName = nameStr;
                }

                var difficulty = LobbyPhotonRoomMapper.ReadDifficultyPresetFromProps(info.CustomProperties);
                items.Add(new RoomListItem(
                    roomId,
                    displayName,
                    info.PlayerCount,
                    info.MaxPlayers,
                    info.IsOpen,
                    difficulty));
            }

            return items;
        }
    }
}
