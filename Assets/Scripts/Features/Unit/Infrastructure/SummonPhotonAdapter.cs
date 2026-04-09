using Features.Combat;
using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Features.Unit.Presentation;
using Features.Wave.Infrastructure;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// Photon을 통한 BattleEntity 소환 구현.
    /// ISummonExecutionPort의 Infrastructure 어댑터.
    /// </summary>
    public sealed class SummonPhotonAdapter : MonoBehaviour, ISummonExecutionPort
    {
        [Header("BattleEntity Prefab")]
        [SerializeField] private GameObject _battleEntityPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform _spawnParent;

        private IEventPublisher _eventBus;
        private CombatBootstrap _combatBootstrap;
        private UnitPositionQueryAdapter _unitPositionQuery;

        /// <summary>
        /// 소환기 초기화. ISummonExecutionPort는 순수 생성만 담당하므로
        /// Combat/위치 등록 의존성은 여기서 직접 주입받는다.
        /// </summary>
        public void Initialize(IEventPublisher eventBus, CombatBootstrap combatBootstrap, UnitPositionQueryAdapter unitPositionQuery)
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _unitPositionQuery = unitPositionQuery;
        }

        /// <summary>
        /// BattleEntity 생성 (Owner 기반).
        /// </summary>
        public DomainEntityId SpawnBattleEntity(Unit unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
        {
            var spawnPos = new Vector3(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);

            // Photon을 통한 소환 (Owner가 인스턴스화)
            var spawnedGo = PhotonNetwork.Instantiate(
                _battleEntityPrefab.name,
                spawnPos,
                Quaternion.identity,
                0,
                new object[] { unitSpec.Id.Value, ownerId.Value });

            // 부모 설정
            if (_spawnParent != null)
            {
                spawnedGo.transform.SetParent(_spawnParent, worldPositionStays: true);
            }

            // BattleEntityPrefabSetup 초기화
            var prefabSetup = spawnedGo.GetComponent<BattleEntityPrefabSetup>();
            if (prefabSetup != null)
            {
                prefabSetup.Initialize(_eventBus, _combatBootstrap, _unitPositionQuery, unitSpec, ownerId);
            }
            else
            {
                Debug.LogError("[SummonPhotonAdapter] BattleEntityPrefabSetup is missing on the prefab.", this);
            }

            // 소환 완료 이벤트 예약
            // Note: 실제 이벤트는 BattleEntityPrefabSetup.Initialize 내에서 발행
            // 여기서는 ID만 반환

            return new DomainEntityId($"battle-{unitSpec.Id.Value}-{spawnedGo.GetInstanceID()}");
        }
    }
}
