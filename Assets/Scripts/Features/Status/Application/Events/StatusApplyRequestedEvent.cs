using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Events
{
    public readonly struct StatusApplyRequestedEvent
    {
        public DomainEntityId TargetId { get; }
        public StatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }
        public DomainEntityId SourceId { get; }
        public float TickInterval { get; }

        public StatusApplyRequestedEvent(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval = 0f)
        {
            TargetId = targetId;
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            SourceId = sourceId;
            TickInterval = tickInterval;
        }
    }
}
