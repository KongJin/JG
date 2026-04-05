using Features.Combat.Application.Events;
using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class PlayerNetworkEventHandler
    {
        private readonly IEventPublisher _publisher;
        private readonly IPlayerLookupPort _playerLookup;

        public PlayerNetworkEventHandler(
            IEventPublisher publisher,
            IPlayerNetworkCallbackPort networkCallbacks,
            IPlayerLookupPort playerLookup = null
        )
        {
            _publisher = publisher;
            _playerLookup = playerLookup;

            networkCallbacks.OnRemoteJumped = HandleRemoteJumped;
            networkCallbacks.OnRemoteDamaged = HandleRemoteDamaged;
            networkCallbacks.OnRemoteRespawned = HandleRemoteRespawned;
            networkCallbacks.OnHealthSynced = HandleHealthSynced;
            networkCallbacks.OnManaSynced = HandleManaSynced;
            networkCallbacks.OnLifeStateSynced = HandleLifeStateSynced;
            networkCallbacks.OnRemoteRescued = HandleRemoteRescued;
            networkCallbacks.OnRemoteRescueChannelStarted = HandleRemoteRescueChannelStarted;
            networkCallbacks.OnRemoteRescueChannelCancelled = HandleRemoteRescueChannelCancelled;
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

        private void HandleRemoteRespawned(DomainEntityId targetId)
        {
            var player = _playerLookup?.Resolve(targetId);
            if (player == null)
                return;

            player.Respawn();
            _publisher.Publish(new PlayerRespawnedEvent(
                targetId,
                player.CurrentHp,
                player.MaxHp
            ));
        }

        private void HandleHealthSynced(DomainEntityId targetId, float currentHp, float maxHp)
        {
            var player = _playerLookup?.Resolve(targetId);
            player?.Hydrate(currentHp, player.CurrentMana);
            _publisher.Publish(new PlayerHealthChangedEvent(targetId, currentHp, maxHp, 0f, false));
        }

        private void HandleManaSynced(DomainEntityId targetId, float currentMana, float maxMana)
        {
            var player = _playerLookup?.Resolve(targetId);
            player?.Hydrate(player.CurrentHp, currentMana);
            _publisher.Publish(new PlayerManaChangedEvent(targetId, currentMana, maxMana));
        }

        private void HandleLifeStateSynced(DomainEntityId targetId, LifeState state)
        {
            var player = _playerLookup?.Resolve(targetId);

            switch (state)
            {
                case LifeState.Downed:
                    player?.ForceDowned();
                    _publisher.Publish(new PlayerDownedEvent(targetId, default));
                    break;
                case LifeState.Dead:
                    player?.Die();
                    _publisher.Publish(new PlayerDiedEvent(targetId, default));
                    break;
                case LifeState.Alive:
                    if (player != null && !player.IsAlive)
                    {
                        player.Respawn();
                        _publisher.Publish(new PlayerRespawnedEvent(
                            targetId, player.CurrentHp, player.MaxHp));
                        _publisher.Publish(new PlayerHealthChangedEvent(
                            targetId, player.CurrentHp, player.MaxHp, 0f, false));
                        _publisher.Publish(new PlayerManaChangedEvent(
                            targetId, player.CurrentMana, player.MaxMana));
                    }
                    break;
            }
        }

        private void HandleRemoteRescued(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            var player = _playerLookup?.Resolve(targetId);
            if (player == null || !player.IsDowned)
                return;

            var hp = player.MaxHp * RescueRule.HpPercent;
            var mana = player.MaxMana * RescueRule.ManaPercent;
            player.Rescue(hp, mana);

            _publisher.Publish(new PlayerRescuedEvent(
                targetId,
                rescuerId,
                player.CurrentHp,
                player.MaxHp,
                player.CurrentMana,
                player.MaxMana
            ));

            _publisher.Publish(new PlayerHealthChangedEvent(
                targetId, player.CurrentHp, player.MaxHp, 0f, false));

            _publisher.Publish(new PlayerManaChangedEvent(
                targetId, player.CurrentMana, player.MaxMana));
        }

        private void HandleRemoteRescueChannelStarted(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            _publisher.Publish(new RescueChannelStartedEvent(rescuerId, targetId));
        }

        private void HandleRemoteRescueChannelCancelled(DomainEntityId targetId)
        {
            _publisher.Publish(new RescueChannelCancelledEvent(targetId));
        }
    }
}
