namespace Features.Status.Domain
{
    public static class GrowthRule
    {
        public static int CalculateCount(int baseCount, float countMagnitude)
        {
            var bonus = (int)System.Math.Ceiling(baseCount * (double)countMagnitude);
            return baseCount + bonus;
        }
    }
}
