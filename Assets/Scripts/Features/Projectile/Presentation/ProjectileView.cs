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
