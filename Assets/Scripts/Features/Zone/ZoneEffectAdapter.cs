using Shared.Attributes;
using Features.Zone.Application;
using Features.Zone.Application.Events;
using Features.Zone.Application.Ports;
using Features.Zone.Domain;
using Features.Zone.Presentation;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Zone
{
    public sealed class ZoneEffectAdapter : MonoBehaviour, IZoneEffectPort
    {
        [Required, SerializeField]
        private ZoneView _zonePrefab;

        [Required, SerializeField]
        private Transform _spawnRoot;

        [Required, SerializeField]
        private Color _zoneColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        private GameObjectPool _zonePool;
        private HandleZoneContactUseCase _zoneContactUseCase;

        private void Awake()
        {
            if (_zonePrefab == null)
            {
                Debug.LogError("[ZoneEffectAdapter] ZoneView prefab is missing.", this);
                return;
            }

            _zonePool = new GameObjectPool(_zonePrefab.gameObject, _spawnRoot);
        }

        public void Initialize(IEventPublisher eventPublisher)
        {
            _zoneContactUseCase = new HandleZoneContactUseCase(eventPublisher);
        }

        public void SpawnZone(
            Float3 position,
            float radius,
            float duration,
            DomainEntityId zoneId,
            DomainEntityId casterId,
            float baseDamage,
            ZoneStatusPayload statusPayload,
            float allyDamageScale = 1f)
        {
            if (_zonePool == null)
                return;

            var worldPosition = position.ToVector3();
            if (!_zonePool.RentComponents<ZoneView, ZoneCollisionDetector>(
                    worldPosition,
                    Quaternion.identity,
                    out var view,
                    out var detector))
            {
                Debug.LogError("[ZoneEffectAdapter] Zone prefab is missing ZoneView or ZoneCollisionDetector.", this);
                return;
            }

            view.Initialize(radius, duration);
            view.SetColor(_zoneColor);
            view.name = $"{_zonePrefab.name}_{Time.time}";
            detector.Initialize(zoneId, casterId, baseDamage, statusPayload, _zoneContactUseCase, allyDamageScale);
        }
    }
}
