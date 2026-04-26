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
        [SerializeField] private Text _energyText;
        [SerializeField] private Image _sliderBackgroundImage;

        private IEventSubscriber _eventBus;
        private DomainEntityId _playerId;

        private void Awake()
        {
            ApplyPresentationDefaults();
        }

        public void Initialize(IEventSubscriber eventBus, DomainEntityId playerId, float maxEnergy)
        {
            _eventBus = eventBus;
            _playerId = playerId;

            _energySlider.maxValue = maxEnergy;
            _energySlider.value = maxEnergy;
            UpdateDisplay(maxEnergy, maxEnergy);

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
            UpdateDisplay(e.CurrentEnergy, e.MaxEnergy);
        }

        private void UpdateDisplay(float currentEnergy, float maxEnergy)
        {
            _energyFillImage.color = new Color(0.28f, 0.77f, 0.95f, 1f);

            if (_energyText != null)
            {
                _energyText.text = $"ENERGY  {Mathf.FloorToInt(currentEnergy)} / {Mathf.FloorToInt(maxEnergy)}";
            }
        }

        private void ApplyPresentationDefaults()
        {
            if (_energyText != null)
            {
                _energyText.fontSize = 18;
                _energyText.fontStyle = FontStyle.Bold;
                _energyText.alignment = TextAnchor.UpperLeft;
                _energyText.color = new Color(0.87f, 0.94f, 1f, 1f);
            }

            if (_sliderBackgroundImage != null)
            {
                _sliderBackgroundImage.color = new Color(0.11f, 0.16f, 0.22f, 0.96f);
            }
        }
    }
}
