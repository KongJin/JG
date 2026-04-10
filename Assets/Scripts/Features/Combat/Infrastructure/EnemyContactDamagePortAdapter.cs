using Features.Combat.Domain;
using Features.Enemy.Application.Ports;
using Shared.Kernel;

namespace Features.Combat.Infrastructure
{
    public sealed class EnemyContactDamagePortAdapter : IEnemyContactDamagePort
    {
        private readonly CombatBootstrap _combatBootstrap;

        public EnemyContactDamagePortAdapter(CombatBootstrap combatBootstrap)
        {
            _combatBootstrap = combatBootstrap;
        }

        public Result ApplyContactDamage(DomainEntityId targetId, float damage, DomainEntityId attackerId)
        {
            return _combatBootstrap.ApplyDamage(targetId, damage, DamageType.Physical, attackerId);
        }
    }
}
