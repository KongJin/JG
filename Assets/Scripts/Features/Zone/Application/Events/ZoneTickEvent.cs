using Shared.Kernel;

namespace Features.Zone.Application.Events
{
    /// <summary>Published each tick when a zone hits a target inside its area.</summary>
    public readonly struct ZoneTickEvent
    {
        public ZoneTickEvent(
            DomainEntityId zoneId,
            DomainEntityId casterId,
            DomainEntityId targetId,
            float baseDamage,
            ZoneStatusPayload statusPayload = default,
            float allyDamageScale = 1f)
        {
            ZoneId = zoneId;
            CasterId = casterId;
            TargetId = targetId;
            BaseDamage = baseDamage;
            StatusPayload = statusPayload;
            AllyDamageScale = allyDamageScale;
        }

        public DomainEntityId ZoneId { get; }
        public DomainEntityId CasterId { get; }
        public DomainEntityId TargetId { get; }
        public float BaseDamage { get; }
        public ZoneStatusPayload StatusPayload { get; }
        public float AllyDamageScale { get; }
    }

    /// <summary>
    /// Zone 틱 이벤트용 상태 효과 데이터.
    /// Features.Status.Domain.StatusPayload 의존성을 끊기 위한 Zone-owned 값 타입.
    /// </summary>
    public readonly struct ZoneStatusPayload
    {
        public ZoneStatusPayload(
            ZoneStatusType type = ZoneStatusType.None,
            float magnitude = 0f,
            float duration = 0f,
            float tickInterval = 0f)
        {
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            TickInterval = tickInterval;
        }

        public ZoneStatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }
        public float TickInterval { get; }
        public bool HasEffect => Type != ZoneStatusType.None;

        public static ZoneStatusPayload None => default;

        public static ZoneStatusPayload Create(ZoneStatusType type, float magnitude, float duration, float tickInterval = 0f)
        {
            return new ZoneStatusPayload(type, magnitude, duration, tickInterval);
        }

        public enum ZoneStatusType
        {
            None = 0,
            Slow = 1,
            Haste = 2,
            DoT = 3,
        }
    }
}
