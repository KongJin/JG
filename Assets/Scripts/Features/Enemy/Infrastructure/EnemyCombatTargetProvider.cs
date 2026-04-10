using Features.Combat.Application.Ports;
using Features.Enemy.Application.Events;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Enemy.Infrastructure
{
    public sealed class EnemyCombatTargetProvider : ICombatTargetProvider
    {
        private readonly Domain.Enemy _enemy;
        private readonly DomainEntityId _enemyId;
        private readonly IEventPublisher _eventPublisher;
        private bool _deathPublished;

        public EnemyCombatTargetProvider(
            Domain.Enemy enemy,
            DomainEntityId enemyId,
            IEventPublisher eventPublisher)
        {
            _enemy = enemy;
            _enemyId = enemyId;
            _eventPublisher = eventPublisher;
        }

        public float GetDefense() => _enemy.Spec.Defense;

        public float GetCurrentHealth() => _enemy.CurrentHp;

        public CombatTargetDamageResult ApplyDamage(float damage)
        {
            if (_enemy.IsDead)
                return new CombatTargetDamageResult(_enemy.CurrentHp, true, false);

            var remaining = _enemy.TakeDamage(damage);
            _eventPublisher.Publish(new EnemyHealthChangedEvent(_enemyId, remaining, _enemy.MaxHp, damage, _enemy.IsDead));

            if (_enemy.IsDead && !_deathPublished)
            {
                _deathPublished = true;
                _enemy.Die();
                _eventPublisher.Publish(new EnemyDiedEvent(_enemyId, default));
            }

            return new CombatTargetDamageResult(remaining, _enemy.IsDead, false);
        }
    }
}
