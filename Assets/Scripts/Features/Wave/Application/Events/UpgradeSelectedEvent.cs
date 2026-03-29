using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Wave.Application.Events
{
    public readonly struct UpgradeSelectedEvent
    {
        public UpgradeSelectedEvent(DomainEntityId playerId, StatusType chosenType)
        {
            PlayerId = playerId;
            ChosenType = chosenType;
        }

        public DomainEntityId PlayerId { get; }
        public StatusType ChosenType { get; }
    }
}
