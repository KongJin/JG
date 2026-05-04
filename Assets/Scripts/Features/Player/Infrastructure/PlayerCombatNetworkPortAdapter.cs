using Features.Combat.Application.Ports;
using Features.Player.Application.Ports;
using Shared.Kernel;

namespace Features.Player.Infrastructure
{
    public sealed class PlayerCombatNetworkPortAdapter : ICombatNetworkCommandPort
    {
        private readonly IPlayerNetworkPort _playerNetwork;

        public PlayerCombatNetworkPortAdapter(IPlayerNetworkPort playerNetwork)
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
