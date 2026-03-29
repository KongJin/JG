using Features.Combat.Domain;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Projectile.Domain
{
    public sealed class Projectile : Entity
    {
        public Projectile(
            DomainEntityId id,
            DomainEntityId ownerId,
            ProjectileSpec spec,
            float baseDamage,
            DamageType damageType,
            StatusPayload statusPayload = default
        ) : base(id)
        {
            OwnerId = ownerId;
            Spec = spec;
            BaseDamage = baseDamage;
            DamageType = damageType;
            StatusPayload = statusPayload;
            IsAlive = true;
            HitCount = 0;
        }

        public DomainEntityId OwnerId { get; }
        public ProjectileSpec Spec { get; }
        public float BaseDamage { get; }
        public DamageType DamageType { get; }
        public StatusPayload StatusPayload { get; }
        public bool IsAlive { get; private set; }
        public int HitCount { get; private set; }

        public void Destroy()
        {
            IsAlive = false;
        }

        public void RegisterHit()
        {
            HitCount++;
        }
    }
}
