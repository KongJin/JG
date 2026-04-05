using Shared.Kernel;

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

        public void ForceState(int waveIndex, WaveState state, float countdownRemaining = 0f)
        {
            CurrentWaveIndex = waveIndex;
            State = state;
            if (state == WaveState.Countdown)
                CountdownRemaining = countdownRemaining;
        }

        public void SetDefeat()
        {
            State = WaveState.Defeat;
        }
    }

    public static class ObjectiveCoreIds
    {
        public const string DefaultValue = "objective-core";

        public static DomainEntityId Default => new DomainEntityId(DefaultValue);
    }

    public sealed class ObjectiveCore
    {
        public ObjectiveCore(DomainEntityId id, float maxHp, float defense)
        {
            Id = id;
            MaxHp = maxHp;
            Defense = defense;
            CurrentHp = maxHp;
        }

        public DomainEntityId Id { get; }
        public float MaxHp { get; }
        public float Defense { get; }
        public float CurrentHp { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0f;

        public float TakeDamage(float damage)
        {
            if (IsDestroyed)
                return CurrentHp;

            if (damage < 0f)
                damage = 0f;

            CurrentHp -= damage;
            if (CurrentHp < 0f)
                CurrentHp = 0f;

            return CurrentHp;
        }
    }
}
