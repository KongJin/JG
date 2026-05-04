using System;

namespace Features.Status.Domain
{
    public interface IStatusRuleSet
    {
        StackPolicy GetPolicy(StatusType type);
        int GetMaxStacks(StatusType type);
        float CalculateBurnDamage(float magnitude);
    }

    public static class StatusRule
    {
        private const int BurnMaxStacks = 3;
        private const int UpgradeMaxStacks = 10;
        public static IStatusRuleSet Default { get; } = new DefaultStatusRuleSet();

        public static StackPolicy GetPolicy(StatusType type)
        {
            return Default.GetPolicy(type);
        }

        public static int GetMaxStacks(StatusType type)
        {
            return Default.GetMaxStacks(type);
        }

        public static float ApplySpeedModifier(float baseSpeed, float hasteMagnitude, float slowMagnitude)
        {
            var multiplier = 1f + hasteMagnitude - slowMagnitude;
            if (multiplier < 0.1f) multiplier = 0.1f;
            return baseSpeed * multiplier;
        }

        public static float CalculateBurnDamage(float magnitude)
        {
            return Default.CalculateBurnDamage(magnitude);
        }

        private sealed class DefaultStatusRuleSet : IStatusRuleSet
        {
            public StackPolicy GetPolicy(StatusType type)
            {
            switch (type)
            {
                case StatusType.Burn:
                case StatusType.Expand:
                case StatusType.Extend:
                case StatusType.Multiply:
                case StatusType.Count:
                    return StackPolicy.Independent;
                default:
                    return StackPolicy.Refresh;
            }
            }

            public int GetMaxStacks(StatusType type)
            {
            switch (type)
            {
                case StatusType.Burn: return BurnMaxStacks;
                case StatusType.Expand:
                case StatusType.Extend:
                case StatusType.Multiply:
                case StatusType.Count: return UpgradeMaxStacks;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown StatusType");
            }
            }

            public float CalculateBurnDamage(float magnitude)
            {
                return magnitude;
            }
        }
    }
}
