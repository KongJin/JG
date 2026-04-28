using System.Collections;
using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Application;
using Features.Combat.Presentation;
using Features.Garage;
using Features.Garage.Application;
using Features.Garage.Domain;
using Features.Lobby.Infrastructure;
using Features.Lobby.Application.Ports;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Status;
using Features.Unit;
using Features.Unit.Application;
using Features.Unit.Application.Ports;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Zone;
using Photon.Pun;
using Shared.Analytics;
using Shared.Attributes;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using Shared.Runtime.Sound;
using Shared.Ui;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneRoot : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [SerializeField] private string _playerPrefabName = "PlayerCharacter";
        [SerializeField] private float _spawnRadius = 3f;
        [Required, SerializeField] private Camera _camera;
        [Required, SerializeField] private CameraFollower _cameraFollower;
        [SerializeField] private GameObject _healthHudPrefab;
        [Required, SerializeField] private ProjectileSpawner _projectileSpawner;
        [Required, SerializeField] private CombatSetup _combatSetup;
        [Required, SerializeField] private ZoneSetup _zoneSetup;
        [Required, SerializeField] private SceneErrorPresenter _sceneErrorPresenter;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [Required, SerializeField] private EnergyBarView _energyBarView;
        // Runtime-spawned by PhotonNetwork.Instantiate and assigned via PlayerSceneRegistry arrival.
        [SerializeField] private PlayerSetup _localPlayerSetup;

        [Header("Unit & Garage")]
        [Required, SerializeField] private UnitSetup _unitSetup;
        [Required, SerializeField] private GarageSetup _garageSetup;

        [Header("Unit Summon UI")]
        [SerializeField] private UnitSlotsContainer _unitSlotsContainer;
        [Header("Combat Feedback")]
        [SerializeField] private DamageNumberSpawner _damageNumberSpawner;

        [Header("Status (Buff/Debuff)")]
        [Required, SerializeField] private StatusSetup _statusSetup;

        [Header("Wave (PvE)")]
        [SerializeField] private WaveSetup _waveSetup;
        [Tooltip("PvE일 때 필수. Combat.Initialize 직후 RegisterTarget.")]
        [SerializeField] private CoreObjectiveSetup _coreObjective;

        [Header("Scene Transition")]
        [SerializeField] private string _lobbySceneName = "LobbyScene";

        private EventBus _eventBus;
        private DisposableScope _disposables;
        private IAnalyticsPort _analytics;
        private IPlayerLookupPort _playerLookup;
        private string _matchId;
        private float _sceneStartTime;
        private bool _dropOffLogged;
        private readonly Queue<PlayerSetup> _pendingRemotePlayers = new();
        private bool _remotePlayerWiringReady;

        // Unit specs per player (computed from GarageRoster)
        private Dictionary<DomainEntityId, Unit.Domain.Unit[]> _playerUnitSpecs = new();
        private RestoreGarageRosterUseCase _restoreGarageRosterUseCase;
        private ComputePlayerUnitSpecsUseCase _computePlayerUnitSpecsUseCase;
        private readonly GameSceneGarageBootstrapFlow _garageBootstrapFlow = new();
        private readonly GameScenePlayerConnector _playerConnector = new();
        private readonly GameSceneAudioBootstrapFlow _audioBootstrapFlow = new();
#if UNITY_EDITOR
        private Coroutine _mcpFinalWaveClearRoutine;
        private bool _mcpFinalWaveClearGameEnded;
