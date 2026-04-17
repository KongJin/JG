using Features.Account.Application.Ports;
using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Garage
{
    /// <summary>
    /// Garage Feature의 Scene-level wiring + Composition Root.
    /// EventBus 주입, Infrastructure 어댑터 생성, UseCase 조립.
    /// 비즈니스 로직 없음 — 조립만 담당.
    /// </summary>
    public sealed class GarageSetup : MonoBehaviour
    {
        [Required, SerializeField]
        private GarageNetworkAdapter _networkAdapter;

        [FormerlySerializedAs("_panelView")]
        [SerializeField]
        private GaragePageController _pageController;

        private IAccountDataPort _accountDataPort;
        private RosterValidationProvider _rosterValidationProvider;
        private DisposableScope _disposables;

        // Application UseCases
        public InitializeGarageUseCase InitializeGarage { get; private set; }
        public ComposeUnitUseCase ComposeUnit { get; private set; }
        public ValidateRosterUseCase ValidateRoster { get; private set; }
        public SaveRosterUseCase SaveRoster { get; private set; }
        public IGarageNetworkPort NetworkPort => _networkAdapter;
        public IAccountDataPort AccountDataPort => _accountDataPort;

        public GarageSetup Setup => this;

        /// <summary>
        /// Garage Feature 초기화.
        /// 씬 Setup(예: LobbySetup)에서 EventBus와 Unit 조합 포트를 주입하고 호출한다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IUnitCompositionPort compositionPort,
            ModuleCatalog unitCatalog,
            IAccountDataPort accountDataPort = null)
        {
            _accountDataPort = accountDataPort;
            _rosterValidationProvider = new RosterValidationProvider(unitCatalog);

            // Composition root — UseCase 조립
            _disposables?.Dispose();
            _disposables = new DisposableScope();
            var localPersistence = new GarageJsonPersistence();

            ComposeUnit = new ComposeUnitUseCase(compositionPort);
            InitializeGarage = new InitializeGarageUseCase(
                localPersistence,
                accountDataPort as InitializeGarageUseCase.ICloudGarageLoadPort,
                eventBus);
            ValidateRoster = new ValidateRosterUseCase(_rosterValidationProvider);

            if (accountDataPort != null)
            {
                SaveRoster = new SaveRosterUseCase(
                    accountDataPort as SaveRosterUseCase.ICloudGaragePort,
                    localPersistence,
                    _networkAdapter,
                    eventBus);
            }
            else
            {
                // 폴백: AccountSetup 미연결 시 로컬 저장만 (GameScene 등)
                SaveRoster = new SaveRosterUseCase(
                    null,
                    localPersistence,
                    _networkAdapter,
                    eventBus);
            }

            if (_pageController != null)
                _pageController.Initialize(this, BuildPanelCatalog(unitCatalog));
        }

        private static GaragePanelCatalog BuildPanelCatalog(ModuleCatalog unitCatalog)
        {
            var frames = new System.Collections.Generic.List<GaragePanelCatalog.FrameOption>();
            for (int i = 0; i < unitCatalog.UnitFrames.Count; i++)
            {
                var frame = unitCatalog.UnitFrames[i];
                frames.Add(new GaragePanelCatalog.FrameOption
                {
                    Id = frame.FrameId,
                    DisplayName = frame.DisplayName,
                    BaseHp = frame.BaseHp,
                    BaseAttackSpeed = frame.BaseAttackSpeed
                });
            }

            var firepower = new System.Collections.Generic.List<GaragePanelCatalog.FirepowerOption>();
            for (int i = 0; i < unitCatalog.FirepowerModules.Count; i++)
            {
                var module = unitCatalog.FirepowerModules[i];
                firepower.Add(new GaragePanelCatalog.FirepowerOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    AttackDamage = module.AttackDamage,
                    AttackSpeed = module.AttackSpeed,
                    Range = module.Range
                });
            }

            var mobility = new System.Collections.Generic.List<GaragePanelCatalog.MobilityOption>();
            for (int i = 0; i < unitCatalog.MobilityModules.Count; i++)
            {
                var module = unitCatalog.MobilityModules[i];
                mobility.Add(new GaragePanelCatalog.MobilityOption
                {
                    Id = module.ModuleId,
                    DisplayName = module.DisplayName,
                    HpBonus = module.HpBonus,
                    MoveRange = module.MoveRange,
                    AnchorRange = module.AnchorRange
                });
            }

            return new GaragePanelCatalog(frames, firepower, mobility);
        }

        /// <summary>
        /// 씬 전환 시 정리.
        /// </summary>
        public void Cleanup()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}
