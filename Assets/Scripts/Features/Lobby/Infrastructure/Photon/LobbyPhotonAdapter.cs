using System;
using Shared.Attributes;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Photon.Pun;
using Photon.Realtime;
using Shared.Gameplay;
using UnityEngine;
using DomainRoom = Features.Lobby.Domain.Room;
using PhotonPlayer = Photon.Realtime.Player;
using PhotonRoom = Photon.Realtime.Room;
using Result = Shared.Kernel.Result;
using Shared.Kernel;

namespace Features.Lobby.Infrastructure.Photon
{
    public sealed class LobbyPhotonAdapter
        : MonoBehaviourPunCallbacks,
            ILobbyNetworkPort,
            IOnEventCallback
    {
        [Required, SerializeField]
        private string DefaultBattleSceneName = "BattleScene";

        private readonly PhotonPlayerPropertyManager _propertyManager = new();

        private readonly LobbyPhotonPendingState _pendingState = new();
        private readonly LobbyPhotonCallbackTranslator _callbackTranslator = new();

        public System.Action<DomainRoom> OnCreateRoomSucceeded { get; set; }
        public System.Action<string> OnErrorOccurred { get; set; }
        public System.Action<JoinRoomData> OnJoinRoomSucceeded { get; set; }
        public System.Action<DomainEntityId, DomainEntityId> OnLeaveRoomSucceeded { get; set; }
        public System.Action<DomainEntityId, RoomMember> OnRemotePlayerEntered { get; set; }
        public System.Action<DomainEntityId, DomainEntityId> OnRemotePlayerLeft { get; set; }
        public System.Action<PlayerPropertiesData> OnPlayerPropertiesChanged { get; set; }
        public System.Action<DomainEntityId> OnGameStarted { get; set; }
        public System.Action<List<RoomListItem>> OnRoomListUpdated { get; set; }

        private void Start()
        {
            EnsureConnected();
        }

        public Result CreateRoom(DomainRoom room)
        {
            if (room == null)
                return Result.Failure("Room is required.");

            var connected = LobbyPhotonCommandValidator.ValidateConnected();
            if (!connected.IsSuccess)
                return connected;

            var notInRoom = LobbyPhotonCommandValidator.ValidateNotInRoom();
            if (!notInRoom.IsSuccess)
                return notInRoom;

            if (room.Capacity > byte.MaxValue)
                return Result.Failure("Room capacity exceeds Photon max byte size.");

            var owner = room.FindMember(room.OwnerId);
// csharp-guardrails: allow-null-defense
            if (owner == null)
                return Result.Failure("Room owner is required.");

            if (!_propertyManager.SetLocalMemberProperties(owner))
                return Result.Failure("Local player is unavailable.");

            var options = new RoomOptions
            {
                MaxPlayers = (byte)room.Capacity,
                IsVisible = true,
                IsOpen = true,
                CleanupCacheOnLeave = true,
                CustomRoomProperties = new Hashtable
                {
                    [LobbyPhotonConstants.RoomDisplayNameKey] = room.Name,
                    [DifficultyPreset.RoomPropertyKey] = room.DifficultyPresetId,
                },
                CustomRoomPropertiesForLobby = new[]
                {
                    LobbyPhotonConstants.RoomDisplayNameKey,
                    DifficultyPreset.RoomPropertyKey,
                },
            };

            _pendingState.SetCreateRoom(room);

            var created = PhotonNetwork.CreateRoom(room.Id.Value, options, TypedLobby.Default);
            if (!created)
            {
                _pendingState.Clear();
                return Result.Failure("Failed to send CreateRoom request to Photon.");
            }

            return Result.Success();
        }

        public Result JoinRoom(DomainEntityId roomId, RoomMember localMember)
        {
            if (localMember == null)
                return Result.Failure("Member is required.");

            if (string.IsNullOrWhiteSpace(roomId.Value))
                return Result.Failure("Room id is required.");

            var connected = LobbyPhotonCommandValidator.ValidateConnected();
            if (!connected.IsSuccess)
                return connected;

            var notInRoom = LobbyPhotonCommandValidator.ValidateNotInRoom();
            if (!notInRoom.IsSuccess)
                return notInRoom;

            if (!_propertyManager.SetLocalMemberProperties(localMember))
                return Result.Failure("Local player is unavailable.");

            _pendingState.SetJoinRoom();

            var joined = PhotonNetwork.JoinRoom(roomId.Value);
            if (!joined)
            {
                _pendingState.Clear();
                return Result.Failure("Failed to send JoinRoom request to Photon.");
            }

            return Result.Success();
        }

        public Result LeaveRoom(DomainEntityId roomId, DomainEntityId memberId)
        {
            var inRoom = LobbyPhotonCommandValidator.ValidateInRoom();
            if (!inRoom.IsSuccess)
                return inRoom;

            if (
                !string.Equals(
                    PhotonNetwork.CurrentRoom.Name,
                    roomId.Value,
                    StringComparison.Ordinal
                )
            )
                return Result.Failure("Current room does not match target room id.");

            _pendingState.SetLeaveRoom(roomId, memberId);

            var left = PhotonNetwork.LeaveRoom();
            if (!left)
            {
                _pendingState.Clear();
                return Result.Failure("Failed to send LeaveRoom request to Photon.");
            }

            return Result.Success();
        }

        public Result ChangeTeam(DomainEntityId memberId, TeamType team)
        {
            var inRoom = LobbyPhotonCommandValidator.ValidateInRoom();
            if (!inRoom.IsSuccess)
                return inRoom;

            var localMember = LobbyPhotonCommandValidator.ValidateLocalMember(_propertyManager, memberId);
            if (!localMember.IsSuccess)
                return localMember;

            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { [LobbyPhotonConstants.TeamKey] = (int)team }
            );
            return Result.Success();
        }

