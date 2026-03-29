using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Zone.Domain
{
    public sealed class ZoneSpec : ValueObject
    {
        public ZoneSpec(
            float radius,
            float duration,
            ZoneAnchorType anchorType,
            ZoneHitType hitType,
            float baseDamage = 0f,
            StatusPayload statusPayload = default)
        {
            Radius = radius;
            Duration = duration;
            AnchorType = anchorType;
            HitType = hitType;
            BaseDamage = baseDamage;
            StatusPayload = statusPayload;
        }

        public float Radius { get; }
        public float Duration { get; }
        public ZoneAnchorType AnchorType { get; }
        public ZoneHitType HitType { get; }
        public float BaseDamage { get; }
        public StatusPayload StatusPayload { get; }

        public override bool Equals(object obj)
        {
            if (obj is not ZoneSpec other) return false;
            return Radius == other.Radius
                && Duration == other.Duration
                && AnchorType == other.AnchorType
                && HitType == other.HitType
                && BaseDamage == other.BaseDamage
                && StatusPayload.Equals(other.StatusPayload);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Radius.GetHashCode();
                hash = hash * 31 + Duration.GetHashCode();
                hash = hash * 31 + AnchorType.GetHashCode();
                hash = hash * 31 + HitType.GetHashCode();
                hash = hash * 31 + BaseDamage.GetHashCode();
                hash = hash * 31 + StatusPayload.GetHashCode();
                return hash;
            }
        }
    }
}
