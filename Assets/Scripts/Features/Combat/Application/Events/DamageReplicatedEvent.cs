using Features.Combat.Domain;
using Shared.Kernel;

namespace Features.Combat.Application.Events
{
    public readonly struct DamageReplicatedEvent
    {
        public DamageReplicatedEvent(
            DomainEntityId targetId,
            float damage,
            DamageType damageType,
            DomainEntityId attackerId = default
        )
        {
            TargetId = targetId;
            Damage = damage;
            DamageType = damageType;
            AttackerId = attackerId;
        }

        public DomainEntityId TargetId { get; }
        public float Damage { get; }
        public DamageType DamageType { get; }
        public DomainEntityId AttackerId { get; }
    }
}
