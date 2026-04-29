using Shared.Attributes;
using System;
using Features.Enemy.Infrastructure;
using Features.Wave.Application.Ports;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    [CreateAssetMenu(fileName = "WaveTableData", menuName = "Wave/WaveTableData")]
    public sealed class WaveTableData : ScriptableObject, IWaveTablePort
    {
        [Required, SerializeField] private WaveEntry[] waves;

        public WaveEntry[] Waves => waves;

        int IWaveTablePort.WaveCount => waves != null ? waves.Length : 0;

        float IWaveTablePort.GetCountdownDuration(int waveIndex) => waves[waveIndex].CountdownDuration;

        int IWaveTablePort.GetEnemyCount(int waveIndex) => waves[waveIndex].Count;

        public bool TryGetEnemyDataByNetworkKey(string networkKey, out EnemyData enemyData)
        {
            enemyData = null;

            if (waves == null || string.IsNullOrWhiteSpace(networkKey))
                return false;

            for (var i = 0; i < waves.Length; i++)
            {
                var candidate = waves[i]?.EnemyData;
                if (candidate != null && string.Equals(candidate.ResourcesLoadPath, networkKey, StringComparison.Ordinal))
                {
                    enemyData = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFirstEnemyData(out EnemyData enemyData)
        {
            enemyData = null;

            if (waves == null)
                return false;

            for (var i = 0; i < waves.Length; i++)
            {
                if (waves[i]?.EnemyData == null)
                    continue;

                enemyData = waves[i].EnemyData;
                return true;
            }

            return false;
        }

        [Serializable]
        public sealed class WaveEntry
        {
            [Required, SerializeField] private EnemyData enemyData;
            [SerializeField] private int count = 3;
            [SerializeField] private float spawnDelay = 0.5f;
            [SerializeField] private float countdownDuration = 3f;
            [SerializeField] private float spawnRadius = 8f;

            public EnemyData EnemyData => enemyData;
            public int Count => count;
            public float SpawnDelay => spawnDelay;
            public float CountdownDuration => countdownDuration;
            public float SpawnRadius => spawnRadius;
        }
    }
}
