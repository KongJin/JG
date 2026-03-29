using System.Collections.Generic;
using Features.Status.Domain;
using Features.Zone.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Zone.Presentation
{
    public sealed class ZoneCollisionDetector : MonoBehaviour, IPoolResetHandler
    {
        private const float DefaultTickInterval = 0.5f;

        private DomainEntityId _zoneId;
        private DomainEntityId _casterId;
        private float _baseDamage;
        private StatusPayload _statusPayload;
        private float _tickInterval;
        private IEventPublisher _publisher;
        private bool _initialized;

        private readonly Dictionary<string, float> _lastTickTimes = new();

        public void Initialize(
            DomainEntityId zoneId,
            DomainEntityId casterId,
            float baseDamage,
            StatusPayload statusPayload,
            IEventPublisher publisher)
        {
            _zoneId = zoneId;
            _casterId = casterId;
            _baseDamage = baseDamage;
            _statusPayload = statusPayload;
            _tickInterval = statusPayload.TickInterval > 0f ? statusPayload.TickInterval : DefaultTickInterval;
            _publisher = publisher;
            _initialized = true;
            _lastTickTimes.Clear();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_initialized || _publisher == null)
                return;

            var holder = other.GetComponentInParent<EntityIdHolder>();
            if (holder == null || !holder.IsInitialized)
                return;

            var targetId = holder.Id;
            var key = targetId.Value;
            var now = Time.time;

            if (_lastTickTimes.TryGetValue(key, out var lastTick) && now - lastTick < _tickInterval)
                return;

            _lastTickTimes[key] = now;
            _publisher.Publish(new ZoneTickEvent(_zoneId, _casterId, targetId, _baseDamage, _statusPayload));
        }

        public void OnRentFromPool()
        {
            _lastTickTimes.Clear();
            _initialized = false;
        }

        public void OnReturnToPool()
        {
            _lastTickTimes.Clear();
            _initialized = false;
        }
    }
}
