using Features.Combat;
using Features.Enemy;
using Features.Enemy.Infrastructure;
using Features.Wave.Application.Ports;
using Photon.Pun;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Infrastructure
{
    public sealed class EnemySpawnAdapter : MonoBehaviour, IEnemySpawnPort
    {
        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private EnemyData _currentEnemyData;
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

        public void SetCurrentEnemyData(EnemyData data)
        {
            _currentEnemyData = data;
        }

        public void SpawnEnemy(string prefabName, float x, float y, float z)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var position = new Vector3(x, y, z);
            var go = PhotonNetwork.Instantiate(prefabName, position, Quaternion.identity);

            var setup = go.GetComponent<EnemySetup>();
            if (setup != null && _currentEnemyData != null)
                setup.Initialize(_eventBus, _combatBootstrap, _currentEnemyData, _playerQuery);
            else
                Debug.LogError("[EnemySpawnAdapter] EnemySetup or EnemyData is missing on spawned enemy.");
        }
    }
}
