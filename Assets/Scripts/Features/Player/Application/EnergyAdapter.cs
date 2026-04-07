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

        private float _lastSyncTime;
        private const float SyncInterval = 0.25f;

        // Energy 재생 곡선 관련 (시간에 따라 증가)
        private float _baseRegenRate = 5f; // 초당 기본 재생
        private float _gameStartTime;

        public EnergyAdapter(
            Domain.Player player,
            IPlayerNetworkCommandPort network,
            IEventPublisher publisher)
        {
            _player = player;
            _network = network;
            _publisher = publisher;
            _gameStartTime = UnityEngine.Time.time;
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
            var regenRate = GetRegenRate(currentTime);
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

        /// <summary>
        /// 게임 경과 시간에 따라 증가하는 Energy 재생률 계산.
        /// </summary>
        private float GetRegenRate(float currentTime)
        {
            var elapsed = currentTime - _gameStartTime;
            // 단순 선형 증가: 초당 0.1씩 증가 (최대 2배)
            // 예: 0초 = 5/s, 50초 = 10/s
            var scalingFactor = 1f + (elapsed / 100f); // 100초에 2배
            return _baseRegenRate * scalingFactor;
        }

        private void PublishEnergyChanged()
        {
            _publisher.Publish(new PlayerEnergyChangedEvent(
                _player.Id, _player.CurrentEnergy, _player.MaxEnergy));
        }
    }
}
