using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Features.Player.Application.Ports;
using Shared.Kernel;

namespace Features.Player.Infrastructure
{
    public sealed class PlayerCombatNetworkPortAdapter : ICombatNetworkCommandPort
    {
        private readonly IPlayerNetworkCommandPort _playerNetwork;

        public PlayerCombatNetworkPortAdapter(IPlayerNetworkCommandPort playerNetwork)
        {
            _playerNetwork = playerNetwork;
        }

        public void SendDamage(
            DomainEntityId targetId,
            float damage,
            DamageType damageType,
            DomainEntityId attackerId
        )
        {
            _playerNetwork.SendDamage(targetId, damage, damageType, attackerId);
        }

        public void SendRespawn(DomainEntityId targetId)
        {
            _playerNetwork.SendRespawn(targetId);
        }
    }
}
