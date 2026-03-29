using Features.Enemy.Application.Events;
using Features.Enemy.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Enemy.Application
{
    public sealed class SpawnEnemyUseCase
    {
        private readonly IEventPublisher _eventBus;

        public SpawnEnemyUseCase(IEventPublisher eventBus)
        {
            _eventBus = eventBus;
        }

        public Enemy.Domain.Enemy Execute(DomainEntityId id, EnemySpec spec)
        {
            var enemy = new Enemy.Domain.Enemy(id, spec);
            _eventBus.Publish(new EnemySpawnedEvent(id));
            return enemy;
        }
    }
}
