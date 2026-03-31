using Shared.Kernel;

namespace Features.Player.Application.Events
{
    public readonly struct RescueChannelCancelledEvent
    {
        public DomainEntityId TargetId { get; }

        public RescueChannelCancelledEvent(DomainEntityId targetId)
        {
            TargetId = targetId;
        }
    }
}
