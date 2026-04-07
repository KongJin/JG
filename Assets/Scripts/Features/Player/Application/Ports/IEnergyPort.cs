using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IEnergyPort
    {
        bool TrySpendEnergy(DomainEntityId ownerId, float cost);
        float GetCurrentEnergy(DomainEntityId ownerId);
    }
}
