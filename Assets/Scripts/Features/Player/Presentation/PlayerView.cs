using Features.Player.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Player.Presentation
{
    public sealed class PlayerView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject _visualRoot;

        private IEventSubscriber _eventBus;
        private DomainEntityId _localPlayerId;

        public void Initialize(bool isLocal, IEventSubscriber eventBus, DomainEntityId localPlayerId = default)
        {
            _eventBus = eventBus;
            _localPlayerId = localPlayerId;
            SetVisualVisible(true);

            if (!isLocal)
                return;

            _eventBus.Subscribe(this, new System.Action<PlayerMovedEvent>(OnPlayerMoved));
            _eventBus.Subscribe(this, new System.Action<PlayerJumpedEvent>(OnPlayerJumped));
            _eventBus.Subscribe(this, new System.Action<PlayerDownedEvent>(OnPlayerDowned));
            _eventBus.Subscribe(this, new System.Action<PlayerRescuedEvent>(OnPlayerRescued));
            _eventBus.Subscribe(this, new System.Action<PlayerRespawnedEvent>(OnPlayerRespawned));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void OnPlayerMoved(PlayerMovedEvent e)
        {
            // 향후 애니메이션 연동용. 지금은 no-op.
        }

        private void OnPlayerJumped(PlayerJumpedEvent e)
        {
            Debug.Log($"[Player] Jumped: {e.PlayerId}");
        }

        private void OnPlayerDowned(PlayerDownedEvent e)
        {
            if (e.PlayerId != _localPlayerId)
                return;

            SetVisualVisible(false);
        }

        private void OnPlayerRescued(PlayerRescuedEvent e)
        {
            if (e.RescuedId != _localPlayerId)
                return;

            SetVisualVisible(true);
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (e.PlayerId != _localPlayerId)
                return;

            SetVisualVisible(true);
        }

        private void SetVisualVisible(bool visible)
        {
            _visualRoot.SetActive(visible);
        }
    }
}
