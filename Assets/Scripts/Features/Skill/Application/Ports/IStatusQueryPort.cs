using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Skill.Application.Ports
{
    public interface IStatusQueryPort
    {
        float GetMagnitude(DomainEntityId targetId, StatusType type);
        int GetStacks(DomainEntityId targetId, StatusType type);
    }
}
