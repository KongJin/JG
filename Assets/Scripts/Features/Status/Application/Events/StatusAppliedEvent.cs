using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Events
{
    public readonly struct StatusAppliedEvent
    {
        public DomainEntityId TargetId { get; }
        public StatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }

        public StatusAppliedEvent(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration)
        {
            TargetId = targetId;
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
        }
    }
}
