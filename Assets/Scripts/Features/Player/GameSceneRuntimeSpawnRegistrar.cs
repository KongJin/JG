using System.Collections.Generic;
using Features.Enemy;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneRuntimeSpawnRegistrar : MonoBehaviour
    {
        [SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [SerializeField] private EnemySceneRegistry _enemySceneRegistry;
        [SerializeField] private float _scanIntervalSeconds = 0.25f;

        private readonly HashSet<int> _announcedPlayers = new();
        private readonly HashSet<int> _announcedEnemies = new();
        private float _nextScanTime;

        private void Update()
        {
            if (Time.unscaledTime < _nextScanTime)
                return;

            _nextScanTime = Time.unscaledTime + Mathf.Max(0.05f, _scanIntervalSeconds);
            ScanNow();
        }

        public void ScanNow()
        {
            AnnouncePlayers();
            AnnounceEnemies();
        }

        private void AnnouncePlayers()
        {
            if (_playerSceneRegistry == null)
                return;

            var players = FindObjectsByType<PlayerSetup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var playerSetup in players)
            {
                if (playerSetup == null)
                    continue;

                var id = playerSetup.GetInstanceID();
                if (!_announcedPlayers.Add(id))
                    continue;

                _playerSceneRegistry.NotifyArrived(playerSetup);
            }
        }

        private void AnnounceEnemies()
        {
            if (_enemySceneRegistry == null)
                return;

            var enemies = FindObjectsByType<EnemySetup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;

                var id = enemy.GetInstanceID();
                if (!_announcedEnemies.Add(id))
                    continue;

                _enemySceneRegistry.NotifyArrived(enemy);
            }
        }
    }
}
