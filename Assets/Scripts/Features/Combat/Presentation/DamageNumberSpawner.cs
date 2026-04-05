using System;
using Features.Combat.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Combat.Presentation
{
    public sealed class DamageNumberSpawner : MonoBehaviour
    {
        [Required, SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        private IEventSubscriber _eventBus;

        public void Initialize(IEventSubscriber eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (e.Damage <= 0f) return;

            if (!EntityIdHolder.TryGet(e.TargetId, out var holder))
                return;

            var pos = holder.transform.position + spawnOffset;
            var go = Instantiate(damageNumberPrefab, pos, Quaternion.identity);
            var view = go.GetComponent<DamageNumberView>();
            if (view != null)
                view.Show(e.Damage);
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
