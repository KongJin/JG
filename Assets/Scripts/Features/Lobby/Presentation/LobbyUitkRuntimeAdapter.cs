using System;
using System.Collections.Generic;
using Features.Garage.Presentation;
using Features.Lobby.Domain;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyUitkRuntimeAdapter
    {
        private readonly UIDocument _document;
        private readonly UIDocument _garageDocument;
        private readonly GarageSetBUitkRuntimeAdapter _garageAdapter;
        private readonly VisualTreeAsset _lobbyShellTree;
        private readonly VisualTreeAsset _operationMemoryTree;
        private readonly VisualTreeAsset _accountSyncTree;
        private readonly VisualTreeAsset _connectionReconnectTree;
        private readonly UnityEngine.Object _logContext;

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

        public LobbyUitkRuntimeAdapter(
            UIDocument document,
            UIDocument garageDocument,
            GarageSetBUitkRuntimeAdapter garageAdapter,
            VisualTreeAsset lobbyShellTree,
            VisualTreeAsset operationMemoryTree,
            VisualTreeAsset accountSyncTree,
            VisualTreeAsset connectionReconnectTree,
            UnityEngine.Object logContext)
        {
            _document = document;
            _garageDocument = garageDocument;
            _garageAdapter = garageAdapter;
            _lobbyShellTree = lobbyShellTree;
            _operationMemoryTree = operationMemoryTree;
            _accountSyncTree = accountSyncTree;
            _connectionReconnectTree = connectionReconnectTree;
            _logContext = logContext;
        }

        public event Action CreateRoomRequested;
        public event Action<DomainEntityId> RoomSelected;
        public event Action LeaveRoomRequested;
        public event Action<TeamType> TeamChangeRequested;
        public event Action ReadyToggled;
        public event Action GameStartRequested;
        public event Action LobbyPageRequested;
        public event Action GaragePageRequested;
        public event Action RecordsPageRequested;
        public event Action AccountPageRequested;
        public event Action ConnectionPageRequested;
        public event Action AccountRefreshRequested;

        public string DisplayNameText => _displayNameInput?.value ?? string.Empty;

        public LobbyCreateRoomInput CreateRoomInput => new LobbyCreateRoomInput(
            _roomNameInput?.value ?? "Room",
            Mathf.Max(1, _capacityInput?.value ?? 4),
            DisplayNameText,
            Mathf.Max(0, _difficultyInput?.value ?? 0));

        public bool Bind()
        {
            if (_root != null)
                return true;

            if (_document == null)
                return false;

            _root = _document.rootVisualElement;
            if (_root == null)
                return false;

            if (_root.Q<VisualElement>("LobbyShellScreen") == null && _lobbyShellTree != null)
            {
                _root.Clear();
                _lobbyShellTree.CloneTree(_root);
            }

            if (_root.Q<VisualElement>("LobbyShellScreen") == null)
            {
                Debug.LogError(
                    "[LobbyPageController] LobbyShellScreen is missing. Assign LobbyShell UXML; runtime generated UI is not available.",
                    _logContext);
                _root = null;
                return false;
            }

            BindAuthoredTree();
            return true;
        }

        public void RenderRooms(LobbyRoomListViewModel viewModel)
        {
            if (!Bind())
                return;

            viewModel ??= LobbyRoomListViewModel.Empty;
            _roomList?.Clear();
            if (_roomCountLabel != null)
                _roomCountLabel.text = viewModel.CountText;

            if (viewModel.Rows == null || viewModel.Rows.Count == 0)
            {
                _roomList?.Add(LobbyUitkElements.Label(viewModel.EmptyText, "uitk-body"));
                return;
            }

            for (var i = 0; i < viewModel.Rows.Count; i++)
            {
                var room = viewModel.Rows[i];
                var row = new Button(() => RoomSelected?.Invoke(room.RoomId))
                {
                    text = room.Text
                };
                row.AddToClassList("uitk-list-row");
                row.SetEnabled(room.IsEnabled);
                _roomList.Add(row);
            }
        }

        public void RenderRoomDetail(LobbyRoomDetailViewModel viewModel)
        {
            if (!Bind())
                return;

            viewModel ??= LobbyRoomDetailViewModel.Empty;

            if (_roomDetailTitle != null)
                _roomDetailTitle.text = viewModel.TitleText;
            if (_roomDetailMeta != null)
                _roomDetailMeta.text = viewModel.MetaText;

            _memberList?.Clear();
            for (var i = 0; i < viewModel.MemberRows.Count; i++)
            {
                _memberList?.Add(LobbyUitkElements.Label(
                    viewModel.MemberRows[i],
                    "uitk-list-row-label"));
            }

            if (_readyButton != null)
                _readyButton.text = viewModel.ReadyButtonText;
            if (_startButton != null)
                _startButton.SetEnabled(viewModel.CanStartGame);
        }

        public void ShowLobbyPage()
        {
            if (!Bind())
                return;

            SetPageVisibility(lobby: true, garage: false, records: false, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_lobbyNav);
            SetShell("로비", "동기화 대기");
        }

        public void ShowGaragePage()
        {
            if (!Bind())
                return;

            EnsureGarageSurface();
            SetPageVisibility(lobby: false, garage: true, records: false, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_garageNav);
            SetShell("차고", "출격 편성 동기화");
        }

        public void ShowRecordsPage()
        {
            if (!Bind())
                return;

            EnsureRecordsSurface();
            SetPageVisibility(lobby: false, garage: false, records: true, account: false, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(_recordsNav);
            SetShell("기록", "LOCAL LOG / SYNC PENDING");
        }

        public void ShowAccountPage()
        {
            if (!Bind())
                return;

            EnsureAccountSurface();
            SetPageVisibility(lobby: false, garage: false, records: false, account: true, connection: false);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            SetShell("계정", "NOVA_SYS / CFG.17");
        }

        public void ShowConnectionPage()
        {
            if (!Bind())
                return;

            EnsureConnectionSurface();
            SetPageVisibility(lobby: false, garage: false, records: false, account: false, connection: true);
            SetGarageDocumentVisible(false);
            SetNavState(null);
            SetShell("연결", "SESSION CHECK");
        }

        public void RenderAccountState(LobbyAccountViewModel viewModel)
        {
            if (!Bind())
                return;

            EnsureAccountSurface();
            viewModel ??= LobbyAccountViewModel.Empty;

            LobbyUitkElements.SetText(_accountPage, "PilotIdLabel", viewModel.PilotIdText);
            LobbyUitkElements.SetText(_accountPage, "GoogleLinkStatusLabel", viewModel.GoogleLinkStatusText);
            LobbyUitkElements.SetText(_accountPage, "UidStatusLabel", viewModel.UidStatusText);
            LobbyUitkElements.SetText(_accountPage, "GarageSyncStateLabel", viewModel.GarageSyncStateText);
            LobbyUitkElements.SetText(_accountPage, "OperationSyncStateLabel", viewModel.OperationSyncStateText);
            LobbyUitkElements.SetText(_accountPage, "CloudSyncStateLabel", viewModel.CloudSyncStateText);
            LobbyUitkElements.SetText(_accountPage, "BlockedReasonBodyLabel", viewModel.BlockedReasonBodyText);
            LobbyUitkElements.SetText(_accountPage, "GarageSummaryLabel", viewModel.GarageSummaryText);
            LobbyUitkElements.SetText(_accountPage, "OperationBufferLabel", viewModel.OperationBufferText);
            LobbyUitkElements.SetText(_accountPage, "ConflictStateLabel", viewModel.ConflictStateText);
            LobbyUitkElements.SetText(_accountPage, "LoadingStateLabel", viewModel.LoadingStateText);
            LobbyUitkElements.SetText(_accountPage, "BgmValueLabel", viewModel.BgmValueText);
            LobbyUitkElements.SetText(_accountPage, "SfxValueLabel", viewModel.SfxValueText);
            LobbyUitkElements.SetText(_accountPage, "SaveModeLabel", viewModel.SaveModeText);
            LobbyUitkElements.SetText(_accountPage, "CloudModeLabel", viewModel.CloudModeText);
        }

        public void RenderOperationMemory(LobbyOperationMemoryViewModel viewModel)
        {
            if (!Bind())
                return;

            EnsureRecordsSurface();
            if (_recordsPage == null)
                return;

            viewModel ??= LobbyOperationMemoryViewModel.Empty;
            RenderLatestOperation(_recordsPage.Q<VisualElement>("LatestOperationCard"), viewModel.Latest);
            RenderRecentOperations(_recordsPage.Q<VisualElement>("RecentOperations"), viewModel.RecentRows);
            RenderUnitTrace(_recordsPage.Q<VisualElement>("UnitTrace"), viewModel.Trace);
        }

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

            RegisterClick("ShellMenuButton", () => ConnectionPageRequested?.Invoke());
            RegisterClick("ShellSettingsButton", () => AccountPageRequested?.Invoke());
            RegisterClick("CreateRoomButton", () => CreateRoomRequested?.Invoke());
            RegisterClick("GarageSummaryButton", () => GaragePageRequested?.Invoke());
            RegisterClick("RedTeamButton", () => TeamChangeRequested?.Invoke(TeamType.Red));
            RegisterClick("BlueTeamButton", () => TeamChangeRequested?.Invoke(TeamType.Blue));
            RegisterClick("ReadyButton", () => ReadyToggled?.Invoke());
            RegisterClick("StartButton", () => GameStartRequested?.Invoke());
            RegisterClick("LeaveRoomButton", () => LeaveRoomRequested?.Invoke());
            RegisterClick("LobbyNavButton", () => LobbyPageRequested?.Invoke());
            RegisterClick("GarageNavButton", () => GaragePageRequested?.Invoke());
            RegisterClick("RecordsNavButton", () => RecordsPageRequested?.Invoke());

            EnsureRecordsSurface();
            EnsureAccountSurface();
            EnsureConnectionSurface();
            EnsureGarageSurface();
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
            if (_garagePage == null || _garageAdapter == null)
                return;

            _garageAdapter.BindToHost(_garagePage);
            _garageAdapter.SetDocumentRootVisible(false);
        }

        private void RegisterClick(string buttonName, Action callback)
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

            _recordsPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _recordsPage.Q<Button>("GarageButton")?.RegisterCallback<ClickEvent>(_ => GaragePageRequested?.Invoke());
        }

        private void EnsureAccountSurface()
        {
            if (_accountPage == null || _accountPage.childCount > 0)
                return;

            if (_accountSyncTree != null)
                _accountSyncTree.CloneTree(_accountPage);

            _accountPage.Q<Button>("ManualSyncRetryButton")?.RegisterCallback<ClickEvent>(_ => AccountRefreshRequested?.Invoke());
            _accountPage.Q<Button>("LinkAccountButton")?.RegisterCallback<ClickEvent>(_ => ConnectionPageRequested?.Invoke());
        }

        private void EnsureConnectionSurface()
        {
            if (_connectionPage == null || _connectionPage.childCount > 0)
                return;

            if (_connectionReconnectTree != null)
                _connectionReconnectTree.CloneTree(_connectionPage);

            _connectionPage.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _connectionPage.Q<Button>("ReturnLobbyButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
            _connectionPage.Q<Button>("ManualRetryButton")?.RegisterCallback<ClickEvent>(_ => LobbyPageRequested?.Invoke());
        }

        private void SetPageVisibility(
            bool lobby,
            bool garage,
            bool records,
            bool account,
            bool connection)
        {
            LobbyUitkElements.SetPage(_lobbyPage, lobby);
            LobbyUitkElements.SetPage(_garagePage, garage);
            LobbyUitkElements.SetPage(_recordsPage, records);
            LobbyUitkElements.SetPage(_accountPage, account);
            LobbyUitkElements.SetPage(_connectionPage, connection);
        }

        private void SetNavState(Button selected)
        {
            LobbyUitkElements.SetSelected(_lobbyNav, selected == _lobbyNav);
            LobbyUitkElements.SetSelected(_garageNav, selected == _garageNav);
            LobbyUitkElements.SetSelected(_recordsNav, selected == _recordsNav);
        }

        private void SetShell(string title, string state)
        {
            if (_shellTitle != null)
                _shellTitle.text = title;
            if (_shellState != null)
                _shellState.text = state;
        }

        private static void RenderLatestOperation(VisualElement card, LobbyOperationLatestViewModel viewModel)
        {
            if (card == null)
                return;

            viewModel ??= LobbyOperationLatestViewModel.Empty;
            card.Clear();
            if (!viewModel.HasRecord)
            {
                card.Add(LobbyUitkElements.Label("LATEST_OP", "memory-kicker"));
                card.Add(LobbyUitkElements.Label(viewModel.ResultText, viewModel.ResultClass));
                card.Add(LobbyUitkElements.Label(viewModel.PressureText, "memory-sitrep-text"));
                return;
            }

            var header = new VisualElement();
            header.AddToClassList("memory-card-header");
            var titleStack = new VisualElement();
            titleStack.Add(LobbyUitkElements.Label("LATEST_OP", "memory-kicker"));
            titleStack.Add(LobbyUitkElements.Label(viewModel.ResultText, viewModel.ResultClass));
            header.Add(titleStack);
            header.Add(LobbyUitkElements.Label(viewModel.TimeText, "memory-time"));
            card.Add(header);

            var stats = new VisualElement();
            stats.AddToClassList("memory-stat-grid");
            AddStat(stats, "생존", viewModel.SurvivalText, "memory-stat-value memory-stat-value--blue");
            AddStat(stats, "공세", viewModel.WaveText, "memory-stat-value");
            AddStat(stats, "코어", viewModel.CoreText, viewModel.CoreClass);
            AddStat(stats, "제거", viewModel.KillText, "memory-stat-value");
            card.Add(stats);

            var sitrep = new VisualElement();
            sitrep.AddToClassList("memory-sitrep");
            sitrep.Add(LobbyUitkElements.Label("SITREP", "memory-sitrep-label"));
            sitrep.Add(LobbyUitkElements.Label(viewModel.PressureText, "memory-sitrep-text"));
            card.Add(sitrep);
        }

        private static void RenderRecentOperations(
            VisualElement section,
            IReadOnlyList<LobbyOperationRowViewModel> rows)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(LobbyUitkElements.Label("RECENT OPERATIONS", "memory-section-title"));
            if (rows == null || rows.Count == 0)
            {
                var empty = new VisualElement();
                LobbyUitkElements.AddClasses(empty, "operation-row operation-row--empty");
                empty.Add(LobbyUitkElements.Label("전적 기록 대기중", "operation-empty-text"));
                section.Add(empty);
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var viewModel = rows[i];
                var row = new VisualElement();
                LobbyUitkElements.AddClasses(row, viewModel.RowClass);

                var line = new VisualElement();
                LobbyUitkElements.AddClasses(line, viewModel.LineClass);
                row.Add(line);

                var main = new VisualElement();
                main.AddToClassList("operation-row-main");
                main.Add(LobbyUitkElements.Label(viewModel.TitleText, viewModel.TitleClass));
                main.Add(LobbyUitkElements.Label(viewModel.MetaText, "operation-meta"));
                row.Add(main);

                row.Add(LobbyUitkElements.Label(viewModel.CoreText, viewModel.CoreClass));
                section.Add(row);
            }
        }

        private static void RenderUnitTrace(VisualElement section, LobbyOperationTraceViewModel viewModel)
        {
            if (section == null)
                return;

            viewModel ??= LobbyOperationMemoryViewModel.Empty.Trace;
            section.Clear();
            section.Add(LobbyUitkElements.Label("기체 전적", "memory-section-title"));
            var chips = new VisualElement();
            chips.AddToClassList("memory-chip-row");
            chips.Add(LobbyUitkElements.Label(viewModel.CountChipText, "memory-chip"));
            chips.Add(LobbyUitkElements.Label("LOCAL FIRST", "memory-chip memory-chip--blue"));
            chips.Add(LobbyUitkElements.Label(viewModel.RecentDataChipText, "memory-chip memory-chip--orange"));
            section.Add(chips);
        }

        private static void AddStat(VisualElement parent, string label, string value, string valueClass)
        {
            var cell = new VisualElement();
            cell.AddToClassList("memory-stat-cell");
            cell.Add(LobbyUitkElements.Label(label, "memory-stat-label"));
            cell.Add(LobbyUitkElements.Label(value, valueClass));
            parent.Add(cell);
        }

    }

    internal readonly struct LobbyCreateRoomInput
    {
        public LobbyCreateRoomInput(
            string roomName,
            int capacity,
            string displayName,
            int difficultyPresetId)
        {
            RoomName = roomName;
            Capacity = capacity;
            DisplayName = displayName;
            DifficultyPresetId = difficultyPresetId;
        }

        public string RoomName { get; }
        public int Capacity { get; }
        public string DisplayName { get; }
        public int DifficultyPresetId { get; }
    }
}
