using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Shared.Kernel;

namespace Features.Player.Infrastructure
{
    public sealed class EntityAffiliationAdapter : IEntityAffiliationPort
    {
        private const string PlayerPrefix = "player-";

        public RelationshipType GetRelationship(DomainEntityId attackerId, DomainEntityId targetId)
        {
            if (attackerId.Equals(targetId))
                return RelationshipType.Self;

            if (IsPlayer(attackerId) && IsPlayer(targetId))
                return RelationshipType.Ally;

            return RelationshipType.Enemy;
        }

        private static bool IsPlayer(DomainEntityId id)
        {
            return id.Value != null && id.Value.StartsWith(PlayerPrefix);
        }
    }
}
