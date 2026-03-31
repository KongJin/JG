namespace Features.Player.Domain
{
    public static class BleedoutRule
    {
        public const float Duration = 10f;

        public static bool IsExpired(float elapsed) => elapsed >= Duration;
    }
}
