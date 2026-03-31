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
        [SerializeField] private bool isStarter;

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
        [SerializeField] private bool hasStatusEffect;
        [SerializeField] private StatusType statusType;
        [SerializeField] private float statusMagnitude;
        [SerializeField] private float statusDuration;
        [SerializeField] private float statusTickInterval;

        public string SkillId => skillId;
        public bool IsStarter => isStarter;
        public SkillPresentationData Presentation => presentation;

        public Domain.Skill ToDomain()
        {
            var id = new DomainEntityId(skillId);
            var payload = hasStatusEffect
                ? StatusPayload.Create(statusType, statusMagnitude, statusDuration, statusTickInterval)
                : StatusPayload.None;
            var spec = new SkillSpec(damage, manaCost, range, duration, projectileCount, payload);
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
                    Debug.LogError($"[SkillData] Unknown delivery type: {deliveryType}");
                    return new SelfDelivery();
            }
        }
    }
}
