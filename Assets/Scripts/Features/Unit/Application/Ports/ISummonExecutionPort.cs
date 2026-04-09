using Features.Unit.Domain;
using Shared.Kernel;
using Shared.Math;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Application.Ports
{
    /// <summary>
    /// 소환 실행 포트. BattleEntity 생성을 추상화.
    /// Consumer(Unit)가 정의하고 Provider(Unit Infrastructure)가 구현한다.
    /// </summary>
    public interface ISummonExecutionPort
    {
        DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId);
    }
}
