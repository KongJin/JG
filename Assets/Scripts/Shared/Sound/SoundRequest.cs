using Shared.Math;

namespace Shared.Sound
{
    public readonly struct SoundRequest
    {
        public string SoundKey { get; }
        public Float3 Position { get; }
        public PlaybackPolicy Policy { get; }
        public string OwnerId { get; }
        public float CooldownHint { get; }

        public SoundRequest(
            string soundKey,
            Float3 position,
            PlaybackPolicy policy,
            string ownerId,
            float cooldownHint = 0f)
        {
            SoundKey = soundKey;
            Position = position;
            Policy = policy;
            OwnerId = ownerId;
            CooldownHint = cooldownHint;
        }
    }
}
