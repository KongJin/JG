using System.Collections.Generic;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _document;

        private LobbyRoomInputHandler _inputHandler;
        private IEventSubscriber _eventBus;
        private DisposableScope _disposables = new();
        private readonly List<DomainEntityId> _visibleRoomIds = new();
        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private bool _localIsReady;

        private VisualElement _root;
        private VisualElement _lobbyPage;
        private VisualElement _garagePage;
        private VisualElement _roomList;
        private VisualElement _memberList;
        private Label _shellTitle;
        private Label _shellState;
        private Label _roomCountLabel;
        private Label _roomDetailTitle;
        private Label _roomDetailMeta;
        private TextField _roomNameInput;
        private TextField _displayNameInput;
        private IntegerField _capacityInput;
        private IntegerField _difficultyInput;
        private Button _readyButton;
        private Button _startButton;
        private Button _lobbyNav;
        private Button _garageNav;
        private Button _recordsNav;

        private void Awake()
        {
            EnsureDocument();
            BuildTree();
            ShowLobbyPage();
        }

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases)
        {
            _eventBus = eventBus;
            _inputHandler = new LobbyRoomInputHandler(useCases, eventPublisher);
            EnsureDocument();
            BuildTree();

            _disposables.Dispose();
            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
            _eventBus.Subscribe<RoomListReceivedEvent>(this, RenderRoomList);
            _eventBus.Subscribe<RoomUpdatedEvent>(this, RenderRoom);

            ShowLobbyPage();
        }

        public void RenderLobby(LobbySnapshot lobby)
        {
            RenderRooms(lobby.Rooms);
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            RenderRooms(e.Rooms);
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            _currentRoomId = e.Room.Id;
            _localMemberId = e.LocalMemberId;
            RenderRoomDetail(e.Room);
            ShowLobbyPage();
        }

        public void OpenGaragePage()
        {
            ShowGaragePage();
        }

        public void OpenLobbyPage()
        {
            ShowLobbyPage();
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

        private void BuildTree()
        {
            if (_document == null || _root != null)
                return;

            _root = _document.rootVisualElement;
            if (_root == null)
                return;

            _root.Clear();
            _root.AddToClassList("shared-shell-screen");

            var topShell = new VisualElement { name = "SharedTopShell" };
            topShell.AddToClassList("shared-top-shell");
            topShell.Add(new Button(ShowLobbyPage) { text = "=", name = "ShellMenuButton" });
            topShell[0].AddToClassList("shared-icon-button");
            var titleStack = new VisualElement();
            titleStack.AddToClassList("shared-title-stack");
            _shellTitle = new Label("로비");
            _shellTitle.AddToClassList("shared-title");
            _shellState = new Label("동기화 대기");
            _shellState.AddToClassList("shared-state");
            titleStack.Add(_shellTitle);
            titleStack.Add(_shellState);
            topShell.Add(titleStack);
            var settings = new Button(ShowGaragePage) { text = "S", name = "ShellSettingsButton" };
            settings.AddToClassList("shared-icon-button");
            topShell.Add(settings);
            _root.Add(topShell);

            var workspace = new VisualElement { name = "SharedWorkspace" };
            workspace.AddToClassList("shared-workspace");
            _lobbyPage = BuildLobbyPage();
            _garagePage = BuildGaragePlaceholderPage();
            workspace.Add(_lobbyPage);
            workspace.Add(_garagePage);
            _root.Add(workspace);

            var nav = new VisualElement { name = "SharedNavigationBar" };
            nav.AddToClassList("shared-navigation-bar");
            _lobbyNav = AddNavButton(nav, "LobbyNavButton", "로비", ShowLobbyPage);
            _garageNav = AddNavButton(nav, "GarageNavButton", "차고", ShowGaragePage);
            _recordsNav = AddNavButton(nav, "RecordsNavButton", "기록", ShowRecordsPage);
            _root.Add(nav);
        }

        private VisualElement BuildLobbyPage()
        {
            var page = new ScrollView(ScrollViewMode.Vertical) { name = "LobbyUitkPage" };
            page.AddToClassList("uitk-page");

            var createCard = new VisualElement { name = "CreateRoomCard" };
            createCard.AddToClassList("uitk-card");
            createCard.Add(Label("CREATE ROOM", "uitk-kicker"));
            _roomNameInput = new TextField("Room") { value = "Room" };
            _displayNameInput = new TextField("Pilot") { value = "Pilot" };
            _capacityInput = new IntegerField("Capacity") { value = 4 };
            _difficultyInput = new IntegerField("Difficulty") { value = 0 };
            createCard.Add(_roomNameInput);
            createCard.Add(_displayNameInput);
            createCard.Add(_capacityInput);
            createCard.Add(_difficultyInput);
            var createButton = new Button(CreateRoom) { text = "방 만들기" };
            createButton.AddToClassList("uitk-primary-button");
            createCard.Add(createButton);
            page.Add(createCard);

            var roomsCard = new VisualElement { name = "RoomsSectionCard" };
            roomsCard.AddToClassList("uitk-card");
            _roomCountLabel = Label("0 open rooms", "uitk-section-title");
            roomsCard.Add(_roomCountLabel);
            _roomList = new VisualElement { name = "RoomList" };
            roomsCard.Add(_roomList);
            page.Add(roomsCard);

            var detailCard = new VisualElement { name = "RoomDetailCard" };
            detailCard.AddToClassList("uitk-card");
            _roomDetailTitle = Label("방을 선택하세요", "uitk-section-title");
            _roomDetailMeta = Label("대기 중", "uitk-body");
            _memberList = new VisualElement { name = "MemberList" };
            detailCard.Add(_roomDetailTitle);
            detailCard.Add(_roomDetailMeta);
            detailCard.Add(_memberList);
            var actions = new VisualElement();
            actions.AddToClassList("uitk-action-row");
            actions.Add(new Button(() => ChangeTeam(TeamType.Red)) { text = "RED" });
            actions.Add(new Button(() => ChangeTeam(TeamType.Blue)) { text = "BLUE" });
            _readyButton = new Button(ToggleReady) { text = "Ready" };
            _startButton = new Button(StartGame) { text = "Start" };
            actions.Add(_readyButton);
            actions.Add(_startButton);
            actions.Add(new Button(LeaveRoom) { text = "Leave" });
            detailCard.Add(actions);
            page.Add(detailCard);

            return page;
        }

        private static VisualElement BuildGaragePlaceholderPage()
        {
            var page = new VisualElement { name = "GarageUitkHost" };
            page.AddToClassList("uitk-page");
            page.Add(Label("GARAGE", "uitk-section-title"));
            page.Add(Label("Garage SetB UITK document owns the production workspace.", "uitk-body"));
            return page;
        }

        private static Button AddNavButton(VisualElement nav, string name, string text, System.Action callback)
        {
            var button = new Button(callback) { name = name, text = text };
            button.AddToClassList("shared-nav-item");
            nav.Add(button);
            return button;
        }

        private void RenderRooms(IReadOnlyList<RoomSnapshot> rooms)
        {
            _visibleRoomIds.Clear();
            _roomList?.Clear();
            var count = rooms?.Count ?? 0;
            if (_roomCountLabel != null)
                _roomCountLabel.text = count == 1 ? "1 open room" : $"{count} open rooms";

            if (rooms == null || rooms.Count == 0)
            {
                _roomList?.Add(Label("열린 방이 없습니다.", "uitk-body"));
                return;
            }

            for (var i = 0; i < rooms.Count; i++)
            {
                var index = i;
                var room = rooms[i];
                _visibleRoomIds.Add(room.Id);
                var row = new Button(() => JoinRoom(index))
                {
                    text = $"{room.Name}  {room.Members.Count}/{room.Capacity}  {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}"
                };
                row.AddToClassList("uitk-list-row");
                row.SetEnabled(room.Members.Count < room.Capacity);
                _roomList.Add(row);
            }
        }

        private void RenderRooms(IReadOnlyList<RoomListItem> rooms)
        {
            _visibleRoomIds.Clear();
            _roomList?.Clear();
            var count = rooms?.Count ?? 0;
            if (_roomCountLabel != null)
                _roomCountLabel.text = count == 1 ? "1 open room" : $"{count} open rooms";

            if (rooms == null || rooms.Count == 0)
            {
                _roomList?.Add(Label("열린 방이 없습니다.", "uitk-body"));
                return;
            }

            for (var i = 0; i < rooms.Count; i++)
            {
                var index = i;
                var room = rooms[i];
                _visibleRoomIds.Add(room.RoomId);
                var row = new Button(() => JoinRoom(index))
                {
                    text = $"{room.RoomName}  {room.PlayerCount}/{room.MaxPlayers}  {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}"
                };
                row.AddToClassList("uitk-list-row");
                row.SetEnabled(room.PlayerCount < room.MaxPlayers);
                _roomList.Add(row);
            }
        }

        private void RenderRoomDetail(RoomSnapshot room)
        {
            if (_roomDetailTitle != null)
                _roomDetailTitle.text = room.Name;
            if (_roomDetailMeta != null)
                _roomDetailMeta.text = $"{room.Members.Count}/{room.Capacity} | {DifficultyPresetFormatter.ToShortLabel(room.DifficultyPresetId)}";

            _memberList?.Clear();
            _localIsReady = false;
            foreach (var member in room.Members)
            {
                if (member.Id.Equals(_localMemberId))
                    _localIsReady = member.IsReady;
                var state = member.IsReady ? "READY" : "WAIT";
                _memberList?.Add(Label($"{member.DisplayName} | {member.Team} | {state}", "uitk-list-row-label"));
            }

            if (_readyButton != null)
                _readyButton.text = _localIsReady ? "Cancel" : "Ready";
            if (_startButton != null)
                _startButton.SetEnabled(room.OwnerId.Equals(_localMemberId));
        }

        private void CreateRoom()
        {
            _inputHandler?.CreateRoom(
                _roomNameInput?.value ?? "Room",
                Mathf.Max(1, _capacityInput?.value ?? 4),
                _displayNameInput?.value ?? string.Empty,
                Mathf.Max(0, _difficultyInput?.value ?? 0));
        }

        private void JoinRoom(int visibleIndex)
        {
            if (_inputHandler == null || visibleIndex < 0 || visibleIndex >= _visibleRoomIds.Count)
                return;

            _inputHandler.JoinRoom(_visibleRoomIds[visibleIndex], _displayNameInput?.value ?? string.Empty);
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
            SetPage(_lobbyPage, true);
            SetPage(_garagePage, false);
            SetNavState(_lobbyNav);
            if (_shellTitle != null)
                _shellTitle.text = "로비";
        }

        private void ShowGaragePage()
        {
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, true);
            SetNavState(_garageNav);
            if (_shellTitle != null)
                _shellTitle.text = "차고";
        }

        private void ShowRecordsPage()
        {
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, true);
            SetNavState(_recordsNav);
            if (_shellTitle != null)
                _shellTitle.text = "기록";
        }

        private void SetNavState(Button selected)
        {
            SetSelected(_lobbyNav, selected == _lobbyNav);
            SetSelected(_garageNav, selected == _garageNav);
            SetSelected(_recordsNav, selected == _recordsNav);
        }

        private static void SetPage(VisualElement page, bool visible)
        {
            if (page != null)
                page.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetSelected(VisualElement element, bool selected)
        {
            if (element == null)
                return;
            if (selected)
                element.AddToClassList("shared-nav-item--selected");
            else
                element.RemoveFromClassList("shared-nav-item--selected");
        }

        private static Label Label(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
