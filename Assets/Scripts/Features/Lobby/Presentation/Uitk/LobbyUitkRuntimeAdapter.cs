using System;
using Features.Garage.Presentation;
using Features.Lobby.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Localization;
using Shared.Math;
using Shared.Runtime.Sound;
using Shared.Sound;
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
        private VisualElement _createRoomOverlay;
        private Label _shellTitle;
        private Label _shellState;
        private TextField _roomNameInput;
        private TextField _displayNameInput;
        private IntegerField _capacityInput;
        private IntegerField _difficultyInput;
        private Button _lobbyNav;
        private Button _garageNav;
        private Button _recordsNav;
        private LobbyRoomListSurface _roomListSurface;
        private LobbyGarageSummarySurface _garageSummarySurface;
        private LobbyRoomSelectionOverlay _roomSelectionOverlay;
        private LobbyRoomDetailSurface _roomDetailSurface;
        private LobbyShellPageRouter _pageRouter;
        private LobbyAuxiliarySurfaceBinder _auxiliarySurfaces;
        private readonly LobbyUitkClickScope _clickScope = new();
        private IEventPublisher _clickSoundPublisher;
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
        public event Action JoinSelectedRoomRequested;
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

// csharp-guardrails: allow-null-defense
        public string DisplayNameText => _displayNameInput?.value ?? string.Empty;

        public LobbyCreateRoomInput CreateRoomInput => new LobbyCreateRoomInput(
// csharp-guardrails: allow-null-defense
            _roomNameInput?.value ?? "Room",
// csharp-guardrails: allow-null-defense
            Mathf.Max(1, _capacityInput?.value ?? 4),
            DisplayNameText,
// csharp-guardrails: allow-null-defense
            Mathf.Max(0, _difficultyInput?.value ?? 0));

        public void SetClickSoundPublisher(IEventPublisher publisher)
        {
            _clickSoundPublisher = publisher;
        }

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

// csharp-guardrails: allow-null-defense
            if (_document == null)
                return false;

            _root = _document.rootVisualElement;
            // csharp-guardrails: allow-null-defense
            if (_root == null)
                return false;

