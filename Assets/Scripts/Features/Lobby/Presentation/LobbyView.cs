using System;
using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Garage.Presentation;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
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
        private IEventSubscriber _eventBus;
        private DisposableScope _disposables = new();
        private readonly List<DomainEntityId> _visibleRoomIds = new();
        private DomainEntityId _currentRoomId;
        private DomainEntityId _localMemberId;
        private bool _localIsReady;

        private VisualElement _root;
        private VisualElement _lobbyPage;
        private VisualElement _garagePage;
        private VisualElement _recordsPage;
        private VisualElement _accountPage;
        private VisualElement _connectionPage;
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
            EnsureAccountSurface();

            var displayName = string.IsNullOrWhiteSpace(profile?.displayName)
                ? "LOCAL PILOT"
                : profile.displayName.Trim();
            var authType = string.IsNullOrWhiteSpace(profile?.authType)
                ? "LOCAL"
                : profile.authType.Trim().ToUpperInvariant();
            var uidText = string.IsNullOrWhiteSpace(profile?.uid)
                ? "UID WAIT"
                : $"UID {Shorten(profile.uid)}";
            var operationCount = new OperationRecordJsonStore().Load().Count;
            var garageCount = accountData?.GarageRoster?.Count ?? 0;
            var settings = accountData?.Settings;

            SetText(_accountPage, "PilotIdLabel", displayName);
            SetText(_accountPage, "GoogleLinkStatusLabel", authType == "GOOGLE" ? "G-LINK OK" : "G-LINK WAIT");
            SetText(_accountPage, "UidStatusLabel", uidText);
            SetText(_accountPage, "GarageSyncStateLabel", garageCount > 0 ? $"{garageCount}/4" : "로컬");
            SetText(_accountPage, "OperationSyncStateLabel", $"{operationCount}/5");
            SetText(_accountPage, "CloudSyncStateLabel", authType == "GOOGLE" ? "준비" : "대기");
            SetText(_accountPage, "BlockedReasonBodyLabel", authType == "GOOGLE" ? "동기화 가능" : "Google 연결 필요");
            SetText(_accountPage, "GarageSummaryLabel", garageCount > 0 ? $"편성 {garageCount}기" : "편성 대기");
            SetText(_accountPage, "OperationBufferLabel", $"{operationCount}/5");
            SetText(_accountPage, "ConflictStateLabel", "정상");
            SetText(_accountPage, "LoadingStateLabel", "READY");
            SetText(_accountPage, "BgmValueLabel", $"{Mathf.RoundToInt((settings?.bgmVolume ?? 0.8f) * 100f)}%");
            SetText(_accountPage, "SfxValueLabel", $"{Mathf.RoundToInt((settings?.sfxVolume ?? 1f) * 100f)}%");
            SetText(_accountPage, "SaveModeLabel", "LOCAL FIRST");
            SetText(_accountPage, "CloudModeLabel", authType == "GOOGLE" ? "READY" : "WAIT");
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

            if (_root.Q<VisualElement>("LobbyShellScreen") == null && _lobbyShellTree != null)
            {
                _root.Clear();
                _lobbyShellTree.CloneTree(_root);
            }

            if (_root.Q<VisualElement>("LobbyShellScreen") != null)
            {
                BindAuthoredTree();
                return;
            }

            Debug.LogError(
                "[LobbyView] LobbyShellScreen is missing. Assign LobbyShell UXML instead of using runtime fallback UI.",
                this);
            return;
        }

