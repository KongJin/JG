using System;
using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Garage.Presentation;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Features.Lobby.Application.Ports;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyPageController : MonoBehaviour
    {
        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private UIDocument _document;

        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private UIDocument _garageDocument;

        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private GarageSetBUitkRuntimeAdapter _garageAdapter;

        [Header("UXML Surfaces")]
        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private VisualTreeAsset _lobbyShellTree;

        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private VisualTreeAsset _operationMemoryTree;

        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private VisualTreeAsset _accountSyncTree;

        [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
        private VisualTreeAsset _connectionReconnectTree;

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [SerializeField]
        private bool _showEditorPreviewRoom = true;
#endif

        private LobbyRoomInputHandler _inputHandler;
        private LobbyUitkRuntimeAdapter _uitk;
        private readonly LobbyPagePresenter _presenter = new();
        private IEventSubscriber _eventBus;
        private IOperationRecordStore _operationRecordStore;
        private DisposableScope _disposables = new();
        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private DomainEntityId _selectedRoomId;
        private bool _localIsReady;
        private bool _hasNetworkRoomList;
        private IReadOnlyList<RoomSnapshot> _latestLobbyRooms = Array.Empty<RoomSnapshot>();
        private IReadOnlyList<RoomListItem> _latestNetworkRooms = Array.Empty<RoomListItem>();

#if UNITY_EDITOR
        private const string EditorPreviewRoomIdPrefix = "ui-preview-room-";
        private static readonly IReadOnlyList<RoomListItem> EditorPreviewRooms = BuildEditorPreviewRooms();
#endif

        private void Awake()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.ShowLobbyPage();
        }

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases,
            IOperationRecordStore operationRecordStore)
        {
            if (operationRecordStore == null)
                throw new ArgumentNullException(nameof(operationRecordStore));

            _eventBus = eventBus;
            _operationRecordStore = operationRecordStore;
            _inputHandler = new LobbyRoomInputHandler(useCases, eventPublisher);
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.SetClickSoundPublisher(eventPublisher);

            _disposables.Dispose();
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
            _eventBus.Subscribe<RoomListReceivedEvent>(this, RenderRoomList);
            _eventBus.Subscribe<RoomUpdatedEvent>(this, RenderRoom);

// csharp-guardrails: allow-null-defense
            _uitk?.ShowLobbyPage();
        }

        public void RenderLobby(LobbySnapshot lobby)
        {
// csharp-guardrails: allow-null-defense
            _latestLobbyRooms = lobby.Rooms ?? Array.Empty<RoomSnapshot>();
            ClearRoomContextIfMissing();
            RenderLobbyHomeState();
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            _hasNetworkRoomList = true;
// csharp-guardrails: allow-null-defense
            _latestNetworkRooms = e.Rooms ?? Array.Empty<RoomListItem>();
            ClearRoomContextIfMissing();
            RenderLobbyHomeState();
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            _currentRoomId = e.Room.Id;
            _localMemberId = e.LocalMemberId;
            _selectedRoomId = e.Room.Id;
            var viewModel = _presenter.BuildRoomDetail(e.Room, _localMemberId);
            _localIsReady = viewModel.LocalIsReady;
// csharp-guardrails: allow-null-defense
            _uitk?.RenderRooms(BuildRoomsViewModel(_currentRoomId));
// csharp-guardrails: allow-null-defense
            _uitk?.RenderRoomSelection(LobbyRoomSelectionViewModel.Empty);
// csharp-guardrails: allow-null-defense
            _uitk?.RenderRoomDetail(viewModel);
        }

        public void OpenGaragePage()
        {
            ShowGaragePage();
        }

        public void OpenLobbyPage()
        {
            ShowLobbyPage();
        }

        public void OpenRecordsPage()
        {
            ShowRecordsPage();
        }

        public void OpenAccountPage()
        {
            ShowAccountPage();
        }

        public void OpenConnectionPage()
        {
            ShowConnectionPage();
        }

        public void RenderAccountState(AccountProfile profile, AccountData accountData)
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.RenderGarageSummary(_presenter.BuildGarageSummary(accountData));
// csharp-guardrails: allow-null-defense
            _uitk?.RenderAccountState(
                _presenter.BuildAccount(
                    profile,
                    accountData,
                    LoadOperationRecords().Count));
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _uitk?.Dispose();
            _disposables.Dispose();
        }

        private void EnsureDocument()
        {
// csharp-guardrails: allow-null-defense
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void EnsureAdapter()
        {
// csharp-guardrails: allow-null-defense
            if (_uitk != null)
                return;

            EnsureDocument();
// csharp-guardrails: allow-null-defense
            if (_document == null)
                return;

            _uitk = new LobbyUitkRuntimeAdapter(
                _document,
                _garageDocument,
                _garageAdapter,
                _lobbyShellTree,
                _operationMemoryTree,
                _accountSyncTree,
                _connectionReconnectTree,
                this);
            BindAdapterEvents(_uitk);
        }

        private void BindAdapterSurface()
        {
            EnsureAdapter();
// csharp-guardrails: allow-null-defense
            if (_uitk == null || !_uitk.Bind())
                return;

// csharp-guardrails: allow-null-defense
            if (_operationRecordStore != null)
                _uitk.RenderOperationMemory(_presenter.BuildOperationMemory(LoadOperationRecords()));
        }

        private void BindAdapterEvents(LobbyUitkRuntimeAdapter adapter)
        {
            adapter.CreateRoomRequested += CreateRoom;
            adapter.RoomSelected += PreviewRoom;
            adapter.JoinSelectedRoomRequested += JoinSelectedRoom;
            adapter.LeaveRoomRequested += LeaveRoom;
            adapter.TeamChangeRequested += ChangeTeam;
            adapter.ReadyToggled += ToggleReady;
            adapter.GameStartRequested += StartGame;
            adapter.LobbyPageRequested += ShowLobbyPage;
            adapter.GaragePageRequested += ShowGaragePage;
            adapter.RecordsPageRequested += ShowRecordsPage;
            adapter.AccountPageRequested += ShowAccountPage;
            adapter.ConnectionPageRequested += ShowConnectionPage;
            adapter.AccountRefreshRequested += () => RenderAccountState(null, null);
        }

        private void CreateRoom()
        {
// csharp-guardrails: allow-null-defense
            var input = _uitk?.CreateRoomInput ?? new LobbyCreateRoomInput("Room", 4, string.Empty, 0);
// csharp-guardrails: allow-null-defense
            _inputHandler?.CreateRoom(
                input.RoomName,
                input.Capacity,
                input.DisplayName,
                input.DifficultyPresetId);
        }

        private void JoinRoom(DomainEntityId roomId)
        {
// csharp-guardrails: allow-null-defense
            _inputHandler?.JoinRoom(roomId, _uitk?.DisplayNameText ?? string.Empty);
        }

        private void PreviewRoom(DomainEntityId roomId)
        {
            _selectedRoomId = roomId;
// csharp-guardrails: allow-null-defense
            _uitk?.RenderRooms(BuildRoomsViewModel(_selectedRoomId));
// csharp-guardrails: allow-null-defense
            _uitk?.RenderRoomSelection(BuildRoomSelectionViewModel(_selectedRoomId));
            if (!HasRoomMemberContext())
// csharp-guardrails: allow-null-defense
                _uitk?.RenderRoomDetail(LobbyRoomDetailViewModel.Empty);
        }

        private void JoinSelectedRoom()
        {
            if (string.IsNullOrWhiteSpace(_selectedRoomId.Value))
                return;

#if UNITY_EDITOR
            if (IsEditorPreviewRoom(_selectedRoomId))
                return;
#endif

            JoinRoom(_selectedRoomId);
        }

        private void LeaveRoom()
        {
            if (HasRoomMemberContext())
// csharp-guardrails: allow-null-defense
                _inputHandler?.LeaveRoom(_currentRoomId, _localMemberId);
        }

        private void ChangeTeam(TeamType team)
        {
            if (HasRoomMemberContext())
// csharp-guardrails: allow-null-defense
                _inputHandler?.ChangeTeam(_currentRoomId, _localMemberId, team);
        }

        private void ToggleReady()
        {
            if (HasRoomMemberContext())
// csharp-guardrails: allow-null-defense
                _inputHandler?.SetReady(_currentRoomId, _localMemberId, !_localIsReady);
        }

        private void StartGame()
        {
            if (!string.IsNullOrWhiteSpace(_currentRoomId.Value))
// csharp-guardrails: allow-null-defense
                _inputHandler?.StartGame(_currentRoomId);
        }

        private bool HasRoomMemberContext()
        {
            return !string.IsNullOrWhiteSpace(_currentRoomId.Value) &&
                   !string.IsNullOrWhiteSpace(_localMemberId.Value);
        }

        private void ShowLobbyPage()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.ShowLobbyPage();
            RenderLobbyHomeState();
        }

        private void ShowGaragePage()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.ShowGaragePage();
        }

        private void ShowRecordsPage()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.RenderOperationMemory(_presenter.BuildOperationMemory(LoadOperationRecords()));
