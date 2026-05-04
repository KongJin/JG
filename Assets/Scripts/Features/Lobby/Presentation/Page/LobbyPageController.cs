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
        private UIDocument _document;

        [SerializeField]
        private UIDocument _garageDocument;

        [SerializeField]
        private GarageSetBUitkRuntimeAdapter _garageAdapter;

        [Header("UXML Surfaces")]
        [SerializeField]
        private VisualTreeAsset _lobbyShellTree;

        [SerializeField]
        private VisualTreeAsset _operationMemoryTree;

        [SerializeField]
        private VisualTreeAsset _accountSyncTree;

        [SerializeField]
        private VisualTreeAsset _connectionReconnectTree;

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

        private void Awake()
        {
            BindAdapterSurface();
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

            _disposables.Dispose();
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
            _eventBus.Subscribe<RoomListReceivedEvent>(this, RenderRoomList);
            _eventBus.Subscribe<RoomUpdatedEvent>(this, RenderRoom);

            _uitk?.ShowLobbyPage();
        }

        public void RenderLobby(LobbySnapshot lobby)
        {
            _latestLobbyRooms = lobby.Rooms ?? Array.Empty<RoomSnapshot>();
            ClearRoomContextIfMissing();
            RenderLobbyHomeState();
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            _hasNetworkRoomList = true;
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
            _uitk?.RenderRooms(BuildRoomsViewModel(_currentRoomId));
            _uitk?.RenderRoomSelection(LobbyRoomSelectionViewModel.Empty);
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
            _uitk?.RenderGarageSummary(_presenter.BuildGarageSummary(accountData));
            _uitk?.RenderAccountState(
                _presenter.BuildAccount(
                    profile,
                    accountData,
                    LoadOperationRecords().Count));
        }

        private void OnDestroy()
        {
            _uitk?.Dispose();
            _disposables.Dispose();
        }

        private void EnsureDocument()
        {
            if (_document == null)
                _document = ComponentAccess.Get<UIDocument>(gameObject);
        }

        private void EnsureAdapter()
        {
            if (_uitk != null)
                return;

            EnsureDocument();
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
            if (_uitk == null || !_uitk.Bind())
                return;

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
            var input = _uitk?.CreateRoomInput ?? new LobbyCreateRoomInput("Room", 4, string.Empty, 0);
            _inputHandler?.CreateRoom(
                input.RoomName,
                input.Capacity,
                input.DisplayName,
                input.DifficultyPresetId);
        }

        private void JoinRoom(DomainEntityId roomId)
        {
            _inputHandler?.JoinRoom(roomId, _uitk?.DisplayNameText ?? string.Empty);
        }

        private void PreviewRoom(DomainEntityId roomId)
        {
            _selectedRoomId = roomId;
            _uitk?.RenderRooms(BuildRoomsViewModel(_selectedRoomId));
            _uitk?.RenderRoomSelection(BuildRoomSelectionViewModel(_selectedRoomId));
            if (!HasRoomMemberContext())
                _uitk?.RenderRoomDetail(LobbyRoomDetailViewModel.Empty);
        }

        private void JoinSelectedRoom()
        {
            if (string.IsNullOrWhiteSpace(_selectedRoomId.Value))
                return;

            JoinRoom(_selectedRoomId);
        }

        private void LeaveRoom()
        {
            if (HasRoomMemberContext())
                _inputHandler?.LeaveRoom(_currentRoomId, _localMemberId);
        }

        private void ChangeTeam(TeamType team)
        {
            if (HasRoomMemberContext())
                _inputHandler?.ChangeTeam(_currentRoomId, _localMemberId, team);
        }

        private void ToggleReady()
        {
            if (HasRoomMemberContext())
                _inputHandler?.SetReady(_currentRoomId, _localMemberId, !_localIsReady);
        }

        private void StartGame()
        {
            if (!string.IsNullOrWhiteSpace(_currentRoomId.Value))
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
            _uitk?.ShowLobbyPage();
            RenderLobbyHomeState();
        }

        private void ShowGaragePage()
        {
            BindAdapterSurface();
            _uitk?.ShowGaragePage();
        }

        private void ShowRecordsPage()
        {
            BindAdapterSurface();
            _uitk?.RenderOperationMemory(_presenter.BuildOperationMemory(LoadOperationRecords()));
            _uitk?.ShowRecordsPage();
        }

        private RecentOperationRecords LoadOperationRecords()
        {
            if (_operationRecordStore == null)
                throw new InvalidOperationException("Lobby operation record store is not initialized.");

            return _operationRecordStore.Load();
        }

        private void ShowAccountPage()
        {
            BindAdapterSurface();
            _uitk?.ShowAccountPage();
        }

        private void ShowConnectionPage()
        {
            BindAdapterSurface();
            _uitk?.ShowConnectionPage();
        }

        private void RenderLobbyHomeState()
        {
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
                return _presenter.BuildRooms(_latestNetworkRooms, highlightedRoomId);

            return _presenter.BuildRooms(_latestLobbyRooms, highlightedRoomId);
        }

        private LobbyRoomSelectionViewModel BuildRoomSelectionViewModel(DomainEntityId selectedRoomId)
        {
            if (string.IsNullOrWhiteSpace(selectedRoomId.Value))
                return LobbyRoomSelectionViewModel.Empty;

            if (_hasNetworkRoomList)
                return _presenter.BuildRoomSelection(_latestNetworkRooms, selectedRoomId);

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

            if (_hasNetworkRoomList)
            {
                for (var i = 0; i < _latestNetworkRooms.Count; i++)
                {
                    if (_latestNetworkRooms[i].RoomId == roomId)
                        return true;
                }

                return false;
            }

            if (_latestLobbyRooms == null)
                return false;

            for (var i = 0; i < _latestLobbyRooms.Count; i++)
            {
                if (_latestLobbyRooms[i].Id == roomId)
                    return true;
            }

            return false;
        }
    }
}
