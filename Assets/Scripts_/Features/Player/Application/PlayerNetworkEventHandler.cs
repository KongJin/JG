using Features.Combat.Application.Events;
using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class PlayerNetworkEventHandler
    {
        private readonly IEventPublisher _publisher;
        private readonly Domain.Player _remotePlayer;

        public PlayerNetworkEventHandler(
            IEventPublisher publisher,
            IPlayerNetworkCallbackPort networkCallbacks,
            Domain.Player remotePlayer = null
        )
        {
            _publisher = publisher;
            _remotePlayer = remotePlayer;

            networkCallbacks.OnRemoteJumped = HandleRemoteJumped;
            networkCallbacks.OnRemoteDamaged = HandleRemoteDamaged;
            networkCallbacks.OnRemoteDied = HandleRemoteDied;
            networkCallbacks.OnRemoteRespawned = HandleRemoteRespawned;
            networkCallbacks.OnHealthSynced = HandleHealthSynced;
        }

        private void HandleRemoteJumped(DomainEntityId playerId)
        {
            _publisher.Publish(new PlayerJumpedEvent(playerId));
        }

        private void HandleRemoteDamaged(DomainEntityId targetId, float damage,
            Features.Combat.Domain.DamageType damageType, DomainEntityId attackerId)
        {
            _publisher.Publish(new DamageReplicatedEvent(targetId, damage, damageType, attackerId));
        }

        private void HandleRemoteDied(DomainEntityId targetId, DomainEntityId killerId)
        {
            // Death is conveyed via the damage replication path (DamageReplicatedEvent →
            // ExecuteReplicated → DamageAppliedEvent → PlayerDamageEventHandler → PlayerDiedEvent).
            // No separate handling needed here.
        }

        private void HandleRemoteRespawned(DomainEntityId targetId)
        {
            if (_remotePlayer == null)
                return;

            _remotePlayer.Respawn();
            _publisher.Publish(new PlayerRespawnedEvent(
                targetId,
                _remotePlayer.CurrentHp,
                _remotePlayer.MaxHp
            ));
        }

        private void HandleHealthSynced(DomainEntityId targetId, float currentHp, float maxHp)
        {
            // Reserved for late-join health sync via CustomProperties.
        }
    }
}
