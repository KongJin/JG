using Features.Combat.Domain;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Projectile.Application.Events
{
    public readonly struct ProjectileHitEvent
    {
        public ProjectileHitEvent(
            DomainEntityId projectileId,
            DomainEntityId ownerId,
            DomainEntityId targetId,
            float baseDamage,
            DamageType damageType,
            StatusPayload statusPayload = default
        )
        {
            ProjectileId = projectileId;
            OwnerId = ownerId;
            TargetId = targetId;
            BaseDamage = baseDamage;
            DamageType = damageType;
            StatusPayload = statusPayload;
        }

        public DomainEntityId ProjectileId { get; }
        public DomainEntityId OwnerId { get; }
        public DomainEntityId TargetId { get; }
        public float BaseDamage { get; }
        public DamageType DamageType { get; }
        public StatusPayload StatusPayload { get; }
    }
}
