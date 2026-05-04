using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkPort
    {
        void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId);
        void SendRespawn(DomainEntityId targetId);
        void SyncHealth(DomainEntityId targetId, float currentHp, float maxHp);
        void SyncEnergy(DomainEntityId targetId, float currentEnergy, float maxEnergy);
        void SyncLifeState(DomainEntityId playerId, LifeState state);

        System.Action<DomainEntityId, float, DamageType, DomainEntityId> OnRemoteDamaged { set; }
        System.Action<DomainEntityId> OnRemoteRespawned { set; }
        System.Action<DomainEntityId, float, float> OnHealthSynced { set; }
        System.Action<DomainEntityId, float, float> OnEnergySynced { set; }
        System.Action<DomainEntityId, LifeState> OnLifeStateSynced { set; }
    }
}
