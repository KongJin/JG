using Shared.Kernel;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Application
{
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

        public static ValidationResult Validate(float initialEnergy, UnitSpec[] unitSpecs)
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
