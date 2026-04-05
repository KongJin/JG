using System;
using Features.Enemy.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Enemy.Presentation
{
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [Required, SerializeField] private Slider healthSlider;
        [Required, SerializeField] private Image fillImage;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color lowColor = Color.red;
        [SerializeField] private float lowHealthThreshold = 0.3f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _enemyId;

        public void Initialize(IEventSubscriber eventBus, DomainEntityId enemyId, float maxHp)
        {
            _eventBus = eventBus;
            _enemyId = enemyId;

            healthSlider.maxValue = maxHp;
            healthSlider.value = maxHp;
            UpdateFillColor(1f);

            _eventBus.Subscribe(this, new Action<EnemyHealthChangedEvent>(OnHealthChanged));
            _eventBus.Subscribe(this, new Action<EnemyDiedEvent>(OnDied));
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.forward = cam.transform.forward;
        }

        private void OnHealthChanged(EnemyHealthChangedEvent e)
        {
            if (!_enemyId.Equals(e.EnemyId)) return;

            healthSlider.value = e.CurrentHp;
            UpdateFillColor(e.CurrentHp / e.MaxHp);
        }

        private void OnDied(EnemyDiedEvent e)
        {
            if (!_enemyId.Equals(e.EnemyId)) return;
            gameObject.SetActive(false);
        }

        private void UpdateFillColor(float healthPercent)
        {
            fillImage.color = healthPercent <= lowHealthThreshold ? lowColor : normalColor;
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
