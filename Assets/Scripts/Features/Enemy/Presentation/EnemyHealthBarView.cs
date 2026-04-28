using System;
using Features.Enemy.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy.Presentation
{
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color lowColor = Color.red;
        [SerializeField] private float lowHealthThreshold = 0.3f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _enemyId;

        public float CurrentHp { get; private set; }
        public float MaxHp { get; private set; }
        public Color CurrentColor { get; private set; }

        public void Initialize(IEventSubscriber eventBus, DomainEntityId enemyId, float maxHp)
        {
            _eventBus = eventBus;
            _enemyId = enemyId;
            MaxHp = maxHp;
            CurrentHp = maxHp;
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
            if (!_enemyId.Equals(e.EnemyId))
                return;

            MaxHp = e.MaxHp;
            CurrentHp = e.CurrentHp;
            UpdateFillColor(e.MaxHp > 0f ? e.CurrentHp / e.MaxHp : 0f);
        }

        private void OnDied(EnemyDiedEvent e)
        {
            if (_enemyId.Equals(e.EnemyId))
                gameObject.SetActive(false);
        }

        private void UpdateFillColor(float healthPercent)
        {
            CurrentColor = healthPercent <= lowHealthThreshold ? lowColor : normalColor;
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
