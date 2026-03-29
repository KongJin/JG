using Shared.Kernel;

namespace Features.Status.Application.Events
{
    public readonly struct StatusTickDamageEvent
    {
        public DomainEntityId TargetId { get; }
        public float Damage { get; }
        public DomainEntityId SourceId { get; }

        public StatusTickDamageEvent(DomainEntityId targetId, float damage, DomainEntityId sourceId)
        {
            TargetId = targetId;
            Damage = damage;
            SourceId = sourceId;
        }
    }
}
