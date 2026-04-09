using Features.Combat;
using Features.Unit.Application.Events;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using Features.Unit.Presentation;
using Features.Wave.Infrastructure;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;

namespace Features.Unit
{
    public sealed class BattleEntityPrefabSetup : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
    {
        public static event System.Action<BattleEntityPrefabSetup> BattleEntityArrived;

        [Required, SerializeField] private BattleEntityView _view;
        [Required, SerializeField] private BattleEntityPhotonController _photonController;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;

        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private UnitPositionQueryAdapter _unitPositionQuery;
        private BattleEntity _battleEntity;
        private bool _initialized;

        public DomainEntityId BattleEntityId { get; private set; }
        public bool IsInitialized => _initialized;

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            // Signal arrival for non-master fallback (same pattern as EnemySetup)
            BattleEntityArrived?.Invoke(this);
        }

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery,
            Unit unitSpec,
            DomainEntityId ownerId)
        {
            if (_initialized) return;

            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _unitPositionQuery = unitPositionQuery;

            var battleEntityId = new DomainEntityId($"battle-{unitSpec.Id.Value}-{gameObject.GetInstanceID()}");
            BattleEntityId = battleEntityId;

            // BattleEntity 도메인 생성 (실제 UnitSpec 사용)
            var spawnPos = new Float3(transform.position.x, transform.position.y, transform.position.z);
            _battleEntity = new BattleEntity(battleEntityId, unitSpec, ownerId, spawnPos);

            // EntityIdHolder 설정
            _entityIdHolder.Set(battleEntityId);

            // Combat target으로 등록
            combatBootstrap.RegisterTarget(battleEntityId, new BattleEntityCombatTargetProvider(_battleEntity));

            // Unit 위치 쿼리에 등록
            unitPositionQuery.RegisterUnit(transform);

            // View 초기화
            _view.Initialize(eventBus, _battleEntity);

            // Photon controller 초기화
            _photonController.SetBattleEntity(_battleEntity, eventBus, eventBus, ownerId);

            // 소환 완료 이벤트 발행
            eventBus.Publish(new Application.Events.UnitSummonCompletedEvent(ownerId, battleEntityId, unitSpec));

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_unitPositionQuery != null && transform != null)
                _unitPositionQuery.UnregisterUnit(transform);
        }
    }
}
