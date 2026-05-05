using Shared.Attributes;
using Features.Projectile.Application;
using Features.Projectile.Application.Events;
using Features.Projectile.Infrastructure;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Runtime.Pooling;
using Shared.Time;
using UnityEngine;

namespace Features.Projectile
{
    public sealed class ProjectileSetup : MonoBehaviour
    {
        [Required, SerializeField]
        private ProjectilePhysicsAdapter _projectilePrefab;

        [Required, SerializeField]
        private Transform _spawnRoot;

        private IEventSubscriber _eventBus;
        private IEventPublisher _publisher;
        private SpawnProjectileUseCase _spawnUseCase;
        private DisposableScope _disposables = new DisposableScope();
        private GameObjectPool _projectilePool;

        public void Initialize(IEventSubscriber eventBus, IEventPublisher publisher)
        {
            // csharp-guardrails: allow-null-defense
            if (_projectilePrefab == null)
            {
                Debug.LogError("[ProjectileSetup] Projectile prefab is missing.", this);
                return;
            }

            _eventBus = eventBus;
            _publisher = publisher;
            _spawnUseCase = new SpawnProjectileUseCase(new ClockAdapter(), _publisher);
            _projectilePool = new GameObjectPool(_projectilePrefab.gameObject, _spawnRoot);
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(
                this,
                new System.Action<ProjectileRequestedEvent>(OnProjectileRequested)
            );
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnProjectileRequested(ProjectileRequestedEvent e)
        {
            var pos = e.Position.ToVector3();
            var dir = e.Direction.ToVector3();
            if (dir.sqrMagnitude <= 0.001f)
                dir = Vector3.forward;
            else
                dir.Normalize();

            var spawnPosition = pos + dir;
            var trajectoryTargetPosition = e.Position;

            if (e.Spec.TrajectoryType == Domain.Trajectory.TrajectoryType.Orbit)
            {
                spawnPosition = pos + dir * Domain.Trajectory.OrbitTrajectory.DefaultOrbitRadius;
            }

            var rotation = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

            var physicsAdapter = _projectilePool.RentComponent<ProjectilePhysicsAdapter>(spawnPosition, rotation);
// csharp-guardrails: allow-null-defense
            if (physicsAdapter == null)
            {
                Debug.LogError("[ProjectileSetup] ProjectilePhysicsAdapter is missing on pooled projectile.", this);
                return;
            }

            physicsAdapter.Initialize(_publisher);
            _spawnUseCase.Execute(
                physicsAdapter,
                e.OwnerId,
                e.Spec,
                e.BaseDamage,
                e.DamageType,
                trajectoryTargetPosition,
                e.StatusPayload,
                e.AllyDamageScale
            );
        }
    }
}
