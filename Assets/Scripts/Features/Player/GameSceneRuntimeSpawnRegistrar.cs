using System.Collections.Generic;
using Features.Enemy;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneRuntimeSpawnRegistrar : MonoBehaviour
    {
        private static GameSceneRuntimeSpawnRegistrar _active;

        [SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [SerializeField] private EnemySceneRegistry _enemySceneRegistry;

        private readonly HashSet<int> _announcedPlayers = new();
        private readonly HashSet<int> _announcedEnemies = new();

        public static void NotifyPlayerArrived(PlayerSetup playerSetup)
        {
            if (_active == null)
                return;

            _active.AnnouncePlayer(playerSetup);
        }

        public static void NotifyEnemyArrived(EnemySetup enemySetup)
        {
            if (_active == null)
                return;

            _active.AnnounceEnemy(enemySetup);
        }

        private void Awake()
        {
            if (_active != null && _active != this)
                Debug.LogWarning("[GameSceneRuntimeSpawnRegistrar] Replacing active scene registrar.", this);

            _active = this;
        }

        private void OnDestroy()
        {
            if (_active == this)
                _active = null;
        }

        private void AnnouncePlayer(PlayerSetup playerSetup)
        {
            if (_playerSceneRegistry == null || playerSetup == null)
                return;

            var id = playerSetup.GetInstanceID();
            if (!_announcedPlayers.Add(id))
                return;

            _playerSceneRegistry.NotifyArrived(playerSetup);
        }

        private void AnnounceEnemy(EnemySetup enemy)
        {
            if (_enemySceneRegistry == null || enemy == null)
                return;

            var id = enemy.GetInstanceID();
            if (!_announcedEnemies.Add(id))
                return;

            _enemySceneRegistry.NotifyArrived(enemy);
        }
    }
}
