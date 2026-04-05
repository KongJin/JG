using System;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveLoopUseCase _waveLoop;
        private IWaveTablePort _waveTable;
        private IWaveSpawnPort _spawnPort;
        private IEventSubscriber _subscriber;
        private IEventPublisher _publisher;
        private DomainEntityId _localPlayerId;

        public void Initialize(
            WaveLoopUseCase waveLoop,
            IWaveTablePort waveTable,
            IWaveSpawnPort spawnPort,
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            DomainEntityId localPlayerId)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnPort = spawnPort;
            _subscriber = subscriber;
            _publisher = publisher;
            _localPlayerId = localPlayerId;

            _subscriber.Subscribe(this, new Action<GameStartEvent>(OnGameStart));
        }

        public void StartFirstWave()
        {
            StartCountdownForCurrentWave();
        }

        private void Update()
        {
            if (_waveLoop == null) return;

            if (_waveLoop.CurrentState == WaveState.Countdown)
            {
                var finished = _waveLoop.TickCountdown(Time.deltaTime);
                if (finished)
                    BeginCurrentWave();
            }
            else if (_waveLoop.CurrentState == WaveState.Cleared)
            {
                StartCountdownForCurrentWave();
            }
        }

        private void OnGameStart(GameStartEvent e)
        {
            StartFirstWave();
        }

        private void StartCountdownForCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount) return;

            _waveLoop.BeginCountdown(_waveTable.GetCountdownDuration(waveIndex));
        }

        private void BeginCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount) return;

            _waveLoop.BeginWave(_waveTable.GetEnemyCount(waveIndex));
            _spawnPort.SpawnWave(waveIndex);
        }

        private void OnDestroy()
        {
            _subscriber?.UnsubscribeAll(this);
        }
    }
}
