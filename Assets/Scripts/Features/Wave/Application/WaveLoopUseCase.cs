using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class WaveLoopUseCase
    {
        private readonly IEventPublisher _eventBus;
        private readonly WaveProgress _progress;

        public WaveLoopUseCase(IEventPublisher eventBus, int totalWaves)
        {
            _eventBus = eventBus;
            _progress = new WaveProgress(totalWaves);
        }

        public WaveState CurrentState => _progress.State;
        public int CurrentWaveIndex => _progress.CurrentWaveIndex;
        public int TotalWaves => _progress.TotalWaves;
        public float CountdownRemaining => _progress.CountdownRemaining;

        public void BeginCountdown(float duration)
        {
            _progress.StartCountdown(duration);
            _eventBus.Publish(new WaveCountdownStartedEvent(
                _progress.CurrentWaveIndex,
                _progress.TotalWaves,
                duration
            ));
        }

        public bool TickCountdown(float deltaTime)
        {
            return _progress.TickCountdown(deltaTime);
        }

        public void BeginWave(int enemyCount)
        {
            _progress.StartWave(enemyCount);
            _eventBus.Publish(new WaveStartedEvent(
                _progress.CurrentWaveIndex,
                _progress.TotalWaves,
                enemyCount
            ));
        }

        public void HandleEnemyDied()
        {
            var waveCleared = _progress.OnEnemyDied();
            if (!waveCleared) return;

            if (_progress.State == WaveState.Victory)
            {
                _eventBus.Publish(new WaveVictoryEvent());
            }
            else
            {
                _eventBus.Publish(new WaveClearedEvent(_progress.CurrentWaveIndex));
                _progress.AdvanceWave();
            }
        }

        public void ForceState(int waveIndex, WaveState state, float countdownRemaining = 0f)
        {
            _progress.ForceState(waveIndex, state, countdownRemaining);
            _eventBus.Publish(new WaveHydratedEvent(
                _progress.CurrentWaveIndex,
                _progress.TotalWaves,
                _progress.State,
                _progress.CountdownRemaining
            ));
        }

        public void HandleAllPlayersDead()
        {
            EnterDefeatIfActive();
        }

        public void EnterDefeatIfActive()
        {
            if (_progress.State == WaveState.Victory || _progress.State == WaveState.Defeat)
                return;

            _progress.SetDefeat();
            _eventBus.Publish(new WaveDefeatEvent());
        }
    }

    public sealed class SpawnObjectiveCoreUseCase
    {
        public ObjectiveCore Execute(DomainEntityId id, float maxHp, float defense)
        {
            return new ObjectiveCore(id, maxHp, defense);
        }
    }
}
