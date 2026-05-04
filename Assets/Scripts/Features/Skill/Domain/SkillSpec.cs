using Features.Status.Domain;

namespace Features.Skill.Domain
{
    public sealed class SkillSpec : Shared.Kernel.ValueObject
    {
        private const int ValuePrecision = 4;

        public SkillSpec(
            float damage,
            float manaCost,
            float range,
            float duration = 0f,
            int projectileCount = 1,
            StatusPayload statusPayload = default,
            SkillGameplayTags gameplayTags = SkillGameplayTags.None)
        {
            if (damage < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(damage), damage, "Skill damage cannot be negative.");
            if (manaCost < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(manaCost), manaCost, "Skill mana cost cannot be negative.");
            if (range < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(range), range, "Skill range cannot be negative.");
            if (duration < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(duration), duration, "Skill duration cannot be negative.");

            Damage = RoundValue(damage);
            ManaCost = RoundValue(manaCost);
            Range = RoundValue(range);
            Duration = RoundValue(duration);
            ProjectileCount = projectileCount < 1 ? 1 : projectileCount;
            StatusPayload = statusPayload;
            GameplayTags = gameplayTags;
        }

        public float Damage { get; }
        public float ManaCost { get; }
        public float Range { get; }
        public float Duration { get; }
        public int ProjectileCount { get; }
        public StatusPayload StatusPayload { get; }
        public SkillGameplayTags GameplayTags { get; }

        private static float RoundValue(float value)
        {
            return (float)System.Math.Round(value, ValuePrecision);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SkillSpec other) return false;
            return Damage == other.Damage
                && ManaCost == other.ManaCost
                && Range == other.Range
                && Duration == other.Duration
                && ProjectileCount == other.ProjectileCount
                && StatusPayload.Equals(other.StatusPayload)
                && GameplayTags == other.GameplayTags;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Damage.GetHashCode();
                hash = hash * 31 + ManaCost.GetHashCode();
                hash = hash * 31 + Range.GetHashCode();
                hash = hash * 31 + Duration.GetHashCode();
                hash = hash * 31 + ProjectileCount.GetHashCode();
                hash = hash * 31 + StatusPayload.GetHashCode();
                hash = hash * 31 + GameplayTags.GetHashCode();
                return hash;
            }
        }
    }
}
