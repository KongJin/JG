using Features.Combat;
using Features.Garage.Application.Ports;
using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Infrastructure;
using Features.Wave.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Unit
{
    /// <summary>
    /// Unit Feature의 Scene-level wiring + Composition Root.
    /// EventBus 주입, Infrastructure 어댑터 생성, UseCase 조립.
    /// 비즈니스 로직 없음 — 조립만 담당.
    /// </summary>
    public sealed class UnitSetup : MonoBehaviour
    {
        [Header("Unit Catalog")]
        [Required, SerializeField]
        private ModuleCatalog _moduleCatalog;

        [Header("BattleEntity")]
        [SerializeField]
        private SummonPhotonAdapter _summonAdapter;

        private BattleEntitySetup _battleEntitySetup;
        private UnitCompositionProvider _compositionProvider;
        private DisposableScope _disposables;

        // Application UseCases
        public ComposeUnitUseCase ComposeUnit { get; private set; }

        public BattleEntitySetup BattleEntitySetup => _battleEntitySetup;

        /// <summary>
        /// Unit Setup의 ModuleCatalog 참조.
        /// 외부 Setup이 Inspector 연결된 카탈로그에 접근할 때 사용.
        /// </summary>
        public ModuleCatalog Catalog => _moduleCatalog;

        /// <summary>
        /// IUnitCompositionPort 구현체.
        /// 외부 Setup이 Garage에 주입할 때 사용.
        /// </summary>
        public IUnitCompositionPort CompositionPort => _compositionProvider;

        /// <summary>
        /// Unit Feature 초기화.
        /// 씬 Setup(예: LobbySetup)에서 EventBus를 주입하고 호출한다.
        /// </summary>
        public void Initialize(EventBus eventBus)
        {
            // Infrastructure 어댑터 생성
            _compositionProvider = new UnitCompositionProvider(_moduleCatalog);

            // Composition root — UseCase 조립
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            ComposeUnit = new ComposeUnitUseCase(_compositionProvider);

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
            CombatSetup combatSetup,
            UnitPositionQueryAdapter unitPositionQuery)
        {
            if (_summonAdapter == null)
            {
                Debug.LogError("[UnitSetup] SummonPhotonAdapter is missing. BattleEntity initialization cannot continue.", this);
                return;
            }

            _battleEntitySetup.Initialize(eventBus, energyPort, _summonAdapter, combatSetup, unitPositionQuery);
        }

        /// <summary>
        /// 씬 전환 시 정리.
        /// </summary>
        public void Cleanup()
        {
            _battleEntitySetup?.Cleanup();
            _disposables?.Dispose();
            _disposables = null;
            _battleEntitySetup = null;
        }

        private void OnDestroy()
        {
            _battleEntitySetup?.Cleanup();
            _disposables?.Dispose();
        }
    }
}
