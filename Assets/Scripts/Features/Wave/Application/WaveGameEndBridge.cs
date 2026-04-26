using Features.Player.Application.Events;
using Features.Wave.Application.Events;
using Shared.EventBus;
using System;

namespace Features.Wave.Application
{
    /// <summary>
    /// Wave 승패 이벤트를 GameEndEvent로 변환하여 발행한다.
    /// Wave 피처의 Application 계층이 GameEndEvent 발행 책임을 갖는다.
    /// </summary>
    public sealed class WaveGameEndBridge : IDisposable
    {
        private readonly IEventPublisher _publisher;
        private readonly Func<float> _getElapsedTimeSeconds;
        private readonly Func<int> _getReachedWave;

        public WaveGameEndBridge(
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            Func<float> getElapsedTimeSeconds,
            Func<int> getReachedWave
        )
        {
            _publisher = publisher;
            _getElapsedTimeSeconds = getElapsedTimeSeconds;
            _getReachedWave = getReachedWave;

            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            _publisher.Publish(new GameEndEvent(
                isVictory: true,
                message: "Victory!",
                reachedWave: GetReachedWave(),
                playTimeSeconds: GetElapsedTimeSeconds()
            ));
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            _publisher.Publish(new GameEndEvent(
                isVictory: false,
                message: "Defeat!",
                reachedWave: GetReachedWave(),
                playTimeSeconds: GetElapsedTimeSeconds()
            ));
        }

        private float GetElapsedTimeSeconds()
        {
            return _getElapsedTimeSeconds != null ? Math.Max(0f, _getElapsedTimeSeconds()) : 0f;
        }

        private int GetReachedWave()
        {
            return _getReachedWave != null ? Math.Max(0, _getReachedWave()) : 0;
        }

        public void Dispose()
        {
        }
    }
}
