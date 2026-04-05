using Features.Combat.Domain;
using Features.Player.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkCommandPort
    {
        void SendJump(DomainEntityId playerId);
        void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId);
        void SendRespawn(DomainEntityId targetId);
        void SyncHealth(DomainEntityId targetId, float currentHp, float maxHp);
        void SyncMana(DomainEntityId targetId, float currentMana, float maxMana);
        void SyncLifeState(DomainEntityId playerId, LifeState state);
        void SendRescue(DomainEntityId rescuerId, DomainEntityId targetId);
        void SendRescueChannelStart(DomainEntityId rescuerId, DomainEntityId targetId);
        void SendRescueChannelCancel(DomainEntityId targetId);
    }
}
