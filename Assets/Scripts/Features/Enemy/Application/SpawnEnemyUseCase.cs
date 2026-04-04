using System;
using Features.Enemy.Application.Events;
using Features.Enemy.Domain;
using Features.Wave.Application.Ports;
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

    public static class EnemyMoveTargetResolver
    {
        public static bool TryGetMoveDestination(
            in EnemySpec spec,
            float ex,
            float ey,
            float ez,
            IPlayerPositionQuery players,
            ICoreObjectiveQuery core,
            out float dx,
            out float dy,
            out float dz)
        {
            switch (spec.TargetMode)
            {
                case EnemyTargetMode.ChaseNearestPlayer:
                    (dx, dy, dz) = players.GetNearestPlayerPosition(ex, ey, ez);
                    return true;

                case EnemyTargetMode.ChaseCore:
                    if (core.TryGetCoreWorldPosition(out dx, out dy, out dz))
                        return true;
                    (dx, dy, dz) = players.GetNearestPlayerPosition(ex, ey, ez);
                    return true;

                case EnemyTargetMode.ChaseCoreAggroPlayerInRadius:
                    if (spec.AggroRadius > 0f &&
                        players.TryGetNearestPlayerWithinHorizontalRadius(
                            ex, ey, ez, spec.AggroRadius, out dx, out dy, out dz))
                        return true;
                    if (core.TryGetCoreWorldPosition(out dx, out dy, out dz))
                        return true;
                    (dx, dy, dz) = players.GetNearestPlayerPosition(ex, ey, ez);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(spec), spec.TargetMode, null);
            }
        }
    }
}
