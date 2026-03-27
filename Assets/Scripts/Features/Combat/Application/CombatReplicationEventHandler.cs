using Features.Combat.Application.Events;
using Shared.Kernel;

namespace Features.Combat.Application
{
    public sealed class CombatReplicationEventHandler
    {
        private readonly ApplyDamageUseCase _applyDamage;

        public CombatReplicationEventHandler(ApplyDamageUseCase applyDamage)
        {
            _applyDamage = applyDamage;
        }

        public Result HandleDamageReplicated(DamageReplicatedEvent e)
        {
            return _applyDamage.ExecuteReplicated(
                e.TargetId,
                e.Damage,
                e.DamageType,
                e.AttackerId
            );
        }
    }
}
