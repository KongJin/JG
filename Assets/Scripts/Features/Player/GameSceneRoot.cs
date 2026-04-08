using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Application;
using Features.Combat.Presentation;
using Features.Garage;
using Features.Garage.Application;
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
using Shared.Runtime.Sound;
using Shared.Time;
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
        [Required, SerializeField] private GameObject _healthHudPrefab;
        [Required, SerializeField] private Canvas _hudCanvas;
        [Required, SerializeField] private ProjectileSpawner _projectileSpawner;
        [Required, SerializeField] private CombatBootstrap _combatBootstrap;
        [Required, SerializeField] private ZoneSetup _zoneSetup;
        [Required, SerializeField] private SceneErrorPresenter _sceneErrorPresenter;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [Required, SerializeField] private EnergyRegenTicker _energyRegenTicker;
        [Required, SerializeField] private EnergyBarView _energyBarView;
        [Required, SerializeField] private PlayerSetup _localPlayerSetup;

        [Header("Unit & Garage")]
        [Required, SerializeField] private UnitBootstrap _unitBootstrap;
        [Required, SerializeField] private GarageBootstrap _garageBootstrap;

        [Header("Unit Summon UI")]
        [SerializeField] private UnitSlotsContainer _unitSlotsContainer;
        [SerializeField] private RectTransform _unitSlotsParent;
        [SerializeField] private UnitSlotView _unitSlotPrefab;

        [Header("Combat Feedback")]
        [SerializeField] private DamageNumberSpawner _damageNumberSpawner;

        [Header("Status (Buff/Debuff)")]
        [Required, SerializeField] private StatusSetup _statusSetup;

        [Header("Wave (PvE)")]
        [SerializeField] private WaveBootstrap _waveBootstrap;
        [Tooltip("PvE일 때 필수. Combat.Initialize 직후 RegisterTarget.")]
        [SerializeField] private CoreObjectiveBootstrap _coreObjective;

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

        /// <summary>
        /// Unit/Garage Feature 초기화 및 Unit 스펙 계산.
        /// </summary>
        private void InitializeUnitAndGarage(PlayerSetup localPlayerSetup)
        {
            // 1. Unit Bootstrap 초기화
            _unitBootstrap.Initialize(_eventBus);

            // 2. Garage Bootstrap 초기화
            _garageBootstrap.Initialize(
                _eventBus,
                _unitBootstrap.CompositionPort,
                _unitBootstrap.Catalog);

            // 3. UseCase들 생성
            _restoreGarageRosterUseCase = new RestoreGarageRosterUseCase(_garageBootstrap.Setup.NetworkPort);
            _computePlayerUnitSpecsUseCase = new ComputePlayerUnitSpecsUseCase(
                _garageBootstrap.Setup.ComposeUnit,
                new ClockAdapter(),
                _eventBus);

            // 4. GarageRoster 복원 (CustomProperties에서 읽기)
            var loadouts = RestoreGarageRosterFromRoom(localPlayerSetup.PlayerId);

            // 5. Unit 스펙 계산
            ComputeUnitSpecs(localPlayerSetup.PlayerId, loadouts);
        }

        /// <summary>
        /// Room CustomProperties에서 GarageRoster를 복원한다.
        /// </summary>
        private GarageRoster.UnitLoadout[] RestoreGarageRosterFromRoom(DomainEntityId playerId)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[GameSceneRoot] Cannot restore GarageRoster: not in room.");
                return new GarageRoster.UnitLoadout[0];
            }

            return _restoreGarageRosterUseCase.Execute();
        }

        /// <summary>
        /// 로컬 플레이어의 Unit 스펙을 계산한다.
        /// </summary>
        private void ComputeUnitSpecs(DomainEntityId playerId, GarageRoster.UnitLoadout[] loadouts)
        {
            var units = _computePlayerUnitSpecsUseCase.Execute(loadouts, playerId);
            _playerUnitSpecs[playerId] = units;

            Debug.Log($"[GameSceneRoot] Computed {units.Length} unit specs for player {playerId.Value}");
        }

        /// <summary>
        /// Phase 4: 소환 슬롯 UI 초기화.
        /// 계산된 Unit specs를 기반으로 3개 표시 슬롯을 생성한다.
        /// </summary>
        private void InitializeSummonSlots(DomainEntityId playerId)
        {
            if (!_playerUnitSpecs.TryGetValue(playerId, out var specs) || specs.Length == 0)
            {
                Debug.LogWarning("[GameSceneRoot] No unit specs for player. Summon slots not initialized.");
                return;
            }

            if (_unitSlotsContainer == null)
            {
                Debug.LogWarning("[GameSceneRoot] UnitSlotsContainer not assigned. Summon UI skipped.");
                return;
            }

            var energyPort = new UnitEnergyAdapter(_localPlayerSetup.EnergyAdapterInstance);

            _unitSlotsContainer.Initialize(
                _eventBus,
                _unitBootstrap.BattleEntitySetup.SummonUnit,
                energyPort,
                specs,
                playerId,
                transform.position); // TODO: 실제 배치 영역 중앙 좌표로 변경
        }

        private void Awake()
        {
            PlayerSetup.RemoteArrived += OnRemotePlayerArrived;
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

            // TODO: GetComponent 대체 - Anti-pattern.md에서 GetComponent 금지
            // 현재 Photon instantiate 후 PlayerSetup 획득 불가피
            // 향후 LocalPlayerArrived 이벤트 패턴으로 변경 권장
            _localPlayerSetup = player.GetComponent<PlayerSetup>();
            _cameraFollower.Initialize(player.transform, _camera.transform.position - player.transform.position);

            // Status (must initialize before PlayerSetup so SpeedModifier is ready)
            _statusSetup.Initialize(_eventBus, _localPlayerSetup.StatusNetworkAdapter, _localPlayerSetup.StatusNetworkAdapter, PhotonNetwork.IsMasterClient);

            _localPlayerSetup.InitializeLocal(
                _eventBus,
                new DefaultPlayerSpecProvider(),
                _statusSetup.SpeedModifier,
                _playerSceneRegistry,
                _playerLookup);

            // Combat
            _combatBootstrap.Initialize(_eventBus, _localPlayerSetup.CombatNetworkPort, _localPlayerSetup.PlayerId, new EntityAffiliationAdapter());

            if (_waveBootstrap != null && _coreObjective == null)
            {
                Debug.LogError(
                    "[GameSceneRoot] WaveBootstrap is set but CoreObjective is missing. Assign CoreObjectiveBootstrap on the objective GameObject.");
            }

            if (_coreObjective != null)
                _coreObjective.RegisterCombatTarget(_combatBootstrap);

            if (_damageNumberSpawner != null)
                _damageNumberSpawner.Initialize(_eventBus);

            if (_waveBootstrap == null)
            {
                var gameEndHandler = new GameEndEventHandler(_eventBus, _eventBus, localSetup.PlayerId);
                _disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndHandler));
            }

            ConnectPlayer(_localPlayerSetup);

            // Energy (Mana renamed to Energy)
            _energyRegenTicker.Initialize(_localPlayerSetup.EnergyAdapterInstance);
            _energyBarView.Initialize(_eventBus, _localPlayerSetup.PlayerId, _localPlayerSetup.MaxEnergy);

            // SoundPlayer is a DDOL singleton created from JG_LobbyScene.
            // Running JG_GameScene directly is allowed, but audio stays unavailable.
            if (SoundPlayer.Instance == null)
            {
                Debug.LogError(
                    "[GameSceneRoot] SoundPlayer.Instance is null. Start from JG_LobbyScene so the DDOL SoundPlayer is created; playing JG_GameScene alone will not load it.");
            }
            else
            {
                SoundPlayer.Instance.Initialize(_eventBus, localSetup.PlayerId.Value);
            }

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
            if (_waveBootstrap != null)
            {
                if (_coreObjective == null)
                {
                    Debug.LogError(
                        "[GameSceneRoot] Cannot initialize Wave without CoreObjectiveBootstrap.");
                    return;
                }

                _waveBootstrap.Initialize(_eventBus, _combatBootstrap, _localPlayerSetup.PlayerId,
                    _coreObjective);
                _waveBootstrap.RegisterPlayer(player.transform);

                // Phase 3: BattleEntity 소환 시스템 연결
                _unitBootstrap.InitializeBattleEntity(
                    _eventBus,
                    _localPlayerSetup.EnergyAdapterInstance,
                    _combatBootstrap,
                    _waveBootstrap.UnitPositionQuery);

                // Phase 4: 소환 UI 초기화
                InitializeSummonSlots(_localPlayerSetup.PlayerId);
            }
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!setup.IsInitialized)
            {
                var specProvider = new DefaultPlayerSpecProvider();
                if (setup.NetworkAdapter.IsMine)
                {
                    setup.InitializeLocal(
                        _eventBus,
                        specProvider,
                        _statusSetup.SpeedModifier,
                        _playerSceneRegistry,
                        _playerLookup);
                }
                else
                {
                    setup.InitializeRemote(_eventBus, specProvider, _playerLookup);
                }
            }

            if (!_playerSceneRegistry.TryRegister(setup))
                return;

            var hudGo = Instantiate(_healthHudPrefab, _hudCanvas.transform, false);
            var hudView = hudGo.GetComponent<PlayerHealthHudView>();
            hudView.Initialize(
                _eventBus,
                setup.PlayerId,
                setup.MaxHp,
                setup.NetworkAdapter.IsMine,
                setup.transform,
                _camera,
                _hudCanvas);

            _combatBootstrap.RegisterTarget(setup.PlayerId, setup.CombatTargetProvider);

            if (_waveBootstrap != null)
                _waveBootstrap.RegisterPlayer(setup.transform);

            // Wire remote player's StatusNetworkAdapter so RPC callbacks reach the shared handler
            if (!setup.NetworkAdapter.IsMine)
                _statusSetup.RegisterRemoteCallbackPort(setup.StatusNetworkAdapter);

            // Hydrate remote players from CustomProperties AFTER registry registration
            // so that IPlayerLookupPort.Resolve() can find the domain player
            if (!setup.NetworkAdapter.IsMine)
                setup.NetworkAdapter.HydrateFromProperties();
        }

        private void OnRemotePlayerArrived(PlayerSetup setup)
        {
            if (!_remotePlayerWiringReady)
            {
                _pendingRemotePlayers.Enqueue(setup);
                return;
            }

            ConnectPlayer(setup);
        }

        private void OnDestroy()
        {
            PlayerSetup.RemoteArrived -= OnRemotePlayerArrived;

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
        }
    }
}
