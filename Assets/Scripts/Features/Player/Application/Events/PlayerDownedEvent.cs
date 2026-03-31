using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct PlayerDownedEvent
    {
        public DomainEntityId PlayerId { get; }
        public DomainEntityId AttackerId { get; }

        public PlayerDownedEvent(DomainEntityId playerId, DomainEntityId attackerId)
        {
            PlayerId = playerId;
            AttackerId = attackerId;
        }
    }
}
