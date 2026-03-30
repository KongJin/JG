using Shared.Kernel;

namespace Features.Combat.Application.Events
{
    public readonly struct FriendlyFireAppliedEvent
    {
        public FriendlyFireAppliedEvent(DomainEntityId attackerId, DomainEntityId targetId, float damage)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
        }

        public DomainEntityId AttackerId { get; }
        public DomainEntityId TargetId { get; }
        public float Damage { get; }
    }
}
