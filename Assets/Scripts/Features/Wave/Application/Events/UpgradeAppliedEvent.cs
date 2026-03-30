using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Wave.Application.Events
{
    public readonly struct UpgradeAppliedEvent
    {
        public UpgradeAppliedEvent(DomainEntityId playerId, StatusType chosenType, int currentStacks)
        {
            PlayerId = playerId;
            ChosenType = chosenType;
            CurrentStacks = currentStacks;
        }

        public DomainEntityId PlayerId { get; }
        public StatusType ChosenType { get; }
        public int CurrentStacks { get; }
    }
}
