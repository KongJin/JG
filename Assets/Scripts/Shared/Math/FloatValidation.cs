namespace Shared.Math
{
    public static class FloatValidation
    {
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Float3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }
    }
}
