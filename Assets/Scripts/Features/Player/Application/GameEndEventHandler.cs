using Features.Player.Application.Events;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Player.Application
{
    public sealed class GameEndEventHandler
    {
        private readonly IEventPublisher _publisher;
        private readonly DomainEntityId _localPlayerId;
        private bool _gameEnded;

        public GameEndEventHandler(
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            DomainEntityId localPlayerId
        )
        {
            _publisher = publisher;
            _localPlayerId = localPlayerId;

            subscriber.Subscribe(this, new System.Action<PlayerDiedEvent>(OnPlayerDied));
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (_gameEnded)
                return;

            _gameEnded = true;

            var isLocalPlayerDead = e.PlayerId == _localPlayerId;
            var message = isLocalPlayerDead ? "Defeat!" : "Victory!";

            _publisher.Publish(
                new GameEndEvent(e.PlayerId, e.AttackerId, isLocalPlayerDead, message)
            );
            _publisher.Publish(
                new UiErrorRequestedEvent(UiErrorMessage.Modal(message, "Game", canDismiss: false))
            );
        }
    }
}
