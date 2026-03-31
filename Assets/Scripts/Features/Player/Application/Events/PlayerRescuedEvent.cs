using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct PlayerRescuedEvent
    {
        public DomainEntityId RescuedId { get; }
        public DomainEntityId RescuerId { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }
        public float CurrentMana { get; }
        public float MaxMana { get; }

        public PlayerRescuedEvent(
            DomainEntityId rescuedId,
            DomainEntityId rescuerId,
            float currentHp,
            float maxHp,
            float currentMana,
            float maxMana
        )
        {
            RescuedId = rescuedId;
            RescuerId = rescuerId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentMana = currentMana;
            MaxMana = maxMana;
        }
    }
}
