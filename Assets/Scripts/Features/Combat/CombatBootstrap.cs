using Shared.Attributes;
using Features.Combat.Application;
using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Features.Combat.Infrastructure;
using Features.Combat.Presentation;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Combat
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [Required, SerializeField]
        private CombatTargetAdapter _targetAdapter;

        [Required, SerializeField]
        private CombatTargetView[] _targetViews = new CombatTargetView[0];

        [SerializeField]
        private FriendlyFireFeedbackView _friendlyFireFeedbackView;

        private ApplyDamageUseCase _applyDamage;
        private CombatNetworkEventHandler _eventHandler;
        private CombatReplicationEventHandler _replicationEventHandler;
        private ZoneDamageHandler _zoneDamageHandler;
        private EventBus _eventBus;
        private DisposableScope _disposables = new DisposableScope();

        public void Initialize(
            EventBus eventBus,
            ICombatNetworkCommandPort networkPort,
            DomainEntityId localAuthorityId,
            IEntityAffiliationPort affiliation,
            IFriendlyFireScalingPort ffScaling = null
        )
        {
            if (eventBus == null)
            {
                Debug.LogError("[CombatBootstrap] EventBus is not provided.", this);
                return;
            }

            _eventBus = eventBus;
            _disposables.Dispose();
            _disposables = new DisposableScope();

            _targetAdapter.Initialize();
            _applyDamage = new ApplyDamageUseCase(
                _targetAdapter, _eventBus,
                networkPort ?? NoOpCombatNetworkPort.Instance,
                affiliation,
                ffScaling
            );
            _eventHandler = new CombatNetworkEventHandler(_applyDamage, _eventBus, localAuthorityId);
            _replicationEventHandler = new CombatReplicationEventHandler(_applyDamage, _eventBus);
            _zoneDamageHandler = new ZoneDamageHandler(_applyDamage, _eventBus, localAuthorityId);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, _eventHandler));
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, _replicationEventHandler));
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, _zoneDamageHandler));

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

            if (_friendlyFireFeedbackView != null)
                _friendlyFireFeedbackView.Initialize(_eventBus, _eventBus, localAuthorityId);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        public void RegisterTarget(DomainEntityId targetId, ICombatTargetProvider provider)
        {
            _targetAdapter.Register(targetId, provider);
        }

        public Result ApplyDamage(DomainEntityId targetId, float baseDamage, DamageType damageType,
            DomainEntityId attackerId = default, float allyDamageScale = 1f)
        {
            if (_applyDamage == null)
                return Result.Failure("Combat system is not initialized.");

            return _applyDamage.Execute(targetId, baseDamage, damageType, attackerId, allyDamageScale);
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
