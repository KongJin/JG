using Shared.Attributes;
using System.Collections.Generic;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Runtime.Pooling;
using Shared.Runtime.Sound;
using Shared.Sound;
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

        private LobbyUseCases _useCases;
        private IEventPublisher _eventPublisher;
        private GameObjectPool _roomItemPool;
        private readonly List<GameObject> _activeItems = new();

        public void Initialize(LobbyUseCases useCases, IEventPublisher eventPublisher)
        {
            _useCases = useCases;
            _eventPublisher = eventPublisher;
            _roomItemPool = new GameObjectPool(_roomItemPrefab.gameObject, _roomListContent);
            _createRoomButton?.onClick.AddListener(HandleCreateRoom);

            UpdateListChrome(0);
        }

        public Result CreateRoom(string roomName, int capacity, string ownerDisplayName, int difficultyPresetId = 0) =>
            _useCases.CreateRoom(roomName, capacity, ownerDisplayName, difficultyPresetId);

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

            UpdateListChrome(rooms != null ? rooms.Count : 0);
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

            UpdateListChrome(rooms != null ? rooms.Count : 0);
        }

        private void HandleCreateRoom()
        {
            PublishSound("ui_confirm");

            var roomName = _roomNameInput != null ? _roomNameInput.text : "Room";
            var capacityText = _capacityInput != null ? _capacityInput.text : "4";
            var displayName = _displayNameInput != null ? _displayNameInput.text : string.Empty;

            if (!int.TryParse(capacityText, out var capacity))
                capacity = 4;

            var difficulty = _difficultyDropdown != null ? _difficultyDropdown.value : 0;

            var result = _useCases.CreateRoom(roomName, capacity, displayName, difficulty);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void OnJoinRoomClicked(DomainEntityId roomId)
        {
            PublishSound("ui_select");

            var displayName = _displayNameInput != null ? _displayNameInput.text : string.Empty;
            var result = _useCases.JoinRoom(roomId, displayName);
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
        }

        private void PublishSound(string soundKey)
        {
            _eventPublisher?.Publish(new SoundRequestEvent(new SoundRequest(
                soundKey,
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                SoundPlayer.LobbyOwnerId,
                0.05f)));
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
                var countBadge = _roomListCountText.transform.parent != null
                    ? _roomListCountText.transform.parent.gameObject
                    : null;
                countBadge?.SetActive(roomCount > 0);

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
