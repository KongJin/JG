using System;
using Features.Unit.Application.Events;
using Shared.EventBus;
using Shared.Math;
using Shared.Sound;

namespace Features.Unit.Application
{
    public sealed class UnitSummonSoundEventHandler
    {
        private readonly IEventPublisher _publisher;

        public UnitSummonSoundEventHandler(IEventSubscriber subscriber, IEventPublisher publisher)
        {
            _publisher = publisher;
            subscriber.Subscribe(this, new Action<UnitSummonCompletedEvent>(OnSummonCompleted));
            subscriber.Subscribe(this, new Action<UnitSummonFailedEvent>(OnSummonFailed));
        }

        private void OnSummonCompleted(UnitSummonCompletedEvent e)
        {
            _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                "battle_summon",
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                e.PlayerId.Value)));
        }

        private void OnSummonFailed(UnitSummonFailedEvent e)
        {
            _publisher.Publish(new SoundRequestEvent(new SoundRequest(
                "ui_confirm",
                Float3.Zero,
                PlaybackPolicy.LocalOnly,
                e.PlayerId.Value,
                0.1f)));
        }
    }
}
