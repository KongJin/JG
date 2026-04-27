using Features.Player.Application;
using Features.Unit.Application.Ports;
using Shared.Kernel;

namespace Features.Player.Infrastructure
{
    /// <summary>
    /// Unit Feature에서 Player의 Energy를 사용할 수 있도록 하는 어댑터.
    /// IUnitEnergyPort의 구현체 (Player Infrastructure에 위치).
    /// </summary>
    public sealed class UnitEnergyAdapter : IUnitEnergyPort
    {
        private readonly EnergyAdapter _energyAdapter;

        public UnitEnergyAdapter(EnergyAdapter energyAdapter)
        {
            _energyAdapter = energyAdapter;
        }

        public bool TrySpendEnergy(DomainEntityId ownerId, float cost)
        {
            return _energyAdapter.TrySpendEnergy(ownerId, cost);
        }

        public void RefundEnergy(DomainEntityId ownerId, float amount)
        {
            _energyAdapter.RefundEnergy(ownerId, amount);
        }

        public float GetCurrentEnergy(DomainEntityId ownerId)
        {
            return _energyAdapter.GetCurrentEnergy(ownerId);
        }
    }
}
