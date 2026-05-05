using System;
using Features.Combat.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime;
using Shared.Runtime.Pooling;
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
            var view = ComponentAccess.InstantiateComponent<DamageNumberView>(
                damageNumberPrefab,
                pos,
                Quaternion.identity);
            // csharp-guardrails: allow-null-defense
            if (view != null)
                view.Show(e.Damage);
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _eventBus?.UnsubscribeAll(this);
        }
    }
}