        public Result SetReady(DomainEntityId memberId, bool isReady)
        {
            var inRoom = LobbyPhotonCommandValidator.ValidateInRoom();
            if (!inRoom.IsSuccess)
                return inRoom;

            var localMember = LobbyPhotonCommandValidator.ValidateLocalMember(_propertyManager, memberId);
            if (!localMember.IsSuccess)
                return localMember;

            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { [LobbyPhotonConstants.IsReadyKey] = isReady }
            );
            return Result.Success();
        }

        public Result StartGame(DomainEntityId roomId)
        {
            var inRoom = LobbyPhotonCommandValidator.ValidateInRoom();
            if (!inRoom.IsSuccess)
                return inRoom;

            if (!PhotonNetwork.IsMasterClient)
                return Result.Failure("Only the room master can start the game.");

            var raised = PhotonNetwork.RaiseEvent(
                LobbyPhotonConstants.GameStartedEventCode,
                roomId.Value,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable
            );
            if (!raised)
                return Result.Failure("Failed to raise game started event.");

            PhotonNetwork.LoadLevel(DefaultBattleSceneName);
            return Result.Success();
        }

        // ===== Photon Callbacks =====

        public override void OnConnectedToMaster()
        {
            Debug.Log("[LobbyPhotonAdapter] Connected to Master. Joining lobby...");
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("[LobbyPhotonAdapter] Joined lobby. Ready for matchmaking.");
        }

        public override void OnCreatedRoom()
        {
            _callbackTranslator.HandleCreatedRoom(_pendingState, OnCreateRoomSucceeded);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            _pendingState.ClearCreateRoom();
            OnErrorOccurred?.Invoke($"Create room failed ({returnCode}): {message}");
        }

        public override void OnJoinedRoom()
        {
            _callbackTranslator.HandleJoinedRoom(_pendingState, OnJoinRoomSucceeded);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            _pendingState.ClearJoin();
            OnErrorOccurred?.Invoke($"Join room failed ({returnCode}): {message}");
        }

        public override void OnLeftRoom()
        {
            var pendingLeave = _pendingState.TakePendingLeave();
            var roomId = pendingLeave.RoomId;
            var memberId = pendingLeave.MemberId;

            if (string.IsNullOrWhiteSpace(roomId.Value))
            {
                Debug.LogWarning(
                    "[LobbyPhotonAdapter] Unexpected OnLeftRoom: no pending leave info."
                );
                return;
            }

            OnLeaveRoomSucceeded?.Invoke(roomId, memberId);
        }

        public override void OnPlayerEnteredRoom(PhotonPlayer newPlayer)
        {
            _callbackTranslator.HandlePlayerEnteredRoom(newPlayer, OnRemotePlayerEntered);
        }

        public override void OnPlayerLeftRoom(PhotonPlayer otherPlayer)
        {
            _callbackTranslator.HandlePlayerLeftRoom(otherPlayer, OnRemotePlayerLeft);
        }

        public override void OnPlayerPropertiesUpdate(PhotonPlayer targetPlayer, Hashtable changedProps)
        {
            _callbackTranslator.HandlePlayerPropertiesUpdate(targetPlayer, changedProps, OnPlayerPropertiesChanged);
        }

        public void OnEvent(EventData photonEvent)
        {
            _callbackTranslator.HandleEvent(photonEvent, OnGameStarted);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            OnRoomListUpdated?.Invoke(_callbackTranslator.BuildRoomListItems(roomList));
        }

        private static void EnsureConnected()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (PhotonNetwork.IsConnectedAndReady)
            {
                if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
                    PhotonNetwork.JoinLobby();
                return;
            }

            if (PhotonNetwork.IsConnected)
                return;

            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[LobbyPhotonAdapter] Connecting...");
        }
    }
}
