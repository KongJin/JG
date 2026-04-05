using Features.Combat.Application.Events;
using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Combat.Application
{
    public sealed class ApplyDamageUseCase
    {
        private readonly ICombatTargetPort _target;
        private readonly IEventPublisher _eventBus;
        private readonly ICombatNetworkCommandPort _network;
        private readonly IEntityAffiliationPort _affiliation;

        public ApplyDamageUseCase(
            ICombatTargetPort target,
            IEventPublisher eventBus,
            ICombatNetworkCommandPort network,
            IEntityAffiliationPort affiliation
        )
        {
            _target = target;
            _eventBus = eventBus;
            _network = network;
            _affiliation = affiliation;
        }

        public Result Execute(string targetIdValue, float baseDamage, DamageType damageType)
        {
            if (string.IsNullOrWhiteSpace(targetIdValue))
                return Result.Failure("Target id is required.");

            return Execute(new DomainEntityId(targetIdValue), baseDamage, damageType);
        }

        public Result Execute(DomainEntityId targetId, float baseDamage, DamageType damageType,
            DomainEntityId attackerId = default, float allyDamageScale = 1f)
        {
            if (!_target.Exists(targetId))
                return Result.Failure($"Combat target not found: {targetId.Value}");

            var defense = _target.GetDefense(targetId);
            var finalDamage = DamageRule.Calculate(baseDamage, defense, damageType);

            var hasAttacker = !string.IsNullOrWhiteSpace(attackerId.Value);
            var rel = hasAttacker
                ? _affiliation.GetRelationship(attackerId, targetId)
                : RelationshipType.Enemy;

            if (rel == RelationshipType.Self)
                return Result.Success();

            finalDamage *= RelationshipRule.GetDamageMultiplier(rel);

            var damageResult = _target.ApplyDamage(targetId, finalDamage);

            _network.SendDamage(targetId, finalDamage, damageType, attackerId);

            _eventBus.Publish(
                new DamageAppliedEvent(
                    targetId,
                    finalDamage,
                    damageType,
                    damageResult.RemainingHealth,
                    damageResult.IsDead,
                    attackerId
                )
            );

            return Result.Success();
        }

        public Result ExecuteReplicated(
            DomainEntityId targetId,
            float damage,
            DamageType damageType,
            DomainEntityId attackerId = default
        )
        {
            if (!_target.Exists(targetId))
                return Result.Failure($"Combat target not found: {targetId.Value}");

            var damageResult = _target.ApplyDamage(targetId, damage);

            _eventBus.Publish(
                new DamageAppliedEvent(
                    targetId,
                    damage,
                    damageType,
                    damageResult.RemainingHealth,
                    damageResult.IsDead,
                    attackerId
                )
            );

            return Result.Success();
        }
    }
}
