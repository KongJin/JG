using Features.Combat;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Features.Wave.Infrastructure;
using Photon.Pun;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// Photon instantiate 시 BattleEntity 도메인 생성 + Combat/위치 등록.
    /// MonoBehaviourPunCallbacks + IPunInstantiateMagicCallback이므로 Infrastructure에 배치.
    /// </summary>
    public sealed class BattleEntityPrefabSetup : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
    {
        [Required, SerializeField] private MonoBehaviour _view;
        [Required, SerializeField] private BattleEntityPhotonController _photonController;
        [Required, SerializeField] private BattleEntityAttackDriver _attackDriver;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;

        private UnitPositionQueryAdapter _unitPositionQuery;
        private BattleEntity _battleEntity;
        private IBattleEntityViewPort _viewPort;
        private bool _initialized;

        public DomainEntityId BattleEntityId { get; private set; }
        public bool IsInitialized => _initialized;

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            // Instantiation data에서 unitId, ownerId, 초기HP 추출
            var data = info.photonView.InstantiationData;
// csharp-guardrails: allow-null-defense
            if (data != null && data.Length >= 2)
            {
                var unitIdStr = (string)data[0];
                var ownerIdStr = (string)data[1];
                var initialHp = data.Length >= 3 ? (float)data[2] : 100f;

                // GameSceneRoot 또는 UnitSetup를 통해 의존성 주입받아야 함
                // 현재는 정적 이벤트로 전달 (Composition Root가 Subscribe)
                _pendingUnitId = unitIdStr;
                _pendingOwnerId = new DomainEntityId(ownerIdStr);
                _pendingInitialHp = initialHp;
            }

        }

        private string _pendingUnitId;
        private DomainEntityId _pendingOwnerId;
        private float _pendingInitialHp;

        public void TryInitializeFromPending(
            EventBus eventBus,
            CombatSetup combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery,
            UnitSpec unitSpec)
        {
// csharp-guardrails: allow-null-defense
            if (_pendingUnitId == null || unitSpec == null) return;
            Initialize(eventBus, combatBootstrap, unitPositionQuery, unitSpec, _pendingOwnerId, _pendingInitialHp);
        }

        public void Initialize(
            EventBus eventBus,
            CombatSetup combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery,
            UnitSpec unitSpec,
            DomainEntityId ownerId)
        {
            // Owner 경로: instantiation data 무시하고 unitSpec 직접 사용
// csharp-guardrails: allow-null-defense
            if (_pendingUnitId != null)
            {
                // 이미 pending 데이터가 있으면 late-join 시나리오
                Initialize(eventBus, combatBootstrap, unitPositionQuery, unitSpec, _pendingOwnerId, _pendingInitialHp);
            }
            else
            {
                Initialize(eventBus, combatBootstrap, unitPositionQuery, unitSpec, ownerId, unitSpec.FinalHp);
            }
        }

        private void Initialize(
            EventBus eventBus,
            CombatSetup combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery,
            UnitSpec unitSpec,
            DomainEntityId ownerId,
            float initialHp)
        {
            if (_initialized) return;

            _unitPositionQuery = unitPositionQuery;
            _viewPort = _view as IBattleEntityViewPort;

// csharp-guardrails: allow-null-defense
            if (_viewPort == null)
            {
                Debug.LogError("[BattleEntityPrefabSetup] View must implement IBattleEntityViewPort.", this);
                return;
            }

            if (!BattleEntityNetworkId.TryBuild(unitSpec, photonView, out var battleEntityId))
            {
                Debug.LogError("[BattleEntityPrefabSetup] BattleEntity requires a PhotonView with an allocated ViewID before initialization.", this);
                return;
            }
            BattleEntityId = battleEntityId;

            // BattleEntity 도메인 생성 (실제 UnitSpec 사용, late-join 시 초기HP 보정)
            var spawnPos = new Float3(transform.position.x, transform.position.y, transform.position.z);
            _battleEntity = new BattleEntity(battleEntityId, unitSpec, ownerId, spawnPos, initialHp: initialHp);

            // EntityIdHolder 설정
            _entityIdHolder.Set(battleEntityId);

            // Combat target으로 등록
            combatBootstrap.RegisterTarget(battleEntityId, new BattleEntityCombatTargetProvider(_battleEntity));

            // Unit 위치 쿼리에 등록
            unitPositionQuery.RegisterUnit(transform);

            // View 초기화
            _viewPort.Initialize(eventBus, _battleEntity);

            // Photon controller 초기화
            _photonController.SetBattleEntity(_battleEntity, eventBus, eventBus, ownerId);

            // 공격 루프 초기화
            _attackDriver.Initialize(combatBootstrap, _battleEntity);

            _initialized = true;
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _attackDriver?.Clear();

            // csharp-guardrails: allow-null-defense
            if (_unitPositionQuery != null && transform != null)
                _unitPositionQuery.UnregisterUnit(transform);
        }
    }
}