// csharp-guardrails: allow-null-defense
            _uitk?.ShowRecordsPage();
        }

        private RecentOperationRecords LoadOperationRecords()
        {
// csharp-guardrails: allow-null-defense
            if (_operationRecordStore == null)
                throw new InvalidOperationException("Lobby operation record store is not initialized.");

            return _operationRecordStore.Load();
        }

        private void ShowAccountPage()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.ShowAccountPage();
        }

        private void ShowConnectionPage()
        {
            BindAdapterSurface();
// csharp-guardrails: allow-null-defense
            _uitk?.ShowConnectionPage();
        }

        private void RenderLobbyHomeState()
        {
// csharp-guardrails: allow-null-defense
            if (_uitk == null)
                return;

            var highlightedRoomId = HasRoomMemberContext() ? _currentRoomId : _selectedRoomId;
            _uitk.RenderRooms(BuildRoomsViewModel(highlightedRoomId));

            if (HasRoomMemberContext())
                return;

            _uitk.RenderRoomDetail(LobbyRoomDetailViewModel.Empty);
            _uitk.RenderRoomSelection(BuildRoomSelectionViewModel(_selectedRoomId));
        }

        private LobbyRoomListViewModel BuildRoomsViewModel(DomainEntityId highlightedRoomId)
        {
            if (_hasNetworkRoomList)
                return _presenter.BuildRooms(ChooseEditorPreviewRooms(_latestNetworkRooms), highlightedRoomId);

#if UNITY_EDITOR
// csharp-guardrails: allow-null-defense
            if ((_latestLobbyRooms?.Count ?? 0) == 0 && ShouldShowEditorPreviewRoom())
                return _presenter.BuildRooms(EditorPreviewRooms, highlightedRoomId);
#endif

            return _presenter.BuildRooms(_latestLobbyRooms, highlightedRoomId);
        }

        private LobbyRoomSelectionViewModel BuildRoomSelectionViewModel(DomainEntityId selectedRoomId)
        {
            if (string.IsNullOrWhiteSpace(selectedRoomId.Value))
                return LobbyRoomSelectionViewModel.Empty;

            if (_hasNetworkRoomList)
                return _presenter.BuildRoomSelection(ChooseEditorPreviewRooms(_latestNetworkRooms), selectedRoomId);

#if UNITY_EDITOR
            if (IsEditorPreviewRoom(selectedRoomId))
                return _presenter.BuildRoomSelection(EditorPreviewRooms, selectedRoomId);
#endif

            return _presenter.BuildRoomSelection(_latestLobbyRooms, selectedRoomId);
        }

        private void ClearRoomContextIfMissing()
        {
            if (!string.IsNullOrWhiteSpace(_selectedRoomId.Value) &&
                !RoomExists(_selectedRoomId))
            {
                _selectedRoomId = default;
            }

            if (string.IsNullOrWhiteSpace(_currentRoomId.Value))
                return;

            if (RoomExists(_currentRoomId))
                return;

            _currentRoomId = default;
            _localMemberId = default;
            _localIsReady = false;
        }

        private bool RoomExists(DomainEntityId roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId.Value))
                return false;

