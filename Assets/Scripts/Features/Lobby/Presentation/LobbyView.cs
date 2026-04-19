using Features.Garage.Presentation.Theme;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
    {
        [Header("Pages")]
        [Required, SerializeField]
        private GameObject _lobbyPageRoot;

        [Required, SerializeField]
        private GameObject _garagePageRoot;

        [Header("Tabs")]
        [Required, SerializeField]
        private Button _lobbyTabButton;

        [Required, SerializeField]
        private Button _garageTabButton;

        [Header("Tab Wiring")]
        [Required, SerializeField]
        private TMP_Text _lobbyTabText;

        [Required, SerializeField]
        private TMP_Text _garageTabText;

        [Header("Navigation Visuals")]
        [SerializeField]
        private Color _activeTabColor = new Color(0.286f, 0.463f, 1f, 1f);

        [SerializeField]
        private Color _inactiveTabColor = new Color(0.086f, 0.157f, 0.196f, 1f);

        [SerializeField]
        private Color _activeTextColor = Color.white;

        [SerializeField]
        private Color _inactiveTextColor = new Color(0.545f, 0.584f, 0.651f, 1f);

        [Header("Tab Border References")]
        [Required, SerializeField]
        private Image _lobbyTabBorder;

        [Required, SerializeField]
        private Image _garageTabBorder;

        [Header("Panels")]
        [Required, SerializeField]
        private GameObject _roomListPanel;

        [Required, SerializeField]
        private GameObject _roomDetailPanel;

        [Header("Focus")]
        [Required, SerializeField]
        private CanvasGroup _lobbyPageCanvasGroup;

        [Required, SerializeField]
        private CanvasGroup _garagePageCanvasGroup;

        [Header("Views")]
        [Required, SerializeField]
        private RoomListView _roomListView;

        [Required, SerializeField]
        private RoomDetailView _roomDetailView;

        [Header("Lobby Summary")]
        [Required, SerializeField]
        private LobbyGarageSummaryView _garageSummaryView;

        [Header("Game Start")]
        [Required, SerializeField]
        private string _gameSceneName = "GameScene";

        private IEventSubscriber _eventBus;
        private IEventPublisher _eventPublisher;
        private DisposableScope _disposables = new DisposableScope();
        private bool _tabsHooked;
        private bool _showingRoomDetail;
        private readonly LobbyPageStateController _pageStateController = new();
        private readonly LobbySceneLoadCoordinator _sceneLoadCoordinator = new();

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases
        )
        {
            _eventBus = eventBus;
            _eventPublisher = eventPublisher;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _roomListView.Initialize(useCases, eventPublisher);
            _roomDetailView.Initialize(useCases, eventBus, eventPublisher);
            _garageSummaryView.Initialize(eventBus);
            HookTabs();

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
            _eventBus.Subscribe<RoomUpdatedEvent>(this, e => RenderRoom(e));
            _eventBus.Subscribe<RoomListReceivedEvent>(this, e => RenderRoomList(e));
            _eventBus.Subscribe<GameStartedEvent>(this, e => RenderStartGame(e.Room));

            ShowLobbyPage();
            ShowRoomList();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        public void RenderLobby(LobbySnapshot lobby)
        {
            _roomListView.Render(lobby.Rooms);
            ShowRoomList();
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            _roomListView.Render(e.Rooms);
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            _roomDetailView.SetLocalMemberId(e.LocalMemberId);
            _roomDetailView.Render(e.Room);
            ShowRoomDetail();
        }

        public void RenderStartGame(RoomSnapshot room)
        {
            _sceneLoadCoordinator.RequestGameSceneLoad(_eventPublisher, _gameSceneName, room.Name);
        }

        private void ShowRoomList()
        {
            _showingRoomDetail = false;
            _pageStateController.ShowRoomList(_roomListPanel, _roomDetailPanel);
        }

        private void ShowRoomDetail()
        {
            _showingRoomDetail = true;
            _pageStateController.ShowRoomDetail(_roomListPanel, _roomDetailPanel);
        }

        private void HookTabs()
        {
            if (_tabsHooked)
                return;

            _tabsHooked = true;

            _lobbyTabButton.onClick.AddListener(ShowLobbyPage);
            _garageTabButton.onClick.AddListener(ShowGaragePage);
        }

        private void ShowLobbyPage()
        {
            _pageStateController.ShowLobbyPage(
                _lobbyPageRoot,
                _lobbyPageCanvasGroup,
                _garagePageRoot,
                _garagePageCanvasGroup,
                _roomListPanel,
                _roomDetailPanel,
                _showingRoomDetail,
                UpdateTabState);
        }

        private void ShowGaragePage()
        {
            _pageStateController.ShowGaragePage(
                _lobbyPageRoot,
                _lobbyPageCanvasGroup,
                _garagePageRoot,
                _garagePageCanvasGroup,
                _roomListPanel,
                _roomDetailPanel,
                _showingRoomDetail,
                UpdateTabState);
        }

        /// <summary>
        /// MCP 캡처용 public 메서드
        /// </summary>
        public void OpenGaragePage()
        {
            ShowGaragePage();
        }

        private void UpdateTabState(bool lobbyActive)
        {
            _pageStateController.EnableNavigationButtons(_lobbyTabButton, _garageTabButton);

            // In page-switcher mode only one navigation button is visible on screen at a time,
            // so both buttons should read as clear actions instead of active/inactive tabs.
            UpdateTabVisuals(_lobbyTabButton, true);
            UpdateTabVisuals(_garageTabButton, true);
        }

        private void UpdateTabVisuals(Button tabButton, bool isActive)
        {
            // 배경 색상
            if (tabButton.TryGetComponent<Image>(out var bgImage))
            {
                bgImage.color = isActive ? _activeTabColor : _inactiveTabColor;
            }

            // 활성 탭 보더 강조 (왼쪽 3px)
            var border = tabButton == _lobbyTabButton ? _lobbyTabBorder : _garageTabBorder;
            border.enabled = isActive;
            border.color = isActive ? ThemeColors.AccentBlue : Color.clear;

            // 텍스트 색상
            var label = tabButton == _lobbyTabButton ? _lobbyTabText : _garageTabText;
            label.color = isActive ? _activeTextColor : _inactiveTextColor;
        }

        internal static void SetPageState(GameObject root, CanvasGroup group, bool isVisible)
        {
            if (root != null)
                root.SetActive(isVisible);

            if (group != null)
            {
                group.alpha = isVisible ? 1f : 0f;
                group.interactable = isVisible;
                group.blocksRaycasts = isVisible;
            }
        }
    }

    internal sealed class LobbyPageStateController
    {
        public void ShowLobbyPage(
            GameObject lobbyRoot,
            CanvasGroup lobbyGroup,
            GameObject garageRoot,
            CanvasGroup garageGroup,
            GameObject roomListPanel,
            GameObject roomDetailPanel,
            bool showingRoomDetail,
            System.Action<bool> updateTabState)
        {
            LobbyView.SetPageState(lobbyRoot, lobbyGroup, true);
            LobbyView.SetPageState(garageRoot, garageGroup, false);
            ApplyRoomPanelState(roomListPanel, roomDetailPanel, showingRoomDetail);
            updateTabState?.Invoke(true);
        }

        public void ShowGaragePage(
            GameObject lobbyRoot,
            CanvasGroup lobbyGroup,
            GameObject garageRoot,
            CanvasGroup garageGroup,
            GameObject roomListPanel,
            GameObject roomDetailPanel,
            bool showingRoomDetail,
            System.Action<bool> updateTabState)
        {
            LobbyView.SetPageState(lobbyRoot, lobbyGroup, false);
            LobbyView.SetPageState(garageRoot, garageGroup, true);
            ApplyRoomPanelState(roomListPanel, roomDetailPanel, showingRoomDetail);
            updateTabState?.Invoke(false);
        }

        public void ShowRoomList(GameObject roomListPanel, GameObject roomDetailPanel)
        {
            ApplyRoomPanelState(roomListPanel, roomDetailPanel, false);
        }

        public void ShowRoomDetail(GameObject roomListPanel, GameObject roomDetailPanel)
        {
            ApplyRoomPanelState(roomListPanel, roomDetailPanel, true);
        }

        public void EnableNavigationButtons(Button lobbyTabButton, Button garageTabButton)
        {
            if (lobbyTabButton != null)
                lobbyTabButton.interactable = true;
            if (garageTabButton != null)
                garageTabButton.interactable = true;
        }

        private static void ApplyRoomPanelState(GameObject roomListPanel, GameObject roomDetailPanel, bool showingRoomDetail)
        {
            if (roomListPanel != null)
                roomListPanel.SetActive(!showingRoomDetail);

            if (roomDetailPanel != null)
                roomDetailPanel.SetActive(showingRoomDetail);
        }
    }

    internal sealed class LobbySceneLoadCoordinator
    {
        public void RequestGameSceneLoad(IEventPublisher eventPublisher, string sceneName, string roomName)
        {
            Debug.Log($"[Lobby] Start game: {roomName}");
            eventPublisher?.Publish(new SceneLoadRequestedEvent(sceneName));
        }
    }
}
