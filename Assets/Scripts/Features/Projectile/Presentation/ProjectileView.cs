using Shared.Attributes;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Projectile.Presentation
{
    public sealed class ProjectileView : MonoBehaviour, IPoolResetHandler
    {
        [Required, SerializeField] private TrailRenderer _trail;
        [Required, SerializeField] private LifetimeRelease _lifetimeRelease;
        [SerializeField] private float _lifetime = 6f;

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
