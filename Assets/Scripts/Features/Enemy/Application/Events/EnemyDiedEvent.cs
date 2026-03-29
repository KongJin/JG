using Shared.Kernel;

namespace Features.Enemy.Application.Events
{
    public readonly struct EnemyDiedEvent
    {
        public DomainEntityId EnemyId { get; }
        public DomainEntityId KillerId { get; }

        public EnemyDiedEvent(DomainEntityId enemyId, DomainEntityId killerId)
        {
            EnemyId = enemyId;
            KillerId = killerId;
        }
    }
}
