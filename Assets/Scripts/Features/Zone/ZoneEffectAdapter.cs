using Shared.Attributes;
using Features.Zone.Application.Events;
using Features.Zone.Application.Ports;
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
        private IEventPublisher _eventPublisher;

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
            _eventPublisher = eventPublisher;
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
            var viewGo = _zonePool.Rent(worldPosition, Quaternion.identity);
            var view = viewGo.GetComponent<ZoneView>();
            var detector = viewGo.GetComponent<ZoneCollisionDetector>();

            view.Initialize(radius, duration);
            view.SetColor(_zoneColor);
            view.name = $"{_zonePrefab.name}_{Time.time}";
            detector.Initialize(zoneId, casterId, baseDamage, statusPayload, _eventPublisher, allyDamageScale);
        }
    }
}
