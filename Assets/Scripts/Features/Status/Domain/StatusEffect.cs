using Shared.Kernel;

namespace Features.Status.Domain
{
    public sealed class StatusEffect
    {
        public StatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public DomainEntityId SourceId { get; }
        public float TickInterval { get; }
        public float TimeSinceLastTick { get; private set; }

        public StatusEffect(
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval = 0f)
        {
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            SourceId = sourceId;
            TickInterval = tickInterval;
        }

        public bool IsExpired => Elapsed >= Duration;

        public void Tick(float deltaTime)
        {
            Elapsed += deltaTime;
            TimeSinceLastTick += deltaTime;
        }

        public bool ConsumeTickIfReady()
        {
            if (TickInterval <= 0f) return false;
            if (TimeSinceLastTick < TickInterval) return false;
            TimeSinceLastTick -= TickInterval;
            return true;
        }

        public void Refresh(float newDuration)
        {
            Duration = newDuration;
            Elapsed = 0f;
        }
    }
}