// csharp-guardrails: allow-null-defense
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

            // csharp-guardrails: allow-null-defense
            _roomListSurface?.Render(viewModel);
        }

        public void RenderGarageSummary(LobbyGarageSummaryViewModel viewModel)
        {
            if (!Bind())
                return;

            // csharp-guardrails: allow-null-defense
            _garageSummarySurface?.Render(viewModel);
        }

        public void RenderRoomSelection(LobbyRoomSelectionViewModel viewModel)
        {
            if (!Bind())
                return;

// csharp-guardrails: allow-null-defense
            if (_roomSelectionOverlay != null && _roomSelectionOverlay.Render(viewModel))
                SetCreateRoomOverlayVisible(false);
        }

        public void RenderRoomDetail(LobbyRoomDetailViewModel viewModel)
        {
            if (!Bind())
                return;

            // csharp-guardrails: allow-null-defense
            if (_roomDetailSurface != null && _roomDetailSurface.Render(viewModel))
            {
                SetCreateRoomOverlayVisible(false);
                SetRoomSelectionOverlayVisible(false);
            }
        }

        public void ShowLobbyPage() => ShowPage(LobbyShellPageId.Lobby);

        public void ShowGaragePage() => ShowPage(LobbyShellPageId.Garage);

        public void ShowRecordsPage() => ShowPage(LobbyShellPageId.Records);

        public void ShowAccountPage() => ShowPage(LobbyShellPageId.Account);

        public void ShowConnectionPage() => ShowPage(LobbyShellPageId.Connection);

        private void ShowPage(LobbyShellPageId pageId)
        {
            if (!Bind())
                return;

// csharp-guardrails: allow-null-defense
            _pageRouter?.Show(pageId);
            SetCreateRoomOverlayVisible(false);
            SetRoomSelectionOverlayVisible(false);
            SetGarageDocumentVisible(false);
        }

        public void RenderAccountState(LobbyAccountViewModel viewModel)
        {
            if (!Bind())
                return;

// csharp-guardrails: allow-null-defense
            _auxiliarySurfaces?.EnsureAccount();
            LobbyAccountStateRenderer.Render(_accountPage, viewModel);
        }

        public void RenderOperationMemory(LobbyOperationMemoryViewModel viewModel)
        {
            if (!Bind())
                return;

// csharp-guardrails: allow-null-defense
            _auxiliarySurfaces?.EnsureRecords();
// csharp-guardrails: allow-null-defense
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
            _createRoomOverlay = Required<VisualElement>("CreateRoomOverlay");
            _shellTitle = Required<Label>("ShellTitleLabel");
            _shellState = Required<Label>("ShellStateLabel");
            _roomNameInput = Required<TextField>("RoomNameInput");
            _displayNameInput = _root.Q<TextField>("DisplayNameInput");
            _capacityInput = Required<IntegerField>("CapacityInput");
            _difficultyInput = Required<IntegerField>("DifficultyInput");
            _lobbyNav = Required<Button>("LobbyNavButton");
            _garageNav = Required<Button>("GarageNavButton");
            _recordsNav = Required<Button>("RecordsNavButton");

            _roomListSurface = new LobbyRoomListSurface(
                Required<ScrollView>("RoomListScroll"),
                Required<VisualElement>("RoomList"),
                Required<VisualElement>("RoomListEmptyStateCard"),
                Required<VisualElement>("CreateRoomCard"),
                Required<Label>("RoomCountLabel"),
                Required<Label>("RoomListEmptyStateBodyLabel"),
                roomId => RoomSelected?.Invoke(roomId));
            _garageSummarySurface = new LobbyGarageSummarySurface(
                Required<VisualElement>("GarageSummarySlotRow"),
                Required<Label>("GarageSummaryStatusLabel"),
                Required<Label>("GarageSummaryTitleLabel"),
                Required<Label>("GarageSummaryBodyLabel"));
            _roomSelectionOverlay = new LobbyRoomSelectionOverlay(
                Required<VisualElement>("RoomSelectionOverlay"),
                Required<VisualElement>("RoomSelectionSlotRow"),
                Required<Label>("RoomSelectionTitleLabel"),
                Required<Label>("RoomSelectionMetaLabel"),
                Required<Label>("RoomSelectionStatusLabel"),
                Required<Label>("RoomSelectionBodyLabel"),
                Required<Button>("JoinSelectedRoomButton"));
            _roomDetailSurface = new LobbyRoomDetailSurface(
                Required<VisualElement>("RoomDetailCard"),
                Required<VisualElement>("RoomActionRow"),
                Required<VisualElement>("MemberList"),
                Required<Label>("RoomDetailTitleLabel"),
                Required<Label>("RoomDetailMetaLabel"),
                Required<Button>("ReadyButton"),
                Required<Button>("StartButton"));

            _roomDetailSurface.Hide();
            _roomListSurface.HideEmptyState();
            SetCreateRoomOverlayVisible(false);
            SetRoomSelectionOverlayVisible(false);
            _garageSummarySurface.Render(LobbyGarageSummaryViewModel.Empty);
            _auxiliarySurfaces = new LobbyAuxiliarySurfaceBinder(
                _recordsPage,
                _accountPage,
                _connectionPage,
                _operationMemoryTree,
                _accountSyncTree,
                _connectionReconnectTree,
                RegisterClick,
                () => LobbyPageRequested?.Invoke(),
                () => GaragePageRequested?.Invoke(),
                () => AccountRefreshRequested?.Invoke(),
                () => ConnectionPageRequested?.Invoke());

            _pageRouter = new LobbyShellPageRouter(
                new[]
                {
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Lobby,
                        _lobbyPage,
                        _lobbyNav,
                        "로비",
                        GameText.Get("common.save_waiting")),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Garage,
                        _garagePage,
                        _garageNav,
                        "차고",
                        "덱 저장 상태",
                        EnsureGarageSurface),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Records,
                        _recordsPage,
                        _recordsNav,
                        GameText.Get("records.title"),
                        "로컬 저장 / 저장 대기 중",
// csharp-guardrails: allow-null-defense
                        () => _auxiliarySurfaces?.EnsureRecords()),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Account,
                        _accountPage,
                        null,
                        "계정",
                        "저장 설정",
