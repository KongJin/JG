using Features.Combat.Domain;
using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface IPlayerNetworkCommandPort
    {
        void SendJump(DomainEntityId playerId);
        void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId);
        void SendDeath(DomainEntityId targetId, DomainEntityId killerId);
        void SendRespawn(DomainEntityId targetId);
        void SyncHealth(DomainEntityId targetId, float currentHp, float maxHp);
    }
}
