using Features.Combat.Application.Events;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Combat.Application
{
    public sealed class CombatReplicationEventHandler
    {
        private readonly ApplyDamageUseCase _applyDamage;

        public CombatReplicationEventHandler(ApplyDamageUseCase applyDamage, IEventSubscriber eventBus)
        {
            _applyDamage = applyDamage;
            eventBus.Subscribe(this, new System.Action<DamageReplicatedEvent>(OnDamageReplicated));
        }

        private void OnDamageReplicated(DamageReplicatedEvent e)
        {
            _applyDamage.ExecuteReplicated(e.TargetId, e.Damage, e.DamageType, e.AttackerId);
        }
    }
}
