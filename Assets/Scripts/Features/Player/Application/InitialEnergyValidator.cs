using Shared.Kernel;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Player.Application
{
    /// <summary>
    /// 초기 Energy가 가장 저렴한 유닛 소환 비용 이상인지 검증한다.
    /// 게임 시작 전 GameSceneRoot에서 호출한다.
    /// </summary>
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

        /// <summary>
        /// 초기 Energy가 유효한지 검증한다.
        /// </summary>
        public static ValidationResult Validate(float initialEnergy, UnitSpec[] unitSpecs)
        {
            var minCost = float.MaxValue;

            if (unitSpecs == null || unitSpecs.Length == 0)
            {
                return new ValidationResult(true, initialEnergy, 0f);
            }

            foreach (var spec in unitSpecs)
            {
                if (spec.SummonCost < minCost)
                {
                    minCost = spec.SummonCost;
                }
            }

            return new ValidationResult(initialEnergy >= minCost, initialEnergy, minCost);
        }
    }
}
