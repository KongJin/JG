using Features.Combat.Domain;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkCallbackPort
    {
        System.Action<DomainEntityId, float, DamageType, DomainEntityId> OnRemoteDamaged { set; }
        System.Action<DomainEntityId> OnRemoteRespawned { set; }
        System.Action<DomainEntityId, float, float> OnHealthSynced { set; }
        System.Action<DomainEntityId, float, float> OnManaSynced { set; }
        System.Action<DomainEntityId, LifeState> OnLifeStateSynced { set; }
    }
}
