using Features.Skill.Domain;
using Features.Skill.Domain.Delivery;
using Features.Status.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Skill.Application.Ports
{
    /// <summary>RPC 전송 시 사용하는 네트워크 데이터</summary>
    public readonly struct SkillCastNetworkData
    {
        public DomainEntityId SkillId { get; }
        public DomainEntityId CasterId { get; }
        public int SlotIndex { get; }
        public float Damage { get; }
        public float Duration { get; }
        public float Range { get; }
        public DeliveryType DeliveryType { get; }
        public int TrajectoryType { get; }
        public int HitType { get; }
        public float Speed { get; }
        public float Radius { get; }
        public Float3 Position { get; }
        public Float3 Direction { get; }
        public Float3 TargetPosition { get; }
        public StatusPayload StatusPayload { get; }
        public int ProjectileCount { get; }
        public float AllyDamageScale { get; }
        public SkillGameplayTags GameplayTags { get; }

        public SkillCastNetworkData(
            DomainEntityId skillId, DomainEntityId casterId, int slotIndex,
            float damage, float duration, float range,
            DeliveryType deliveryType,
            int trajectoryType, int hitType, float speed, float radius,
            Float3 position, Float3 direction, Float3 targetPosition,
            StatusPayload statusPayload = default,
            int projectileCount = 1,
            float allyDamageScale = 1f,
            SkillGameplayTags gameplayTags = SkillGameplayTags.None)
        {
            SkillId = skillId;
            CasterId = casterId;
            SlotIndex = slotIndex;
            Damage = damage;
            Duration = duration;
            Range = range;
            DeliveryType = deliveryType;
            TrajectoryType = trajectoryType;
            HitType = hitType;
            Speed = speed;
            Radius = radius;
            Position = position;
            Direction = direction;
            TargetPosition = targetPosition;
            StatusPayload = statusPayload;
            ProjectileCount = projectileCount;
            AllyDamageScale = allyDamageScale;
            GameplayTags = gameplayTags;
        }
    }

    /// <summary>
    /// 스킬 네트워크 포트 - 송신/수신 통합.
    /// Command/Callback 분리보다 feature 단위 통합이 ownership 명확성을 유지함.
    /// </summary>
    public interface ISkillNetworkPort
    {
        /// <summary>
        /// 스킬 캐스팅 데이터를 네트워크로 송신 (Command).
        /// </summary>
        void SendSkillCasted(SkillCastNetworkData data);

        /// <summary>
        /// 네트워크에서 스킬 캐스팅 데이터 수신 시 호출될 콜백 (Callback).
        /// </summary>
        System.Action<SkillCastNetworkData> OnSkillCasted { set; }
    }
}
