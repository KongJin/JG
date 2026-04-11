using Features.Garage.Application.Ports;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Unit.Infrastructure;
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
        private GarageNetworkAdapter _networkAdapter;

        [SerializeField]
        private GaragePanelView _panelView;

        private GarageSetup _setup;
        private GarageJsonPersistence _persistence;
        private RosterValidationProvider _rosterValidationProvider;

        public GarageSetup Setup => _setup;

        /// <summary>
        /// Garage Feature 초기화.
        /// 씬 Bootstrap(예: LobbyBootstrap)에서 EventBus와 Unit 조합 포트를 주입하고 호출한다.
        /// </summary>
        public void Initialize(
            EventBus eventBus,
            IUnitCompositionPort compositionPort,
            ModuleCatalog unitCatalog)
        {
            // Infrastructure 어댑터 생성 (순수 C#만 new, Photon/MonoBehaviour는 Inspector 연결)
            _persistence = new GarageJsonPersistence();
            _rosterValidationProvider = new RosterValidationProvider(unitCatalog);

            // Composition root 생성 및 초기화
            _setup = new GarageSetup();
            _setup.Initialize(
                eventBus,
                _networkAdapter,
                _persistence,
                compositionPort,
                _rosterValidationProvider);

            if (_panelView != null)
                _panelView.Initialize(eventBus, _setup, BuildPanelCatalog(unitCatalog));
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
            _setup?.Cleanup();
            _setup = null;
        }

        private void OnDestroy()
        {
            _setup?.Cleanup();
        }
    }
}
