using System;
using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class WaveEventHandler
    {
        private readonly WaveLoopUseCase _waveLoop;
        private readonly IAlivePlayerQuery _aliveQuery;

        public WaveEventHandler(
            IEventSubscriber subscriber,
            WaveLoopUseCase waveLoop,
            IAlivePlayerQuery aliveQuery)
        {
            _waveLoop = waveLoop;
            _aliveQuery = aliveQuery;

            subscriber.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));
            subscriber.Subscribe(this, new Action<PlayerDiedEvent>(OnPlayerDied));
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
    }
}
