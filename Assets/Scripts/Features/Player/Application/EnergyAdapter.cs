using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    /// <summary>
    /// Energy 리소스 어댑터. 유닛 소환 전용 자원의 차감/재생/네트워크 동기화를 담당.
    /// </summary>
    public sealed class EnergyAdapter : IEnergyPort
    {
        private readonly Domain.Player _player;
        private readonly IPlayerNetworkCommandPort _network;
        private readonly IEventPublisher _publisher;
        private readonly EnergyRegenCurve _regenCurve;
        private readonly float _gameStartTime;

        private float _lastSyncTime;
        private const float SyncInterval = 0.25f;

        public EnergyAdapter(
            Domain.Player player,
            IPlayerNetworkCommandPort network,
            IEventPublisher publisher,
            EnergyRegenCurve regenCurve)
            : this(player, network, publisher, regenCurve, 0f)
        {
        }

        public EnergyAdapter(
            Domain.Player player,
            IPlayerNetworkCommandPort network,
            IEventPublisher publisher,
            EnergyRegenCurve regenCurve,
            float gameStartTime)
        {
            _player = player;
            _network = network;
            _publisher = publisher;
            _regenCurve = regenCurve;
            _gameStartTime = gameStartTime;
        }

        public bool TrySpendEnergy(DomainEntityId ownerId, float cost)
        {
            if (!_player.SpendEnergy(cost))
                return false;

            PublishEnergyChanged();
            _network.SyncEnergy(_player.Id, _player.CurrentEnergy, _player.MaxEnergy);
            return true;
        }

        public float GetCurrentEnergy(DomainEntityId ownerId)
        {
            return _player.CurrentEnergy;
        }

        public void TickRegen(float deltaTime, float currentTime)
        {
            var prevEnergy = _player.CurrentEnergy;
            var elapsed = currentTime - _gameStartTime;
            var regenRate = _regenCurve.GetRegenRate(elapsed);
            _player.RegenEnergy(deltaTime, regenRate);

            if (_player.CurrentEnergy == prevEnergy)
                return;

            PublishEnergyChanged();

            if (currentTime - _lastSyncTime >= SyncInterval)
            {
                _lastSyncTime = currentTime;
                _network.SyncEnergy(_player.Id, _player.CurrentEnergy, _player.MaxEnergy);
            }
        }

        private void PublishEnergyChanged()
        {
            _publisher.Publish(new PlayerEnergyChangedEvent(
                _player.Id, _player.CurrentEnergy, _player.MaxEnergy));
        }
    }
}
