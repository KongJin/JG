using Features.Unit.Domain;

namespace Features.Garage.Application.Ports
{
    /// <summary>
    /// Unit 조합에 필요한 모듈 스탯 조회 포트.
    /// Garage가 정의하고(소비자), Unit Infrastructure가 구현한다(제공자).
    /// architecture.md cross-feature port placement 규칙 준수.
    /// </summary>
    public interface IUnitCompositionPort
    {
        ModuleStats GetFrameBaseStats(string frameId);
        ModuleStats GetFirepowerStats(string moduleId);
        ModuleStats GetMobilityStats(string moduleId);
        CostCalculator.StatCostTuning GetCostTuning();
        string GetPassiveTraitId(string frameId);
        int GetPassiveTraitCostBonus(string frameId);
    }
}
