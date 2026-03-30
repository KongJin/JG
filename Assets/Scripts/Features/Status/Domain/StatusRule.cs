namespace Features.Status.Domain
{
    public static class StatusRule
    {
        private const int BurnMaxStacks = 3;
        private const int UpgradeMaxStacks = 10;

        public static StackPolicy GetPolicy(StatusType type)
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

        public static int GetMaxStacks(StatusType type)
        {
            switch (type)
            {
                case StatusType.Burn: return BurnMaxStacks;
                case StatusType.Expand:
                case StatusType.Extend:
                case StatusType.Multiply:
                case StatusType.Count: return UpgradeMaxStacks;
                default: return 1;
            }
        }

        public static float ApplySpeedModifier(float baseSpeed, float hasteMagnitude, float slowMagnitude)
        {
            var multiplier = 1f + hasteMagnitude - slowMagnitude;
            if (multiplier < 0.1f) multiplier = 0.1f;
            return baseSpeed * multiplier;
        }

        public static float CalculateBurnDamage(float magnitude)
        {
            return magnitude;
        }
    }
}
