using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
    {
        [Header("Pages")]
        [Required, SerializeField]
        private GameObject _lobbyPageRoot;

        [Required, SerializeField]
        private GameObject _garagePageRoot;

        [Header("Navigation")]
        [Required, SerializeField]
        private LobbyGarageNavBarView _navigationBar;

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

        private IEventSubscriber _eventBus;
        private DisposableScope _disposables = new DisposableScope();
        private bool _navigationHooked;
        private bool _showingRoomDetail;
        private readonly LobbyPageStateController _pageStateController = new();

        private void Awake()
        {
            HookNavigation();
            ShowLobbyPage();
            ShowRoomList();
        }

        public void Initialize(
            IEventSubscriber eventBus,
            IEventPublisher eventPublisher,
            LobbyUseCases useCases
        )
        {
            _eventBus = eventBus;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            var roomInputHandler = new LobbyRoomInputHandler(useCases, eventPublisher);
            _roomListView?.Initialize(roomInputHandler);
            _roomDetailView?.Initialize(roomInputHandler, eventBus);
            _garageSummaryView?.Initialize(eventBus);
            HookNavigation();

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            if (_roomListView != null)
            {
                _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
                _eventBus.Subscribe<RoomListReceivedEvent>(this, e => RenderRoomList(e));
            }

            if (_roomDetailView != null)
                _eventBus.Subscribe<RoomUpdatedEvent>(this, e => RenderRoom(e));

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
            ShowRoomList();
        }

        public void RenderRoom(RoomUpdatedEvent e)
        {
            if (_roomDetailView == null)
                return;

            _roomDetailView.SetLocalMemberId(e.LocalMemberId);
            _roomDetailView.Render(e.Room);
            ShowRoomDetail();
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

        private void HookNavigation()
        {
            if (_navigationHooked)
                return;

            _navigationHooked = true;
            _navigationBar.Bind(ShowLobbyPage, ShowGaragePage);
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

        /// <summary>
        /// MCP 캡처용 public 메서드
        /// </summary>
        public void OpenLobbyPage()
        {
            ShowLobbyPage();
        }

        private void UpdateTabState(bool lobbyActive)
        {
            _navigationBar.SetState(lobbyActive);
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
        private static void ApplyRoomPanelState(GameObject roomListPanel, GameObject roomDetailPanel, bool showingRoomDetail)
        {
            if (roomListPanel != null)
                roomListPanel.SetActive(!showingRoomDetail);

            if (roomDetailPanel != null)
            {
                SetOptionalPanelRootActive(roomDetailPanel, showingRoomDetail);
                roomDetailPanel.SetActive(showingRoomDetail);
            }
        }

        private static void SetOptionalPanelRootActive(GameObject panel, bool isActive)
        {
            var parent = panel != null && panel.transform.parent != null
                ? panel.transform.parent.gameObject
                : null;
            if (parent == null)
                return;

            if (parent.name.Contains("RoomDetailPanelRoot"))
                parent.SetActive(isActive);
        }
    }

}
