using Shared.Kernel;

namespace Features.Enemy.Application.Events
{
    public readonly struct EnemySpawnedEvent
    {
        public DomainEntityId EnemyId { get; }

        public EnemySpawnedEvent(DomainEntityId enemyId)
        {
            EnemyId = enemyId;
        }
    }
}
