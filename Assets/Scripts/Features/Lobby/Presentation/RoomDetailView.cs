using Features.Lobby.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class RoomDetailView : MonoBehaviour
    {
        public void Initialize(LobbyRoomInputHandler inputHandler, IEventSubscriber eventSubscriber) { }
        public void SetLocalMemberId(DomainEntityId memberId) { }
        public void Render(RoomSnapshot room) { }
    }
}
