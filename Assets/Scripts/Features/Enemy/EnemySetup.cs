using Features.Combat;
using Features.Enemy.Application;
using Features.Enemy.Infrastructure;
using Features.Enemy.Presentation;
using Features.Wave.Application.Ports;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy
{
    public sealed class EnemySetup : MonoBehaviour
    {
        [SerializeField] private EnemyData _defaultData;
        [SerializeField] private EnemyNetworkAdapter _networkAdapter;
        [SerializeField] private EnemyAiAdapter _aiAdapter;
        [SerializeField] private EnemyView _view;
        [SerializeField] private EnemyContactDamageDetector _contactDetector;
        [SerializeField] private EntityIdHolder _entityIdHolder;

        private EventBus _eventBus;
        private EnemyDamageEventHandler _damageHandler;

        public DomainEntityId EnemyId { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            IPlayerPositionQuery playerQuery)
        {
            Initialize(eventBus, combatBootstrap, _defaultData, playerQuery);
        }

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            EnemyData data,
            IPlayerPositionQuery playerQuery)
        {
            if (IsInitialized)
                return;

            if (eventBus == null)
            {
                Debug.LogError("[EnemySetup] EventBus is missing.", this);
                return;
            }

            if (combatBootstrap == null)
            {
                Debug.LogError("[EnemySetup] CombatBootstrap is missing.", this);
                return;
            }

            if (_networkAdapter == null)
            {
                Debug.LogError("[EnemySetup] EnemyNetworkAdapter is missing.", this);
                return;
            }

            if (data == null)
            {
                Debug.LogError("[EnemySetup] EnemyData is missing.", this);
                return;
            }

            _eventBus = eventBus;
            EnemyId = _networkAdapter.StableEnemyId;
            var spec = data.ToSpec();

            var enemy = new SpawnEnemyUseCase(eventBus).Execute(EnemyId, spec);
            combatBootstrap.RegisterTarget(EnemyId, new EnemyCombatTargetProvider(enemy));
            _damageHandler = new EnemyDamageEventHandler(enemy, EnemyId, eventBus, eventBus);

            if (_entityIdHolder != null)
                _entityIdHolder.Set(EnemyId);

            if (PhotonNetwork.IsMasterClient)
            {
                if (_aiAdapter != null)
                    _aiAdapter.Initialize(spec.MoveSpeed, playerQuery);
                else
                    Debug.LogError("[EnemySetup] EnemyAiAdapter is missing.", this);

                if (_contactDetector != null)
                    _contactDetector.Initialize(combatBootstrap, EnemyId, spec.ContactDamage, spec.ContactCooldown);
                else
                    Debug.LogError("[EnemySetup] EnemyContactDamageDetector is missing.", this);
            }
            else
            {
                if (_aiAdapter != null) _aiAdapter.enabled = false;
                if (_contactDetector != null) _contactDetector.enabled = false;
            }

            if (_view != null)
                _view.Initialize(eventBus, EnemyId);
            else
                Debug.LogError("[EnemySetup] EnemyView is missing.", this);

            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (_damageHandler != null)
                _eventBus?.UnsubscribeAll(_damageHandler);
        }
    }
}
