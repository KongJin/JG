using System;
using Features.Combat.Application.Events;
using Features.Enemy.Application.Events;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Enemy.Application
{
    public sealed class EnemyDamageEventHandler
    {
        private readonly Domain.Enemy _enemy;
        private readonly DomainEntityId _enemyId;
        private readonly IEventPublisher _eventBus;
        private bool _deathPublished;

        public EnemyDamageEventHandler(
            Domain.Enemy enemy,
            DomainEntityId enemyId,
            IEventPublisher eventBus,
            IEventSubscriber subscriber)
        {
            _enemy = enemy;
            _enemyId = enemyId;
            _eventBus = eventBus;
            subscriber.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_enemyId.Equals(e.TargetId))
                return;

            _eventBus.Publish(new EnemyHealthChangedEvent(
                _enemyId,
                _enemy.CurrentHp,
                _enemy.MaxHp,
                e.Damage,
                _enemy.IsDead
            ));

            if (e.IsDead && !_deathPublished)
            {
                _deathPublished = true;
                _enemy.Die();
                _eventBus.Publish(new EnemyDiedEvent(_enemyId, e.AttackerId));
            }
        }
    }
}
