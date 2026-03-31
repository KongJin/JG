using System;
using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Shared.EventBus;

namespace Features.Player.Application
{
    public sealed class BleedoutTracker
    {
        private readonly Domain.Player _player;
        private readonly IEventPublisher _publisher;
        private readonly IPlayerNetworkCommandPort _network;
        private bool _active;

        public float Elapsed => _player.BleedoutElapsed;

        public BleedoutTracker(
            Domain.Player player,
            IEventPublisher publisher,
            IEventSubscriber subscriber,
            IPlayerNetworkCommandPort network)
        {
            _player = player;
            _publisher = publisher;
            _network = network;

            subscriber.Subscribe(this, new Action<PlayerDownedEvent>(OnDowned));
            subscriber.Subscribe(this, new Action<PlayerRescuedEvent>(OnRescued));
            subscriber.Subscribe(this, new Action<PlayerRespawnedEvent>(OnRespawned));
        }

        public void Tick(float deltaTime)
        {
            if (!_active)
                return;

            _player.TickBleedout(deltaTime);

            if (_player.IsDead)
            {
                _active = false;
                _publisher.Publish(new PlayerDiedEvent(_player.Id, default));
            }
        }

        private void OnDowned(PlayerDownedEvent e)
        {
            if (!_player.Id.Equals(e.PlayerId))
                return;

            _active = true;
        }

        private void OnRescued(PlayerRescuedEvent e)
        {
            if (!_player.Id.Equals(e.RescuedId))
                return;

            _active = false;
        }

        private void OnRespawned(PlayerRespawnedEvent e)
        {
            if (!_player.Id.Equals(e.PlayerId))
                return;

            _active = false;
        }
    }
}
