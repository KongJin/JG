using Features.Combat.Domain;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkCallbackPort
    {
        System.Action<DomainEntityId> OnRemoteJumped { set; }
        System.Action<DomainEntityId, float, DamageType, DomainEntityId> OnRemoteDamaged { set; }
        System.Action<DomainEntityId> OnRemoteRespawned { set; }
        System.Action<DomainEntityId, float, float> OnHealthSynced { set; }
        System.Action<DomainEntityId, float, float> OnManaSynced { set; }
        System.Action<DomainEntityId, LifeState> OnLifeStateSynced { set; }
        System.Action<DomainEntityId, DomainEntityId> OnRemoteRescued { set; }
        System.Action<DomainEntityId, DomainEntityId> OnRemoteRescueChannelStarted { set; }
        System.Action<DomainEntityId> OnRemoteRescueChannelCancelled { set; }
    }
}
