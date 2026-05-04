using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IEnergyManagementPort
    {
        bool TrySpendEnergy(DomainEntityId ownerId, float cost);
        void RefundEnergy(DomainEntityId ownerId, float amount);
        float GetCurrentEnergy(DomainEntityId ownerId);
        void TickRegen(float deltaTime, float currentTime);
    }
}
