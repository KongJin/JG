using Features.Garage.Application.Ports;
using Features.Unit.Domain;

namespace Features.Unit.Infrastructure
{
    /// <summary>
    /// IUnitCompositionPort 구현.
    /// ModuleCatalog를 통해 SO 데이터 조회.
    /// </summary>
    public sealed class UnitCompositionProvider : IUnitCompositionPort
    {
        private readonly ModuleCatalog _catalog;

        public UnitCompositionProvider(ModuleCatalog catalog)
        {
            _catalog = catalog;
        }

        public ModuleStats GetFrameBaseStats(string frameId)
        {
            var frame = _catalog.GetUnitFrame(frameId);
            if (frame == null) return default;

            return new ModuleStats(
                hpBonus: frame.BaseHp,
                defense: frame.Defense);
        }

        public ModuleStats GetFirepowerStats(string moduleId)
        {
            var m = _catalog.GetFirepowerModule(moduleId);
            if (m == null) return default;

            return new ModuleStats(
                attackDamage: m.AttackDamage,
                attackSpeed: m.AttackSpeed,
                range: m.Range);
        }

        public ModuleStats GetMobilityStats(string moduleId)
        {
            var m = _catalog.GetMobilityModule(moduleId);
            if (m == null) return default;

            return new ModuleStats(
                moveSpeed: m.MoveSpeed,
                moveRange: m.MoveRange);
        }

        public CostCalculator.StatCostTuning GetCostTuning()
        {
            return _catalog != null && _catalog.StatTuning != null
                ? _catalog.StatTuning.ToCostTuning()
                : CostCalculator.StatCostTuning.Default;
        }

        public string GetPassiveTraitId(string frameId)
        {
            var frame = _catalog.GetUnitFrame(frameId);
            return frame?.PassiveTrait?.TraitId ?? string.Empty;
        }

        public int GetPassiveTraitCostBonus(string frameId)
        {
            var frame = _catalog.GetUnitFrame(frameId);
            return frame?.PassiveTrait?.CostBonus ?? 0;
        }
    }
}
