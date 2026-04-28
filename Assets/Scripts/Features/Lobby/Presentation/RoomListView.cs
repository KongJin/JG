using Shared.Attributes;
using System.Collections.Generic;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
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

        [Header("Create Room — optional")]
        [SerializeField]
        private TMP_Dropdown _difficultyDropdown;

        [Header("Room List")]
        [Required, SerializeField]
        private Transform _roomListContent;

        [Required, SerializeField]
        private RoomItemView _roomItemPrefab;

        [Header("Room List")]
        [Required, SerializeField]
        private TMP_Text _roomListCountText;

        [Required, SerializeField]
        private TMP_Text _roomListEmptyStateText;

        private LobbyRoomInputHandler _inputHandler;
        private GameObjectPool _roomItemPool;
        private readonly List<GameObject> _activeItems = new();

        public void Initialize(LobbyRoomInputHandler inputHandler)
        {
            _inputHandler = inputHandler;
            _roomItemPool = new GameObjectPool(_roomItemPrefab.gameObject, _roomListContent);
            _createRoomButton?.onClick.AddListener(HandleCreateRoom);

            UpdateListChrome(0);
        }

        public void Render(IReadOnlyList<RoomSnapshot> rooms)
        {
            ClearList();

            foreach (var room in rooms)
            {
                var item = _roomItemPool.RentComponent<RoomItemView>(Vector3.zero, Quaternion.identity);
                item.Bind(room, OnJoinRoomClicked);
                _activeItems.Add(item.gameObject);
            }

            UpdateListChrome(rooms != null ? rooms.Count : 0);
        }

        public void Render(IReadOnlyList<RoomListItem> rooms)
        {
            ClearList();

            foreach (var room in rooms)
            {
                var item = _roomItemPool.RentComponent<RoomItemView>(Vector3.zero, Quaternion.identity);
                item.Bind(room, OnJoinRoomClicked);
                _activeItems.Add(item.gameObject);
            }

            UpdateListChrome(rooms != null ? rooms.Count : 0);
        }

        private void HandleCreateRoom()
        {
            var roomName = _roomNameInput != null ? _roomNameInput.text : "Room";
            var capacityText = _capacityInput != null ? _capacityInput.text : "4";
            var displayName = _displayNameInput != null ? _displayNameInput.text : string.Empty;

            if (!int.TryParse(capacityText, out var capacity))
                capacity = 4;

            var difficulty = _difficultyDropdown != null ? _difficultyDropdown.value : 0;

            _inputHandler.CreateRoom(roomName, capacity, displayName, difficulty);
        }

        private void OnJoinRoomClicked(DomainEntityId roomId)
        {
            var displayName = _displayNameInput != null ? _displayNameInput.text : string.Empty;
            _inputHandler.JoinRoom(roomId, displayName);
        }

        private void ClearList()
        {
            foreach (var go in _activeItems)
                _roomItemPool.Return(go);
            _activeItems.Clear();
        }

        private void UpdateListChrome(int roomCount)
        {
            if (_roomListCountText != null)
            {
                _roomListCountText.text = roomCount switch
                {
                    <= 0 => "0 open rooms",
                    1 => "1 open room",
                    _ => $"{roomCount} open rooms",
                };
            }

            if (_roomListEmptyStateText != null)
            {
                _roomListEmptyStateText.gameObject.SetActive(roomCount <= 0);
            }
        }
    }
}
