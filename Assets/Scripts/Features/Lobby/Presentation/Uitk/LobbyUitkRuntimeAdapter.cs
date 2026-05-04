using System;
using Features.Garage.Presentation;
using Features.Lobby.Domain;
using Shared.Kernel;
using Shared.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyUitkRuntimeAdapter : IDisposable
    {
        private readonly UIDocument _document;
        private readonly UIDocument _garageDocument;
        private readonly GarageSetBUitkRuntimeAdapter _garageAdapter;
        private readonly GarageSetBUitkDocumentHost _garageDocumentHost;
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
        private LobbyShellPageRouter _pageRouter;
        private readonly LobbyUitkClickScope _clickScope = new();
        private bool _isBound;
        private bool _isDisposed;

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
            _garageDocumentHost = garageAdapter != null
                ? new GarageSetBUitkDocumentHost(garageDocument, garageAdapter)
                : null;
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

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _clickScope.Dispose();
        }

        public bool Bind()
        {
            if (_isBound)
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

            try
            {
                BindAuthoredTree();
                _isBound = true;
            }
            catch
            {
                _isBound = false;
                _root = null;
                _pageRouter = null;
                throw;
            }

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
                _roomList?.Add(CreateLabel(viewModel.EmptyText, "uitk-body lobby-room-empty"));
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
                _memberList?.Add(CreateLabel(
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
            ShowPage(LobbyShellPageId.Lobby);
        }

        public void ShowGaragePage()
        {
            ShowPage(LobbyShellPageId.Garage);
        }

        public void ShowRecordsPage()
        {
            ShowPage(LobbyShellPageId.Records);
        }

        public void ShowAccountPage()
        {
            ShowPage(LobbyShellPageId.Account);
        }

        public void ShowConnectionPage()
        {
            ShowPage(LobbyShellPageId.Connection);
        }

        private void ShowPage(LobbyShellPageId pageId)
        {
            if (!Bind())
                return;

            _pageRouter?.Show(pageId);
            SetGarageDocumentVisible(false);
        }

        public void RenderAccountState(LobbyAccountViewModel viewModel)
        {
            if (!Bind())
                return;

            EnsureAccountSurface();
            viewModel ??= LobbyAccountViewModel.Empty;

            UitkElementUtility.SetText(_accountPage, "PilotIdLabel", viewModel.PilotIdText);
            UitkElementUtility.SetText(_accountPage, "GoogleLinkStatusLabel", viewModel.GoogleLinkStatusText);
            UitkElementUtility.SetText(_accountPage, "UidStatusLabel", viewModel.UidStatusText);
            UitkElementUtility.SetText(_accountPage, "GarageSyncStateLabel", viewModel.GarageSyncStateText);
            UitkElementUtility.SetText(_accountPage, "OperationSyncStateLabel", viewModel.OperationSyncStateText);
            UitkElementUtility.SetText(_accountPage, "CloudSyncStateLabel", viewModel.CloudSyncStateText);
            UitkElementUtility.SetText(_accountPage, "BlockedReasonBodyLabel", viewModel.BlockedReasonBodyText);
            UitkElementUtility.SetText(_accountPage, "GarageSummaryLabel", viewModel.GarageSummaryText);
            UitkElementUtility.SetText(_accountPage, "OperationBufferLabel", viewModel.OperationBufferText);
            UitkElementUtility.SetText(_accountPage, "ConflictStateLabel", viewModel.ConflictStateText);
            UitkElementUtility.SetText(_accountPage, "LoadingStateLabel", viewModel.LoadingStateText);
            UitkElementUtility.SetText(_accountPage, "BgmValueLabel", viewModel.BgmValueText);
            UitkElementUtility.SetText(_accountPage, "SfxValueLabel", viewModel.SfxValueText);
            UitkElementUtility.SetText(_accountPage, "SaveModeLabel", viewModel.SaveModeText);
            UitkElementUtility.SetText(_accountPage, "CloudModeLabel", viewModel.CloudModeText);
        }

        public void RenderOperationMemory(LobbyOperationMemoryViewModel viewModel)
        {
            if (!Bind())
                return;

            EnsureRecordsSurface();
            if (_recordsPage == null)
                return;

            LobbyOperationMemoryRenderer.Render(_recordsPage, viewModel);
        }

        private void BindAuthoredTree()
        {
            _lobbyPage = Required<VisualElement>("LobbyUitkPage");
            _garagePage = Required<VisualElement>("GarageUitkHost");
            _recordsPage = Required<VisualElement>("RecordsUitkHost");
            _accountPage = Required<VisualElement>("AccountUitkHost");
            _connectionPage = Required<VisualElement>("ConnectionUitkHost");
            _roomList = Required<VisualElement>("RoomList");
            _memberList = Required<VisualElement>("MemberList");
            _shellTitle = Required<Label>("ShellTitleLabel");
            _shellState = Required<Label>("ShellStateLabel");
            _roomCountLabel = Required<Label>("RoomCountLabel");
            _roomDetailTitle = Required<Label>("RoomDetailTitleLabel");
            _roomDetailMeta = Required<Label>("RoomDetailMetaLabel");
            _roomNameInput = Required<TextField>("RoomNameInput");
            _displayNameInput = Required<TextField>("DisplayNameInput");
            _capacityInput = Required<IntegerField>("CapacityInput");
            _difficultyInput = Required<IntegerField>("DifficultyInput");
            _readyButton = Required<Button>("ReadyButton");
            _startButton = Required<Button>("StartButton");
            _lobbyNav = Required<Button>("LobbyNavButton");
            _garageNav = Required<Button>("GarageNavButton");
            _recordsNav = Required<Button>("RecordsNavButton");
            _pageRouter = new LobbyShellPageRouter(
                new[]
                {
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Lobby,
                        _lobbyPage,
                        _lobbyNav,
                        "로비",
                        "동기화 대기"),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Garage,
                        _garagePage,
                        _garageNav,
                        "차고",
                        "출격 편성 동기화",
                        EnsureGarageSurface),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Records,
                        _recordsPage,
                        _recordsNav,
                        "기록",
                        "LOCAL LOG / SYNC PENDING",
                        EnsureRecordsSurface),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Account,
                        _accountPage,
                        null,
                        "계정",
                        "NOVA_SYS / CFG.17",
                        EnsureAccountSurface),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Connection,
                        _connectionPage,
                        null,
                        "연결",
                        "SESSION CHECK",
                        EnsureConnectionSurface)
                },
                _shellTitle,
                _shellState);

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

            EnsureGarageSurface();
        }

        private bool SetGarageDocumentVisible(bool isVisible)
        {
            if (_garageDocument != null)
            {
                if (_garageDocumentHost != null && _garageDocumentHost.SetDocumentRootVisible(isVisible))
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

            _garageDocumentHost?.BindToHost(_garagePage);
            _garageDocumentHost?.SetDocumentRootVisible(false);
        }

        private void RegisterClick(string buttonName, Action callback)
        {
            RegisterClick(_root, buttonName, callback);
        }

        private void RegisterClick(VisualElement root, string buttonName, Action callback)
        {
            var button = Required<Button>(root, buttonName);
            if (callback != null)
                _clickScope.Register(button, callback);
        }

        private void EnsureRecordsSurface()
        {
            if (_recordsPage == null || _recordsPage.childCount > 0)
                return;

            if (_operationMemoryTree != null)
            {
                _operationMemoryTree.CloneTree(_recordsPage);

                RegisterClick(_recordsPage, "BackButton", () => LobbyPageRequested?.Invoke());
                RegisterClick(_recordsPage, "GarageButton", () => GaragePageRequested?.Invoke());
            }
        }

        private void EnsureAccountSurface()
        {
            if (_accountPage == null || _accountPage.childCount > 0)
                return;

            if (_accountSyncTree != null)
            {
                _accountSyncTree.CloneTree(_accountPage);

                RegisterClick(_accountPage, "ManualSyncRetryButton", () => AccountRefreshRequested?.Invoke());
                RegisterClick(_accountPage, "LinkAccountButton", () => ConnectionPageRequested?.Invoke());
            }
        }

        private void EnsureConnectionSurface()
        {
            if (_connectionPage == null || _connectionPage.childCount > 0)
                return;

            if (_connectionReconnectTree != null)
            {
                _connectionReconnectTree.CloneTree(_connectionPage);

                RegisterClick(_connectionPage, "BackButton", () => LobbyPageRequested?.Invoke());
                RegisterClick(_connectionPage, "ReturnLobbyButton", () => LobbyPageRequested?.Invoke());
                RegisterClick(_connectionPage, "ManualRetryButton", () => LobbyPageRequested?.Invoke());
            }
        }

        private T Required<T>(string name) where T : VisualElement
        {
            return Required<T>(_root, name);
        }

        private static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            return UitkElementUtility.Required<T>(root, name, "Lobby UITK");
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = UitkElementUtility.CreateLabel(text, className);
            label.style.color = UiThemeColors.TextPrimary;
            return label;
        }

    }
}
