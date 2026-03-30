using Features.Skill.Application.Ports;
using Features.Skill.Domain;
using Features.Skill.Domain.Delivery;
using Features.Status.Domain;
using Shared.Kernel;
using Shared.Math;

using DomainSkill = Features.Skill.Domain.Skill;

namespace Features.Skill.Application
{
    public sealed class CastSkillUseCase
    {
        private readonly IManaPort _manaPort;
        private readonly ISkillNetworkCommandPort _network;
        private readonly IStatusQueryPort _statusQuery;

        public CastSkillUseCase(
            IManaPort manaPort,
            ISkillNetworkCommandPort network,
            IStatusQueryPort statusQuery = null
        )
        {
            _manaPort = manaPort;
            _network = network;
            _statusQuery = statusQuery;
        }

        public Result Execute(
            DomainSkill skill,
            int slotIndex,
            DomainEntityId casterId,
            float currentTime,
            Float3 position,
            Float3 direction,
            bool hasTarget,
            Float3 targetPosition
        )
        {
            var manaCost = skill.Spec.ManaCost;
            if (_statusQuery != null)
            {
                var extendMag = _statusQuery.GetMagnitude(casterId, StatusType.Extend);
                if (extendMag > 0f)
                    manaCost *= 1f / (1f + extendMag);
            }

            var manaCheck = ManaRule.CanCast(manaCost, _manaPort.GetCurrentMana(casterId));
            if (manaCheck.IsFailure)
                return manaCheck;

            if (skill.Delivery is TargetedDelivery && !hasTarget)
                return Result.Failure("Targeted skills require a valid target.");

            var result = skill.Delivery.Deliver(skill.Id, casterId, skill.Spec);
            var pr = result as ProjectileDeliveryResult;

            _manaPort.TrySpendMana(casterId, manaCost);

            var damage = skill.Spec.Damage;
            var range = skill.Spec.Range;
            var radius = pr?.ProjectileSpec.Radius ?? 0f;
            if (_statusQuery != null)
            {
                var expandMag = _statusQuery.GetMagnitude(casterId, StatusType.Expand);
                if (expandMag > 0f)
                {
                    range *= (1f + expandMag);
                    radius *= (1f + expandMag);
                }

                var multiplyMag = _statusQuery.GetMagnitude(casterId, StatusType.Multiply);
                if (multiplyMag > 0f)
                    damage *= (1f + multiplyMag);
            }

            var projectileCount = skill.Spec.ProjectileCount;
            if (_statusQuery != null)
            {
                var countMag = _statusQuery.GetMagnitude(casterId, StatusType.Count);
                if (countMag > 0f)
                    projectileCount = GrowthRule.CalculateCount(skill.Spec.ProjectileCount, countMag);
            }

            _network.SendSkillCasted(
                new SkillCastNetworkData(
                    skill.Id,
                    casterId,
                    slotIndex,
                    damage,
                    skill.Spec.Duration,
                    range,
                    result.DeliveryType,
                    pr != null ? (int)pr.ProjectileSpec.TrajectoryType : 0,
                    pr != null ? (int)pr.ProjectileSpec.HitType : 0,
                    pr?.ProjectileSpec.Speed ?? 0f,
                    radius,
                    position,
                    direction,
                    targetPosition,
                    skill.Spec.StatusPayload,
                    projectileCount
                )
            );

            return Result.Success();
        }
    }
}
