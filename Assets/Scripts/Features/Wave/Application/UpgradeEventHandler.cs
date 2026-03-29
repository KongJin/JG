using System;
using Features.Status.Application.Events;
using Features.Wave.Application.Events;
using Shared.EventBus;

namespace Features.Wave.Application
{
    /// <summary>
    /// Subscribes to UpgradeSelectedEvent and applies a permanent StatusEffect
    /// (Duration = float.MaxValue) via StatusApplyRequestedEvent.
    /// </summary>
    public sealed class UpgradeEventHandler
    {
        private const float UpgradeMagnitudePerLevel = 0.15f;

        private readonly IEventPublisher _publisher;

        public UpgradeEventHandler(IEventPublisher publisher, IEventSubscriber subscriber)
        {
            _publisher = publisher;
            subscriber.Subscribe(this, new Action<UpgradeSelectedEvent>(OnUpgradeSelected));
        }

        private void OnUpgradeSelected(UpgradeSelectedEvent e)
        {
            _publisher.Publish(new StatusApplyRequestedEvent(
                e.PlayerId,
                e.ChosenType,
                UpgradeMagnitudePerLevel,
                float.MaxValue,
                e.PlayerId));
        }
    }
}
