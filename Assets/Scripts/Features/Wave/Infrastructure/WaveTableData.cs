using System;
using Features.Enemy.Infrastructure;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    [CreateAssetMenu(fileName = "WaveTableData", menuName = "Wave/WaveTableData")]
    public sealed class WaveTableData : ScriptableObject
    {
        [SerializeField] private WaveEntry[] waves;

        public WaveEntry[] Waves => waves;

        [Serializable]
        public sealed class WaveEntry
        {
            [SerializeField] private EnemyData enemyData;
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
