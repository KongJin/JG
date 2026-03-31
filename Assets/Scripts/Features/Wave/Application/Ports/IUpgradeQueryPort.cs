using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Wave.Application.Ports
{
    public interface IUpgradeQueryPort
    {
        int GetStacks(DomainEntityId targetId, StatusType type);
    }
}
