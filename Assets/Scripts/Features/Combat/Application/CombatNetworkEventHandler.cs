using Features.Projectile.Application.Events;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Combat.Application
{
    public sealed class CombatNetworkEventHandler
    {
        private readonly ApplyDamageUseCase _applyDamage;
        private readonly DomainEntityId _localAuthorityId;
        private readonly bool _hasAuthorityFilter;

        public CombatNetworkEventHandler(
            ApplyDamageUseCase applyDamage,
            IEventSubscriber eventBus,
            DomainEntityId localAuthorityId = default
        )
        {
            _applyDamage = applyDamage;
            _localAuthorityId = localAuthorityId;
            _hasAuthorityFilter = !string.IsNullOrWhiteSpace(localAuthorityId.Value);
            eventBus.Subscribe(this, new System.Action<ProjectileHitEvent>(OnProjectileHit));
        }

        private void OnProjectileHit(ProjectileHitEvent e)
        {
            if (_hasAuthorityFilter && !e.OwnerId.Equals(_localAuthorityId))
                return;

            _applyDamage.Execute(
                e.TargetId,
                e.BaseDamage,
                ConvertDamageType(e.DamageType),
                e.OwnerId,
                e.AllyDamageScale
            );
        }

        private static Domain.DamageType ConvertDamageType(HitDamageType damageType)
        {
            return damageType switch
            {
                HitDamageType.Physical => Domain.DamageType.Physical,
                HitDamageType.Magical => Domain.DamageType.Magical,
                HitDamageType.True => Domain.DamageType.True,
                _ => Domain.DamageType.Physical,
            };
        }
    }
}
