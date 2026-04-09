using Features.Combat;
using Features.Enemy.Application.Ports;
using Features.Unit.Presentation;
using Features.Wave.Application;
using Features.Wave.Domain;
using Features.Wave.Infrastructure;
using Shared.Attributes;
using Shared.Kernel;
using UnityEngine;

namespace Features.Wave
{
    public sealed class CoreObjectiveBootstrap : MonoBehaviour, ICoreObjectiveQuery
    {
        [Required, SerializeField] private Transform _coreAnchor;
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;
        [SerializeField] private float _maxHp = 1500f;
        [SerializeField] private float _defense;

        [Header("Placement Area")]
        [Tooltip("Core로부터 Z축 방향 오프셋 (양수 = 적 진영 방향).")]
        [SerializeField] private float _placementForwardOffset = 0f;
        [Tooltip("배치 영역 너비 (X 방향).")]
        [SerializeField] private float _placementWidth = 8f;
        [Tooltip("배치 영역 깊이 (Z 방향).")]
        [SerializeField] private float _placementDepth = 5f;
        [Tooltip("배치 영역 시각화 View (Inspector에서 필수 연결).")]
        [Required, SerializeField] private PlacementAreaView _placementAreaView;

        private PlacementArea _placementArea;
        private bool _registered;

        public DomainEntityId CoreId => ObjectiveCoreIds.Default;
        public float CoreMaxHp => _maxHp;

        /// <summary>
        /// 배치 영역을 반환한다. 아직 초기화되지 않았으면 null.
        /// </summary>
        public PlacementArea PlacementArea => _placementArea;

        /// <summary>
        /// 배치 영역 시각화 View를 반환한다.
        /// </summary>
        public PlacementAreaView PlacementAreaView => _placementAreaView;

        /// <summary>
        /// 배치 영역을 초기화한다 (Core 위치 기반).
        /// </summary>
        public void InitializePlacementArea()
        {
            if (_placementArea != null)
                return;

            _placementArea = new PlacementArea(
                width: _placementWidth,
                depth: _placementDepth,
                forwardOffset: _placementForwardOffset
            );

            if (TryGetCoreWorldPosition(out float x, out float y, out float z))
            {
                _placementArea.SetCorePosition(new Vector3(x, y, z));
            }
            else
            {
                Debug.LogWarning("[CoreObjectiveBootstrap] Core position not available for PlacementArea initialization.");
            }

            // 시각화 초기화
            InitializePlacementAreaView();
        }

        private void InitializePlacementAreaView()
        {
            if (_placementArea == null) return;
            if (_placementAreaView == null) return; // Inspector에서 연결 필요

            var validMat = PlacementAreaMaterialFactory.CreateValidMaterial();
            _placementAreaView.Initialize(_placementArea, validMat);
        }

        public bool TryGetCoreWorldPosition(out float x, out float y, out float z)
        {
            if (_coreAnchor == null)
            {
                x = y = z = 0f;
                return false;
            }

            var p = _coreAnchor.position;
            x = p.x;
            y = p.y;
            z = p.z;
            return true;
        }

        public void RegisterCombatTarget(CombatBootstrap combatBootstrap)
        {
            if (_registered)
                return;

            if (combatBootstrap == null)
            {
                Debug.LogError("[CoreObjectiveBootstrap] CombatBootstrap is missing.", this);
                return;
            }

            var id = ObjectiveCoreIds.Default;
            _entityIdHolder.Set(id);
            var domain = new SpawnObjectiveCoreUseCase().Execute(id, _maxHp, _defense);
            combatBootstrap.RegisterTarget(id, new ObjectiveCoreCombatTargetProvider(domain));
            _registered = true;
        }
    }
}
