using System.Collections.Generic;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class RoomListView : MonoBehaviour
    {
        public void Initialize(LobbyRoomInputHandler inputHandler) { }
        public void Render(IReadOnlyList<RoomSnapshot> rooms) { }
        public void Render(IReadOnlyList<RoomListItem> rooms) { }
    }
}
