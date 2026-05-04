using Features.Account.Application.Ports;
using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;

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

        [SerializeField]
        private GarageSetBUitkPageController _setBUitkPageController;

        [SerializeField]
        private NovaPartVisualCatalog _novaPartVisualCatalog;

        [SerializeField]
        private NovaPartAlignmentCatalog _novaPartAlignmentCatalog;

        private IAccountDataPort _accountDataPort;
        private IEventPublisher _eventPublisher;
        private GaragePanelCatalog _panelCatalog;
        private readonly GaragePanelCatalogFactory _panelCatalogFactory = new();

        // Application UseCases
        public InitializeGarageUseCase InitializeGarage { get; private set; }
        public ComposeUnitUseCase ComposeUnit { get; private set; }
        public ValidateRosterUseCase ValidateRoster { get; private set; }
        public SaveRosterUseCase SaveRoster { get; private set; }
        public IGarageNetworkPort NetworkPort => _networkAdapter;
        public IAccountDataPort AccountDataPort => _accountDataPort;

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
            _eventPublisher = eventBus;
            var rosterValidationProvider = new RosterValidationProvider(unitCatalog);
            _panelCatalog = _panelCatalogFactory.Build(
                unitCatalog,
                _novaPartVisualCatalog,
                _novaPartAlignmentCatalog);

            // Composition root — UseCase 조립
            var localPersistence = new GarageJsonPersistence();
            var operationRecordStore = new OperationRecordJsonStore();
            var recentOperations = operationRecordStore.Load();

            ComposeUnit = new ComposeUnitUseCase(compositionPort);
            InitializeGarage = new InitializeGarageUseCase(
                localPersistence,
                _networkAdapter,
                accountDataPort as ICloudGarageLoadPort,
                new GarageRosterLegacyIdMigrator());
            ValidateRoster = new ValidateRosterUseCase(rosterValidationProvider);

            SaveRoster = new SaveRosterUseCase(
                accountDataPort as ICloudGaragePort,
                localPersistence,
                _networkAdapter);

            InitializeControllers(recentOperations);

            _ = GarageOperationRecordSyncFlow.SyncAsync(
                operationRecordStore,
                accountDataPort as IOperationRecordCloudPort,
                result => InitializeControllers(result));
        }

        private void InitializeControllers(RecentOperationRecords recentOperations)
        {
            if (_setBUitkPageController != null)
            {
                _setBUitkPageController.Initialize(
                    InitializeGarage,
                    ComposeUnit,
                    ValidateRoster,
                    SaveRoster,
                    _eventPublisher,
                    _panelCatalog,
                    recentOperations);
            }
        }
    }
}
