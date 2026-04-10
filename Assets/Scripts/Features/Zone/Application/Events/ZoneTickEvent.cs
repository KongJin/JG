using Features.Zone.Domain;
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
}
