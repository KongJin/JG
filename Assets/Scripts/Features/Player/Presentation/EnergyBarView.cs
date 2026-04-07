using Shared.Attributes;
using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Player.Presentation
{
    public sealed class EnergyBarView : MonoBehaviour
    {
        [Required, SerializeField] private Slider _energySlider;
        [Required, SerializeField] private Image _energyFillImage;

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;

        public void Initialize(IEventSubscriber eventBus, DomainEntityId playerId, float maxEnergy)
        {
            _eventBus = eventBus;
            _playerId = playerId;

            _energySlider.maxValue = maxEnergy;
            _energySlider.value = maxEnergy;

            _eventBus.Subscribe(this, new System.Action<PlayerEnergyChangedEvent>(OnEnergyChanged));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void OnEnergyChanged(PlayerEnergyChangedEvent e)
        {
            if (!e.PlayerId.Equals(_playerId))
                return;

            _energySlider.maxValue = e.MaxEnergy;
            _energySlider.value = e.CurrentEnergy;
        }
    }
}
