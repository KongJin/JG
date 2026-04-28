using System;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Shared.Kernel;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class RoomItemView : MonoBehaviour
    {
        public void Bind(RoomSnapshot room, Action<DomainEntityId> onJoinClicked) { }
        public void Bind(RoomListItem room, Action<DomainEntityId> onJoinClicked) { }
    }
}