/* Authored LobbyShell UXML is required; runtime builder is removed. xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx*/
        private void BindAuthoredTree()
        {
            _lobbyPage = _root.Q<VisualElement>("LobbyUitkPage");
            _garagePage = _root.Q<VisualElement>("GarageUitkHost");
            _recordsPage = _root.Q<VisualElement>("RecordsUitkHost");
            _accountPage = _root.Q<VisualElement>("AccountUitkHost");
            _connectionPage = _root.Q<VisualElement>("ConnectionUitkHost");
            _roomList = _root.Q<VisualElement>("RoomList");
            _memberList = _root.Q<VisualElement>("MemberList");
            _shellTitle = _root.Q<Label>("ShellTitleLabel");
            _shellState = _root.Q<Label>("ShellStateLabel");
            _roomCountLabel = _root.Q<Label>("RoomCountLabel");
            _roomDetailTitle = _root.Q<Label>("RoomDetailTitleLabel");
            _roomDetailMeta = _root.Q<Label>("RoomDetailMetaLabel");
            _roomNameInput = _root.Q<TextField>("RoomNameInput");
            _displayNameInput = _root.Q<TextField>("DisplayNameInput");
            _capacityInput = _root.Q<IntegerField>("CapacityInput");
            _difficultyInput = _root.Q<IntegerField>("DifficultyInput");
            _readyButton = _root.Q<Button>("ReadyButton");
            _startButton = _root.Q<Button>("StartButton");
            _lobbyNav = _root.Q<Button>("LobbyNavButton");
            _garageNav = _root.Q<Button>("GarageNavButton");
            _recordsNav = _root.Q<Button>("RecordsNavButton");

            RegisterClick("ShellMenuButton", ShowConnectionPage);
            RegisterClick("ShellSettingsButton", ShowAccountPage);
            RegisterClick("CreateRoomButton", CreateRoom);
            RegisterClick("GarageSummaryButton", ShowGaragePage);
            RegisterClick("RedTeamButton", () => ChangeTeam(TeamType.Red));
            RegisterClick("BlueTeamButton", () => ChangeTeam(TeamType.Blue));
            RegisterClick("ReadyButton", ToggleReady);
            RegisterClick("StartButton", StartGame);
            RegisterClick("LeaveRoomButton", LeaveRoom);
            RegisterClick("LobbyNavButton", ShowLobbyPage);
            RegisterClick("GarageNavButton", ShowGaragePage);
            RegisterClick("RecordsNavButton", ShowRecordsPage);

            EnsureRecordsSurface();
            EnsureAccountSurface();
            EnsureConnectionSurface();
            EnsureGarageSurface();
            RenderOperationMemory(new OperationRecordJsonStore().Load());
        }

