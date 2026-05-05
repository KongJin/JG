using System.Collections.Generic;
using Features.Enemy;
using Shared.Attributes;
using UnityEngine;

namespace Features.Player
{
    public sealed class BattleSceneRuntimeSpawnRegistrar : MonoBehaviour
    {
        private static BattleSceneRuntimeSpawnRegistrar _active;

        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [Required, SerializeField] private EnemySceneRegistry _enemySceneRegistry;

        private readonly HashSet<int> _announcedPlayers = new();
        private readonly HashSet<int> _announcedEnemies = new();

        public static void NotifyPlayerArrived(PlayerSetup playerSetup)
        {
// csharp-guardrails: allow-null-defense
            if (_active == null)
                return;

            _active.AnnouncePlayer(playerSetup);
        }

        public static void NotifyEnemyArrived(EnemySetup enemySetup)
        {
// csharp-guardrails: allow-null-defense
            if (_active == null)
                return;

            _active.AnnounceEnemy(enemySetup);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _active = null;
        }

        private void Awake()
        {
// csharp-guardrails: allow-null-defense
            if (_active != null && _active != this)
                Debug.LogWarning("[BattleSceneRuntimeSpawnRegistrar] Replacing active scene registrar.", this);

            _active = this;
        }

        private void OnDestroy()
        {
            if (_active == this)
                _active = null;
        }

        private void AnnouncePlayer(PlayerSetup playerSetup)
        {
// csharp-guardrails: allow-null-defense
            if (_playerSceneRegistry == null || playerSetup == null)
                return;

            var id = playerSetup.GetInstanceID();
            if (!_announcedPlayers.Add(id))
                return;

            _playerSceneRegistry.NotifyArrived(playerSetup);
        }

        private void AnnounceEnemy(EnemySetup enemy)
        {
// csharp-guardrails: allow-null-defense
            if (_enemySceneRegistry == null || enemy == null)
                return;

            var id = enemy.GetInstanceID();
            if (!_announcedEnemies.Add(id))
                return;

            _enemySceneRegistry.NotifyArrived(enemy);
        }
    }
}
