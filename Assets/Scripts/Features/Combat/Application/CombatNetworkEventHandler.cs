using Features.Projectile.Application.Events;
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
            DomainEntityId localAuthorityId = default
        )
        {
            _applyDamage = applyDamage;
            _localAuthorityId = localAuthorityId;
            _hasAuthorityFilter = !string.IsNullOrWhiteSpace(localAuthorityId.Value);
        }

        public Result HandleProjectileHit(ProjectileHitEvent e)
        {
            if (_hasAuthorityFilter && !e.OwnerId.Equals(_localAuthorityId))
                return Result.Success();

            return _applyDamage.Execute(e.TargetId, e.BaseDamage, e.DamageType, e.OwnerId);
        }
    }
}
