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
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _eventBus?.UnsubscribeAll(this);
        }

        private void SetVisualVisible(bool visible)
        {
            _visualRoot.SetActive(visible);
        }
    }
}
