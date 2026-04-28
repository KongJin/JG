using System;
using Shared.Attributes;
using Features.Combat;
using Features.Combat.Infrastructure;
using Features.Enemy.Application;
using Features.Enemy.Application.Events;
using Features.Enemy.Application.Ports;
using Features.Enemy.Infrastructure;
using Features.Enemy.Presentation;
using Features.Player;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;

namespace Features.Enemy
{
    public sealed class EnemySetup : MonoBehaviour, IPunInstantiateMagicCallback
    {
        [Required, SerializeField] private EnemyNetworkAdapter _networkAdapter;
        [Required, SerializeField] private EnemyAiAdapter _aiAdapter;
        [Required, SerializeField] private EnemyView _view;
        [SerializeField] private GameObject _healthBarPrefab;
        [Required, SerializeField] private EnemyContactDamageDetector _contactDetector;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;

        private EventBus _eventBus;

        public DomainEntityId EnemyId { get; private set; }
        public bool IsInitialized { get; private set; }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            GameSceneRuntimeSpawnRegistrar.NotifyEnemyArrived(this);
        }

        public void Initialize(
            EventBus eventBus,
            CombatSetup combatBootstrap,
            IPlayerPositionQuery playerQuery,
            ICoreObjectiveQuery coreQuery)
        {
            Initialize(eventBus, combatBootstrap, ResolveDataFromInstantiation(), playerQuery, coreQuery);
        }

        public void Initialize(
            EventBus eventBus,
            CombatSetup combatBootstrap,
            EnemyData data,
            IPlayerPositionQuery playerQuery,
            ICoreObjectiveQuery coreQuery)
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
                Debug.LogError("[EnemySetup] CombatSetup is missing.", this);
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
            combatBootstrap.RegisterTarget(EnemyId, new EnemyCombatTargetProvider(enemy, EnemyId, eventBus));

            _entityIdHolder.Set(EnemyId);

            if (PhotonNetwork.IsMasterClient)
            {
                _aiAdapter.Initialize(spec, playerQuery, coreQuery);
                _contactDetector.Initialize(
                    new EnemyContactDamagePortAdapter(combatBootstrap),
                    EnemyId,
                    spec.ContactDamage,
                    spec.ContactCooldown
                );
            }
            else
            {
                _aiAdapter.enabled = false;
                _contactDetector.enabled = false;
            }

            _view.Initialize(eventBus, EnemyId);

            if (_healthBarPrefab != null)
            {
                var hbView = ComponentAccess.InstantiateComponent<EnemyHealthBarView>(
                    _healthBarPrefab,
                    transform);
                if (hbView != null)
                {
                    hbView.transform.localPosition = new Vector3(0f, 2f, 0f);
                    hbView.Initialize(eventBus, EnemyId, spec.MaxHp);
                }
            }

            if (PhotonNetwork.IsMasterClient)
                _eventBus.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));

            IsInitialized = true;
        }

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            if (!EnemyId.Equals(e.EnemyId)) return;
            PhotonNetwork.Destroy(gameObject);
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private EnemyData ResolveDataFromInstantiation()
        {
            var pv = ComponentAccess.Get<PhotonView>(gameObject);
            if (pv != null &&
                pv.InstantiationData != null &&
                pv.InstantiationData.Length > 0 &&
                pv.InstantiationData[0] is string path &&
                !string.IsNullOrWhiteSpace(path))
            {
                var loaded = Resources.Load<EnemyData>(path);
                if (loaded != null)
                    return loaded;
            }

            return LoadDefaultData();
        }

        private static EnemyData LoadDefaultData()
        {
            return Resources.Load<EnemyData>("Enemy/BasicEnemy");
        }
    }
}