#endif

        /// <summary>
        /// Unit/Garage Feature 초기화 및 Unit 스펙 계산.
        /// </summary>
        private void InitializeUnitAndGarage(PlayerSetup localPlayerSetup)
        {
            var bootstrapResult = _garageBootstrapFlow.Initialize(
                _eventBus,
                _unitSetup,
                _garageSetup,
                localPlayerSetup.PlayerId);

            _restoreGarageRosterUseCase = bootstrapResult.RestoreGarageRosterUseCase;
            _computePlayerUnitSpecsUseCase = bootstrapResult.ComputePlayerUnitSpecsUseCase;
            var units = bootstrapResult.PlayerUnits;
            _playerUnitSpecs[localPlayerSetup.PlayerId] = units;

            Debug.Log($"[GameSceneRoot] Computed {units.Length} unit specs for player {localPlayerSetup.PlayerId.Value}");
        }

        /// <summary>
        /// Phase 4: 소환 슬롯 UI 초기화 + 초기 Energy 검증.
        /// 계산된 Unit specs를 기반으로 3개 표시 슬롯을 생성한다.
        /// </summary>
        private void ValidateAndInitializeSummonSlots(DomainEntityId playerId)
        {
            if (!_playerUnitSpecs.TryGetValue(playerId, out var specs) || specs.Length == 0)
            {
                Debug.LogWarning("[GameSceneRoot] No unit specs for player. Summon slots not initialized.");
                return;
            }

            // P8-4: 초기 Energy 검증
            var initialEnergy = _localPlayerSetup.EnergyAdapterInstance.GetCurrentEnergy(playerId);
            var energyResult = Features.Unit.Application.InitialEnergyValidator.Validate(initialEnergy, specs);
            if (!energyResult.IsValid)
            {
                Debug.LogWarning(
                    $"[GameSceneRoot] Player {playerId.Value} starts with insufficient energy " +
                    $"({energyResult.InitialEnergy:F1} < {energyResult.MinSummonCost:F1}). " +
                    $"Consider increasing initial energy.");
            }

            if (_unitSlotsContainer == null)
            {
                Debug.LogWarning("[GameSceneRoot] UnitSlotsContainer not assigned. Summon UI skipped.");
                return;
            }

            // PlacementArea 가져오기
            var placementArea = _coreObjective?.PlacementArea;
            if (placementArea == null)
            {
                Debug.LogWarning("[GameSceneRoot] PlacementArea not available. Using default spawn position.");
            }

            var energyPort = new UnitEnergyAdapter(_localPlayerSetup.EnergyAdapterInstance);

            _unitSlotsContainer.Initialize(
                _eventBus,
                _unitSetup.BattleEntitySetup.SummonUnit,
                energyPort,
                specs,
                playerId,
                placementArea,
                _coreObjective?.PlacementAreaView);
        }

        private void Awake()
        {
            _playerSceneRegistry.PlayerArrived += OnPlayerArrived;
            _playerSceneRegistry.DrainPendingArrivals(OnPlayerArrived);
        }

        private void CompleteLocalPlayerInitialization()
        {
            if (_localPlayerSetup == null) return;

            _cameraFollower.Initialize(_localPlayerSetup.transform, _camera.transform.position - _localPlayerSetup.transform.position);

            // Status (must initialize before PlayerSetup so SpeedModifier is ready)
            _statusSetup.Initialize(_eventBus, _localPlayerSetup.StatusNetworkAdapter, _localPlayerSetup.StatusNetworkAdapter, PhotonNetwork.IsMasterClient);

            _localPlayerSetup.InitializeLocal(
                _eventBus,
                new DefaultPlayerSpecProvider(),
                _statusSetup.SpeedModifier,
                _playerSceneRegistry,
                _playerLookup);

            // Combat
            _combatSetup.Initialize(_eventBus, _localPlayerSetup.CombatNetworkPort, _localPlayerSetup.PlayerId, new EntityAffiliationAdapter());

            if (_waveSetup != null && _coreObjective == null)
            {
                Debug.LogError(
                    "[GameSceneRoot] WaveSetup is set but CoreObjective is missing. Assign CoreObjectiveSetup on the objective GameObject.");
            }

            if (_coreObjective != null)
            {
                _coreObjective.RegisterCombatTarget(_combatSetup);
                _coreObjective.InitializePlacementArea();
            }

            if (_damageNumberSpawner != null)
                _damageNumberSpawner.Initialize(_eventBus);

            // GameEnd 핸들러 — PvE/PvP 모두에서 GameEndEvent 구독
            var gameEndHandler = new GameEndEventHandler(_eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndHandler));

            // GameEndAnalytics — 소환/처치 카운팅 + GameEndReportRequestedEvent 발행
            var gameEndAnalytics = new GameEndAnalytics(
                _eventBus,
                _eventBus,
                _coreObjective != null ? _coreObjective.CoreId : default,
                _coreObjective != null ? _coreObjective.CoreMaxHp : 0f);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndAnalytics));

            var operationRecordHandler = new OperationRecordGameEndHandler(
                _eventBus,
                new SaveOperationRecordUseCase(new OperationRecordJsonStore()),
                logWarning: Debug.LogWarning);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, operationRecordHandler));

            // GameEnd 리포트 로깅 (Bootstrap 책임 — Debug.Log 허용)
            _eventBus.Subscribe(this, new System.Action<Features.Player.Application.Events.GameEndReportRequestedEvent>(OnGameEndReport));
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));

            ConnectPlayer(_localPlayerSetup);

            // Energy (Mana renamed to Energy)
            _energyBarView.Initialize(
                _eventBus,
                _localPlayerSetup.PlayerId,
                _localPlayerSetup.MaxEnergy,
                _localPlayerSetup.EnergyAdapterInstance);

            // SoundPlayer is usually carried from LobbyScene; direct BattleScene runs create a runtime host.
            _audioBootstrapFlow.InitializeOrReport(_eventBus, _localPlayerSetup.PlayerId.Value);

            // ProjectileSpawner, ZoneSetup은 EventBus만 필요
            _projectileSpawner.Initialize(_eventBus, _eventBus);
            _zoneSetup.Initialize(_eventBus);

            // 원격 플레이어 wiring도 선택 전에 완료 — Status RPC 유실 방지
            _remotePlayerWiringReady = true;
            while (_pendingRemotePlayers.Count > 0)
                ConnectPlayer(_pendingRemotePlayers.Dequeue());

            // Unit/Garage 초기화 및 Unit 스펙 계산
            InitializeUnitAndGarage(_localPlayerSetup);

            // Wave 초기화 (Skill 선택 제거, 바로 시작)
            if (_waveSetup != null)
            {
                if (_coreObjective == null)
                {
                    Debug.LogError(
                        "[GameSceneRoot] Cannot initialize Wave without CoreObjectiveSetup.");
                    return;
                }

                _waveSetup.Initialize(_eventBus, _combatSetup, _localPlayerSetup.PlayerId,
                    _coreObjective);
                _waveSetup.RegisterPlayer(_localPlayerSetup.transform);

                // Phase 3: BattleEntity 소환 시스템 연결
                _unitSetup.InitializeBattleEntity(
                    _eventBus,
                    new UnitEnergyAdapter(_localPlayerSetup.EnergyAdapterInstance),
                    _combatSetup,
                    _waveSetup.UnitPositionQuery);

                // Phase 4: 소환 UI 초기화
                ValidateAndInitializeSummonSlots(_localPlayerSetup.PlayerId);
            }
        }

        private void Start()
        {
            _remotePlayerWiringReady = false;
            _eventBus = new EventBus();
            _disposables = new DisposableScope();

            // Analytics
            _analytics = new FirebaseAnalyticsAdapter();
            _matchId = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "unknown";
            _sceneStartTime = Time.realtimeSinceStartup;
            _analytics.LogGameStart(_matchId);
            RoundCounter.Increment();
            var analyticsHandler = new GameAnalyticsEventHandler(_analytics, _eventBus, _sceneStartTime, () => Time.realtimeSinceStartup);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, analyticsHandler));

            _playerLookup = new PlayerLookupAdapter(_playerSceneRegistry);
            _sceneErrorPresenter.Initialize(_eventBus);

            if (!PhotonNetwork.InRoom)
            {
                _eventBus.Publish(
                    new UiErrorRequestedEvent(
                        UiErrorMessage.Modal(
                            "You are not connected to a room. Please return to the lobby and join again.",
                            "GameScene"
                        )
                    )
                );
                return;
            }

            // Spawn local player
            var offset = Random.insideUnitCircle * _spawnRadius;
            var spawnPosition = new Vector3(offset.x, 0f, offset.y);
            var player = PhotonNetwork.Instantiate(
                _playerPrefabName,
                spawnPosition,
                Quaternion.identity);

            // PlayerSceneRegistry arrival is raised when the local Photon instance arrives.
            // CompleteLocalPlayerInitialization() runs from OnPlayerArrived.
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!_playerConnector.Connect(
                setup,
                _eventBus,
                _statusSetup,
                _playerSceneRegistry,
                _playerLookup))
                return;

            if (_healthHudPrefab != null)
            {
                var hudView = ComponentAccess.InstantiateComponent<PlayerHealthHudView>(
                    _healthHudPrefab,
                    transform);
                hudView.Initialize(
                    _eventBus,
                    setup.PlayerId,
                    setup.MaxHp,
                    setup.NetworkAdapter.IsMine,
                    setup.transform,
                    _camera);
            }

            _combatSetup.RegisterTarget(setup.PlayerId, setup.CombatTargetProvider);

            if (_waveSetup != null)
                _waveSetup.RegisterPlayer(setup.transform);
        }

        private void OnPlayerArrived(PlayerSetup setup)
        {
            if (setup == null)
                return;

            if (setup.NetworkAdapter.IsMine)
            {
                _localPlayerSetup = setup;
                CompleteLocalPlayerInitialization();
                return;
            }

            if (!_remotePlayerWiringReady)
            {
                _pendingRemotePlayers.Enqueue(setup);
                return;
            }

            ConnectPlayer(setup);
        }

        private void OnGameEndReport(Features.Player.Application.Events.GameEndReportRequestedEvent e)
        {
#if UNITY_EDITOR
            _mcpFinalWaveClearGameEnded = true;
#endif
            Debug.Log($"[GameEnd] ===== Game Result =====");
            Debug.Log($"  Result:     {(e.IsVictory ? "Victory" : "Defeat")}");
            Debug.Log($"  Wave:       {e.ReachedWave}");
            Debug.Log($"  Play Time:  {e.PlayTimeSeconds:F1}s ({e.PlayTimeSeconds / 60f:F1}m)");
            Debug.Log($"  Summons:    {e.SummonCount}");
            Debug.Log($"  Unit Kills: {e.UnitKillCount}");
            if (e.CoreMaxHealth > 0f)
                Debug.Log($"  Core HP:    {e.CoreRemainingHealth:F0}/{e.CoreMaxHealth:F0}");

            for (var i = 0; i < e.ContributionCards.Length; i++)
            {
                var card = e.ContributionCards[i];
                Debug.Log($"  Card {i + 1}:   {card.Title} - {card.Body}");
            }
            Debug.Log($"[GameEnd] =========================");

            // Firebase Analytics 전송
            if (_analytics != null)
            {
                _analytics.LogGameResult(e.IsVictory, e.ReachedWave, e.PlayTimeSeconds, e.SummonCount, e.UnitKillCount);
            }
        }

