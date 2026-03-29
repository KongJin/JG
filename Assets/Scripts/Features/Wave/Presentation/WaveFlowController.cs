using Features.Wave.Application;
using Features.Wave.Domain;
using Features.Wave.Infrastructure;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveFlowController : MonoBehaviour
    {
        private WaveLoopUseCase _waveLoop;
        private WaveTableData _waveTable;
        private EnemySpawnAdapter _spawnAdapter;

        public void Initialize(
            WaveLoopUseCase waveLoop,
            WaveTableData waveTable,
            EnemySpawnAdapter spawnAdapter)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnAdapter = spawnAdapter;

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
            if (waveIndex >= _waveTable.Waves.Length) return;

            var entry = _waveTable.Waves[waveIndex];
            _waveLoop.BeginCountdown(entry.CountdownDuration);
        }

        private void BeginCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.Waves.Length) return;

            var entry = _waveTable.Waves[waveIndex];
            _waveLoop.BeginWave(entry.Count);
            _spawnAdapter.SpawnWaveEnemies(entry);
        }
    }
}
