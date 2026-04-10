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
            ZoneStatusPayload statusPayload = default)
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
        public ZoneStatusPayload StatusPayload { get; }

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

    /// <summary>
    /// Zone 틱 이벤트용 상태 효과 데이터.
    /// Features.Status.Domain.StatusPayload 의존성을 끊기 위한 Zone-owned 값 타입.
    /// Domain 레이어에 배치하여 ZoneSpec에서 참조 가능하도록 함.
    /// </summary>
    public readonly struct ZoneStatusPayload
    {
        public ZoneStatusPayload(
            ZoneStatusType type = ZoneStatusType.None,
            float magnitude = 0f,
            float duration = 0f,
            float tickInterval = 0f)
        {
            Type = type;
            Magnitude = magnitude;
            Duration = duration;
            TickInterval = tickInterval;
        }

        public ZoneStatusType Type { get; }
        public float Magnitude { get; }
        public float Duration { get; }
        public float TickInterval { get; }
        public bool HasEffect => Type != ZoneStatusType.None;

        public static ZoneStatusPayload None => default;

        public static ZoneStatusPayload Create(
            ZoneStatusType type,
            float magnitude,
            float duration,
            float tickInterval = 0f)
        {
            return new ZoneStatusPayload(type, magnitude, duration, tickInterval);
        }

        public enum ZoneStatusType
        {
            None = 0,
            Slow = 1,
            Haste = 2,
            DoT = 3,
        }
    }
}
