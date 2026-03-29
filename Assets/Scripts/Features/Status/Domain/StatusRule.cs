namespace Features.Status.Domain
{
    public static class StatusRule
    {
        private const int BurnMaxStacks = 3;

        public static StackPolicy GetPolicy(StatusType type)
        {
            return type == StatusType.Burn ? StackPolicy.Independent : StackPolicy.Refresh;
        }

        public static int GetMaxStacks(StatusType type)
        {
            return type == StatusType.Burn ? BurnMaxStacks : 1;
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
