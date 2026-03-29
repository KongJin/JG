using System.Collections;
using Features.Combat;
using Features.Enemy;
using Features.Enemy.Infrastructure;
using Features.Wave.Application.Ports;
using Photon.Pun;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    public sealed class EnemySpawnAdapter : MonoBehaviour
    {
        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private IPlayerPositionQuery _playerQuery;

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            IPlayerPositionQuery playerQuery)
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _playerQuery = playerQuery;
        }

        public void SpawnEnemy(EnemyData data, float x, float y, float z)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var position = new Vector3(x, y, z);
            var go = PhotonNetwork.Instantiate(data.PrefabName, position, Quaternion.identity);

            var setup = go.GetComponent<EnemySetup>();
            if (setup != null)
                setup.Initialize(_eventBus, _combatBootstrap, data, _playerQuery);
            else
                Debug.LogError("[EnemySpawnAdapter] EnemySetup is missing on spawned enemy.");
        }

        public void SpawnWaveEnemies(WaveTableData.WaveEntry entry)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            StartCoroutine(SpawnWaveEnemiesCoroutine(entry));
        }

        private IEnumerator SpawnWaveEnemiesCoroutine(WaveTableData.WaveEntry entry)
        {
            for (var i = 0; i < entry.Count; i++)
            {
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var x = Mathf.Cos(angle) * entry.SpawnRadius;
                var z = Mathf.Sin(angle) * entry.SpawnRadius;

                SpawnEnemy(entry.EnemyData, x, 0.75f, z);

                if (entry.SpawnDelay > 0f)
                    yield return new WaitForSeconds(entry.SpawnDelay);
            }
        }
    }
}
