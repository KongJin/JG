using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Ports
{
    public interface IStatusNetworkCommandPort
    {
        void SendApplyStatus(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval);

        void SendTickDamage(DomainEntityId targetId, float damage, DomainEntityId sourceId);
    }
}
