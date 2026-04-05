using System;
using Shared.Attributes;
using Features.Combat;
using Features.Enemy.Application;
using Features.Enemy.Application.Events;
using Features.Enemy.Application.Ports;
using Features.Enemy.Infrastructure;
using Features.Enemy.Presentation;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Enemy
{
    public sealed class EnemySetup : MonoBehaviour, IPunInstantiateMagicCallback
    {
        public static event System.Action<EnemySetup> EnemyArrived;

        [Required, SerializeField] private EnemyNetworkAdapter _networkAdapter;
        [Required, SerializeField] private EnemyAiAdapter _aiAdapter;
        [Required, SerializeField] private EnemyView _view;
        [SerializeField] private GameObject _healthBarPrefab;
        [Required, SerializeField] private EnemyContactDamageDetector _contactDetector;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;

        private EventBus _eventBus;
        private EnemyDamageEventHandler _damageHandler;

        public DomainEntityId EnemyId { get; private set; }
        public bool IsInitialized { get; private set; }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            EnemyArrived?.Invoke(this);
        }

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            IPlayerPositionQuery playerQuery,
            ICoreObjectiveQuery coreQuery)
        {
            Initialize(eventBus, combatBootstrap, ResolveDataFromInstantiation(), playerQuery, coreQuery);
        }

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
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
                Debug.LogError("[EnemySetup] CombatBootstrap is missing.", this);
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

            _entityIdHolder.Set(EnemyId);

            if (PhotonNetwork.IsMasterClient)
            {
                _aiAdapter.Initialize(spec, playerQuery, coreQuery);
                _contactDetector.Initialize(combatBootstrap, EnemyId, spec.ContactDamage, spec.ContactCooldown);
            }
            else
            {
                _aiAdapter.enabled = false;
                _contactDetector.enabled = false;
            }

            _view.Initialize(eventBus, EnemyId);

            if (_healthBarPrefab != null)
            {
                var hbGo = Instantiate(_healthBarPrefab, transform);
                hbGo.transform.localPosition = new Vector3(0f, 2f, 0f);
                var hbView = hbGo.GetComponent<EnemyHealthBarView>();
                if (hbView != null)
                    hbView.Initialize(eventBus, EnemyId, spec.MaxHp);
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
            if (_damageHandler != null)
                _eventBus?.UnsubscribeAll(_damageHandler);
            _eventBus?.UnsubscribeAll(this);
        }

        private EnemyData ResolveDataFromInstantiation()
        {
            var pv = GetComponent<PhotonView>();
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