/* Removed procedural Lobby fallback builder; LobbyShell UXML owns structure. xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx*/
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
                // row style owned by USS.xxxxxxx
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
                // row style owned by USS.xxxxxxx
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
            SetPage(_recordsPage, false);
            SetPage(_accountPage, false);
            SetPage(_connectionPage, false);
            SetGarageDocumentVisible(false);
            SetNavState(_lobbyNav);
            if (_shellTitle != null)
                _shellTitle.text = "로비";
            if (_shellState != null)
                _shellState.text = "동기화 대기";
        }

        private void ShowGaragePage()
        {
            EnsureGarageSurface();
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, true);
            SetPage(_recordsPage, false);
            SetPage(_accountPage, false);
            SetPage(_connectionPage, false);
            SetGarageDocumentVisible(false);
            SetNavState(_garageNav);
            if (_shellTitle != null)
                _shellTitle.text = "차고";
            if (_shellState != null)
                _shellState.text = "출격 편성 동기화";
        }

        private void ShowRecordsPage()
        {
            RenderOperationMemory(new OperationRecordJsonStore().Load());
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, false);
            SetPage(_recordsPage, true);
            SetPage(_accountPage, false);
            SetPage(_connectionPage, false);
            SetGarageDocumentVisible(false);
            SetNavState(_recordsNav);
            if (_shellTitle != null)
                _shellTitle.text = "기록";
            if (_shellState != null)
                _shellState.text = "LOCAL LOG / SYNC PENDING";
        }

        private void ShowAccountPage()
        {
            EnsureAccountSurface();
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, false);
            SetPage(_recordsPage, false);
            SetPage(_accountPage, true);
            SetPage(_connectionPage, false);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            if (_shellTitle != null)
                _shellTitle.text = "계정";
            if (_shellState != null)
                _shellState.text = "NOVA_SYS / CFG.17";
        }

        private void ShowConnectionPage()
        {
            EnsureConnectionSurface();
            SetPage(_lobbyPage, false);
            SetPage(_garagePage, false);
            SetPage(_recordsPage, false);
            SetPage(_accountPage, false);
            SetPage(_connectionPage, true);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            if (_shellTitle != null)
                _shellTitle.text = "연결";
            if (_shellState != null)
                _shellState.text = "SESSION CHECK";
        }

        private bool SetGarageDocumentVisible(bool isVisible)
        {
            if (_garageDocument != null)
            {
                if (_garageAdapter != null && _garageAdapter.SetDocumentRootVisible(isVisible))
                    return isVisible;

                if (!_garageDocument.gameObject.activeSelf)
                    _garageDocument.gameObject.SetActive(true);

                _garageDocument.sortingOrder = 10;
                var root = _garageDocument.rootVisualElement;
                if (root != null)
                    root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            return _garageDocument != null && isVisible;
        }

        private void EnsureGarageSurface()
        {
            if (_garagePage == null)
                return;

            if (_garageAdapter == null)
                return;

            _garageAdapter.BindToHost(_garagePage);
            _garageAdapter.SetDocumentRootVisible(false);
        }

        private void RegisterClick(string buttonName, System.Action callback)
        {
            var button = _root?.Q<Button>(buttonName);
            if (button != null && callback != null)
                button.clicked += callback;
        }

        private void EnsureRecordsSurface()
        {
            if (_recordsPage == null || _recordsPage.childCount > 0)
                return;

            if (_operationMemoryTree != null)
                _operationMemoryTree.CloneTree(_recordsPage);

            _recordsPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => ShowLobbyPage());
            _recordsPage.Q<Button>("GarageButton")?.RegisterCallback<ClickEvent>(_ => ShowGaragePage());
        }

        private void EnsureAccountSurface()
        {
            if (_accountPage == null || _accountPage.childCount > 0)
                return;

            if (_accountSyncTree != null)
                _accountSyncTree.CloneTree(_accountPage);

            _accountPage.Q<Button>("ManualSyncRetryButton")?.RegisterCallback<ClickEvent>(_ => RenderAccountState(null, null));
            _accountPage.Q<Button>("LinkAccountButton")?.RegisterCallback<ClickEvent>(_ => ShowConnectionPage());
        }

        private void EnsureConnectionSurface()
        {
            if (_connectionPage == null || _connectionPage.childCount > 0)
                return;

            if (_connectionReconnectTree != null)
                _connectionReconnectTree.CloneTree(_connectionPage);

            _connectionPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => ShowLobbyPage());
            _connectionPage.Q<Button>("ReturnLobbyButton")?.RegisterCallback<ClickEvent>(_ => ShowLobbyPage());
            _connectionPage.Q<Button>("ManualRetryButton")?.RegisterCallback<ClickEvent>(_ => ShowLobbyPage());
        }

        private void RenderOperationMemory(RecentOperationRecords records)
        {
            EnsureRecordsSurface();
            if (_recordsPage == null)
                return;

            records ??= new RecentOperationRecords();
            var list = records.Records;
            RenderLatestOperation(_recordsPage.Q<VisualElement>("LatestOperationCard"), list.Count > 0 ? list[0] : null);
            RenderRecentOperations(_recordsPage.Q<VisualElement>("RecentOperations"), list);
            RenderUnitTrace(_recordsPage.Q<VisualElement>("UnitTrace"), list);
        }

        private static void RenderLatestOperation(VisualElement card, OperationRecord record)
        {
            if (card == null)
                return;

            card.Clear();
            if (record == null)
            {
                card.Add(Label("LATEST_OP", "memory-kicker"));
                card.Add(Label("작전 기록 없음", "memory-result"));
                card.Add(Label("전투 종료 후 최근 작전 데이터가 여기에 표시됩니다.", "memory-sitrep-text"));
                return;
            }

            var header = new VisualElement();
            header.AddToClassList("memory-card-header");
            var titleStack = new VisualElement();
            titleStack.Add(Label("LATEST_OP", "memory-kicker"));
            titleStack.Add(Label(ResultText(record), record.result == OperationRecordResult.Held ? "memory-result memory-result--held" : "memory-result operation-title--danger"));
            header.Add(titleStack);
            header.Add(Label(FormatClock(record.endedAtUnixMs), "memory-time"));
            card.Add(header);

            var stats = new VisualElement();
            stats.AddToClassList("memory-stat-grid");
            AddStat(stats, "생존", FormatDuration(record.survivalSeconds), "memory-stat-value memory-stat-value--blue");
            AddStat(stats, "공세", record.reachedWave.ToString(), "memory-stat-value");
            AddStat(stats, "코어", FormatCore(record), "memory-stat-value memory-stat-value--orange");
            AddStat(stats, "제거", record.unitKillCount.ToString(), "memory-stat-value");
            card.Add(stats);

            var sitrep = new VisualElement();
            sitrep.AddToClassList("memory-sitrep");
            sitrep.Add(Label("SITREP", "memory-sitrep-label"));
            sitrep.Add(Label(PressureText(record), "memory-sitrep-text"));
            card.Add(sitrep);
        }

        private static void RenderRecentOperations(VisualElement section, IReadOnlyList<OperationRecord> records)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(Label("RECENT OPERATIONS", "memory-section-title"));
            if (records == null || records.Count == 0)
            {
                var empty = new VisualElement();
                AddClasses(empty, "operation-row operation-row--empty");
                empty.Add(Label("전적 기록 대기중", "operation-empty-text"));
                section.Add(empty);
                return;
            }

            for (var i = 0; i < records.Count && i < RecentOperationRecords.MaxRecords; i++)
            {
                var record = records[i];
                var row = new VisualElement();
                AddClasses(row, record.result == OperationRecordResult.Held
                    ? "operation-row operation-row--held"
                    : "operation-row operation-row--danger");

                var line = new VisualElement();
                AddClasses(line, record.result == OperationRecordResult.Held
                    ? "operation-row-line"
                    : "operation-row-line operation-row-line--danger");
                row.Add(line);

                var main = new VisualElement();
                main.AddToClassList("operation-row-main");
                main.Add(Label(ResultText(record), record.result == OperationRecordResult.Held
                    ? "operation-title operation-title--held"
                    : "operation-title operation-title--danger"));
                main.Add(Label($"{FormatClock(record.endedAtUnixMs)} / 공세 {record.reachedWave:00} / {BuildRosterSummary(record)}", "operation-meta"));
                row.Add(main);

                row.Add(Label($"CORE {FormatCore(record)}", record.result == OperationRecordResult.Held
                    ? "operation-core"
                    : "operation-core operation-core--danger"));
                section.Add(row);
            }
        }

        private static void RenderUnitTrace(VisualElement section, IReadOnlyList<OperationRecord> records)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(Label("기체 전적", "memory-section-title"));
            var chips = new VisualElement();
            chips.AddToClassList("memory-chip-row");
            var count = records?.Count ?? 0;
            chips.Add(Label($"{count}/5 RECORDS STORED", "memory-chip"));
            chips.Add(Label("LOCAL FIRST", "memory-chip memory-chip--blue"));
            chips.Add(Label(count > 0 ? "RECENT DATA LOADED" : "NO OPERATIONS", "memory-chip memory-chip--orange"));
            section.Add(chips);
        }

        private static void AddStat(VisualElement parent, string label, string value, string valueClass)
        {
            var cell = new VisualElement();
            cell.AddToClassList("memory-stat-cell");
            cell.Add(Label(label, "memory-stat-label"));
            cell.Add(Label(value, valueClass));
            parent.Add(cell);
        }

        private static void SetText(VisualElement root, string labelName, string text)
        {
            var label = root?.Q<Label>(labelName);
            if (label != null)
                label.text = text ?? string.Empty;
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "WAIT";

            value = value.Trim();
            return value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private static string ResultText(OperationRecord record)
        {
            return record?.result == OperationRecordResult.BaseCollapsed ? "거점 붕괴" : "버텨냄";
        }

        private static string FormatClock(long unixMs)
        {
            if (unixMs <= 0L)
                return "--:--";

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToLocalTime().ToString("HH:mm");
        }

        private static string FormatDuration(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatCore(OperationRecord record)
        {
            if (record == null || !record.hasCoreHealthPercent)
                return "--";

            return $"{Mathf.RoundToInt(record.coreHealthPercent * 100f)}%";
        }

        private static string PressureText(OperationRecord record)
        {
            if (record == null)
                return "작전 기록 대기 중";

            if (record.result == OperationRecordResult.BaseCollapsed)
                return "코어 방어선이 붕괴되었습니다. 다음 편성에서 방어/회복 축을 보강하세요.";

            return record.pressureSummaryKey == "pressure.core-collapsed"
                ? "거점 압박이 치명 단계까지 상승했습니다."
                : "방어선 유지. 최근 편성의 성과가 작전 기록에 반영되었습니다.";
        }

        private static string BuildRosterSummary(OperationRecord record)
        {
            if (record?.primaryRosterUnits == null || record.primaryRosterUnits.Count == 0)
                return "NO ROSTER";

            var units = new List<string>(record.primaryRosterUnits.Count);
            for (var i = 0; i < record.primaryRosterUnits.Count; i++)
                units.Add(record.primaryRosterUnits[i].Replace("|", " / "));

            return string.Join(" + ", units);
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
            AddClasses(label, className);
            label.style.color = new Color(0.86f, 0.91f, 0.96f, 1f);
            return label;
        }

        private static void AddClasses(VisualElement element, string classNames)
        {
            if (element == null || string.IsNullOrWhiteSpace(classNames))
                return;

            var parts = classNames.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
                element.AddToClassList(parts[i]);
        }

/* Removed runtime style helpers; LobbyShell USS owns styling. xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx*/
    }
}
