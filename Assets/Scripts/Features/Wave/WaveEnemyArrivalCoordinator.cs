using Features.Enemy;
using UnityEngine;

namespace Features.Wave
{
    internal sealed class WaveEnemyArrivalCoordinator
    {
        private EnemySceneRegistry _enemySceneRegistry;

        public void Attach(EnemySceneRegistry enemySceneRegistry, System.Action<EnemySetup> onEnemyArrived)
        {
            if (enemySceneRegistry == null || onEnemyArrived == null)
                return;

            _enemySceneRegistry = enemySceneRegistry;
            _enemySceneRegistry.EnemyArrived += onEnemyArrived;
            _enemySceneRegistry.DrainPendingArrivals(onEnemyArrived);
        }

        public void Detach(System.Action<EnemySetup> onEnemyArrived)
        {
// csharp-guardrails: allow-null-defense
            if (_enemySceneRegistry == null || onEnemyArrived == null)
                return;

            _enemySceneRegistry.EnemyArrived -= onEnemyArrived;
        }
    }
}
