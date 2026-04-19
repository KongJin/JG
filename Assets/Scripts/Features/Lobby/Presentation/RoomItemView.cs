using Shared.Attributes;
using System;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Shared.Kernel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Lobby.Presentation
{
    public sealed class RoomItemView : MonoBehaviour
    {
        [Required, SerializeField]
        private TMP_Text _roomNameText;

        [Required, SerializeField]
        private TMP_Text _memberCountText;

        [Required, SerializeField]
        private TMP_Text _difficultyText;

        [Required, SerializeField]
        private TMP_Text _roomMetaText;

        [Required, SerializeField]
        private Button _joinButton;

        private DomainEntityId _roomId;
        private Action<DomainEntityId> _onJoinClicked;

        public void Bind(RoomSnapshot room, Action<DomainEntityId> onJoinClicked)
        {
            Bind(
                room.Id,
                room.Name,
                room.Members.Count,
                room.Capacity,
                room.DifficultyPresetId,
                onJoinClicked);
        }

        public void Bind(RoomListItem room, Action<DomainEntityId> onJoinClicked)
        {
            Bind(
                room.RoomId,
                room.RoomName,
                room.PlayerCount,
                room.MaxPlayers,
                room.DifficultyPresetId,
                onJoinClicked);
        }

        private void Bind(
            DomainEntityId roomId,
            string name,
            int playerCount,
            int capacity,
            int difficultyPresetId,
            Action<DomainEntityId> onJoinClicked
        )
        {
            _roomId = roomId;
            _onJoinClicked = onJoinClicked;

            var difficultyLabel = DifficultyPresetFormatter.ToShortLabel(difficultyPresetId);

            if (_roomNameText != null)
                _roomNameText.text = name;

            if (_memberCountText != null)
                _memberCountText.text = $"{playerCount}/{capacity}";

            if (_difficultyText != null)
                _difficultyText.text = difficultyLabel;

            if (_roomMetaText != null)
                _roomMetaText.text = $"{difficultyLabel} · {playerCount}/{capacity} pilots";

            if (_joinButton != null)
            {
                _joinButton.onClick.RemoveAllListeners();
                _joinButton.onClick.AddListener(HandleJoinClicked);
                _joinButton.interactable = playerCount < capacity;
            }
        }

        private void HandleJoinClicked()
        {
            _onJoinClicked?.Invoke(_roomId);
        }
    }
}
