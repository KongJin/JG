using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Application;
using Features.Combat.Presentation;
using Features.Garage;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Status;
using Features.Unit;
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

        [Header("Unit & Garage")]
        [Required, SerializeField] private UnitBootstrap _unitBootstrap;
        [Required, SerializeField] private GarageBootstrap _garageBootstrap;

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

            // 3. GarageRoster 복원 (CustomProperties에서 읽기)
            RestoreGarageRosterFromRoom();

            // 4. Unit 스펙 계산
            ComputeUnitSpecs(localPlayerSetup.PlayerId);
        }

        /// <summary>
        /// Room CustomProperties에서 GarageRoster를 복원한다.
        /// </summary>
        private void RestoreGarageRosterFromRoom()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[GameSceneRoot] Cannot restore GarageRoster: not in room.");
                return;
            }

            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props == null || !props.ContainsKey("garageRoster"))
            {
                Debug.LogWarning("[GameSceneRoot] GarageRoster not found in room properties.");
                return;
            }

            // TODO: GarageRoster 역직렬화
            // 현재는 GarageFeature에서 CustomProperties 읽는 구현 필요
            // garageBootstrap.Setup.InitializeGarageUseCase.Execute(loadouts) 호출
        }

        /// <summary>
        /// 로컬 플레이어의 Unit 스펙을 계산한다.
        /// </summary>
        private void ComputeUnitSpecs(DomainEntityId playerId)
        {
            // TODO: GarageSetup.InitializeGarageUseCase가 반환하는 UnitLoadout[]을
            // ComposeUnitUseCase로 계산하여 _playerUnitSpecs에 저장
            // _playerUnitSpecs[playerId] = units;
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
            var analyticsHandler = new GameAnalyticsEventHandler(_analytics, _eventBus);
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
                Quaternion.identity
            );

            _cameraFollower.Initialize(player.transform, _camera.transform.position - player.transform.position);

            var localSetup = player.GetComponent<PlayerSetup>();

            // Status (must initialize before PlayerSetup so SpeedModifier is ready)
            _statusSetup.Initialize(_eventBus, localSetup.StatusNetworkAdapter, localSetup.StatusNetworkAdapter, PhotonNetwork.IsMasterClient);

            localSetup.Initialize(_eventBus, speedModifier: _statusSetup.SpeedModifier, sceneRegistry: _playerSceneRegistry, playerLookup: _playerLookup);

            // Combat
            _combatBootstrap.Initialize(_eventBus, localSetup.CombatNetworkPort, localSetup.PlayerId, new EntityAffiliationAdapter());

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

            ConnectPlayer(localSetup);

            // Energy (Mana renamed to Energy)
            _energyRegenTicker.Initialize(localSetup.EnergyAdapterInstance);
            _energyBarView.Initialize(_eventBus, localSetup.PlayerId, localSetup.MaxEnergy);

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
            InitializeUnitAndGarage(localSetup);

            // Wave 초기화 (Skill 선택 제거, 바로 시작)
            if (_waveBootstrap != null)
            {
                if (_coreObjective == null)
                {
                    Debug.LogError(
                        "[GameSceneRoot] Cannot initialize Wave without CoreObjectiveBootstrap.");
                    return;
                }

                _waveBootstrap.Initialize(_eventBus, _combatBootstrap, localSetup.PlayerId,
                    _coreObjective);
                _waveBootstrap.RegisterPlayer(player.transform);
            }
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!setup.IsInitialized)
                setup.Initialize(_eventBus, playerLookup: _playerLookup);

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
