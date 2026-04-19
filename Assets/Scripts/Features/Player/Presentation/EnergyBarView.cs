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
            AutoBindIfNeeded();

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.04f, 0.06f);
                rect.anchorMax = new Vector2(0.96f, 0.24f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            if (_energyText != null)
            {
                var labelRect = _energyText.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0.55f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _energyText.fontSize = 18;
                _energyText.fontStyle = FontStyle.Bold;
                _energyText.alignment = TextAnchor.UpperLeft;
                _energyText.color = new Color(0.87f, 0.94f, 1f, 1f);
            }

            var sliderRect = _energySlider != null ? _energySlider.transform as RectTransform : null;
            if (sliderRect != null)
            {
                sliderRect.anchorMin = new Vector2(0f, 0f);
                sliderRect.anchorMax = new Vector2(1f, 0.46f);
                sliderRect.offsetMin = Vector2.zero;
                sliderRect.offsetMax = Vector2.zero;
            }

            if (_sliderBackgroundImage != null)
            {
                _sliderBackgroundImage.color = new Color(0.11f, 0.16f, 0.22f, 0.96f);
            }
        }

        private void AutoBindIfNeeded()
        {
            if (_energyText == null)
            {
                var label = transform.Find("Label");
                _energyText = label != null ? label.GetComponent<Text>() : null;
            }

            if (_sliderBackgroundImage == null && _energySlider != null)
            {
                var background = _energySlider.transform.Find("Background");
                _sliderBackgroundImage = background != null ? background.GetComponent<Image>() : null;
            }
        }
    }
}
