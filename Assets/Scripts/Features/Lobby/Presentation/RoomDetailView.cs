using Shared.Attributes;
using System.Collections.Generic;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Lobby.Presentation
{
    public sealed class RoomDetailView : MonoBehaviour
    {
        [Header("Room Info")]
        [Required, SerializeField]
        private TMP_Text _roomNameText;

        [Required, SerializeField]
        private TMP_Text _memberCountText;

        [Header("Member List")]
        [Required, SerializeField]
        private Transform _memberListContent;

        [Required, SerializeField]
        private MemberItemView _memberItemPrefab;

        [Header("Actions")]
        [Required, SerializeField]
        private Button _leaveButton;

        [Required, SerializeField]
        private Button _teamRedButton;

        [Required, SerializeField]
        private Button _teamBlueButton;

        [Required, SerializeField]
        private Button _readyButton;

        [Required, SerializeField]
        private TMP_Text _readyButtonText;

        [Required, SerializeField]
        private Button _startGameButton;

        private LobbyUseCases _useCases;
        private IEventPublisher _eventPublisher;

        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private bool _localIsReady;
        private readonly List<MemberItemView> _spawnedItems = new List<MemberItemView>();

        public void Initialize(LobbyUseCases useCases, IEventPublisher eventPublisher)
        {
            _useCases = useCases;
            _eventPublisher = eventPublisher;

            if (_leaveButton != null)
                _leaveButton.onClick.AddListener(HandleLeave);
            if (_teamRedButton != null)
                _teamRedButton.onClick.AddListener(() => HandleChangeTeam(TeamType.Red));
            if (_teamBlueButton != null)
                _teamBlueButton.onClick.AddListener(() => HandleChangeTeam(TeamType.Blue));
            if (_readyButton != null)
                _readyButton.onClick.AddListener(HandleToggleReady);
            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(HandleStartGame);
        }

        public void SetLocalMemberId(DomainEntityId memberId)
        {
            _localMemberId = memberId;
        }

        public Result LeaveRoom(DomainEntityId roomId, DomainEntityId memberId) =>
            _useCases.LeaveRoom(roomId, memberId);

        public Result ChangeTeam(DomainEntityId roomId, DomainEntityId memberId, TeamType team) =>
            _useCases.ChangeTeam(roomId, memberId, team);

        public Result SetReady(DomainEntityId roomId, DomainEntityId memberId, bool isReady) =>
            _useCases.SetReady(roomId, memberId, isReady);

        public Result StartGame(DomainEntityId roomId) => _useCases.StartGame(roomId);

        public void Render(RoomSnapshot room)
        {
            _currentRoomId = room.Id;

            if (_roomNameText != null)
                _roomNameText.text = room.Name;
            if (_memberCountText != null)
                _memberCountText.text = $"{room.Members.Count}/{room.Capacity}";

            RenderMemberList(room.Members);
            UpdateLocalReadyState(room.Members);

            if (_startGameButton != null)
                _startGameButton.interactable = room.OwnerId.Equals(_localMemberId);
        }

        private void RenderMemberList(IReadOnlyList<RoomMemberSnapshot> members)
        {
            ClearMemberList();

            if (_memberItemPrefab == null || _memberListContent == null)
                return;

            foreach (var member in members)
            {
                var item = Instantiate(_memberItemPrefab, _memberListContent);
                item.Bind(member);
                _spawnedItems.Add(item);
            }
        }

        private void UpdateLocalReadyState(IReadOnlyList<RoomMemberSnapshot> members)
        {
            foreach (var member in members)
            {
                if (member.Id.Equals(_localMemberId))
                {
                    _localIsReady = member.IsReady;
                    if (_readyButtonText != null)
                        _readyButtonText.text = _localIsReady ? "Cancel" : "Ready";
                    break;
                }
            }
        }

        private void HandleLeave()
        {
            var result = _useCases.LeaveRoom(_currentRoomId, _localMemberId);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void HandleChangeTeam(TeamType team)
        {
            var result = _useCases.ChangeTeam(_currentRoomId, _localMemberId, team);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void HandleToggleReady()
        {
            var result = _useCases.SetReady(_currentRoomId, _localMemberId, !_localIsReady);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void HandleStartGame()
        {
            var result = _useCases.StartGame(_currentRoomId);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void ClearMemberList()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _spawnedItems.Clear();
        }
    }
}
