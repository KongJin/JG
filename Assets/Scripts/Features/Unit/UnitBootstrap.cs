using Features.Garage.Application.Ports;
using Features.Unit.Infrastructure;
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
        [Required, SerializeField]
        private ModuleCatalog _moduleCatalog;

        private UnitSetup _setup;
        private UnitCompositionProvider _compositionProvider;

        public UnitSetup Setup => _setup;

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
