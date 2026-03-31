using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct RescueChannelStartedEvent
    {
        public DomainEntityId RescuerId { get; }
        public DomainEntityId TargetId { get; }

        public RescueChannelStartedEvent(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            RescuerId = rescuerId;
            TargetId = targetId;
        }
    }
}
