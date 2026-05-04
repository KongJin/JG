using System;
using System.Collections.Generic;
using Features.Lobby.Domain;
using Shared.Kernel;

namespace Features.Lobby.Application.Ports
{
    public sealed class JoinRoomData
    {
        public DomainEntityId RoomId { get; }
        public string RoomName { get; }
        public int Capacity { get; }
        public List<RoomMember> Members { get; }
        public DomainEntityId MasterMemberId { get; }
        public DomainEntityId LocalMemberId { get; }
        public int DifficultyPresetId { get; }

        public JoinRoomData(
            DomainEntityId roomId,
            string roomName,
            int capacity,
            List<RoomMember> members,
            DomainEntityId masterMemberId,
            DomainEntityId localMemberId,
            int difficultyPresetId = 0)
        {
            RoomId = roomId;
            RoomName = roomName;
            Capacity = capacity;
            Members = members;
            MasterMemberId = masterMemberId;
            LocalMemberId = localMemberId;
            DifficultyPresetId = difficultyPresetId;
        }
    }

    public sealed class PlayerPropertiesData
    {
        public DomainEntityId RoomId { get; }
        public DomainEntityId MemberId { get; }
        public TeamType? Team { get; }
        public bool? IsReady { get; }

        public PlayerPropertiesData(DomainEntityId roomId, DomainEntityId memberId, TeamType? team, bool? isReady)
        {
            RoomId = roomId;
            MemberId = memberId;
            Team = team;
            IsReady = isReady;
        }
    }

    public sealed class RoomListItem
    {
        public DomainEntityId RoomId { get; }
        public string RoomName { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public bool IsOpen { get; }
        public int DifficultyPresetId { get; }

        public RoomListItem(
            DomainEntityId roomId,
            string roomName,
            int playerCount,
            int maxPlayers,
            bool isOpen,
            int difficultyPresetId = 0)
        {
            RoomId = roomId;
            RoomName = roomName;
            PlayerCount = playerCount;
            MaxPlayers = maxPlayers;
            IsOpen = isOpen;
            DifficultyPresetId = difficultyPresetId;
        }
    }

    public interface ILobbyNetworkPort
    {
        Result CreateRoom(Room room);
        Result JoinRoom(DomainEntityId roomId, RoomMember localMember);
        Result LeaveRoom(DomainEntityId roomId, DomainEntityId memberId);
        Result ChangeTeam(DomainEntityId memberId, TeamType team);
        Result SetReady(DomainEntityId memberId, bool isReady);
        Result StartGame(DomainEntityId roomId);

        Action<Room> OnCreateRoomSucceeded { set; }
        Action<string> OnErrorOccurred { set; }
        Action<JoinRoomData> OnJoinRoomSucceeded { set; }
        Action<DomainEntityId, DomainEntityId> OnLeaveRoomSucceeded { set; }
        Action<DomainEntityId, RoomMember> OnRemotePlayerEntered { set; }
        Action<DomainEntityId, DomainEntityId> OnRemotePlayerLeft { set; }
        Action<PlayerPropertiesData> OnPlayerPropertiesChanged { set; }
        Action<DomainEntityId> OnGameStarted { set; }
        Action<List<RoomListItem>> OnRoomListUpdated { set; }
    }
}
