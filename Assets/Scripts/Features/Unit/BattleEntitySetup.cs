using Features.Combat;
using Features.Unit.Application;
using Features.Unit.Infrastructure;
using Features.Wave.Infrastructure;
using Shared.EventBus;
using Shared.Lifecycle;

namespace Features.Unit
{
    /// <summary>
    /// BattleEntity Feature의 Composition Root.
    /// 의존성 주입 책임: UseCase 조립, Port 연결.
    /// 순수 C# 클래스 — Bootstrap에서 생성/초기화.
    /// </summary>
    public sealed class BattleEntitySetup
    {
        private DisposableScope _disposables;

        // Application UseCases
        public SummonUnitUseCase SummonUnit { get; private set; }

        /// <summary>
        /// BattleEntity Feature 초기화.
        /// Bootstrap에서 호출. EventBus와 Infrastructure 어댑터를 주입받는다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IUnitEnergyPort energyPort,
            SummonPhotonAdapter summonAdapter,
            CombatBootstrap combatBootstrap,
            UnitPositionQueryAdapter unitPositionQuery)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            // SummonPhotonAdapter에 의존성 주입 (Infrastructure 직접 참조 — Composition Root 권한)
            summonAdapter.Initialize(eventBus, combatBootstrap, unitPositionQuery);

            // Application UseCases 조립
            SummonUnit = new SummonUnitUseCase(energyPort, summonAdapter, eventBus);

            // 사망 핸들러 — UnitDiedEvent 구독, Photon 파괴
            var deathHandler = new UnitDeathEventHandler(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, deathHandler));
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
