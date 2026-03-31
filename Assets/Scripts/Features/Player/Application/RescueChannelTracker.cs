using System;
using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class RescueChannelTracker
    {
        private readonly IEventPublisher _publisher;
        private readonly IPlayerNetworkCommandPort _network;

        public DomainEntityId RescuerId { get; private set; }
        public DomainEntityId TargetId { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsActive { get; private set; }

        public RescueChannelTracker(
            IEventPublisher publisher,
            IEventSubscriber subscriber,
            IPlayerNetworkCommandPort network
        )
        {
            _publisher = publisher;
            _network = network;

            subscriber.Subscribe(this, new Action<PlayerDownedEvent>(OnPlayerDowned));
            subscriber.Subscribe(this, new Action<PlayerDiedEvent>(OnPlayerDied));
            subscriber.Subscribe(this, new Action<PlayerRescuedEvent>(OnPlayerRescued));
        }

        public void Start(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            RescuerId = rescuerId;
            TargetId = targetId;
            Elapsed = 0f;
            IsActive = true;
            _publisher.Publish(new RescueChannelStartedEvent(rescuerId, targetId));
        }

        public bool Tick(float deltaTime)
        {
            if (!IsActive)
                return false;

            Elapsed += deltaTime;
            return RescueRule.IsChannelComplete(Elapsed);
        }

        public void Cancel()
        {
            if (!IsActive)
                return;

            var targetId = TargetId;
            IsActive = false;
            Elapsed = 0f;
            RescuerId = default;
            TargetId = default;
            _publisher.Publish(new RescueChannelCancelledEvent(targetId));
        }

        private void ForceCancel()
        {
            if (!IsActive)
                return;

            var targetId = TargetId;
            _network.SendRescueChannelCancel(targetId);
            Cancel();
        }

        private void OnPlayerDowned(PlayerDownedEvent e)
        {
            if (!IsActive)
                return;

            if (e.PlayerId.Equals(RescuerId))
                ForceCancel();
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (!IsActive)
                return;

            if (e.PlayerId.Equals(RescuerId) || e.PlayerId.Equals(TargetId))
                ForceCancel();
        }

        private void OnPlayerRescued(PlayerRescuedEvent e)
        {
            if (!IsActive)
                return;

            if (e.RescuedId.Equals(TargetId))
                ForceCancel();
        }
    }
}