#if UNITY_EDITOR
            if (IsEditorPreviewRoom(roomId))
                return ShouldShowEditorPreviewRoom() &&
// csharp-guardrails: allow-null-defense
                       ((_hasNetworkRoomList && (_latestNetworkRooms?.Count ?? 0) == 0) ||
// csharp-guardrails: allow-null-defense
                        (!_hasNetworkRoomList && (_latestLobbyRooms?.Count ?? 0) == 0));
#endif

            if (_hasNetworkRoomList)
            {
                for (var i = 0; i < _latestNetworkRooms.Count; i++)
                {
                    if (_latestNetworkRooms[i].RoomId == roomId)
                        return true;
                }

                return false;
            }

// csharp-guardrails: allow-null-defense
            if (_latestLobbyRooms == null)
                return false;

            for (var i = 0; i < _latestLobbyRooms.Count; i++)
            {
                if (_latestLobbyRooms[i].Id == roomId)
                    return true;
            }

            return false;
        }

        private bool ShouldShowEditorPreviewRoom()
        {
#if UNITY_EDITOR
            return _showEditorPreviewRoom;
#else
            return false;
#endif
        }

        private IReadOnlyList<RoomListItem> ChooseEditorPreviewRooms(IReadOnlyList<RoomListItem> rooms)
        {
#if UNITY_EDITOR
// csharp-guardrails: allow-null-defense
            if ((rooms?.Count ?? 0) == 0 && ShouldShowEditorPreviewRoom())
                return EditorPreviewRooms;
#endif

            return rooms;
        }

        private bool IsEditorPreviewRoom(DomainEntityId roomId)
        {
#if UNITY_EDITOR
// csharp-guardrails: allow-null-defense
            return roomId.Value != null &&
                   roomId.Value.StartsWith(EditorPreviewRoomIdPrefix, StringComparison.Ordinal);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static IReadOnlyList<RoomListItem> BuildEditorPreviewRooms()
        {
            var rooms = new RoomListItem[20];
            for (var i = 0; i < rooms.Length; i++)
            {
                var roomNumber = i + 1;
                rooms[i] = new RoomListItem(
                    new DomainEntityId($"{EditorPreviewRoomIdPrefix}{roomNumber:00}"),
                    $"UI 점검용 샘플 방 {roomNumber:00}",
                    playerCount: 1 + i % 3,
                    maxPlayers: 4,
                    isOpen: true,
                    difficultyPresetId: i % 3);
            }

            return rooms;
        }
#endif
    }
}
