using Shared.Attributes;
using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Player.Presentation
{
    public sealed class ManaBarView : MonoBehaviour
    {
        [Required, SerializeField] private Slider _manaSlider;
        [Required, SerializeField] private Image _manaFillImage;

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;

        public void Initialize(IEventSubscriber eventBus, DomainEntityId playerId, float maxMana)
        {
            _eventBus = eventBus;
            _playerId = playerId;

            _manaSlider.maxValue = maxMana;
            _manaSlider.value = maxMana;

            _eventBus.Subscribe(this, new System.Action<PlayerManaChangedEvent>(OnManaChanged));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void OnManaChanged(PlayerManaChangedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            _manaSlider.maxValue = e.MaxMana;
            _manaSlider.value = e.CurrentMana;
        }
    }
}
