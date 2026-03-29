using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Events
{
    public readonly struct StatusExpiredEvent
    {
        public DomainEntityId TargetId { get; }
        public StatusType Type { get; }

        public StatusExpiredEvent(DomainEntityId targetId, StatusType type)
        {
            TargetId = targetId;
            Type = type;
        }
    }
}
