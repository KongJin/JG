using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Shared.EventBus;
using Shared.Gameplay;
using Shared.Kernel;
using Shared.Math;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Application
{
    /// <summary>
    /// 유닛 소환 UseCase.
    /// 1. Energy 차감 시도
    /// 2. BattleEntity 생성 요청
    /// 3. 결과 이벤트 발행
    /// </summary>
    public sealed class SummonUnitUseCase
    {
        private readonly IUnitEnergyPort _energyPort;
        private readonly ISummonExecutionPort _summonPort;
        private readonly IEventPublisher _eventBus;

        public SummonUnitUseCase(
            IUnitEnergyPort energyPort,
            ISummonExecutionPort summonPort,
            IEventPublisher eventBus)
        {
            _energyPort = energyPort;
            _summonPort = summonPort;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 유닛 소환 실행.
        /// </summary>
        /// <param name="playerId">소환 플레이어 ID</param>
        /// <param name="unitSpec">소환할 유닛 스펙</param>
        /// <param name="spawnPosition">배치 위치</param>
        /// <returns>소환 성공 여부</returns>
        public bool Execute(DomainEntityId playerId, UnitSpec unitSpec, Float3 spawnPosition)
        {
            // 1. Energy 차감 시도
            var cost = unitSpec.SummonCost;
            if (!_energyPort.TrySpendEnergy(playerId, cost))
            {
                _eventBus.Publish(new UnitSummonFailedEvent(
                    playerId,
                    unitSpec,
                    $"Not enough energy. Required: {cost}, Current: {_energyPort.GetCurrentEnergy(playerId)}"));
                return false;
            }

            // 2. BattleEntity 생성
            var battleEntityId = _summonPort.SpawnBattleEntity(unitSpec, spawnPosition, playerId);

            // 3. 성공 이벤트 발행
            _eventBus.Publish(new UnitSummonCompletedEvent(
                playerId,
                battleEntityId,
                unitSpec));
            _eventBus.Publish(new BattleUnitDeployedEvent(
                playerId,
                battleEntityId));

            return true;
        }
    }
}
