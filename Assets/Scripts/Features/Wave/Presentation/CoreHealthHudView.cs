using System;
using Features.Combat.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class CoreHealthHudView : MonoBehaviour
    {
        [Required, SerializeField] private Slider healthSlider;
        [Required, SerializeField] private Image fillImage;
        [Required, SerializeField] private Text hpText;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image sliderBackgroundImage;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color dangerColor = Color.red;
        [SerializeField] private float dangerThreshold = 0.3f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _coreId;
        private float _maxHp;

        private void Awake()
        {
            ApplyPresentationDefaults();
        }

        public void Initialize(IEventSubscriber eventBus, DomainEntityId coreId, float maxHp)
        {
            _eventBus = eventBus;
            _coreId = coreId;
            _maxHp = maxHp;
            ApplyPresentationDefaults();

            healthSlider.maxValue = maxHp;
            healthSlider.value = maxHp;
            UpdateDisplay(maxHp);

            _eventBus.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_coreId.Equals(e.TargetId)) return;

            healthSlider.value = e.RemainingHealth;
            UpdateDisplay(e.RemainingHealth);
        }

        private void UpdateDisplay(float currentHp)
        {
            var ratio = _maxHp > 0f ? currentHp / _maxHp : 0f;
            fillImage.color = ratio <= dangerThreshold ? dangerColor : normalColor;

            hpText.text = $"CORE HP\n{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(_maxHp)}";
        }

        private void ApplyPresentationDefaults()
        {
            if (panelImage == null)
            {
                panelImage = GetComponent<Image>();
            }

            if (panelImage != null)
            {
                panelImage.color = new Color(0.10f, 0.14f, 0.19f, 0.94f);
                panelImage.raycastTarget = false;
            }

            if (hpText != null)
            {
                hpText.fontSize = 22;
                hpText.fontStyle = FontStyle.Bold;
                hpText.alignment = TextAnchor.UpperLeft;
                hpText.color = new Color(0.92f, 0.95f, 1f, 1f);
                hpText.raycastTarget = false;
            }

            if (sliderBackgroundImage != null)
            {
                sliderBackgroundImage.color = new Color(0.18f, 0.09f, 0.11f, 0.92f);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
