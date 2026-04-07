using Features.Garage.Application.Ports;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Shared.EventBus;
using Shared.Lifecycle;

namespace Features.Unit
{
    /// <summary>
    /// Unit Feature의 Composition Root.
    /// 의존성 주입 책임: UseCase 조립, Port 연결.
    /// 순수 C# 클래스 — Bootstrap에서 생성/초기화.
    /// </summary>
    public sealed class UnitSetup
    {
        private DisposableScope _disposables;

        // Application UseCases
        public ComposeUnitUseCase ComposeUnit { get; private set; }

        /// <summary>
        /// Unit Feature 초기화.
        /// Bootstrap에서 호출. EventBus와 Infrastructure 어댑터를 주입받는다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IUnitCompositionPort compositionPort)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            // Application UseCases 조립
            ComposeUnit = new ComposeUnitUseCase(compositionPort);
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
