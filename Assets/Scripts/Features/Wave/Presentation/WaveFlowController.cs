using Features.Wave.Application;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveLoopUseCase _waveLoop;
        private IWaveTablePort _waveTable;
        private IWaveSpawnPort _spawnPort;

        public void Initialize(
            WaveLoopUseCase waveLoop,
            IWaveTablePort waveTable,
            IWaveSpawnPort spawnPort)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnPort = spawnPort;

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
    }
}
