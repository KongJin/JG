using System;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Ports
{
    public interface IStatusNetworkPort
    {
        void SendApplyStatus(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval);

        void SendTickDamage(DomainEntityId targetId, float damage, DomainEntityId sourceId);

        Action<DomainEntityId, StatusType, float, float, DomainEntityId, float> OnRemoteStatusApplied { set; }
        Action<DomainEntityId, float, DomainEntityId> OnRemoteTickDamage { set; }
    }
}
