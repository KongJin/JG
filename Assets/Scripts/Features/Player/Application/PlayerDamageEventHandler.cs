using System;
using Features.Combat.Application.Events;
using Features.Player.Application.Events;
using Shared.EventBus;

namespace Features.Player.Application
{
    public sealed class PlayerDamageEventHandler
    {
        private readonly Domain.Player _player;
        private readonly IEventPublisher _eventBus;
        private bool _deathPublished;
        private bool _downedPublished;

        public PlayerDamageEventHandler(
            Domain.Player player,
            IEventPublisher eventBus,
            IEventSubscriber subscriber)
        {
            _player = player;
            _eventBus = eventBus;
            subscriber.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
            subscriber.Subscribe(this, new Action<PlayerRespawnedEvent>(OnRespawned));
            subscriber.Subscribe(this, new Action<PlayerRescuedEvent>(OnRescued));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_player.Id.Equals(e.TargetId))
                return;

            _eventBus.Publish(new PlayerHealthChangedEvent(
                _player.Id,
                _player.CurrentHp,
                _player.MaxHp,
                e.Damage,
                _player.IsDead,
                _player.IsDowned
            ));

            if (e.IsDowned && !_downedPublished)
            {
                _downedPublished = true;
                _eventBus.Publish(new PlayerDownedEvent(_player.Id, e.AttackerId));
            }
            else if (e.IsDead && !_deathPublished)
            {
                _deathPublished = true;
                _player.Die();
                _eventBus.Publish(new PlayerDiedEvent(_player.Id, e.AttackerId));
            }
        }

        private void OnRespawned(PlayerRespawnedEvent e)
        {
            if (!_player.Id.Equals(e.PlayerId))
                return;

            _deathPublished = false;
            _downedPublished = false;
        }

        private void OnRescued(PlayerRescuedEvent e)
        {
            if (!_player.Id.Equals(e.RescuedId))
                return;

            _deathPublished = false;
            _downedPublished = false;
        }
    }
}
