using Features.Account.Application;
using Features.Account.Domain;
using Features.Garage.Presentation;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Features.Player.Infrastructure;
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
        private DisposableScope _disposables = new();
        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private bool _localIsReady;

        private void Awake()
        {
            BindAdapterSurface();
            _uitk?.ShowLobbyPage();
        }

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases)
        {
            _eventBus = eventBus;
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
            _uitk?.RenderRooms(_presenter.BuildRooms(lobby.Rooms));
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            _uitk?.RenderRooms(_presenter.BuildRooms(e.Rooms));
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            _currentRoomId = e.Room.Id;
            _localMemberId = e.LocalMemberId;
            var viewModel = _presenter.BuildRoomDetail(e.Room, _localMemberId);
            _localIsReady = viewModel.LocalIsReady;
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
            _uitk?.RenderAccountState(
                _presenter.BuildAccount(
                    profile,
                    accountData,
                    new OperationRecordJsonStore().Load().Count));
        }

        private void OnDestroy()
        {
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

            _uitk.RenderOperationMemory(_presenter.BuildOperationMemory(new OperationRecordJsonStore().Load()));
        }

        private void BindAdapterEvents(LobbyUitkRuntimeAdapter adapter)
        {
            adapter.CreateRoomRequested += CreateRoom;
            adapter.RoomSelected += JoinRoom;
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
        }

        private void ShowGaragePage()
        {
            BindAdapterSurface();
            _uitk?.ShowGaragePage();
        }

        private void ShowRecordsPage()
        {
            BindAdapterSurface();
            _uitk?.RenderOperationMemory(_presenter.BuildOperationMemory(new OperationRecordJsonStore().Load()));
            _uitk?.ShowRecordsPage();
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
    }
}
