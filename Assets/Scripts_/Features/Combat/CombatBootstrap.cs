using Features.Combat.Application;
using Features.Combat.Application.Events;
using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Features.Combat.Infrastructure;
using Features.Combat.Presentation;
using Features.Projectile.Application.Events;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Combat
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [SerializeField]
        private CombatTargetAdapter _targetAdapter;

        [SerializeField]
        private CombatTargetView[] _targetViews = new CombatTargetView[0];

        private ApplyDamageUseCase _applyDamage;
        private CombatNetworkEventHandler _eventHandler;
        private CombatReplicationEventHandler _replicationEventHandler;
        private EventBus _eventBus;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(EventBus eventBus)
        {
            Initialize(eventBus, null, default);
        }

        public void Initialize(EventBus eventBus, ICombatNetworkCommandPort networkPort)
        {
            Initialize(eventBus, networkPort, default);
        }

        public void Initialize(
            EventBus eventBus,
            ICombatNetworkCommandPort networkPort,
            DomainEntityId localAuthorityId
        )
        {
            if (_targetAdapter == null)
            {
                Debug.LogError("[CombatBootstrap] CombatTargetAdapter is not assigned in Inspector.", this);
                return;
            }

            if (eventBus == null)
            {
                Debug.LogError("[CombatBootstrap] EventBus is not provided.", this);
                return;
            }

            _eventBus = eventBus;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _targetAdapter.Initialize();
            _applyDamage = new ApplyDamageUseCase(_targetAdapter, _eventBus, networkPort ?? NoOpCombatNetworkPort.Instance);
            _eventHandler = new CombatNetworkEventHandler(_applyDamage, localAuthorityId);
            _replicationEventHandler = new CombatReplicationEventHandler(_applyDamage);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new System.Action<ProjectileHitEvent>(OnProjectileHit));
            _eventBus.Subscribe(this, new System.Action<DamageReplicatedEvent>(OnDamageReplicated));

            for (var i = 0; i < _targetViews.Length; i++)
            {
                var view = _targetViews[i];
                if (view == null)
                {
                    Debug.LogError($"[CombatBootstrap] CombatTargetView at index {i} is null.", this);
                    continue;
                }

                view.Initialize(_eventBus);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnProjectileHit(ProjectileHitEvent e)
        {
            if (_eventHandler == null) return;

            var result = _eventHandler.HandleProjectileHit(e);
            if (result.IsFailure)
                Debug.LogWarning($"[CombatBootstrap] Damage failed: {result.Error}");
        }

        private void OnDamageReplicated(DamageReplicatedEvent e)
        {
            if (_replicationEventHandler == null) return;

            var result = _replicationEventHandler.HandleDamageReplicated(e);
            if (result.IsFailure)
                Debug.LogWarning($"[CombatBootstrap] Replicated damage failed: {result.Error}");
        }

        public void RegisterTarget(DomainEntityId targetId, ICombatTargetProvider provider)
        {
            _targetAdapter.Register(targetId, provider);
        }

        public Result ApplyDamage(DomainEntityId targetId, float baseDamage, DamageType damageType,
            DomainEntityId attackerId = default)
        {
            if (_applyDamage == null)
                return Result.Failure("Combat system is not initialized.");

            return _applyDamage.Execute(targetId, baseDamage, damageType, attackerId);
        }

        public Result ApplyDamage(string targetIdValue, float baseDamage, DamageType damageType)
        {
            if (string.IsNullOrWhiteSpace(targetIdValue))
                return Result.Failure("Target id is required.");

            return ApplyDamage(new DomainEntityId(targetIdValue), baseDamage, damageType);
        }

        public Result ResetTarget(DomainEntityId targetId)
        {
            if (_targetAdapter == null)
                return Result.Failure("Combat target adapter is not initialized.");

            return _targetAdapter.ResetTarget(targetId)
                ? Result.Success()
                : Result.Failure($"Combat target not found: {targetId.Value}");
        }

        private sealed class NoOpCombatNetworkPort : ICombatNetworkCommandPort
        {
            public static NoOpCombatNetworkPort Instance { get; } = new NoOpCombatNetworkPort();

            private NoOpCombatNetworkPort() { }

            public void SendDamage(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId) { }
            public void SendDeath(DomainEntityId targetId, DomainEntityId killerId) { }
            public void SendRespawn(DomainEntityId targetId) { }
        }
    }
}
