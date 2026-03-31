using Shared.Attributes;
using System.Collections.Generic;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime.Pooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Lobby.Presentation
{
    public sealed class RoomListView : MonoBehaviour
    {
        [Header("Create Room")]
        [Required, SerializeField]
        private TMP_InputField _roomNameInput;

        [Required, SerializeField]
        private TMP_InputField _capacityInput;

        [Required, SerializeField]
        private TMP_InputField _displayNameInput;

        [Required, SerializeField]
        private Button _createRoomButton;

        [Header("Room List")]
        [Required, SerializeField]
        private Transform _roomListContent;

        [Required, SerializeField]
        private RoomItemView _roomItemPrefab;

        private LobbyUseCases _useCases;
        private IEventPublisher _eventPublisher;
        private GameObjectPool _roomItemPool;
        private readonly List<GameObject> _activeItems = new();

        public void Initialize(LobbyUseCases useCases, IEventPublisher eventPublisher)
        {
            _useCases = useCases;
            _eventPublisher = eventPublisher;
            _roomItemPool = new GameObjectPool(_roomItemPrefab.gameObject, _roomListContent);

            if (_createRoomButton != null)
                _createRoomButton.onClick.AddListener(HandleCreateRoom);
        }

        public Result CreateRoom(string roomName, int capacity, string ownerDisplayName) =>
            _useCases.CreateRoom(roomName, capacity, ownerDisplayName);

        public Result JoinRoom(DomainEntityId roomId, string memberDisplayName) =>
            _useCases.JoinRoom(roomId, memberDisplayName);

        public void Render(IReadOnlyList<RoomSnapshot> rooms)
        {
            ClearList();

            foreach (var room in rooms)
            {
                var go = _roomItemPool.Rent(Vector3.zero, Quaternion.identity);
                go.GetComponent<RoomItemView>().Bind(room, OnJoinRoomClicked);
                _activeItems.Add(go);
            }
        }

        public void Render(IReadOnlyList<RoomListItem> rooms)
        {
            ClearList();

            foreach (var room in rooms)
            {
                var go = _roomItemPool.Rent(Vector3.zero, Quaternion.identity);
                go.GetComponent<RoomItemView>().Bind(room, OnJoinRoomClicked);
                _activeItems.Add(go);
            }
        }

        private void HandleCreateRoom()
        {
            var roomName = _roomNameInput != null ? _roomNameInput.text : "New Room";
            var capacityText = _capacityInput != null ? _capacityInput.text : "4";
            var displayName = _displayNameInput != null ? _displayNameInput.text : "Player";

            if (!int.TryParse(capacityText, out var capacity))
                capacity = 4;

            var result = _useCases.CreateRoom(roomName, capacity, displayName);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void OnJoinRoomClicked(DomainEntityId roomId)
        {
            var displayName = _displayNameInput != null ? _displayNameInput.text : "Player";
            var result = _useCases.JoinRoom(roomId, displayName);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void ClearList()
        {
            foreach (var go in _activeItems)
                _roomItemPool.Return(go);
            _activeItems.Clear();
        }
    }
}