// csharp-guardrails: allow-null-defense
                        () => _auxiliarySurfaces?.EnsureAccount()),
                    new LobbyShellPageRoute(
                        LobbyShellPageId.Connection,
                        _connectionPage,
                        null,
                        "연결",
                        GameText.Get("common.connection_checking"),
// csharp-guardrails: allow-null-defense
                        () => _auxiliarySurfaces?.EnsureConnection())
                },
                _shellTitle,
                _shellState);

            RegisterClick("ShellMenuButton", "ui_click", () => RequestTopPage(LobbyShellPageId.Connection, ConnectionPageRequested));
            RegisterClick("ShellSettingsButton", "ui_click", () => RequestTopPage(LobbyShellPageId.Account, AccountPageRequested));
            RegisterClick("CreateRoomOpenButton", "ui_click", () => SetCreateRoomOverlayVisible(true));
            RegisterClick("EmptyStateCreateButton", "ui_click", () => SetCreateRoomOverlayVisible(true));
            RegisterClick("CreateRoomCancelButton", "ui_back", () => SetCreateRoomOverlayVisible(false));
            RegisterClick("CreateRoomButton", "ui_confirm", () => CreateRoomRequested?.Invoke());
            RegisterClick("RoomSelectionDismissButton", "ui_back", () => SetRoomSelectionOverlayVisible(false));
            RegisterClick("JoinSelectedRoomButton", "ui_select", () => JoinSelectedRoomRequested?.Invoke());
            RegisterClick("RedTeamButton", "ui_select", () => TeamChangeRequested?.Invoke(TeamType.Red));
            RegisterClick("BlueTeamButton", "ui_select", () => TeamChangeRequested?.Invoke(TeamType.Blue));
            RegisterClick("ReadyButton", "ui_confirm", () => ReadyToggled?.Invoke());
            RegisterClick("StartButton", "ui_confirm", () => GameStartRequested?.Invoke());
            RegisterClick("LeaveRoomButton", "ui_back", () => LeaveRoomRequested?.Invoke());
            RegisterClick("LobbyNavButton", "ui_select", () => LobbyPageRequested?.Invoke());
            RegisterClick("GarageNavButton", "ui_select", () => GaragePageRequested?.Invoke());
            RegisterClick("RecordsNavButton", "ui_select", () => RecordsPageRequested?.Invoke());

            EnsureGarageSurface();
        }

        private void SetCreateRoomOverlayVisible(bool visible)
        {
            UitkElementUtility.SetDisplay(_createRoomOverlay, visible);
        }

        private void SetRoomSelectionOverlayVisible(bool visible)
        {
// csharp-guardrails: allow-null-defense
            _roomSelectionOverlay?.SetVisible(visible);
        }

        private bool SetGarageDocumentVisible(bool isVisible)
        {
// csharp-guardrails: allow-null-defense
            if (_garageDocument != null)
            {
                // csharp-guardrails: allow-null-defense
                if (_garageDocumentHost != null && _garageDocumentHost.SetDocumentRootVisible(isVisible))
                    return isVisible;

                if (!_garageDocument.gameObject.activeSelf)
                    _garageDocument.gameObject.SetActive(true);

                _garageDocument.sortingOrder = 10;
                var root = _garageDocument.rootVisualElement;
                // csharp-guardrails: allow-null-defense
                if (root != null)
                    root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

// csharp-guardrails: allow-null-defense
            return _garageDocument != null && isVisible;
        }

        private void EnsureGarageSurface()
        {
// csharp-guardrails: allow-null-defense
            if (_garagePage == null || _garageAdapter == null)
                return;

            // csharp-guardrails: allow-null-defense
            _garageDocumentHost?.BindToHost(_garagePage);
            // csharp-guardrails: allow-null-defense
            _garageDocumentHost?.SetDocumentRootVisible(false);
        }

        private void RegisterClick(string buttonName, Action callback)
        {
            RegisterClick(_root, buttonName, callback);
        }

        private void RequestTopPage(LobbyShellPageId pageId, Action requestPage)
        {
// csharp-guardrails: allow-null-defense
            if (_pageRouter?.CurrentPageId == pageId)
                LobbyPageRequested?.Invoke();
            else
                requestPage?.Invoke();
        }

        private void RegisterClick(string buttonName, string soundKey, Action callback)
        {
            RegisterClick(_root, buttonName, soundKey, callback);
        }

        private void RegisterClick(VisualElement root, string buttonName, Action callback)
        {
            RegisterClick(root, buttonName, null, callback);
        }

        private void RegisterClick(VisualElement root, string buttonName, string soundKey, Action callback)
        {
            var button = Required<Button>(root, buttonName);
            if (callback != null)
                _clickScope.Register(button, () =>
                {
                    PublishClickSound(soundKey);
                    callback();
                });
        }

        private void PublishClickSound(string soundKey)
        {
// csharp-guardrails: allow-null-defense
            if (_clickSoundPublisher == null || string.IsNullOrWhiteSpace(soundKey))
                return;

            _clickSoundPublisher.Publish(new SoundRequestEvent(new SoundRequest(
                soundKey,
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                SoundPlayer.LobbyOwnerId,
                0.05f)));
        }

        private T Required<T>(string name) where T : VisualElement
        {
            return Required<T>(_root, name);
        }

        private static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            return UitkElementUtility.Required<T>(root, name, "Lobby UITK");
        }

    }
}
