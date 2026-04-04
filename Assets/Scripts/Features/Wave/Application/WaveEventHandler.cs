using System;
using Features.Combat.Application.Events;
using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Wave.Application
{
    public sealed class WaveEventHandler
    {
        private readonly WaveLoopUseCase _waveLoop;
        private readonly IAlivePlayerQuery _aliveQuery;
        private readonly DomainEntityId _objectiveCoreId;

        public WaveEventHandler(
            IEventSubscriber subscriber,
            WaveLoopUseCase waveLoop,
            IAlivePlayerQuery aliveQuery,
            DomainEntityId objectiveCoreId)
        {
            _waveLoop = waveLoop;
            _aliveQuery = aliveQuery;
            _objectiveCoreId = objectiveCoreId;

            subscriber.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));
            subscriber.Subscribe(this, new Action<PlayerDiedEvent>(OnPlayerDied));
            subscriber.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            _waveLoop.HandleEnemyDied();
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (!_aliveQuery.AnyPlayerAlive())
                _waveLoop.HandleAllPlayersDead();
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_objectiveCoreId.Equals(e.TargetId))
                return;
            if (!e.IsDead)
                return;

            _waveLoop.EnterDefeatIfActive();
        }
    }
}
