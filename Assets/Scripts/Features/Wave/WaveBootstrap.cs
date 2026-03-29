using Shared.Attributes;
using Features.Combat;
using Features.Enemy;
using Features.Wave.Application;
using Features.Wave.Application.Ports;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Photon.Pun;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Wave
{
    public sealed class WaveBootstrap : MonoBehaviour
    {
        [Required, SerializeField] private WaveTableData _waveTable;
        [Required, SerializeField] private EnemySpawnAdapter _spawnAdapter;
        [Required, SerializeField] private PlayerPositionQueryAdapter _playerPositionQuery;
        [Required, SerializeField] private WaveHudView _hudView;
        [Required, SerializeField] private WaveEndView _endView;
        [Required, SerializeField] private WaveFlowController _flowController;

        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private DisposableScope _disposables;

        public IPlayerPositionQuery PlayerPositionQuery => _playerPositionQuery;

        public void Initialize(EventBus eventBus, CombatBootstrap combatBootstrap)
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;

            _disposables?.Dispose();
            _disposables = new DisposableScope();

            var playerCount = PhotonNetwork.PlayerList.Length;
            var aliveQuery = new AlivePlayerQueryAdapter(eventBus, playerCount);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, aliveQuery));

            _spawnAdapter.Initialize(eventBus, combatBootstrap, _playerPositionQuery);

            var waveLoop = new WaveLoopUseCase(eventBus, _waveTable.Waves.Length);
            var waveHandler = new WaveEventHandler(eventBus, waveLoop, aliveQuery);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, waveHandler));

            _hudView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _hudView));

            _endView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _endView));

            _flowController.Initialize(waveLoop, _waveTable, _spawnAdapter);

            EnemySetup.EnemyArrived += OnEnemyArrived;
        }

        public void RegisterPlayer(Transform playerTransform)
        {
            _playerPositionQuery.RegisterPlayer(playerTransform);
        }

        private void OnEnemyArrived(EnemySetup enemy)
        {
            if (enemy.IsInitialized)
                return;

            // Master는 EnemySpawnAdapter.SpawnEnemy()에서 올바른 EnemyData로 명시적 초기화한다.
            // 이 콜백은 비-Master 클라이언트 전용 fallback이다.
            if (PhotonNetwork.IsMasterClient)
                return;

            enemy.Initialize(_eventBus, _combatBootstrap, _playerPositionQuery);
        }

        private void OnDestroy()
        {
            EnemySetup.EnemyArrived -= OnEnemyArrived;
            _disposables?.Dispose();
        }
    }
}
