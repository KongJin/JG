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
            string unitIdStr,
            DomainEntityId ownerId)
        {
            if (_initialized) return;

            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _unitPositionQuery = unitPositionQuery;

            // UnitSpec 조회 (UnitCatalog에서)
            // TODO: UnitSpec을 인스턴스 데이터나 공유 레지스트리에서 조회
            // 현재는 임시로 빈 spec 사용 — Phase 3에서 Catalog 연동 필요
            var battleEntityId = new DomainEntityId($"battle-{unitIdStr}-{gameObject.GetInstanceID()}");
            BattleEntityId = battleEntityId;

            // 임시 UnitSpec (실제로는 Catalog에서 가져와야 함)
            var tempSpec = CreateTemporaryUnitSpec(unitIdStr);

            // BattleEntity 도메인 생성
            var spawnPos = new Float3(transform.position.x, transform.position.y, transform.position.z);
            _battleEntity = new BattleEntity(battleEntityId, tempSpec, ownerId, spawnPos);

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
            eventBus.Publish(new Application.Events.UnitSummonCompletedEvent(ownerId, battleEntityId, tempSpec));

            _initialized = true;
        }

        /// <summary>
        /// 임시 UnitSpec 생성.
        /// TODO: UnitCatalog에서 실제 스펙을 조회하도록 변경.
        /// </summary>
        private static Unit CreateTemporaryUnitSpec(string unitIdStr)
        {
            return new Unit(
                id: new DomainEntityId(unitIdStr),
                frameId: "temp-frame",
                firepowerModuleId: "temp-firepower",
                mobilityModuleId: "temp-mobility",
                passiveTraitId: null,
                passiveTraitCostBonus: 0,
                finalHp: 100f,
                finalAttackDamage: 10f,
                finalAttackSpeed: 1f,
                finalRange: 5f,
                finalMoveRange: 3f,
                finalAnchorRange: 5f,
                summonCost: 10);
        }

        private void OnDestroy()
        {
            if (_unitPositionQuery != null && transform != null)
                _unitPositionQuery.UnregisterUnit(transform);
        }
    }
}
