using Features.Zone.Application.Ports;
using Features.Zone.Presentation;
using Shared.Math;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Zone
{
    public sealed class ZoneEffectAdapter : MonoBehaviour, IZoneEffectPort
    {
        [SerializeField]
        private ZoneView _zonePrefab;

        [SerializeField]
        private Transform _spawnRoot;

        [SerializeField]
        private Color _zoneColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        private GameObjectPool _zonePool;

        private void Awake()
        {
            if (_zonePrefab == null)
            {
                Debug.LogError("[ZoneEffectAdapter] ZoneView prefab is missing.", this);
                return;
            }

            _zonePool = new GameObjectPool(_zonePrefab.gameObject, _spawnRoot);
        }

        public void SpawnZone(Float3 position, float radius, float duration)
        {
            if (_zonePool == null)
                return;

            var worldPosition = position.ToVector3();
            var viewGo = _zonePool.Rent(worldPosition, Quaternion.identity);
            var view = viewGo.GetComponent<ZoneView>();

            view.Initialize(radius, duration);
            view.SetColor(_zoneColor);
            view.name = $"{_zonePrefab.name}_{Time.time}";
        }
    }
}
