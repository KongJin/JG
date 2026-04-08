using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Shared.Kernel;

namespace Features.Player.Infrastructure
{
    public sealed class EntityAffiliationAdapter : IEntityAffiliationPort
    {
        private const string PlayerPrefix = "player-";
        private const string BattleEntityPrefix = "battle-";
        private const string EnemyPrefix = "enemy-";

        public RelationshipType GetRelationship(DomainEntityId attackerId, DomainEntityId targetId)
        {
            if (attackerId.Equals(targetId))
                return RelationshipType.Self;

            // Players are allies to each other
            if (IsPlayer(attackerId) && IsPlayer(targetId))
                return RelationshipType.Ally;

            // BattleEntities owned by the same player are allies
            if (IsBattleEntity(attackerId) && IsBattleEntity(targetId))
            {
                // Extract owner ID from battle entity ID format: "battle-{unitId}-{instanceId}"
                // For now, all player battle entities are allies (same team)
                return RelationshipType.Ally;
            }

            // Player's ObjectiveCore is ally to player and battle entities
            if (IsCore(attackerId) && (IsPlayer(targetId) || IsBattleEntity(targetId)))
                return RelationshipType.Ally;
            if (IsCore(targetId) && (IsPlayer(attackerId) || IsBattleEntity(attackerId)))
                return RelationshipType.Ally;

            // Enemies vs anything non-enemy = enemy
            if (IsEnemy(attackerId) && !IsEnemy(targetId))
                return RelationshipType.Enemy;
            if (!IsEnemy(attackerId) && IsEnemy(targetId))
                return RelationshipType.Enemy;

            // Default: enemy
            return RelationshipType.Enemy;
        }

        private static bool IsPlayer(DomainEntityId id) =>
            id.Value != null && id.Value.StartsWith(PlayerPrefix);

        private static bool IsBattleEntity(DomainEntityId id) =>
            id.Value != null && id.Value.StartsWith(BattleEntityPrefix);

        private static bool IsEnemy(DomainEntityId id) =>
            id.Value != null && id.Value.StartsWith(EnemyPrefix);

        private static bool IsCore(DomainEntityId id) =>
            id.Value != null && id.Value.Contains("objective-core");
    }
}
