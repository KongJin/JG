using Features.Combat.Domain;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkCommandPort
    {
        void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId);
        void SendRespawn(DomainEntityId targetId);
        void SyncHealth(DomainEntityId targetId, float currentHp, float maxHp);
        void SyncEnergy(DomainEntityId targetId, float currentEnergy, float maxEnergy);
        void SyncLifeState(DomainEntityId playerId, LifeState state);
        
        // TODO: Remove - Skill system compatibility
        void SyncMana(DomainEntityId targetId, float currentMana, float maxMana);
    }
}
