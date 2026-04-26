using Shared.Kernel;

namespace Shared.Gameplay
{
    public readonly struct BattleUnitDeployedEvent
    {
        public BattleUnitDeployedEvent(DomainEntityId playerId, DomainEntityId battleEntityId)
        {
            PlayerId = playerId;
            BattleEntityId = battleEntityId;
        }

        public DomainEntityId PlayerId { get; }
        public DomainEntityId BattleEntityId { get; }
    }
}
