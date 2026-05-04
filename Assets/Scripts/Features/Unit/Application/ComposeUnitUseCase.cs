using Features.Garage.Application.Ports;
using Features.Unit.Domain;
using Shared.Kernel;

namespace Features.Unit.Application
{
    /// <summary>
    /// 모듈 ID들로 유닛을 조합한다.
    /// Unit 도메인의 UnitComposition을 사용하며, SO 데이터 조회는 Port를 통해 수행한다.
    /// </summary>
    public sealed class ComposeUnitUseCase
    {
        private readonly IUnitCompositionPort _port;

        public ComposeUnitUseCase(IUnitCompositionPort port)
        {
            _port = port;
        }

        /// <summary>
        /// 조합 계산.
        /// </summary>
        /// <param name="unitId">유닛 엔티티 ID</param>
        /// <param name="frameId">프레임 ID</param>
        /// <param name="firepowerModuleId">화력 모듈 ID</param>
        /// <param name="mobilityModuleId">기동 모듈 ID</param>
        public Result<Domain.Unit> Execute(
            DomainEntityId unitId,
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId)
        {
            var frameBase = _port.GetFrameBaseStats(frameId);
            var firepower = _port.GetFirepowerStats(firepowerModuleId);
            var mobility = _port.GetMobilityStats(mobilityModuleId);
            var costTuning = _port.GetCostTuning();

            // 검증
            if (!UnitComposition.Validate(mobility.MoveRange, firepower.Range, out var error))
            {
                return Result<Domain.Unit>.Failure(error);
            }

            var passiveTraitId = _port.GetPassiveTraitId(frameId);
            var passiveCostBonus = _port.GetPassiveTraitCostBonus(frameId);

            // CompositionInput 구성
            var input = new UnitComposition.CompositionInput(
                baseHp: frameBase.FrameBaseHp,
                baseDefense: frameBase.Defense,
                firepowerDamage: firepower.AttackDamage,
                firepowerAttackSpeed: firepower.AttackSpeed,
                firepowerRange: firepower.Range,
                mobilityMoveSpeed: mobility.MoveSpeed,
                mobilityMoveRange: mobility.MoveRange,
                passiveTraitCostBonus: passiveCostBonus,
                costTuning: costTuning);

            // 조합
            var composed = UnitComposition.Compose(
                frameId,
                firepowerModuleId,
                mobilityModuleId,
                input);

            return Result<Domain.Unit>.Success(composed.ToUnit(unitId));
        }
    }

    public sealed class InitialEnergyValidator
    {
        public readonly struct ValidationResult
        {
            public bool IsValid { get; }
            public float InitialEnergy { get; }
            public float MinSummonCost { get; }

            public ValidationResult(bool isValid, float initialEnergy, float minCost)
            {
                IsValid = isValid;
                InitialEnergy = initialEnergy;
                MinSummonCost = minCost;
            }
        }

        public static ValidationResult Validate(float initialEnergy, Domain.Unit[] unitSpecs)
        {
            var minCost = float.MaxValue;

            if (unitSpecs == null || unitSpecs.Length == 0)
                return new ValidationResult(true, initialEnergy, 0f);

            foreach (var spec in unitSpecs)
            {
                if (spec.SummonCost < minCost)
                    minCost = spec.SummonCost;
            }

            return new ValidationResult(initialEnergy >= minCost, initialEnergy, minCost);
        }
    }
}
