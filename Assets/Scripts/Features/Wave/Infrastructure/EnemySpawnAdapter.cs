using System.Collections;
using Features.Combat;
using Features.Enemy;
using Features.Enemy.Application.Ports;
using Features.Enemy.Infrastructure;
using Features.Wave.Application;
using Features.Wave.Application.Ports;
using Photon.Pun;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    public sealed class EnemySpawnAdapter : MonoBehaviour, IWaveSpawnPort
    {
        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private IPlayerPositionQuery _playerQuery;
        private ICoreObjectiveQuery _coreObjectiveQuery;
        private WaveTableData _waveTable;
        private float _spawnCountMultiplier = 1f;

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            IPlayerPositionQuery playerQuery,
            ICoreObjectiveQuery coreObjectiveQuery,
            WaveTableData waveTable = null,
            float spawnCountMultiplier = 1f)
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _playerQuery = playerQuery;
            _coreObjectiveQuery = coreObjectiveQuery;
            _waveTable = waveTable;
            _spawnCountMultiplier = spawnCountMultiplier > 0f ? spawnCountMultiplier : 1f;
        }

        public void SpawnEnemy(EnemyData data, float x, float y, float z)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var position = new Vector3(x, y, z);
            var go = PhotonNetwork.Instantiate(
                data.PrefabName,
                position,
                Quaternion.identity,
                0,
                new object[] { data.ResourcesLoadPath });

            var setup = go.GetComponent<EnemySetup>();
            if (setup != null)
                setup.Initialize(_eventBus, _combatBootstrap, data, _playerQuery, _coreObjectiveQuery);
            else
                Debug.LogError("[EnemySpawnAdapter] EnemySetup is missing on spawned enemy.");
        }

        void IWaveSpawnPort.SpawnWave(int waveIndex)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (_waveTable == null || waveIndex >= _waveTable.Waves.Length) return;
            StartCoroutine(SpawnWaveEnemiesCoroutine(_waveTable.Waves[waveIndex]));
        }

        private IEnumerator SpawnWaveEnemiesCoroutine(WaveTableData.WaveEntry entry)
        {
            var spawnCount = DifficultySpawnScale.ScaledSpawnCount(entry.Count, _spawnCountMultiplier);
            for (var i = 0; i < spawnCount; i++)
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
