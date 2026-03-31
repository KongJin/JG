using Shared.Math;

namespace Features.Player.Domain
{
    public static class RescueRule
    {
        public const float ChannelDuration = 1.5f;
        public const float HpPercent = 0.5f;
        public const float ManaPercent = 0.5f;
        public const float InvulnerabilityDuration = 2f;
        public const float MaxRange = 3f;

        public static bool IsChannelComplete(float elapsed) => elapsed >= ChannelDuration;

        public static bool IsInRange(Float3 a, Float3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz <= MaxRange * MaxRange;
        }
    }
}
