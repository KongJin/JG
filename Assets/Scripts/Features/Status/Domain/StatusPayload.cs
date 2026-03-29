namespace Features.Status.Domain
{
    public readonly struct StatusPayload
    {
        public bool HasEffect { get; }
        public StatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }
        public float TickInterval { get; }

        private StatusPayload(StatusType type, float magnitude, float duration, float tickInterval)
        {
            HasEffect = true;
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            TickInterval = tickInterval;
        }

        public static StatusPayload None => default;

        public static StatusPayload Create(StatusType type, float magnitude, float duration, float tickInterval = 0f)
        {
            return new StatusPayload(type, magnitude, duration, tickInterval);
        }
    }
}
