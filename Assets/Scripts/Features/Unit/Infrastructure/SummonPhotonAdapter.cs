using Features.Combat;
using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using Features.Wave.Infrastructure;
using Photon.Pun;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;
using UnitSpec = Features.Unit.Domain.Unit;

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

        private EventBus _eventBus;
        private CombatSetup _combatSetup;
        private UnitPositionQueryAdapter _unitPositionQuery;

        /// <summary>
        /// 소환기 초기화. ISummonExecutionPort는 순수 생성만 담당하므로
        /// Combat/위치 등록 의존성은 여기서 직접 주입받는다.
        /// </summary>
        public void Initialize(EventBus eventBus, CombatSetup combatBootstrap, UnitPositionQueryAdapter unitPositionQuery)
        {
            _eventBus = eventBus;
            _combatSetup = combatBootstrap;
            _unitPositionQuery = unitPositionQuery;
        }

        /// <summary>
        /// BattleEntity 생성 (Owner 기반).
        /// </summary>
        public DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
        {
            var spawnPos = new Vector3(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);

            // Photon을 통한 소환 (Owner가 인스턴스화)
            // Instantiation data에 unitId, ownerId, 초기HP 전달 (late-joiner도 정확한 상태로 스폰)
            var spawnedGo = PhotonNetwork.Instantiate(
                _battleEntityPrefab.name,
                spawnPos,
                Quaternion.identity,
                0,
                new object[] { unitSpec.Id.Value, ownerId.Value, (float)unitSpec.FinalHp });

            // 부모 설정
            if (_spawnParent != null)
            {
                spawnedGo.transform.SetParent(_spawnParent, worldPositionStays: true);
            }

            // BattleEntityPrefabSetup 초기화
            var prefabSetup = ComponentAccess.Get<BattleEntityPrefabSetup>(spawnedGo);
            if (prefabSetup != null)
            {
                prefabSetup.Initialize(_eventBus, _combatSetup, _unitPositionQuery, unitSpec, ownerId);
            }
            else
            {
                Debug.LogError("[SummonPhotonAdapter] BattleEntityPrefabSetup is missing on the prefab.", this);
            }

            var view = ComponentAccess.Get<PhotonView>(spawnedGo);
            var fallbackInstanceId = spawnedGo != null ? spawnedGo.GetInstanceID() : 0;
            return BattleEntityNetworkId.Build(unitSpec, view, fallbackInstanceId);
        }
    }

    internal static class BattleEntityNetworkId
    {
        public static DomainEntityId Build(UnitSpec unitSpec, PhotonView photonView, int fallbackInstanceId)
        {
            if (photonView != null && photonView.ViewID > 0)
                return new DomainEntityId($"battle-{unitSpec.Id.Value}-view-{photonView.ViewID}");

            return new DomainEntityId($"battle-{unitSpec.Id.Value}-{fallbackInstanceId}");
        }
    }
}
