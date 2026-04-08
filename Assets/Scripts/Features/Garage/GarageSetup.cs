using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Unit.Application;
using Shared.EventBus;
using Shared.Lifecycle;

namespace Features.Garage
{
    /// <summary>
    /// Garage Feature의 Composition Root.
    /// 의존성 주입 책임: UseCase 조립, Port 연결.
    /// 순수 C# 클래스 — Bootstrap에서 생성/초기화.
    /// </summary>
    public sealed class GarageSetup
    {
        private DisposableScope _disposables;
        private IGarageNetworkPort _networkPort;

        // Application UseCases
        public InitializeGarageUseCase InitializeGarage { get; private set; }
        public ComposeUnitUseCase ComposeUnit { get; private set; }
        public ValidateRosterUseCase ValidateRoster { get; private set; }
        public SaveRosterUseCase SaveRoster { get; private set; }
        public IGarageNetworkPort NetworkPort => _networkPort;

        /// <summary>
        /// Garage Feature 초기화.
        /// Bootstrap에서 호출. EventBus와 Infrastructure 어댑터를 주입받는다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IGarageNetworkPort networkPort,
            IGaragePersistencePort persistencePort,
            IUnitCompositionPort compositionPort,
            ValidateRosterUseCase.IRosterValidationProvider rosterValidationProvider)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();
            _networkPort = networkPort;

            // Application UseCases 조립
            ComposeUnit = new ComposeUnitUseCase(compositionPort);
            InitializeGarage = new InitializeGarageUseCase(persistencePort, eventBus);
            ValidateRoster = new ValidateRosterUseCase(rosterValidationProvider);
            SaveRoster = new SaveRosterUseCase(persistencePort, networkPort, eventBus);
        }

        /// <summary>
        /// 정리. 씬 전환 시 호출.
        /// </summary>
        public void Cleanup()
        {
            _disposables?.Dispose();
            _disposables = null;
        }
    }
}
