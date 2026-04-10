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
            HitDamageType damageType,
            StatusPayload statusPayload = default,
            float allyDamageScale = 1f
        ) : base(id)
        {
            OwnerId = ownerId;
            Spec = spec;
            BaseDamage = baseDamage;
            DamageType = damageType;
            StatusPayload = statusPayload;
            AllyDamageScale = allyDamageScale;
            IsAlive = true;
            HitCount = 0;
        }

        public DomainEntityId OwnerId { get; }
        public ProjectileSpec Spec { get; }
        public float BaseDamage { get; }
        public HitDamageType DamageType { get; }
        public StatusPayload StatusPayload { get; }
        public float AllyDamageScale { get; }
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
