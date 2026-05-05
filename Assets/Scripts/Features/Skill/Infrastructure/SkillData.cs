using Shared.Attributes;
using Features.Projectile.Domain;
using Features.Projectile.Domain.Hit;
using Features.Projectile.Domain.Trajectory;
using Features.Skill.Domain;
using Features.Skill.Domain.Delivery;
using Features.Status.Domain;
using Shared.Kernel;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    /// <summary>ScriptableObject that defines a single skill's inspector configuration.</summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skill/SkillData")]
    public sealed class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId;

        [Header("Presentation")]
        [Required, SerializeField] private SkillPresentationData presentation;

        [Header("Spec")]
        [SerializeField] private float damage;
        [SerializeField] private float manaCost;
        [SerializeField] private float range;
        [SerializeField] private float duration;
        [SerializeField] private int projectileCount = 1;

        [Header("Delivery")]
        [SerializeField] private DeliveryType deliveryType;

        [Header("Projectile (only when DeliveryType = Projectile)")]
        [SerializeField] private TrajectoryType trajectoryType;
        [SerializeField] private HitType hitType;
        [SerializeField] private float speed;
        [SerializeField] private float radius;

        [Header("Status Effect")]
        [SerializeField] private StatusEffectData statusEffect;

        [Header("Growth")]
        [SerializeField] private GrowthAxisConfig growthAxes;

        [Header("Classification")]
        [Tooltip("비어 있으면 damage·상태이상·딜리버리로 보조 추론. 상세 기준은 SkillGameplayTags / SkillGameplayTagResolver / game_design 문서를 따른다.")]
        [SerializeField] private SkillGameplayTags gameplayTags;

        public string SkillId => skillId;
        public SkillPresentationData Presentation => presentation;
        public GrowthAxisConfig GrowthAxes => growthAxes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(skillId))
                Debug.LogError($"[{name}] skillId is empty.", this);
        }
#endif

        public Domain.Skill ToDomain()
        {
            var id = new DomainEntityId(skillId);
// csharp-guardrails: allow-null-defense
            var payload = statusEffect != null ? statusEffect.ToPayload() : StatusPayload.None;
            var tags = SkillGameplayTagResolver.Resolve(gameplayTags, deliveryType, damage, payload);
            var spec = new SkillSpec(damage, manaCost, range, duration, projectileCount, payload, tags);
            var delivery = CreateDelivery();
            return new Domain.Skill(id, spec, delivery);
        }

        private IDeliveryStrategy CreateDelivery()
        {
            switch (deliveryType)
            {
                case DeliveryType.Projectile:
                    return new ProjectileDelivery(
                        new ProjectileSpec(trajectoryType, hitType, speed, radius));
                case DeliveryType.Zone:
                    return new ZoneDelivery();
                case DeliveryType.Targeted:
                    return new TargetedDelivery();
                case DeliveryType.Self:
                    return new SelfDelivery();
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(deliveryType), deliveryType, null);
            }
        }
    }
}
