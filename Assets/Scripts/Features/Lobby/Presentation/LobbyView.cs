using Features.Garage.Presentation.Theme;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
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
        [SerializeField]
        private Button _lobbyTabButton;

        [SerializeField]
        private Button _garageTabButton;

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
            ConfigureDashboardLayout();

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
            foreach (Transform child in tabButton.transform)
            {
                if (child.TryGetComponent<TMPro.TMP_Text>(out var tmpText))
                {
                    tmpText.color = isActive ? _activeTextColor : _inactiveTextColor;
                }
            }
        }

        private void ConfigureDashboardLayout()
        {
            EnsureDashboardRootsVisible();

            var lobbyRect = _lobbyPageRoot != null ? _lobbyPageRoot.GetComponent<RectTransform>() : null;
            var garageRect = _garagePageRoot != null ? _garagePageRoot.GetComponent<RectTransform>() : null;
            var roomListRect = _roomListPanel != null ? _roomListPanel.GetComponent<RectTransform>() : null;
            var roomDetailRect = _roomDetailPanel != null ? _roomDetailPanel.GetComponent<RectTransform>() : null;
            var summaryRect = _lobbyPageRoot != null ? _lobbyPageRoot.transform.Find("Summary") as RectTransform : null;
            var tabsRect = _lobbyTabButton != null ? _lobbyTabButton.transform.parent as RectTransform : null;

            SetStretch(lobbyRect, 0.03f, 0.10f, 0.38f, 0.88f);
            SetStretch(garageRect, 0.40f, 0.08f, 0.98f, 0.90f);
            SetStretch(summaryRect, 0f, 0.78f, 1f, 1f);
            SetStretch(roomListRect, 0f, 0f, 1f, 0.74f);
            SetStretch(roomDetailRect, 0f, 0f, 1f, 0.74f);

            if (tabsRect != null)
            {
                SetStretch(tabsRect, 0.74f, 0.90f, 0.95f, 0.96f);
            }

            UpdateDashboardFocus();
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
            ApplyCanvasGroup(_lobbyPageRoot, _garageFocused ? 0.92f : 1f);
            ApplyCanvasGroup(_garagePageRoot, _garageFocused ? 1f : 0.96f);
        }

        private static void ApplyCanvasGroup(GameObject target, float alpha)
        {
            if (target == null)
                return;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();

            group.alpha = alpha;
        }

        private static void SetStretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
