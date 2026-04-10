using System.Collections.Generic;
using Features.Status.Application.Events;
using Features.Status.Domain;
using Features.Zone.Application.Events;
using Features.Zone.Domain;
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
        private ZoneStatusPayload _statusPayload;
        private float _allyDamageScale;
        private float _tickInterval;
        private IEventPublisher _publisher;
        private bool _initialized;

        private readonly Dictionary<string, float> _lastTickTimes = new();

        public void Initialize(
            DomainEntityId zoneId,
            DomainEntityId casterId,
            float baseDamage,
            ZoneStatusPayload statusPayload,
            IEventPublisher publisher,
            float allyDamageScale = 1f)
        {
            _zoneId = zoneId;
            _casterId = casterId;
            _baseDamage = baseDamage;
            _statusPayload = statusPayload;
            _allyDamageScale = allyDamageScale;
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
            _publisher.Publish(new ZoneTickEvent(_zoneId, _casterId, targetId, _baseDamage, _statusPayload, _allyDamageScale));

            var statusType = ConvertZoneStatusType(_statusPayload.Type);
            if (!statusType.HasValue)
                return;

            _publisher.Publish(
                new StatusApplyRequestedEvent(
                    targetId,
                    statusType.Value,
                    _statusPayload.Magnitude,
                    _statusPayload.Duration,
                    _casterId,
                    _statusPayload.TickInterval
                )
            );
        }

        private static StatusType? ConvertZoneStatusType(ZoneStatusPayload.ZoneStatusType zoneType)
        {
            return zoneType switch
            {
                ZoneStatusPayload.ZoneStatusType.Slow => StatusType.Slow,
                ZoneStatusPayload.ZoneStatusType.Haste => StatusType.Haste,
                ZoneStatusPayload.ZoneStatusType.DoT => StatusType.Burn,
                ZoneStatusPayload.ZoneStatusType.None => null,
                _ => null,
            };
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
