using Shared.Attributes;
using System.Collections.Generic;
using Features.Garage.Application;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Shared.ErrorHandling;
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

        [SerializeField]
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

        private LobbyUseCases _useCases;
        private IEventSubscriber _eventSubscriber;
        private IEventPublisher _eventPublisher;
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
        private readonly LobbyReadyPolicyController _readyPolicy = new();

        public void Initialize(LobbyUseCases useCases, IEventSubscriber eventSubscriber, IEventPublisher eventPublisher)
        {
            _useCases = useCases;
            _eventSubscriber = eventSubscriber;
            _eventPublisher = eventPublisher;
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
            if (_difficultyText != null)
                _difficultyText.text = $"Difficulty: {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}";

            RenderMemberList(room.Members);
            UpdateLocalReadyState(room.Members);
            UpdateReadyButtonState();

            if (_startGameButton != null)
                _startGameButton.interactable = room.OwnerId.Equals(_localMemberId);
        }

        private void RenderMemberList(IReadOnlyList<RoomMemberSnapshot> members)
        {
            ClearMemberList();

            foreach (var member in members)
            {
                var go = _memberItemPool.Rent(Vector3.zero, Quaternion.identity);
                go.GetComponent<MemberItemView>().Bind(member);
                _activeItems.Add(go);
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
            if (!HasRoomMemberContext())
                return;

            if (!_readyPolicy.CanToggleReady(_garageReadyEligible, _localIsReady))
            {
                UiErrorResultBridge.PublishBannerIfFailure(
                    _eventPublisher,
                    Result.Failure(_readyPolicy.BuildBlockReason(_garageReadyBlockReason)),
                    "Lobby");
                return;
            }

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

            var result = _useCases.SetReady(_currentRoomId, _localMemberId, false);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
            _localIsReady = false;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }

    internal sealed class LobbyReadyPolicyController
    {
        public bool ComputeReadyEligible(Features.Garage.Domain.GarageRoster roster, bool hasUnsavedChanges)
        {
            return roster != null && roster.IsValid && !hasUnsavedChanges;
        }

        public bool CanToggleReady(bool readyEligible, bool localIsReady)
        {
            return readyEligible || localIsReady;
        }

        public bool ShouldForceRelock(bool readyEligible, bool localIsReady, bool hasRoomMemberContext)
        {
            return !readyEligible && localIsReady && hasRoomMemberContext;
        }

        public string BuildRosterBlockReason(Features.Garage.Domain.GarageRoster roster, bool hasUnsavedChanges, bool readyEligible)
        {
            if (hasUnsavedChanges)
                return "Unsaved Garage changes";

            if (readyEligible)
                return "Ready available";

            return roster != null && roster.Count > 0
                ? "Need at least 3 saved units"
                : "Need at least 3 saved units";
        }

        public string BuildDraftBlockReason(string eventBlockReason, bool hasUnsavedChanges, bool readyEligible)
        {
            if (hasUnsavedChanges)
                return "Unsaved Garage changes";

            if (readyEligible)
                return "Ready available";

            return string.IsNullOrWhiteSpace(eventBlockReason)
                ? "Need at least 3 saved units"
                : eventBlockReason;
        }

        public string BuildBlockReason(string currentReason)
        {
            return string.IsNullOrWhiteSpace(currentReason)
                ? "Ready requires a saved Garage roster."
                : currentReason;
        }

        public string BuildReadyButtonLabel(bool hasUnsavedChanges, string currentReason)
        {
            if (hasUnsavedChanges)
                return "Save Garage Draft";

            return string.IsNullOrWhiteSpace(currentReason)
                ? "Need 3 Saved Units"
                : currentReason;
        }
    }
}
