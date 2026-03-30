using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct PlayerManaChangedEvent
    {
        public DomainEntityId PlayerId { get; }
        public float CurrentMana { get; }
        public float MaxMana { get; }

        public PlayerManaChangedEvent(DomainEntityId playerId, float currentMana, float maxMana)
        {
            PlayerId = playerId;
            CurrentMana = currentMana;
            MaxMana = maxMana;
        }
    }
}
