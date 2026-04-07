using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct PlayerEnergyChangedEvent
    {
        public DomainEntityId PlayerId { get; }
        public float CurrentEnergy { get; }
        public float MaxEnergy { get; }

        public PlayerEnergyChangedEvent(DomainEntityId playerId, float currentEnergy, float maxEnergy)
        {
            PlayerId = playerId;
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
        }
    }
}
