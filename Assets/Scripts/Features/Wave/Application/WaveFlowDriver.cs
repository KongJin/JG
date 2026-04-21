using Features.Wave.Application.Ports;
using Features.Wave.Domain;

namespace Features.Wave.Application
{
    public sealed class WaveFlowDriver
    {
        private readonly WaveLoopUseCase _waveLoop;
        private readonly IWaveTablePort _waveTable;
        private readonly IWaveSpawnPort _spawnPort;

        public WaveFlowDriver(
            WaveLoopUseCase waveLoop,
            IWaveTablePort waveTable,
            IWaveSpawnPort spawnPort)
        {
            _waveLoop = waveLoop;
            _waveTable = waveTable;
            _spawnPort = spawnPort;
        }

        public void StartFirstWave()
        {
            StartCountdownForCurrentWave();
        }

        public void Tick(float deltaTime)
        {
            if (_waveLoop.CurrentState == WaveState.Countdown)
            {
                if (_waveLoop.TickCountdown(deltaTime))
                    BeginCurrentWave();
                return;
            }

            if (_waveLoop.CurrentState == WaveState.Cleared)
                StartCountdownForCurrentWave();
        }

        private void StartCountdownForCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount)
                return;

            _waveLoop.BeginCountdown(_waveTable.GetCountdownDuration(waveIndex));
        }

        private void BeginCurrentWave()
        {
            var waveIndex = _waveLoop.CurrentWaveIndex;
            if (waveIndex >= _waveTable.WaveCount)
                return;

            _waveLoop.BeginWave(_waveTable.GetEnemyCount(waveIndex));
            _spawnPort.SpawnWave(waveIndex);
        }
    }
}
