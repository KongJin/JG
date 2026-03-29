using Features.Projectile.Application.Events;
using Features.Projectile.Application.Ports;
using Features.Projectile.Domain;
using Features.Projectile.Domain.Hit;
using Features.Projectile.Domain.Trajectory;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Projectile.Infrastructure
{
    public sealed class ProjectilePhysicsAdapter : MonoBehaviour, IProjectilePhysicsPort, IPoolResetHandler
    {
        private Domain.Projectile _projectile;
        private ITrajectory _trajectory;
        private IHitResolver _hitResolver;
        private IEventPublisher _eventBus;
        private PooledObject _pooledObject;

        private Float3 _origin;
        private Float3 _direction;
        private float _elapsed;
        private Float3 _targetPosition;

        public void Initialize(IEventPublisher eventBus)
        {
            _eventBus = eventBus;
            _pooledObject ??= GetComponent<PooledObject>();
        }

        public void Spawn(
            Domain.Projectile projectile,
            ITrajectory trajectory,
            IHitResolver hitResolver,
            Float3 targetPosition
        )
        {
            _projectile = projectile;
            _trajectory = trajectory;
            _hitResolver = hitResolver;
            _origin = transform.position.ToFloat3();
            _direction = transform.forward.ToFloat3();
            _elapsed = 0f;
            _targetPosition = targetPosition;
        }

        private void Update()
        {
            if (_projectile == null || !_projectile.IsAlive) return;

            _elapsed += Time.deltaTime;

            var input = new TrajectoryInput(
                _origin,
                transform.position.ToFloat3(),
                _direction,
                _projectile.Spec.Speed,
                Time.deltaTime,
                _elapsed,
                _targetPosition);

            var position = _trajectory.Calculate(in input);
            transform.position = position.ToVector3();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_projectile == null || !_projectile.IsAlive) return;

            var holder = other.GetComponentInParent<EntityIdHolder>();
            if (holder == null || !holder.IsInitialized) return;
            if (holder.Id.Equals(_projectile.OwnerId)) return;

            var result = _hitResolver.Resolve(_projectile);
            result.Apply(_projectile);

            _eventBus.Publish(
                new ProjectileHitEvent(
                    _projectile.Id,
                    _projectile.OwnerId,
                    holder.Id,
                    _projectile.BaseDamage,
                    _projectile.DamageType,
                    _projectile.StatusPayload
                )
            );

            if (!_projectile.IsAlive)
                ReleaseSelf();
        }

        public void OnRentFromPool()
        {
            ResetState();
        }

        public void OnReturnToPool()
        {
            ResetState();
        }

        private void ReleaseSelf()
        {
            _pooledObject ??= GetComponent<PooledObject>();
            if (_pooledObject != null)
            {
                _pooledObject.Release();
                return;
            }

            Destroy(gameObject);
        }

        private void ResetState()
        {
            _projectile = null;
            _trajectory = null;
            _hitResolver = null;
            _origin = Float3.Zero;
            _direction = Float3.Zero;
            _elapsed = 0f;
            _targetPosition = Float3.Zero;
        }
    }
}
