using UnityEngine;
using Shared.Runtime.Pooling;

namespace Features.Projectile.Presentation
{
    public sealed class ProjectileView : MonoBehaviour, IPoolResetHandler
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _lifetime = 6f;
        private LifetimeRelease _lifetimeRelease;

        private void Awake()
        {
            _lifetimeRelease = GetComponent<LifetimeRelease>();
            if (_lifetimeRelease == null)
                _lifetimeRelease = gameObject.AddComponent<LifetimeRelease>();
        }

        public void SetColor(Color color)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            if (_trail != null)
            {
                _trail.startColor = color;
                _trail.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        public void OnRentFromPool()
        {
            _lifetimeRelease ??= GetComponent<LifetimeRelease>();
            _lifetimeRelease.Arm(_lifetime);

            if (_trail != null)
                _trail.Clear();
        }

        public void OnReturnToPool()
        {
            if (_trail != null)
                _trail.Clear();
        }
    }
}
