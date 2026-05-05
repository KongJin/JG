using System;
using Features.Combat.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class CoreHealthHudView : MonoBehaviour
    {
        [SerializeField] private Color normalColor = new(0.2f, 0.6f, 1f);
        [SerializeField] private Color dangerColor = Color.red;
        [SerializeField] private float dangerThreshold = 0.3f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _coreId;

        public float CurrentHp { get; private set; }
        public float MaxHp { get; private set; }
        public string HpText { get; private set; } = string.Empty;
        public Color CurrentColor { get; private set; }

        public void Initialize(IEventSubscriber eventBus, DomainEntityId coreId, float maxHp)
        {
            _eventBus = eventBus;
            _coreId = coreId;
            MaxHp = maxHp;
            UpdateDisplay(maxHp);
            _eventBus.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!_coreId.Equals(e.TargetId))
                return;

            UpdateDisplay(e.RemainingHealth);
        }

        private void UpdateDisplay(float currentHp)
        {
            CurrentHp = currentHp;
            var ratio = MaxHp > 0f ? currentHp / MaxHp : 0f;
            CurrentColor = ratio <= dangerThreshold ? dangerColor : normalColor;
            HpText = $"CORE HP\n{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(MaxHp)}";
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
