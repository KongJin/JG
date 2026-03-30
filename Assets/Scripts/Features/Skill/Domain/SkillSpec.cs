using Features.Status.Domain;

namespace Features.Skill.Domain
{
    public sealed class SkillSpec : Shared.Kernel.ValueObject
    {
        private const int Precision = 4;

        public SkillSpec(
            float damage,
            float manaCost,
            float range,
            float duration = 0f,
            int projectileCount = 1,
            StatusPayload statusPayload = default)
        {
            Damage = (float)System.Math.Round(damage, Precision);
            ManaCost = (float)System.Math.Round(manaCost, Precision);
            Range = (float)System.Math.Round(range, Precision);
            Duration = (float)System.Math.Round(duration, Precision);
            ProjectileCount = projectileCount < 1 ? 1 : projectileCount;
            StatusPayload = statusPayload;
        }

        public float Damage { get; }
        public float ManaCost { get; }
        public float Range { get; }
        public float Duration { get; }
        public int ProjectileCount { get; }
        public StatusPayload StatusPayload { get; }

        public override bool Equals(object obj)
        {
            if (obj is not SkillSpec other) return false;
            return Damage == other.Damage
                && ManaCost == other.ManaCost
                && Range == other.Range
                && Duration == other.Duration
                && ProjectileCount == other.ProjectileCount
                && StatusPayload.Equals(other.StatusPayload);
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
                return hash;
            }
        }
    }
}
