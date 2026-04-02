using System;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveLoopUseCase _waveLoop;
        private IWaveTablePort _waveTable;
        private IWaveSpawnPort _spawnPort;
        private IEventSubscriber _subscriber;

        public void Initialize(
            WaveLoopUseCase waveLoop,
            IWaveTablePort waveTable,
            IWaveSpawnPort spawnPort,
            IEventSubscriber subscriber)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnPort = spawnPort;
            _subscriber = subscriber;

            _subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
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
                if (!_waveLoop.EnterUpgradeSelection())
                    StartCountdownForCurrentWave();
            }
        }

        private void OnGameStart(GameStartEvent e)
        {
            StartFirstWave();
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            if (_waveLoop.CurrentState != WaveState.UpgradeSelection) return;

            _waveLoop.ExitUpgradeSelection();
            StartCountdownForCurrentWave();
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
