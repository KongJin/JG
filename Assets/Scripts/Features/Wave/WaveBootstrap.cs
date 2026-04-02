using Shared.Attributes;
using Features.Combat;
using Features.Enemy;
using Features.Skill.Infrastructure;
using Features.Skill.Presentation;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Photon.Pun;
using Photon.Realtime;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PhotonPlayer = Photon.Realtime.Player;

namespace Features.Wave
{
    public sealed class WaveBootstrap : MonoBehaviourPunCallbacks
    {
        [Required, SerializeField] private WaveTableData _waveTable;
        [Required, SerializeField] private EnemySpawnAdapter _spawnAdapter;
        [Required, SerializeField] private PlayerPositionQueryAdapter _playerPositionQuery;
        [Required, SerializeField] private WaveHudView _hudView;
        [Required, SerializeField] private WaveEndView _endView;
        [Required, SerializeField] private WaveFlowController _flowController;
        [Required, SerializeField] private UpgradeSelectionView _upgradeView;
        [Required, SerializeField] private UpgradeResultView _upgradeResultView;
        [Required, SerializeField] private WaveNetworkAdapter _networkAdapter;

        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private DisposableScope _disposables;
        private bool _initialized;
        private bool _gameStarted;

        public IPlayerPositionQuery PlayerPositionQuery => _playerPositionQuery;

        public void Initialize(EventBus eventBus, CombatBootstrap combatBootstrap, DomainEntityId localPlayerId, ISkillRewardPort skillReward, ISkillIconPort iconPort)
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;

            _disposables?.Dispose();
            _disposables = new DisposableScope();

            var playerCount = PhotonNetwork.PlayerList.Length;
            var aliveQuery = new AlivePlayerQueryAdapter(eventBus, playerCount);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, aliveQuery));

            _spawnAdapter.Initialize(eventBus, combatBootstrap, _playerPositionQuery, _waveTable);

            var waveLoop = new WaveLoopUseCase(eventBus, _waveTable.Waves.Length, skillReward);
            var waveHandler = new WaveEventHandler(eventBus, waveLoop, aliveQuery);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, waveHandler));

            var networkHandler = new WaveNetworkEventHandler(eventBus, _networkAdapter, _networkAdapter, waveLoop);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, networkHandler));

            var rewardHandler = new SkillRewardHandler(eventBus, skillReward);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, rewardHandler));

            _hudView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _hudView));

            _endView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _endView));

            _upgradeView.Initialize(eventBus, eventBus, localPlayerId, iconPort);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _upgradeView));

            _upgradeResultView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _upgradeResultView));

            _flowController.Initialize(waveLoop, (IWaveTablePort)_waveTable, (IWaveSpawnPort)_spawnAdapter, eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _flowController));

            EnemySetup.EnemyArrived += OnEnemyArrived;

            _networkAdapter.HydrateFromRoomProperties();

            _initialized = true;
            _gameStarted = false;

            // Master: 전원 SkillsReady 확인 후 GameStartEvent 발행
            if (PhotonNetwork.IsMasterClient)
                TryStartGame();
        }

        public void RegisterPlayer(Transform playerTransform)
        {
            _playerPositionQuery.RegisterPlayer(playerTransform);
        }

        public override void OnPlayerPropertiesUpdate(PhotonPlayer targetPlayer, Hashtable changedProps)
        {
            if (!_initialized || _gameStarted) return;
            if (!PhotonNetwork.IsMasterClient) return;
            if (!changedProps.ContainsKey("skillsReady")) return;

            TryStartGame();
        }

        private void TryStartGame()
        {
            if (_gameStarted) return;

            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (!SkillNetworkAdapter.IsPlayerSkillsReady(player))
                    return;
            }

            _gameStarted = true;
            _eventBus.Publish(new GameStartEvent());
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
