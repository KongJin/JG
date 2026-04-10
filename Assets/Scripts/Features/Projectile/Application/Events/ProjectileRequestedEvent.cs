using Features.Projectile.Domain;
using Features.Status.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Projectile.Application.Events
{
    public readonly struct ProjectileRequestedEvent
    {
        public ProjectileRequestedEvent(
            DomainEntityId ownerId,
            ProjectileSpec spec,
            float baseDamage,
            HitDamageType damageType,
            Float3 position,
            Float3 direction,
            StatusPayload statusPayload = default,
            float allyDamageScale = 1f
        )
        {
            OwnerId = ownerId;
            Spec = spec;
            BaseDamage = baseDamage;
            DamageType = damageType;
            Position = position;
            Direction = direction;
            StatusPayload = statusPayload;
            AllyDamageScale = allyDamageScale;
        }

        public DomainEntityId OwnerId { get; }
        public ProjectileSpec Spec { get; }
        public float BaseDamage { get; }
        public HitDamageType DamageType { get; }
        public Float3 Position { get; }
        public Float3 Direction { get; }
        public StatusPayload StatusPayload { get; }
        public float AllyDamageScale { get; }
    }
}
