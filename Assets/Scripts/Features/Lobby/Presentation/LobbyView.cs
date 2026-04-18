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
        [SerializeField]
        private GameObject _lobbyPageRoot;

        [SerializeField]
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

        [Header("Tab Visuals")]
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

        [Header("Game Start")]
        [Required, SerializeField]
        private string _gameSceneName = "GameScene";

        private IEventSubscriber _eventBus;
        private IEventPublisher _eventPublisher;
        private DisposableScope _disposables = new DisposableScope();
        private bool _tabsHooked;
        private bool _showingRoomDetail;
        private bool _garageFocused;

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases
        )
        {
            if (_roomListView == null)
            {
                Debug.LogError("[LobbyView] _roomListView is not assigned.");
                return;
            }

            if (_roomDetailView == null)
            {
                Debug.LogError("[LobbyView] _roomDetailView is not assigned.");
                return;
            }

            _eventBus = eventBus;
            _eventPublisher = eventPublisher;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _roomListView.Initialize(useCases, eventPublisher);
            _roomDetailView.Initialize(useCases, eventBus, eventPublisher);
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
            if (_roomListView == null)
                return;
            _roomListView.Render(lobby.Rooms);
            ShowRoomList();
        }

        public void RenderRoomList(RoomListReceivedEvent e)
        {
            if (_roomListView == null)
                return;
            _roomListView.Render(e.Rooms);
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            if (_roomDetailView == null)
                return;
            _roomDetailView.SetLocalMemberId(e.LocalMemberId);
            _roomDetailView.Render(e.Room);
            ShowRoomDetail();
        }

        public void RenderStartGame(RoomSnapshot room)
        {
            Debug.Log($"[Lobby] Start game: {room.Name}");
            _eventPublisher.Publish(new SceneLoadRequestedEvent(_gameSceneName));
        }

        private void ShowRoomList()
        {
            _showingRoomDetail = false;

            if (_roomListPanel != null)
                _roomListPanel.SetActive(true);
            if (_roomDetailPanel != null)
                _roomDetailPanel.SetActive(false);
        }

        private void ShowRoomDetail()
        {
            _showingRoomDetail = true;

            if (_roomListPanel != null)
                _roomListPanel.SetActive(false);
            if (_roomDetailPanel != null)
                _roomDetailPanel.SetActive(true);
        }

        private void HookTabs()
        {
            if (_tabsHooked)
                return;

            _tabsHooked = true;

            if (_lobbyTabButton != null)
                _lobbyTabButton.onClick.AddListener(ShowLobbyPage);
            if (_garageTabButton != null)
                _garageTabButton.onClick.AddListener(ShowGaragePage);
        }

        private void ShowLobbyPage()
        {
            _garageFocused = false;
            EnsureDashboardRootsVisible();
            UpdateDashboardFocus();

            if (_showingRoomDetail)
                ShowRoomDetail();
            else
                ShowRoomList();

            UpdateTabState(true);
        }

        private void ShowGaragePage()
        {
            _garageFocused = true;
            EnsureDashboardRootsVisible();
            UpdateDashboardFocus();

            if (_showingRoomDetail)
                ShowRoomDetail();
            else
                ShowRoomList();

            UpdateTabState(false);
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
            if (_lobbyTabButton != null)
                _lobbyTabButton.interactable = true;
            if (_garageTabButton != null)
                _garageTabButton.interactable = true;

            UpdateTabVisuals(_lobbyTabButton, lobbyActive);
            UpdateTabVisuals(_garageTabButton, !lobbyActive);
        }

        private void UpdateTabVisuals(Button tabButton, bool isActive)
        {
            if (tabButton == null)
                return;

            // 배경 색상
            if (tabButton.TryGetComponent<Image>(out var bgImage))
            {
                bgImage.color = isActive ? _activeTabColor : _inactiveTabColor;
            }

            // 활성 탭 보더 강조 (왼쪽 3px)
            var border = tabButton == _lobbyTabButton ? _lobbyTabBorder : _garageTabBorder;
            if (border != null)
            {
                border.enabled = isActive;
                border.color = isActive ? ThemeColors.AccentBlue : Color.clear;
            }

            // 텍스트 색상
            var label = tabButton == _lobbyTabButton ? _lobbyTabText : _garageTabText;
            if (label != null)
                label.color = isActive ? _activeTextColor : _inactiveTextColor;
        }

        private void EnsureDashboardRootsVisible()
        {
            if (_lobbyPageRoot != null)
                _lobbyPageRoot.SetActive(true);
            if (_garagePageRoot != null)
                _garagePageRoot.SetActive(true);
        }

        private void UpdateDashboardFocus()
        {
            ApplyCanvasGroup(_lobbyPageCanvasGroup, _garageFocused ? 0.92f : 1f);
            ApplyCanvasGroup(_garagePageCanvasGroup, _garageFocused ? 1f : 0.96f);
        }

        private static void ApplyCanvasGroup(CanvasGroup group, float alpha)
        {
            if (group == null)
                return;

            group.alpha = alpha;
        }
    }
}
