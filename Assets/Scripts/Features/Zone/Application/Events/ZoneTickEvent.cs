using Features.Status.Domain;
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
            StatusPayload statusPayload = default)
        {
            ZoneId = zoneId;
            CasterId = casterId;
            TargetId = targetId;
            BaseDamage = baseDamage;
            StatusPayload = statusPayload;
        }

        public DomainEntityId ZoneId { get; }
        public DomainEntityId CasterId { get; }
        public DomainEntityId TargetId { get; }
        public float BaseDamage { get; }
        public StatusPayload StatusPayload { get; }
    }
}
