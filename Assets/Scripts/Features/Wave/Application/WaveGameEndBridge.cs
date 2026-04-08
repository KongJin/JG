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
        private readonly Func<float> _getCurrentTimeSeconds;

        public WaveGameEndBridge(
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            Func<float> getCurrentTimeSeconds = null
        )
        {
            _publisher = publisher;
            _getCurrentTimeSeconds = getCurrentTimeSeconds ?? (() => UnityEngine.Time.realtimeSinceStartup);

            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            _publisher.Publish(new GameEndEvent(
                isVictory: true,
                message: "Victory!",
                reachedWave: 0, // WaveLoop에서 관리하는 경우 보강 가능
                playTimeSeconds: _getCurrentTimeSeconds()
            ));
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            _publisher.Publish(new GameEndEvent(
                isVictory: false,
                message: "Defeat!",
                reachedWave: 0,
                playTimeSeconds: _getCurrentTimeSeconds()
            ));
        }

        public void Dispose()
        {
        }
    }
}