#if UNITY_EDITOR
        public void ForceCoreDefeatForMcpSmoke()
        {
            if (_combatSetup == null || _coreObjective == null)
                return;

            var lethalDamage = _coreObjective.CoreMaxHp + 10000f;
            _combatSetup.ApplyDamage(
                _coreObjective.CoreId,
                lethalDamage,
                DamageType.Physical,
                new DomainEntityId("mcp-smoke"));
        }

        public void ForceVictoryForMcpSmoke()
        {
            _eventBus?.Publish(new Features.Wave.Application.Events.WaveVictoryEvent());
        }

        public void RunFinalWaveClearForMcpSmoke(float timeScale = 8f, float maxRealtimeSeconds = 90f)
        {
            if (_mcpFinalWaveClearRoutine != null)
                StopCoroutine(_mcpFinalWaveClearRoutine);

            var requestedTimeScale = timeScale > 0f ? timeScale : 12f;
            var requestedMaxRealtimeSeconds = maxRealtimeSeconds > 0f ? maxRealtimeSeconds : 70f;
            _mcpFinalWaveClearGameEnded = false;
            Debug.Log($"[McpSmoke] Final wave clear smoke started. timeScale={requestedTimeScale:F1}, maxRealtimeSeconds={requestedMaxRealtimeSeconds:F1}");
            _mcpFinalWaveClearRoutine = StartCoroutine(RunFinalWaveClearForMcpSmokeRoutine(
                Mathf.Clamp(requestedTimeScale, 1f, 20f),
                Mathf.Max(1f, requestedMaxRealtimeSeconds)));
        }

        private IEnumerator RunFinalWaveClearForMcpSmokeRoutine(float timeScale, float maxRealtimeSeconds)
        {
            var previousTimeScale = Time.timeScale;
            var elapsed = 0f;
            Time.timeScale = timeScale;

            while (elapsed < maxRealtimeSeconds && !_mcpFinalWaveClearGameEnded)
            {
                var cleared = ClearSpawnedEnemiesForMcpSmoke();
                if (cleared > 0)
                    Debug.Log($"[McpSmoke] Cleared spawned enemies: {cleared}");

                yield return new WaitForSecondsRealtime(0.25f);
                elapsed += 0.25f;
            }

            Time.timeScale = previousTimeScale;
            Debug.Log("[McpSmoke] Final wave clear smoke finished.");
            _mcpFinalWaveClearRoutine = null;
        }

        private int ClearSpawnedEnemiesForMcpSmoke()
        {
            if (_combatSetup == null)
                return 0;

            var holders = FindObjectsByType<EntityIdHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var attackerId = _localPlayerSetup != null
                ? _localPlayerSetup.PlayerId
                : new DomainEntityId("mcp-smoke");
            var cleared = 0;

            foreach (var holder in holders)
            {
                if (holder == null || !holder.IsInitialized)
                    continue;
                if (string.IsNullOrWhiteSpace(holder.Id.Value) || !holder.Id.Value.StartsWith("enemy-"))
                    continue;

                _combatSetup.ApplyDamage(
                    holder.Id,
                    100000f,
                    DamageType.Physical,
                    attackerId);
                cleared++;
            }

            return cleared;
        }
#endif

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (_mcpFinalWaveClearRoutine != null)
            {
                StopCoroutine(_mcpFinalWaveClearRoutine);
                _mcpFinalWaveClearRoutine = null;
            }

            Time.timeScale = 1f;
#endif

            if (_playerSceneRegistry != null)
                _playerSceneRegistry.PlayerArrived -= OnPlayerArrived;

            var playTime = Time.realtimeSinceStartup - _sceneStartTime;
            if (_analytics != null)
                _analytics.LogGameEnd(_matchId, playTime, RoundCounter.Current);

            _disposables?.Dispose();
        }

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            if (_analytics != null)
                _analytics.LogDropOff("game_disconnect", Time.realtimeSinceStartup - _sceneStartTime);
        }

        public override void OnLeftRoom()
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            if (_analytics != null)
                _analytics.LogDropOff("game_leave", Time.realtimeSinceStartup - _sceneStartTime);

            // Lobby 씬으로 전환
            var sceneLoader = new SceneLoaderAdapter();
            sceneLoader.LoadScene(_lobbySceneName);
        }
    }
}
