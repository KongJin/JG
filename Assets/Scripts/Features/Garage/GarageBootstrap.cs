using Features.Garage.Application.Ports;
using Features.Garage.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;

namespace Features.Garage
{
    /// <summary>
    /// Garage Feature의 Scene-level wiring.
    /// EventBus 주입, Infrastructure 어댑터 생성, GarageSetup 초기화.
    /// 비즈니스 로직 없음 — 조립만 담당.
    /// </summary>
    public sealed class GarageBootstrap : MonoBehaviour
    {
        [Required, SerializeField]
        private ModuleCatalog _moduleCatalog;

        [Required, SerializeField]
        private GarageNetworkAdapter _networkAdapter;

        private GarageSetup _setup;
        private GarageJsonPersistence _persistence;
        private CompositionDataProvider _compositionDataProvider;
        private RosterValidationProvider _rosterValidationProvider;

        public GarageSetup Setup => _setup;

        /// <summary>
        /// Garage Feature 초기화.
        /// 씬 Bootstrap(예: LobbyBootstrap)에서 EventBus를 주입하고 호출한다.
        /// </summary>
        public void Initialize(EventBus eventBus)
        {
            // Infrastructure 어댑터 생성 (순수 C#만 new, Photon/MonoBehaviour는 Inspector 연결)
            _persistence = new GarageJsonPersistence();
            _compositionDataProvider = new CompositionDataProvider(_moduleCatalog);
            _rosterValidationProvider = new RosterValidationProvider(_moduleCatalog);

            // Composition root 생성 및 초기화
            _setup = new GarageSetup();
            _setup.Initialize(
                eventBus,
                _networkAdapter,
                _persistence,
                _compositionDataProvider,
                _rosterValidationProvider);
        }

        /// <summary>
        /// 씬 전환 시 정리.
        /// </summary>
        public void Cleanup()
        {
            _setup?.Cleanup();
            _setup = null;
        }

        private void OnDestroy()
        {
            _setup?.Cleanup();
        }
    }
}
