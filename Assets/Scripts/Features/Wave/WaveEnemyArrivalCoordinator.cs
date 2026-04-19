using Features.Enemy;
using UnityEngine;

namespace Features.Wave
{
    internal sealed class WaveEnemyArrivalCoordinator
    {
        private EnemySceneRegistry _enemySceneRegistry;

        public void Attach(MonoBehaviour owner, System.Action<EnemySetup> onEnemyArrived)
        {
            if (owner == null || onEnemyArrived == null)
                return;

            _enemySceneRegistry = owner.GetComponent<EnemySceneRegistry>();
            if (_enemySceneRegistry == null)
                _enemySceneRegistry = owner.gameObject.AddComponent<EnemySceneRegistry>();

            _enemySceneRegistry.EnemyArrived += onEnemyArrived;
            _enemySceneRegistry.DrainPendingArrivals(onEnemyArrived);
        }

        public void Detach(System.Action<EnemySetup> onEnemyArrived)
        {
            if (_enemySceneRegistry == null || onEnemyArrived == null)
                return;

            _enemySceneRegistry.EnemyArrived -= onEnemyArrived;
        }
    }
}
