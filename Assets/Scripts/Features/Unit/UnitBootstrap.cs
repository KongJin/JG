using Features.Combat;
using Features.Garage.Application.Ports;
using Features.Unit.Application.Ports;
using Features.Unit.Infrastructure;
using Features.Wave.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;

namespace Features.Unit
{
    /// <summary>
    /// Unit Feature의 Scene-level wiring.
    /// EventBus 주입, Infrastructure 어댑터 생성, UnitSetup 초기화.
    /// 비즈니스 로직 없음 — 조립만 담당.
    /// </summary>
    public sealed class UnitBootstrap : MonoBehaviour
    {
        [Header("Unit Catalog")]
        [Required, SerializeField]
        private ModuleCatalog _moduleCatalog;

        [Header("BattleEntity")]
        [SerializeField]
        private SummonPhotonAdapter _summonAdapter;

        private UnitSetup _setup;
        private BattleEntitySetup _battleEntitySetup;
        private UnitCompositionProvider _compositionProvider;

        public UnitSetup Setup => _setup;
        public BattleEntitySetup BattleEntitySetup => _battleEntitySetup;

        /// <summary>
        /// Unit Bootstrap의 ModuleCatalog 참조.
        /// 외부 Bootstrap이 Inspector 연결된 카탈로그에 접근할 때 사용.
        /// </summary>
        public ModuleCatalog Catalog => _moduleCatalog;

        /// <summary>
        /// IUnitCompositionPort 구현체.
        /// 외부 Bootstrap이 Garage에 주입할 때 사용.
        /// </summary>
        public IUnitCompositionPort CompositionPort => _compositionProvider;

        /// <summary>
        /// Unit Feature 초기화.
        /// 씬 Bootstrap(예: LobbyBootstrap)에서 EventBus를 주입하고 호출한다.
        /// </summary>
        public void Initialize(EventBus eventBus)
        {
            // Infrastructure 어댑터 생성
            _compositionProvider = new UnitCompositionProvider(_moduleCatalog);

            // Composition root 생성 및 초기화
            _setup = new UnitSetup();
            _setup.Initialize(eventBus, _compositionProvider);

            // BattleEntity Setup
            _battleEntitySetup = new BattleEntitySetup();
            // energyPort는 GameSceneRoot에서 주입 (Player Feature에서 제공)
        }

        /// <summary>
        /// BattleEntity Feature 초기화 (Energy 포트 + Combat + UnitPosition 주입).
        /// GameSceneRoot에서 호출.
        /// </summary>
        public void InitializeBattleEntity(
            EventBus eventBus,
            IUnitEnergyPort energyPort,
            CombatBootstrap combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery)
        {
            if (_summonAdapter == null)
            {
                Debug.LogError("[UnitBootstrap] SummonPhotonAdapter is missing. BattleEntity initialization cannot continue.", this);
                return;
            }

            _battleEntitySetup.Initialize(eventBus, energyPort, _summonAdapter, combatBootstrap, unitPositionQuery);
        }

        /// <summary>
        /// 씬 전환 시 정리.
        /// </summary>
        public void Cleanup()
        {
            _setup?.Cleanup();
            _battleEntitySetup?.Cleanup();
            _setup = null;
            _battleEntitySetup = null;
        }

        private void OnDestroy()
        {
            _setup?.Cleanup();
            _battleEntitySetup?.Cleanup();
        }
    }
}
