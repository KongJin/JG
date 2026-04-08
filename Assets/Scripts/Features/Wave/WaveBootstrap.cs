using Features.Combat;
using Features.Enemy;
using Features.Enemy.Application.Ports;
using Features.Skill.Infrastructure;
using Features.Wave.Application;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Features.Wave.Domain;
using Features.Wave.Infrastructure;
using Features.Wave.Presentation;
using Photon.Pun;
using Photon.Realtime;
using Shared.Attributes;
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
        [Required, SerializeField]
        private WaveTableData _waveTable;

        [Required, SerializeField]
        private EnemySpawnAdapter _spawnAdapter;

        [Required, SerializeField]
        private PlayerPositionQueryAdapter _playerPositionQuery;

        [Required, SerializeField]
        private UnitPositionQueryAdapter _unitPositionQuery;

        [Required, SerializeField]
        private WaveHudView _hudView;

        [Required, SerializeField]
        private WaveEndView _endView;

        [Required, SerializeField]
        private WaveFlowController _flowController;

        [Required, SerializeField]
        private WaveNetworkAdapter _networkAdapter;

        [Required, SerializeField]
        private CoreHealthHudView _coreHealthView;

        [Tooltip(
            "선택. 연결 시 스폰 배율을 이 컴포넌트에서만 조회한다. 비우면 Initialize 시 Room에서 직접 읽는다."
        )]
        [SerializeField]
        private RoomDifficultySpawnScaleProvider _difficultySpawnScale;

        private EventBus _eventBus;
        private CombatBootstrap _combatBootstrap;
        private DisposableScope _disposables;
        private WaveLoopUseCase _waveLoop;
        private ICoreObjectiveQuery _coreObjectiveQuery;
        private HostilePositionQuery _hostilePositionQuery;
        private bool _initialized;
        private bool _gameStarted;

        /// <summary>
        /// Enemy AI가 사용하는 통합 적대 대상 위치 쿼리.
        /// </summary>
        public IPlayerPositionQuery HostilePositionQuery => _hostilePositionQuery;

        /// <summary>
        /// Unit 위치 등록용. GameSceneRoot에서 BattleEntity Transform을 등록한다.
        /// </summary>
        public UnitPositionQueryAdapter UnitPositionQuery => _unitPositionQuery;

        public void Initialize(
            EventBus eventBus,
            CombatBootstrap combatBootstrap,
            DomainEntityId localPlayerId,
            ICoreObjectiveQuery coreObjectiveQuery
        )
        {
            _eventBus = eventBus;
            _combatBootstrap = combatBootstrap;
            _coreObjectiveQuery = coreObjectiveQuery;

            _disposables?.Dispose();
            _disposables = new DisposableScope();

            // Phase 3: 통합 적대 대상 쿼리 (Player + BattleEntity)
            _hostilePositionQuery = new HostilePositionQuery(_playerPositionQuery, _unitPositionQuery);

            var playerCount = PhotonNetwork.PlayerList.Length;
            var aliveQuery = new AlivePlayerQueryAdapter(eventBus, playerCount);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, aliveQuery));

            var spawnMultiplier = ResolveSpawnCountMultiplier();
            _spawnAdapter.Initialize(
                eventBus,
                combatBootstrap,
                _hostilePositionQuery,
                _coreObjectiveQuery,
                _waveTable,
                spawnMultiplier
            );

            _waveLoop = new WaveLoopUseCase(eventBus, _waveTable.Waves.Length);
            var waveLoop = _waveLoop;
            var waveHandler = new WaveEventHandler(
                eventBus,
                waveLoop,
                aliveQuery,
                ObjectiveCoreIds.Default
            );
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, waveHandler));

            var networkHandler = new WaveNetworkEventHandler(
                eventBus,
                _networkAdapter,
                _networkAdapter,
                waveLoop
            );
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, networkHandler));

            _hudView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _hudView));

            _endView.Initialize(eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _endView));

            _flowController.Initialize(
                waveLoop,
                (IWaveTablePort)_waveTable,
                (IWaveSpawnPort)_spawnAdapter,
                eventBus,
                eventBus,
                localPlayerId
            );
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _flowController));

            _coreHealthView.Initialize(
                eventBus,
                coreObjectiveQuery.CoreId,
                coreObjectiveQuery.CoreMaxHp
            );
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _coreHealthView));

            EnemySetup.EnemyArrived += OnEnemyArrived;

            _networkAdapter.HydrateFromRoomProperties();

            _initialized = true;
            // hydrate 결과가 Idle이 아니면 이미 게임이 진행 중이므로 재시작 방지
            _gameStarted = _waveLoop.CurrentState != WaveState.Idle;

            // Master: 전원 SkillsReady 확인 후 GameStartEvent 발행
            if (PhotonNetwork.IsMasterClient)
                TryStartGame();
        }

        public void RegisterPlayer(Transform playerTransform)
        {
            _playerPositionQuery.RegisterPlayer(playerTransform);
        }

        public override void OnPlayerPropertiesUpdate(
            PhotonPlayer targetPlayer,
            Hashtable changedProps
        )
        {
            // Phase 3: No longer waiting for skillsReady — game starts immediately
        }

        public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
        {
            if (!_initialized)
                return;
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (!_gameStarted)
                TryStartGame();
        }

        private float ResolveSpawnCountMultiplier()
        {
            if (_difficultySpawnScale != null)
                return _difficultySpawnScale.SpawnCountMultiplier;
            return DifficultySpawnScale.MultiplierForPreset(RoomDifficultyReader.ReadPresetId());
        }

        private void TryStartGame()
        {
            if (_gameStarted)
                return;

            // Phase 3: No condition — start immediately
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

            enemy.Initialize(
                _eventBus,
                _combatBootstrap,
                _hostilePositionQuery,
                _coreObjectiveQuery
            );
        }

        private void OnDestroy()
        {
            EnemySetup.EnemyArrived -= OnEnemyArrived;
            _disposables?.Dispose();
        }
    }
}
