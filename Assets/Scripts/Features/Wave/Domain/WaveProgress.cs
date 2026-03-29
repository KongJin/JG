namespace Features.Wave.Domain
{
    public sealed class WaveProgress
    {
        public WaveState State { get; private set; }
        public int CurrentWaveIndex { get; private set; }
        public int TotalWaves { get; }
        public int RemainingEnemies { get; private set; }
        public float CountdownRemaining { get; private set; }

        public bool IsLastWave => CurrentWaveIndex >= TotalWaves - 1;

        public WaveProgress(int totalWaves)
        {
            TotalWaves = totalWaves;
            State = WaveState.Idle;
        }

        public void StartCountdown(float duration)
        {
            State = WaveState.Countdown;
            CountdownRemaining = duration;
        }

        public bool TickCountdown(float deltaTime)
        {
            if (State != WaveState.Countdown) return false;

            CountdownRemaining -= deltaTime;
            if (CountdownRemaining <= 0f)
            {
                CountdownRemaining = 0f;
                return true;
            }

            return false;
        }

        public void StartWave(int enemyCount)
        {
            State = WaveState.Active;
            RemainingEnemies = enemyCount;
        }

        public bool OnEnemyDied()
        {
            if (State != WaveState.Active) return false;

            RemainingEnemies--;
            if (RemainingEnemies < 0) RemainingEnemies = 0;

            if (RemainingEnemies == 0)
            {
                State = IsLastWave ? WaveState.Victory : WaveState.Cleared;
                return true;
            }

            return false;
        }

        public void AdvanceWave()
        {
            CurrentWaveIndex++;
        }

        public void SetDefeat()
        {
            State = WaveState.Defeat;
        }
    }
}
