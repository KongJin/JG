using System;
using Features.Combat.Application.Ports;
using Features.Wave.Application.Events;
using Shared.EventBus;

namespace Features.Combat.Application
{
    public sealed class FriendlyFireScalingAdapter : IFriendlyFireScalingPort
    {
        private int _currentWaveIndex;

        public FriendlyFireScalingAdapter(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _currentWaveIndex = e.WaveIndex;
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            _currentWaveIndex = e.WaveIndex;
        }

        // waveIndex is 0-based. Player-facing waves 1-3 (index 0-2): 50%, wave 4+ (index 3+): 37.5%
        public float GetAllyDamageMultiplier()
        {
            return _currentWaveIndex < 3 ? 0.5f : 0.375f;
        }
    }
}
