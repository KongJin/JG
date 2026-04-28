using System.Collections.Generic;
using Features.Zone.Application;
using Features.Status.Application.Events;
using Features.Status.Domain;
using Features.Zone.Application.Events;
using Features.Zone.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime;
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
        private HandleZoneContactUseCase _zoneContactUseCase;
        private bool _initialized;

        private readonly Dictionary<string, float> _lastTickTimes = new();

        public void Initialize(
            DomainEntityId zoneId,
            DomainEntityId casterId,
            float baseDamage,
            ZoneStatusPayload statusPayload,
            HandleZoneContactUseCase zoneContactUseCase,
            float allyDamageScale = 1f)
        {
            _zoneId = zoneId;
            _casterId = casterId;
            _baseDamage = baseDamage;
            _statusPayload = statusPayload;
            _allyDamageScale = allyDamageScale;
            _tickInterval = statusPayload.TickInterval > 0f ? statusPayload.TickInterval : DefaultTickInterval;
            _zoneContactUseCase = zoneContactUseCase;
            _initialized = true;
            _lastTickTimes.Clear();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_initialized || _zoneContactUseCase == null)
                return;

            if (!ComponentAccess.TryGetEntityIdHolder(other, out var holder))
                return;

            var targetId = holder.Id;
            var key = targetId.Value;
            var now = Time.time;

            if (_lastTickTimes.TryGetValue(key, out var lastTick) && now - lastTick < _tickInterval)
                return;

            _lastTickTimes[key] = now;
            _zoneContactUseCase.Execute(
                _zoneId,
                _casterId,
                targetId,
                _baseDamage,
                _statusPayload,
                _allyDamageScale);
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
