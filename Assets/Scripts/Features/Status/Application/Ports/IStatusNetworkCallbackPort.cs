using System;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application.Ports
{
    public interface IStatusNetworkCallbackPort
    {
        Action<DomainEntityId, StatusType, float, float, DomainEntityId, float> OnRemoteStatusApplied { set; }
        Action<DomainEntityId, float, DomainEntityId> OnRemoteTickDamage { set; }
    }
}
