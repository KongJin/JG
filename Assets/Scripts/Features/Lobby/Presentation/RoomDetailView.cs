using Shared.Attributes;
using System.Collections.Generic;
using Features.Garage.Application;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime.Pooling;
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

        [Required, SerializeField]
        private TMP_Text _difficultyText;

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

        private LobbyRoomInputHandler _inputHandler;
        private IEventSubscriber _eventSubscriber;
        private DisposableScope _disposables = new();

        private GameObjectPool _memberItemPool;
        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private bool _localIsReady;
        private bool _garageReadyEligible;
        private bool _garageHasUnsavedChanges;
        private string _garageReadyBlockReason;
        private bool _callbacksHooked;
        private readonly List<GameObject> _activeItems = new();
        private readonly LobbyReadyPolicy _readyPolicy = new();

        public void Initialize(LobbyRoomInputHandler inputHandler, IEventSubscriber eventSubscriber)
        {
            _inputHandler = inputHandler;
            _eventSubscriber = eventSubscriber;
            _memberItemPool = new GameObjectPool(_memberItemPrefab.gameObject, _memberListContent);

            if (!_callbacksHooked)
            {
                _callbacksHooked = true;

                _leaveButton.onClick.AddListener(HandleLeave);
                _teamRedButton.onClick.AddListener(() => HandleChangeTeam(TeamType.Red));
                _teamBlueButton.onClick.AddListener(() => HandleChangeTeam(TeamType.Blue));
                _readyButton.onClick.AddListener(HandleToggleReady);
                _startGameButton.onClick.AddListener(HandleStartGame);
            }

            _disposables.Dispose();
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventSubscriber, this));
            _eventSubscriber.Subscribe<GarageInitializedEvent>(this, e => HandleGarageRosterChanged(e.Roster));
            _eventSubscriber.Subscribe<RosterSavedEvent>(this, e => HandleGarageRosterChanged(e.Roster));
            _eventSubscriber.Subscribe<GarageDraftStateChangedEvent>(this, HandleGarageDraftStateChanged);
        }

        public void SetLocalMemberId(DomainEntityId memberId)
        {
            _localMemberId = memberId;
        }

        public void Render(RoomSnapshot room)
        {
            _currentRoomId = room.Id;

            _roomNameText.text = room.Name;
            _memberCountText.text = $"{room.Members.Count}/{room.Capacity}";
            _difficultyText.text = $"Difficulty: {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}";

            RenderMemberList(room.Members);
            UpdateLocalReadyState(room.Members);
            UpdateReadyButtonState();

            _startGameButton.interactable = room.OwnerId.Equals(_localMemberId);
        }

        private void RenderMemberList(IReadOnlyList<RoomMemberSnapshot> members)
        {
            ClearMemberList();

            foreach (var member in members)
            {
                var item = _memberItemPool.RentComponent<MemberItemView>(Vector3.zero, Quaternion.identity);
                item.Bind(member);
                _activeItems.Add(item.gameObject);
            }
        }

        private void UpdateLocalReadyState(IReadOnlyList<RoomMemberSnapshot> members)
        {
            _localIsReady = false;

            foreach (var member in members)
            {
                if (member.Id.Equals(_localMemberId))
                {
                    _localIsReady = member.IsReady;
                    break;
                }
            }

            UpdateReadyButtonState();
        }

        private void HandleLeave()
        {
            _inputHandler.LeaveRoom(_currentRoomId, _localMemberId);
        }

        private void HandleChangeTeam(TeamType team)
        {
            _inputHandler.ChangeTeam(_currentRoomId, _localMemberId, team);
        }

        private void HandleToggleReady()
        {
            if (!HasRoomMemberContext())
                return;

            if (!_readyPolicy.CanToggleReady(_garageReadyEligible, _localIsReady))
            {
                _inputHandler.PublishFailure(_readyPolicy.BuildBlockReason(_garageReadyBlockReason));
                return;
            }

            _inputHandler.SetReady(_currentRoomId, _localMemberId, !_localIsReady);
        }

        private void HandleStartGame()
        {
            _inputHandler.StartGame(_currentRoomId);
        }

        private void ClearMemberList()
        {
            foreach (var go in _activeItems)
                _memberItemPool.Return(go);
            _activeItems.Clear();
        }

        private void HandleGarageRosterChanged(Features.Garage.Domain.GarageRoster roster)
        {
            _garageReadyEligible = _readyPolicy.ComputeReadyEligible(roster, _garageHasUnsavedChanges);
            _garageReadyBlockReason = _readyPolicy.BuildRosterBlockReason(roster, _garageHasUnsavedChanges, _garageReadyEligible);
            TryRelockReadyIfNeeded();

            UpdateReadyButtonState();
        }

        private void HandleGarageDraftStateChanged(GarageDraftStateChangedEvent e)
        {
            _garageHasUnsavedChanges = e.HasUnsavedChanges;
            _garageReadyEligible = e.ReadyEligible;
            _garageReadyBlockReason = _readyPolicy.BuildDraftBlockReason(e.BlockReason, _garageHasUnsavedChanges, _garageReadyEligible);
            TryRelockReadyIfNeeded();

            UpdateReadyButtonState();
        }

        private void UpdateReadyButtonState()
        {
            bool hasRoomMemberContext = HasRoomMemberContext();
            _readyButton.interactable = hasRoomMemberContext && (_garageReadyEligible || _localIsReady);

            if (!hasRoomMemberContext)
            {
                _readyButtonText.text = "Ready";
                return;
            }

            if (!_garageReadyEligible && !_localIsReady)
            {
                _readyButtonText.text = _readyPolicy.BuildReadyButtonLabel(
                    _garageHasUnsavedChanges,
                    _garageReadyBlockReason);
                return;
            }

            _readyButtonText.text = _localIsReady ? "Cancel" : "Ready";
        }

        private bool HasRoomMemberContext()
        {
            return !string.IsNullOrWhiteSpace(_currentRoomId.Value) &&
                   !string.IsNullOrWhiteSpace(_localMemberId.Value);
        }

        private void TryRelockReadyIfNeeded()
        {
            if (!_readyPolicy.ShouldForceRelock(_garageReadyEligible, _localIsReady, HasRoomMemberContext()))
                return;

            _inputHandler.SetReady(_currentRoomId, _localMemberId, false);
            _localIsReady = false;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }

}
