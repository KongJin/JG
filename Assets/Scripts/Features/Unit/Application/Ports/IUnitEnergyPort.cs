using Shared.Kernel;

namespace Features.Unit.Application.Ports
{
    /// <summary>
    /// Energy 조회/차감 포트. Unit Feature가 Player의 Energy를 사용할 수 있도록 한다.
    /// Cross-feature port: Unit이 소비자, Player가 제공자.
    /// </summary>
    public interface IUnitEnergyPort
    {
        bool TrySpendEnergy(DomainEntityId ownerId, float cost);
        float GetCurrentEnergy(DomainEntityId ownerId);
    }
}
