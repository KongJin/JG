using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct GameEndEvent
    {
        public GameEndEvent(
            DomainEntityId deadPlayerId,
            DomainEntityId killerId,
            bool isLocalPlayerDead,
            string message
        )
        {
            DeadPlayerId = deadPlayerId;
            KillerId = killerId;
            IsLocalPlayerDead = isLocalPlayerDead;
            Message = message;
        }

        public DomainEntityId DeadPlayerId { get; }
        public DomainEntityId KillerId { get; }
        public bool IsLocalPlayerDead { get; }
        public string Message { get; }
    }
}
