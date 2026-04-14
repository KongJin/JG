using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
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
        private Image _lobbyTabBorder;
        private Image _garageTabBorder;

        public void Initialize(IEventSubscriber eventBus, IEventPublisher eventPublisher, LobbyUseCases useCases)
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
            InitializeTabBorders();

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
            if (_lobbyPageRoot != null)
                _lobbyPageRoot.SetActive(true);
            if (_garagePageRoot != null)
                _garagePageRoot.SetActive(false);

            if (_showingRoomDetail)
                ShowRoomDetail();
            else
                ShowRoomList();

            UpdateTabState(true);
        }

        private void ShowGaragePage()
        {
            if (_lobbyPageRoot != null)
                _lobbyPageRoot.SetActive(false);
            if (_garagePageRoot != null)
                _garagePageRoot.SetActive(true);
            if (_roomListPanel != null)
                _roomListPanel.SetActive(false);
            if (_roomDetailPanel != null)
                _roomDetailPanel.SetActive(false);

            UpdateTabState(false);
        }

        private void UpdateTabState(bool lobbyActive)
        {
            if (_lobbyTabButton != null)
                _lobbyTabButton.interactable = !lobbyActive;
            if (_garageTabButton != null)
                _garageTabButton.interactable = lobbyActive;

            UpdateTabVisuals(_lobbyTabButton, lobbyActive);
            UpdateTabVisuals(_garageTabButton, !lobbyActive);
        }

        private void UpdateTabVisuals(Button tabButton, bool isActive)
        {
            if (tabButton == null) return;

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

        private void InitializeTabBorders()
        {
            // 탭 버튼에 왼쪽 보더 Image 추가 (활성 시 강조용)
            CreateTabBorder(_lobbyTabButton, ref _lobbyTabBorder);
            CreateTabBorder(_garageTabButton, ref _garageTabBorder);
        }

        private static void CreateTabBorder(Button tabButton, ref Image borderRef)
        {
            if (tabButton == null) return;

            // 이미 자식에 "TabBorder"가 있는지 확인
            foreach (Transform child in tabButton.transform)
            {
                if (child.name == "TabBorder" && child.TryGetComponent<Image>(out var existingBorder))
                {
                    borderRef = existingBorder;
                    borderRef.enabled = false;
                    return;
                }
            }

            // 새 보더 GameObject 생성 — 왼쪽 3px
            // UI 요소이므로 RectTransform 사용
            var borderGO = new GameObject("TabBorder");
            var rectTransform = borderGO.AddComponent<RectTransform>();
            rectTransform.SetParent(tabButton.transform, false);

            // 왼쪽에 3px 보더 배치
            rectTransform.anchorMin = new Vector2(0f, 0.1f);
            rectTransform.anchorMax = new Vector2(0f, 0.9f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.sizeDelta = new Vector2(3f, 0f);

            borderRef = borderGO.AddComponent<Image>();
            borderRef.enabled = false;
        }
    }
}
