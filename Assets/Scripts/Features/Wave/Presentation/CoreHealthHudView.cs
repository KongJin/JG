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
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color dangerColor = Color.red;
        [SerializeField] private float dangerThreshold = 0.3f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _coreId;
        private float _maxHp;

        public void Initialize(IEventSubscriber eventBus, DomainEntityId coreId, float maxHp)
        {
            _eventBus = eventBus;
            _coreId = coreId;
            _maxHp = maxHp;

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

            hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(_maxHp)}";
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
