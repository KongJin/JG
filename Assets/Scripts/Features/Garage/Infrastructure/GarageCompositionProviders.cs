using Features.Garage.Application;
using Features.Garage.Domain;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// ScriptableObject 카탈로그에서 CompositionInput을 공급.
    /// ComposeUnitUseCase의 의존성 구현.
    /// </summary>
    public sealed class CompositionDataProvider : ComposeUnitUseCase.IUnitCompositionDataProvider
    {
        private readonly ModuleCatalog _catalog;

        public CompositionDataProvider(ModuleCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool TryGetFrameData(string frameId, out UnitComposition.CompositionInput input)
        {
            input = default;

            var frame = _catalog.GetUnitFrame(frameId);
            if (frame == null) return false;

            // 기본값: 프레임의 기본 스탯 + 첫 모듈 조합
            // 실제 조합 계산은 UseCase에서 모듈 ID로 다시 조회하므로
            // 여기서는 프레임 기본값만 제공
            input = new UnitComposition.CompositionInput(
                baseHp: frame.BaseHp,
                baseAttackSpeed: frame.BaseAttackSpeed,
                firepowerDamage: 0f,      // 모듈 조회는 별도 단계에서
                firepowerAttackSpeed: 1f,  // 기본 배율
                firepowerRange: 0f,
                mobilityHpBonus: 0f,
                mobilityMoveRange: frame.BaseMoveRange,
                passiveTraitCostBonus: frame.PassiveTrait != null ? frame.PassiveTrait.CostBonus : 0
            );

            return true;
        }
    }

    /// <summary>
    /// ScriptableObject 카탈로그를 기반으로 조합 유효성 검증 제공.
    /// ValidateRosterUseCase의 의존성 구현.
    /// </summary>
    public sealed class RosterValidationProvider : ValidateRosterUseCase.IRosterValidationProvider
    {
        private readonly ModuleCatalog _catalog;

        public RosterValidationProvider(ModuleCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool TryValidateComposition(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            out string errorMessage)
        {
            var firepower = _catalog.GetFirepowerModule(firepowerModuleId);
            var mobility = _catalog.GetMobilityModule(mobilityModuleId);

            if (firepower == null || mobility == null)
            {
                errorMessage = "모듈 데이터를 찾을 수 없습니다.";
                return false;
            }

            return UnitComposition.Validate(
                mobility.MoveRange,
                firepower.Range,
                out errorMessage);
        }
    }
}
