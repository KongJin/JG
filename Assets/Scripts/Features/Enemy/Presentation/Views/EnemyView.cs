using Shared.Attributes;
using System;
using Features.Enemy.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy.Presentation
{
    public sealed class EnemyView : MonoBehaviour
    {
        [Required, SerializeField] private Renderer meshRenderer;
        [SerializeField] private float flashDuration = 0.1f;

        private IEventSubscriber _eventBus;
        private DomainEntityId _enemyId;
        private Color _originalColor;
        private float _flashTimer;
        private MaterialPropertyBlock _propBlock;

        public void Initialize(IEventSubscriber eventBus, DomainEntityId enemyId)
        {
            _eventBus = eventBus;
            _enemyId = enemyId;

            _propBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(_propBlock);
            _originalColor = meshRenderer.material.color;

            _eventBus.Subscribe(this, new Action<EnemyHealthChangedEvent>(OnHealthChanged));
            _eventBus.Subscribe(this, new Action<EnemyDiedEvent>(OnDied));
        }

        private void Update()
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _propBlock.SetColor("_Color", _originalColor);
                meshRenderer.SetPropertyBlock(_propBlock);
            }
        }

        private void OnHealthChanged(EnemyHealthChangedEvent e)
        {
            if (!_enemyId.Equals(e.EnemyId)) return;

            _propBlock.SetColor("_Color", Color.red);
            meshRenderer.SetPropertyBlock(_propBlock);
            _flashTimer = flashDuration;
        }

        private void OnDied(EnemyDiedEvent e)
        {
            if (!_enemyId.Equals(e.EnemyId)) return;

            _propBlock.SetColor("_Color", Color.black);
            meshRenderer.SetPropertyBlock(_propBlock);
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
