using System;
using Features.Status.Application.Events;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class UpgradeEventHandler
    {
        private const float UpgradeMagnitudePerLevel = 0.15f;

        private readonly IEventPublisher _publisher;
        private readonly IUpgradeQueryPort _upgradeQuery;

        public UpgradeEventHandler(IEventPublisher publisher, IEventSubscriber subscriber, IUpgradeQueryPort upgradeQuery)
        {
            _publisher = publisher;
            _upgradeQuery = upgradeQuery;
            subscriber.Subscribe(this, new Action<UpgradeSelectedEvent>(OnUpgradeSelected));
            subscriber.Subscribe(this, new Action<StatusAppliedEvent>(OnStatusApplied));
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

        private void OnStatusApplied(StatusAppliedEvent e)
        {
            if (e.Duration < float.MaxValue)
                return;

            var stacks = _upgradeQuery?.GetStacks(e.TargetId, e.Type) ?? 0;
            _publisher.Publish(new UpgradeAppliedEvent(e.TargetId, e.Type, stacks));
        }
    }
}
