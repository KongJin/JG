using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Features.Skill.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class ManaAdapter : IManaPort
    {
        private readonly Domain.Player _player;
        private readonly IPlayerNetworkCommandPort _network;
        private readonly IEventPublisher _publisher;

        private float _lastSyncTime;
        private const float SyncInterval = 0.25f;

        public ManaAdapter(
            Domain.Player player,
            IPlayerNetworkCommandPort network,
            IEventPublisher publisher)
        {
            _player = player;
            _network = network;
            _publisher = publisher;
        }

        public bool TrySpendMana(DomainEntityId casterId, float cost)
        {
            if (!_player.SpendMana(cost))
                return false;

            PublishManaChanged();
            _network.SyncMana(_player.Id, _player.CurrentMana, _player.MaxMana);
            return true;
        }

        public float GetCurrentMana(DomainEntityId casterId)
        {
            return _player.CurrentMana;
        }

        public void TickRegen(float deltaTime, float currentTime)
        {
            var prevMana = _player.CurrentMana;
            _player.RegenMana(deltaTime);

            if (_player.CurrentMana == prevMana)
                return;

            PublishManaChanged();

            if (currentTime - _lastSyncTime >= SyncInterval)
            {
                _lastSyncTime = currentTime;
                _network.SyncMana(_player.Id, _player.CurrentMana, _player.MaxMana);
            }
        }

        private void PublishManaChanged()
        {
            _publisher.Publish(new PlayerManaChangedEvent(
                _player.Id, _player.CurrentMana, _player.MaxMana));
        }
    }
}
