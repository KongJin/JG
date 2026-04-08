using Shared.Kernel;

namespace Features.Unit.Application.Ports
{
    /// <summary>
    /// BattleEntity 제거 포트.
    /// Unit Application이 사망 후 정리를 요청하고 Infrastructure가 실제 파괴를 수행한다.
    /// </summary>
    public interface IBattleEntityDespawnPort
    {
        void Despawn(DomainEntityId entityId);
    }
}
