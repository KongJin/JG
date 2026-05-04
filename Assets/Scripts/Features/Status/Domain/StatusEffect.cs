using System;
using Shared.Kernel;

namespace Features.Status.Domain
{
    public sealed class StatusEffect
    {
        public StatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }
        public float Elapsed { get; }
        public DomainEntityId SourceId { get; }
        public float TickInterval { get; }
        public float TimeSinceLastTick { get; }

        public StatusEffect(
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval = 0f)
            : this(type, magnitude, duration, sourceId, tickInterval, elapsed: 0f, timeSinceLastTick: 0f)
        {
        }

        private StatusEffect(
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval,
            float elapsed,
            float timeSinceLastTick)
        {
            if (!Enum.IsDefined(typeof(StatusType), type))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown status type.");
            if (magnitude < 0f)
                throw new ArgumentOutOfRangeException(nameof(magnitude), magnitude, "Status magnitude cannot be negative.");
            if (duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Status duration cannot be negative.");
            if (tickInterval < 0f)
                throw new ArgumentOutOfRangeException(nameof(tickInterval), tickInterval, "Status tick interval cannot be negative.");
            if (elapsed < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Status elapsed time cannot be negative.");
            if (timeSinceLastTick < 0f)
                throw new ArgumentOutOfRangeException(nameof(timeSinceLastTick), timeSinceLastTick, "Status tick timer cannot be negative.");

            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            SourceId = sourceId;
            TickInterval = tickInterval;
            Elapsed = elapsed;
            TimeSinceLastTick = timeSinceLastTick;
        }

        public bool IsExpired => Elapsed >= Duration;

        public StatusEffect Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Status delta time cannot be negative.");

            return new StatusEffect(
                Type,
                Magnitude,
                Duration,
                SourceId,
                TickInterval,
                Elapsed + deltaTime,
                TimeSinceLastTick + deltaTime);
        }

        public StatusEffect ConsumeTickIfReady(out bool consumed)
        {
            consumed = false;
            if (TickInterval <= 0f || TimeSinceLastTick < TickInterval)
                return this;

            consumed = true;
            return new StatusEffect(
                Type,
                Magnitude,
                Duration,
                SourceId,
                TickInterval,
                Elapsed,
                TimeSinceLastTick - TickInterval);
        }

        public StatusEffect Refresh(float newDuration)
        {
            if (newDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(newDuration), newDuration, "Status duration cannot be negative.");

            return new StatusEffect(
                Type,
                Magnitude,
                newDuration,
                SourceId,
                TickInterval,
                elapsed: 0f,
                TimeSinceLastTick);
        }
    }
}
