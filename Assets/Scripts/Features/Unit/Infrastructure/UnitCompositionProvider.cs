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
                attackSpeed: frame.BaseAttackSpeed,
                moveRange: frame.BaseMoveRange);
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
                hpBonus: m.HpBonus,
                moveRange: m.MoveRange,
                anchorRange: m.AnchorRange);
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
