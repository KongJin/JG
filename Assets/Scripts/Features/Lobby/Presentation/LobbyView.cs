using Shared.Attributes;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
    {
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
            _roomDetailView.Initialize(useCases, eventPublisher);

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe<LobbyUpdatedEvent>(this, e => RenderLobby(e.Lobby));
            _eventBus.Subscribe<RoomUpdatedEvent>(this, e => RenderRoom(e));
            _eventBus.Subscribe<RoomListReceivedEvent>(this, e => RenderRoomList(e));
            _eventBus.Subscribe<GameStartedEvent>(this, e => RenderStartGame(e.Room));

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
            if (_roomListPanel != null)
                _roomListPanel.SetActive(true);
            if (_roomDetailPanel != null)
                _roomDetailPanel.SetActive(false);
        }

        private void ShowRoomDetail()
        {
            if (_roomListPanel != null)
                _roomListPanel.SetActive(false);
            if (_roomDetailPanel != null)
                _roomDetailPanel.SetActive(true);
        }
    }
}
